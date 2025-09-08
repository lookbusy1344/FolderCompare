namespace FolderCompare;

using System.Collections;
using System.ComponentModel.DataAnnotations;

public enum ComparisonType
{
	[Display(Name = "NameSize")] NameSize,
	[Display(Name = "Name")] Name,
	[Display(Name = "Hash")] Hash
}

/// <summary>
/// Record holding information about a file
/// </summary>
public readonly record struct FileData(string Name, string Path, long Size, Sha2Value Hash);

/// <summary>
/// Compare two FileData objects based on their Name and Size properties
/// </summary>
public class FileDataNameSizeComparer : IEqualityComparer<FileData>, IEqualityComparer
{
	public bool Equals(FileData x, FileData y) => x.Name == y.Name && x.Size == y.Size;

	public int GetHashCode(FileData obj) => HashCode.Combine(obj.Name, obj.Size);

	public new bool Equals(object? x, object? y)
	{
		if (x == y) {
			return true;
		}

#pragma warning disable IDE0046 // Convert to conditional expression
		if (x == null || y == null) {
			return false;
		}
#pragma warning restore IDE0046 // Convert to conditional expression

		return x is FileData a && y is FileData b
			? Equals(a, b)
			: throw new ArgumentException("Types dont match", nameof(x));
	}

	public int GetHashCode(object obj)
	{
#pragma warning disable IDE0046 // Convert to conditional expression
		if (obj == null) {
			return 0;
		}
#pragma warning restore IDE0046 // Convert to conditional expression

		return obj is FileData x
			? GetHashCode(x)
			: throw new ArgumentException("Incorrect type", nameof(obj));
	}
}

/// <summary>
/// Compare two FileData objects based on filename only
/// </summary>
public class FileDataNameComparer : IEqualityComparer<FileData>, IEqualityComparer
{
	public bool Equals(FileData x, FileData y) => x.Name == y.Name;

	public int GetHashCode(FileData obj) => obj.Name.GetHashCode();

	public new bool Equals(object? x, object? y)
	{
		if (x == y) {
			return true;
		}

#pragma warning disable IDE0046 // Convert to conditional expression
		if (x == null || y == null) {
			return false;
		}
#pragma warning restore IDE0046 // Convert to conditional expression

		return x is FileData a && y is FileData b
			? Equals(a, b)
			: throw new ArgumentException("Types dont match", nameof(x));
	}

	public int GetHashCode(object obj)
	{
#pragma warning disable IDE0046 // Convert to conditional expression
		if (obj == null) {
			return 0;
		}
#pragma warning restore IDE0046 // Convert to conditional expression

		return obj is FileData x
			? GetHashCode(x)
			: throw new ArgumentException("Incorrect type", nameof(obj));
	}
}

/// <summary>
/// Compare two FileData objects based on hashes
/// </summary>
public class FileDataHashComparer : IEqualityComparer<FileData>, IEqualityComparer
{
	public bool Equals(FileData x, FileData y) => x.Hash == y.Hash && x.Size == y.Size;

	public int GetHashCode(FileData obj) => obj.Hash.GetHashCode();

	public new bool Equals(object? x, object? y)
	{
		if (x == y) {
			return true;
		}

#pragma warning disable IDE0046 // Convert to conditional expression
		if (x == null || y == null) {
			return false;
		}
#pragma warning restore IDE0046 // Convert to conditional expression

		return x is FileData a && y is FileData b
			? Equals(a, b)
			: throw new ArgumentException("Types dont match", nameof(x));
	}

	public int GetHashCode(object obj)
	{
#pragma warning disable IDE0046 // Convert to conditional expression
		if (obj == null) {
			return 0;
		}
#pragma warning restore IDE0046 // Convert to conditional expression

		return obj is FileData x
			? GetHashCode(x)
			: throw new ArgumentException("Incorrect type", nameof(obj));
	}
}
