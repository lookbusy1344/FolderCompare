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

public record class CliOptionsx(DirectoryInfo? FolderA, DirectoryInfo? FolderB, ComparisonType Compare, bool OneThread, bool Raw, bool FirstOnly)
{
	public virtual bool Equals(CliOptionsx? other) => other != null
		&& FolderA?.FullName == other.FolderA?.FullName
		&& FolderB?.FullName == other.FolderB?.FullName
		&& Compare == other.Compare
		&& OneThread == other.OneThread
		&& Raw == other.Raw
		&& FirstOnly == other.FirstOnly;

	public override int GetHashCode() => HashCode.Combine(FolderA?.FullName, FolderB?.FullName, Compare, OneThread, Raw, FirstOnly);
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
