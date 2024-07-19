using PicoArgs_dotnet;

namespace FolderCompare;

internal static class Program
{
	public static async Task<int> Main(string[] args)
	{
		AppDomain.CurrentDomain.UnhandledException += App_UnhandledException;

		var opts = ParseCommandLine(args);

		if (!opts.Raw) {
			var info = GitVersion.VersionInfo.Get();
			Console.WriteLine($"FolderCompare {info.GetVersionHash(20)}");
		}

		var path1 = opts.FolderA!.FullName;
		var path2 = opts.FolderB!.FullName;

		if (path1 == path2) {
			throw new Exception("The two folders must be different");
		}

		if (!opts.FolderA.Exists) {
			throw new Exception($"Folder {path1} does not exist");
		}

		if (!opts.FolderB.Exists) {
			throw new Exception($"Folder {path2} does not exist");
		}

		// we need two comparers because we enumerate both folders in parallel
		var comparer1 = BuildComparer(opts.Compare);
		var comparer2 = BuildComparer(opts.Compare);

		if (!opts.Raw) {
			Console.WriteLine($"Comparing \"{path1}\" and \"{path2}\" using {opts.Compare} comparison...");
		}

		HashSet<FileData> files1, files2;
		if (opts.OneThread) {
			if (!opts.Raw) {
				Console.WriteLine($"Scanning {path1}...");
			}

			files1 = ScanFolder(opts.FolderA, !opts.Raw, comparer1);

			if (!opts.Raw) {
				Console.WriteLine($"Scanning {path2}...");
			}

			files2 = ScanFolder(opts.FolderB, !opts.Raw, comparer2);
		} else {
			// scan both folders in parallel
			var task1 = Task.Run(() => ScanFolder(opts.FolderA, false, comparer1));
			var task2 = Task.Run(() => ScanFolder(opts.FolderB, false, comparer2));

			// await both tasks
			var result = await Task.WhenAll(task1, task2);

			files1 = result[0];
			files2 = result[1];
		}

		if (files1.Count == 0) {
			throw new Exception($"No files found in {path1}");
		}

		if (files2.Count == 0) {
			throw new Exception($"No files found in {path2}");
		}

		var c1 = CompareSets(files1, files2, path1, path2, comparer1, opts.Raw);
		var c2 = 0;
		if (!opts.FirstOnly) {
			c2 = CompareSets(files2, files1, path2, path1, comparer1, opts.Raw);
		}

		if (!opts.Raw) {
			Console.WriteLine();
			Console.WriteLine($"There are {c1 + c2} differences");
		}

		return 0;
	}

	/// <summary>
	/// Global exception handler (for unhandled exceptions)
	/// </summary>
	private static void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		Console.WriteLine();
		if (e.ExceptionObject is Exception ex) {
			Console.WriteLine($"ERROR: {ex.Message}");
		} else {
			Console.WriteLine($"ERROR value: {0}", e.ExceptionObject?.ToString() ?? "?");
		}

		Console.WriteLine();
		Console.WriteLine(CommandLineMessage);
		Environment.Exit(1);
	}

	/// <summary>
	/// Parse the command line info
	/// </summary>
	private static Config ParseCommandLine(string[] args)
	{
		var pico = new PicoArgs(args);

		// handle help
		if (pico.Contains("-h", "--help", "-?")) {
			Console.WriteLine(CommandLineMessage);
			Environment.Exit(0);
		}

		var foldera = pico.GetParam("-a", "--foldera");
		var folderb = pico.GetParam("-b", "--folderb");
		var comparisonStr = pico.GetParamOpt("-c", "--comparison");

		var raw = pico.Contains("-r", "--raw");
		var oneThread = pico.Contains("-o", "--one-thread");
		var firstOnly = pico.Contains("-f", "--first-only");

		pico.Finished();

		return new Config(new DirectoryInfo(foldera), new DirectoryInfo(folderb), ParseType(comparisonStr), raw, oneThread, firstOnly);
	}

	private static ComparisonType ParseType(string? s)
	{
		if (string.IsNullOrEmpty(s)) {
			return ComparisonType.Name;
		}
		if (Enum.TryParse(s, true, out ComparisonType result)) {
			return result;
		} else {
			return ComparisonType.Name;
		}
	}

	/// <summary>
	/// Build the required comparer
	/// </summary>
	private static IEqualityComparer<FileData> BuildComparer(ComparisonType t) => t switch {
		ComparisonType.NameSize => new FileDataNameSizeComparer(),
		ComparisonType.Name => new FileDataNameComparer(),
		ComparisonType.Hash => new FileDataHashComparer(),
		_ => throw new NotImplementedException(),
	};

	/// <summary>
	/// Compare the two sets of files and display the differences
	/// </summary>
	private static int CompareSets(HashSet<FileData> a, HashSet<FileData> b, string path1, string path2, IEqualityComparer<FileData> comparer, bool raw)
	{
		var difference = a
			.Except(b, comparer)
			.OrderBy(f => f.Name);

		var count = 0;
		if (!raw) {
			Console.WriteLine();
			Console.WriteLine($"Files in \"{path1}\" but not in \"{path2}\":");
		}
		foreach (var file in difference) {
			Console.WriteLine(file.Path);
			++count;
		}
		if (count == 0 && !raw) {
			Console.WriteLine("None");
		}

		return count;
	}

	/// <summary>
	/// Scan a folder recursively and return a set of FileData objects
	/// </summary>
	private static HashSet<FileData> ScanFolder(DirectoryInfo dir, bool flagduplicates, IEqualityComparer<FileData> comparer)
	{
		var path = dir.FullName;
		var usehash = comparer is FileDataHashComparer;
		var usesize = usehash || comparer is FileDataNameSizeComparer;

		var files = Directory.GetFiles(path, "*", new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true });
		var fileset = new HashSet<FileData>(files.Length, comparer);

		foreach (var file in files) {
			try {
				var name = Path.GetFileName(file);
				var filePath = Path.GetFullPath(file);
				var size = usesize ? new FileInfo(file).Length : 0;
				var hash = (usehash && size > 0) ? HashBuilder.ComputeHashOfFile(file) : Sha2Value.Empty;

				var data = new FileData(name, filePath, size, hash);
				var added = fileset.Add(data);
				if (flagduplicates && !added) {
					Console.Error.WriteLine($"Duplicate file found: {data.Name}");
				}
			}
			catch (Exception ex) {
				Console.Error.WriteLine($"Error accessing file {file}: {ex.Message}");
			}
		}

		return fileset;
	}

	private const string CommandLineMessage = """
		Usage: FolderCompare.exe --foldera c:\1 --folderb c:\2 [--comparison hash] [--one-thread] [--first-only] [--raw]

		Required:
		  -a, --foldera <folder>    Folder A to compare
		  -b, --folderb <folder>    Folder B to compare

		Options:
		  -c, --comparison <type>    Comparison type: name, namesize, hash (default is name)

		  -h, --help                 Show this help
		  -r, --raw                  Raw output, for piping
		  -o, --one-thread           Use only one thread
		  -f, --first-only           Only show files in A missing in B
		""";
}
