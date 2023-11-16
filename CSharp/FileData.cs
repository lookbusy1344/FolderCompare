using System.ComponentModel.DataAnnotations;

namespace FolderCompare;

public enum ComparisonType
{
	[Display(Name = "NameSize")]
	NameSize,
	[Display(Name = "Name")]
	Name,
	[Display(Name = "Hash")]
	Hash
}

public class CliOptions(DirectoryInfo? folderA, DirectoryInfo? folderB, ComparisonType compare, bool oneThread, bool raw, bool firstOnly)
{
	public DirectoryInfo? FolderA { get; init; } = folderA;

	public DirectoryInfo? FolderB { get; init; } = folderB;

	public ComparisonType Compare { get; init; } = compare;

	public bool OneThread { get; init; } = oneThread;

	public bool Raw { get; init; } = raw;

	public bool FirstOnly { get; init; } = firstOnly;
}

/// <summary>
/// Record holding information about a file
/// </summary>
public readonly record struct FileData(string Name, string Path, long Size, Sha2Value Hash);

/// <summary>
/// Compare two FileData objects based on their Name and Size properties
/// </summary>
public class FileDataNameSizeComparer : IEqualityComparer<FileData>
{
	public bool Equals(FileData x, FileData y) => x.Name == y.Name && x.Size == y.Size;

	public int GetHashCode(FileData obj) =>
		// Generate a hash code based on the Name and Size of the file data object
		// or use a tuple:		(obj.Name, obj.Size).GetHashCode();
		obj.Name.GetHashCode() ^ obj.Size.GetHashCode();
}

/// <summary>
/// Compare two FileData objects based on filename only
/// </summary>
public class FileDataNameComparer : IEqualityComparer<FileData>
{
	public bool Equals(FileData x, FileData y) => x.Name == y.Name;

	public int GetHashCode(FileData obj) => obj.Name.GetHashCode();
}

/// <summary>
/// Compare two FileData objects based on hashes
/// </summary>
public class FileDataHashComparer : IEqualityComparer<FileData>
{
	public bool Equals(FileData x, FileData y) => x.Hash == y.Hash && x.Size == y.Size;

	public int GetHashCode(FileData obj) =>
		// Generate a hash code based on the Name and Size of the file data object
		obj.Hash.GetHashCode();
}
