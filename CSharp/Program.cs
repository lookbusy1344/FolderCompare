﻿using System.CommandLine;
using System.CommandLine.Parsing;

namespace FolderCompare;

internal static class Program
{
	public static int Main(string[] args)
	{
		var rootCommand = BuildRootCommand();
		return rootCommand.Invoke(args);
	}

	/// <summary>
	/// Handle the parsed command line arguments
	/// </summary>
	private static async Task ProcessParsedArgsAsync(CliOptions opts)
	{
		var info = GitVersion.VersionInfo.Get();
		if (!opts.Raw) {
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
	}

	/// <summary>
	/// Set up the root command for parsing CLI arguments
	/// </summary>
	private static RootCommand BuildRootCommand()
	{
		var folderAOption = new Option<DirectoryInfo>(
			aliases: ["-a", "--foldera"],
			description: "Folder A to search") {
			IsRequired = true
		};

		var folderBOption = new Option<DirectoryInfo>(
			aliases: ["-b", "--folderb"],
			description: "Folder B to search") {
			IsRequired = true
		};

		var comparisonOption = new Option<ComparisonType>(
			aliases: ["-c", "--comparison"],
			getDefaultValue: () => ComparisonType.Name,
			description: "Comparison type: name, namesize, hash");

		var onethreadFlag = new Option<bool>(
			aliases: ["-o", "--one-thread"],
			getDefaultValue: () => false,
			description: "Use only one thread");

		var rawFlag = new Option<bool>(
			aliases: ["-r", "--raw"],
			getDefaultValue: () => false,
			description: "Raw output, for piping");

		var firstonlyFlag = new Option<bool>(
			aliases: ["-f", "--first-only"],
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

		// this was simpler, but theres a risk of mixing up the params
		// rootCommand.SetHandler(ProcessParsedArgs, folderAOption, folderBOption, comparisonOption, onethreadFlag, rawFlag, firstonlyFlag);

		rootCommand.SetHandler(async context => {
			try {
				// build the options object
				var cliopts = new CliOptions(
					FolderA: context.ParseResult.GetValueForOption(folderAOption),
					FolderB: context.ParseResult.GetValueForOption(folderBOption),
					Compare: context.ParseResult.GetValueForOption(comparisonOption),
					OneThread: context.ParseResult.GetValueForOption(onethreadFlag),
					Raw: context.ParseResult.GetValueForOption(rawFlag),
					FirstOnly: context.ParseResult.GetValueForOption(firstonlyFlag));

				// and process it
				await ProcessParsedArgsAsync(cliopts);
				context.ExitCode = 0;
			}
			catch (Exception ex) {
				Console.WriteLine($"ERROR: {ex.Message}");
				context.ExitCode = 1;
			}
		});

		return rootCommand;
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
}
