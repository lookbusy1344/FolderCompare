using CommandLine;
using System.ComponentModel.DataAnnotations;

namespace FolderCompare;

public enum ComparisonType
{
	[Display(Name = "namesize")]
	namesize,
	[Display(Name = "name")]
	name,
	[Display(Name = "hash")]
	hash,
	[Display(Name = "namehash")]
	namehash
}

public class CliOptions
{
	[Option('a', "foldera", Required = true, HelpText = "Folder A to search", Default = null)]
	public DirectoryInfo? FolderA { get; set; }

	[Option('b', "folderb", Required = true, HelpText = "Folder B to search", Default = null)]
	public DirectoryInfo? FolderB { get; set; }

	[Option('c', "comparison", Required = false, HelpText = "Comparison type: name, namesize, hash, namehash", Default = ComparisonType.name)]
	public ComparisonType Compare { get; set; }

	[Option('o', "one-thread", Required = false, HelpText = "Only use one thread", Default = false)]
	public bool OneThread { get; set; }

	[Option('r', "raw", Required = false, HelpText = "Raw output (for piping)", Default = false)]
	public bool Raw { get; set; }

	[Option('f', "first-only", Required = false, HelpText = "Only show files in A missing in B", Default = false)]
	public bool FirstOnly { get; set; }
}
