
using System.CommandLine;
using System.CommandLine.Parsing;

namespace FolderCompare;

internal static class Program
{
	public static int Main(string[] args)
	{
		try
		{
			var rootCommand = BuildRootCommand();
			return rootCommand.Invoke(args);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"ERROR: {ex.Message}");
			return 99;
		}
	}

	/// <summary>
	/// Handle the parsed command line arguments
	/// </summary>
	private static void ProcessParsedArgs(DirectoryInfo foldera, DirectoryInfo folderb, ComparisonType comparison, bool onethread, bool raw, bool firstonly)
	{
		var info = GitVersion.VersionInfo.Get();
		if (!raw)
			Console.WriteLine($"FolderCompare {info.GetVersionHash(20)}");

		var path1 = foldera.FullName;
		var path2 = folderb.FullName;

		if (path1 == path2) throw new Exception("The two folders must be different");

		// we need two comparers because we enumerate both folders in parallel
		var comparer1 = BuildComparer(comparison);
		var comparer2 = BuildComparer(comparison);

		if (!raw)
			Console.WriteLine($"Comparing \"{path1}\" and \"{path2}\" using {comparison} comparison...");

		HashSet<FileData> files1, files2;
		if (onethread)
		{
			if (!raw)
				Console.WriteLine($"Scanning {path1}...");
			files1 = ScanFolder(foldera, !raw, comparer1);

			if (!raw)
				Console.WriteLine($"Scanning {path2}...");
			files2 = ScanFolder(folderb, !raw, comparer2);
		}
		else
		{
			// scan both folders in parallel
			var task1 = Task.Run(() => ScanFolder(foldera, false, comparer1));
			var task2 = Task.Run(() => ScanFolder(folderb, false, comparer2));
			Task.WaitAll(task1, task2);

			if (task1.IsFaulted) throw task1.Exception!;
			if (task2.IsFaulted) throw task2.Exception!;

			files1 = task1.Result;
			files2 = task2.Result;
		}

		if (files1.Count == 0) throw new Exception($"No files found in {path1}");
		if (files2.Count == 0) throw new Exception($"No files found in {path2}");

		var c1 = CompareSets(files1, files2, path1, path2, comparer1, raw);
		var c2 = 0;
		if (!firstonly)
			c2 = CompareSets(files2, files1, path2, path1, comparer1, raw);

		if (!raw)
		{
			Console.WriteLine();
			Console.WriteLine($"There are {c1 + c2} differences");
		}
	}

	/// <summary>
	/// Set up the root command for parsing CLI arguments
	/// </summary>
	private static RootCommand BuildRootCommand()
	{
		var folderAOption = new Option<DirectoryInfo>(
			aliases: new[] { "-a", "--foldera" },
			description: "Folder A to search")
		{
			IsRequired = true
		};

		var folderBOption = new Option<DirectoryInfo>(
			aliases: new[] { "-b", "--folderb" },
			description: "Folder B to search")
		{
			IsRequired = true
		};

		var comparisonOption = new Option<ComparisonType>(
			aliases: new[] { "-c", "--comparison" },
			getDefaultValue: () => ComparisonType.Name,
			description: "Comparison type: name, namesize, hash");

		var onethreadFlag = new Option<bool>(
			aliases: new[] { "-o", "--one-thread" },
			getDefaultValue: () => false,
			description: "Use only one thread");

		var rawFlag = new Option<bool>(
			aliases: new[] { "-r", "--raw" },
			getDefaultValue: () => false,
			description: "Raw output, for piping");

		var firstonlyFlag = new Option<bool>(
			aliases: new[] { "-f", "--first-only" },
			getDefaultValue: () => false,
			description: "Only show files in A missing in B");

		var ver = GitVersion.VersionInfo.Get();
		var rootCommand = new RootCommand($"FolderCompare .NET {ver.GetVersionHash(20)}");
		rootCommand.AddOption(folderAOption);
		rootCommand.AddOption(folderBOption);
		rootCommand.AddOption(comparisonOption);
		rootCommand.AddOption(onethreadFlag);
		rootCommand.AddOption(rawFlag);
		rootCommand.AddOption(firstonlyFlag);

		rootCommand.SetHandler(ProcessParsedArgs, folderAOption, folderBOption, comparisonOption, onethreadFlag, rawFlag, firstonlyFlag);

		return rootCommand;
	}

	/// <summary>
	/// Build the required comparer
	/// </summary>
	private static IEqualityComparer<FileData> BuildComparer(ComparisonType t) => t switch
	{
		ComparisonType.NameSize => new FileDataNameSizeComparer(),
		ComparisonType.Name => new FileDataNameComparer(),
		ComparisonType.Hash => new FileDataHashComparer(),
		_ => throw new NotImplementedException(),
	};

	/// <summary>
	/// Compare the two sets of files and display the differences
	/// </summary>
	private static int CompareSets(in HashSet<FileData> a, in HashSet<FileData> b, in string path1, in string path2, in IEqualityComparer<FileData> comparer, bool raw)
	{
		var difference = a
			.Except(b, comparer)
			.OrderBy(f => f.Name);

		var count = 0;
		if (!raw)
		{
			Console.WriteLine();
			Console.WriteLine($"Files in \"{path1}\" but not in \"{path2}\":");
		}
		foreach (var file in difference)
		{
			Console.WriteLine(file.Path);
			++count;
		}
		if (count == 0 && !raw)
			Console.WriteLine("None");

		return count;
	}

	/// <summary>
	/// Scan a folder recursively and return a set of FileData objects
	/// </summary>
	private static HashSet<FileData> ScanFolder(in DirectoryInfo dir, bool flagduplicates, in IEqualityComparer<FileData> comparer)
	{
		var path = dir.FullName;
		var usehash = comparer is FileDataHashComparer;
		var usesize = usehash || comparer is FileDataNameSizeComparer;

		var files = Directory.GetFiles(path, "*", new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true });
		var fileset = new HashSet<FileData>(files.Length, comparer);

		foreach (var file in files)
		{
			try
			{
				var name = Path.GetFileName(file);
				var filePath = Path.GetFullPath(file);
				var size = usesize ? new FileInfo(file).Length : 0;
				var hash = (usehash && size > 0) ? HashBuilder.ComputeHashOfFile(file) : Sha2Value.Empty;

				var data = new FileData(name, filePath, size, hash);
				var added = fileset.Add(data);
				if (flagduplicates && !added)
					Console.Error.WriteLine($"Duplicate file found: {data.Name}");
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error accessing file {file}: {ex.Message}");
			}
		}

		return fileset;
	}
}
