﻿using CommandLine;
using System.Diagnostics;
using System.Security.Cryptography;

namespace FolderCompare;

internal class Program
{
	static void Main(string[] args)
	{
		try
		{
			var parsed = Parser.Default.ParseArguments<CliOptions>(args)
				.WithParsed<CliOptions>(o =>
				{
				}).WithNotParsed<CliOptions>(o =>
				{
				});

			if (parsed.Tag == ParserResultType.NotParsed)
			{
				//var info = GitVersion.VersionInfo.Get();

				//Console.WriteLine($"FolderCompare {info.GetVersionHash(20)}");
				//Console.WriteLine("Recursively compare two folders.");
				//Console.WriteLine();
				//Console.WriteLine("Mandatory parameters:");
				//Console.WriteLine("  --foldera <name>, -a <name>          First folder");
				//Console.WriteLine("  --folderb <name>, -b <name>          Second folder");
				//Console.WriteLine();
				//Console.WriteLine("Optional parameters:");
				//Console.WriteLine("  --comparison <type>, -c <type>       How to compare (name, namesize, hash). Default is name");
				return;
			}

			var info = GitVersion.VersionInfo.Get();
			if (!parsed.Value.Raw)
				Console.WriteLine($"FolderCompare {info.GetVersionHash(20)}");


			var path1 = parsed.Value.FolderA!.FullName;
			var path2 = parsed.Value.FolderB!.FullName;

			if (path1 == path2) throw new Exception("The two folders must be different");

			// we need two comparers because we enumerate both folders in parallel
			var comparer1 = BuildComparer(parsed.Value.Compare);
			var comparer2 = BuildComparer(parsed.Value.Compare);

			if (!parsed.Value.Raw)
				Console.WriteLine($"Comparing \"{path1}\" and \"{path2}\" using {parsed.Value.Compare} comparison...");

			HashSet<FileData> files1, files2;
			if (parsed.Value.OneThread)
			{
				if (!parsed.Value.Raw)
					Console.WriteLine($"Scanning {path1}...");
				files1 = ScanFolder(parsed.Value.FolderA!, comparer1);
				if (!parsed.Value.Raw)
					Console.WriteLine($"Scanning {path2}...");
				files2 = ScanFolder(parsed.Value.FolderB!, comparer2);
			}
			else
			{
				// scan both folders in parallel
				var task1 = Task.Run(() => ScanFolder(parsed.Value.FolderA!, comparer1));
				var task2 = Task.Run(() => ScanFolder(parsed.Value.FolderB!, comparer2));
				Task.WaitAll(task1, task2);

				if (task1.IsFaulted) throw task1.Exception!;
				if (task2.IsFaulted) throw task2.Exception!;

				files1 = task1.Result;
				files2 = task2.Result;
			}

			if (files1.Count == 0) throw new Exception($"No files found in {path1}");
			if (files2.Count == 0) throw new Exception($"No files found in {path2}");

			var c1 = CompareSets(files1, files2, path1, path2, comparer1, parsed.Value.Raw);
			var c2 = 0;
			if (!parsed.Value.FirstOnly)
				c2 = CompareSets(files2, files1, path2, path1, comparer1, parsed.Value.Raw);

			if (!parsed.Value.Raw)
			{
				Console.WriteLine();
				Console.WriteLine($"There are {c1 + c2} differences");
			}
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"ERROR: {ex.Message}");
		}
	}

	static private IEqualityComparer<FileData> BuildComparer(ComparisonType t) => t switch
	{
		ComparisonType.namesize => new FileDataNameSizeComparer(),
		ComparisonType.name => new FileDataNameComparer(),
		ComparisonType.hash => new FileDataHashComparer(),
		_ => throw new NotImplementedException(),
	};

	static private int CompareSets(HashSet<FileData> a, HashSet<FileData> b, string path1, string path2, IEqualityComparer<FileData> comparer, bool raw)
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
	static private HashSet<FileData> ScanFolder(DirectoryInfo dir, IEqualityComparer<FileData> comparer)
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
				var hash = (usehash && size > 0) ? ComputeHash(file) : string.Empty;

				var data = new FileData(name, filePath, size, hash);
				var added = fileset.Add(data);
				if (!added)
					Console.Error.WriteLine($"Duplicate file found: {data.Name}");
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error accessing file {file}: {ex.Message}");
			}
		}

		return fileset;
	}

	static private string ComputeHash(string file)
	{
		using var stream = File.OpenRead(file);
		using var sha = SHA256.Create();
		var hash = sha.ComputeHash(stream);
		return Convert.ToBase64String(hash);
	}
}
