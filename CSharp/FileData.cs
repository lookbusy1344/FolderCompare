using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolderCompare;

/// <summary>
/// Record holding information about a file
/// </summary>
public record class FileData(string Name, string Path, long Size, string Hash);

/// <summary>
/// Compare two FileData objects based on their Name and Size properties
/// </summary>
public class FileDataNameSizeComparer : IEqualityComparer<FileData>
{
	public bool Equals(FileData? x, FileData? y)
	{
		if (x is null && y is null)
			return true;
		if (x is null || y is null)
			return false;

		return x.Name == y.Name && x.Size == y.Size;
	}

	public int GetHashCode(FileData obj)
	{
		// Generate a hash code based on the Name and Size of the file data object
		return obj.Name.GetHashCode() ^ obj.Size.GetHashCode();
	}
}

public class FileDataNameComparer : IEqualityComparer<FileData>
{
	public bool Equals(FileData? x, FileData? y)
	{
		if (x is null && y is null)
			return true;
		if (x is null || y is null)
			return false;

		return x.Name == y.Name;
	}

	public int GetHashCode(FileData obj)
	{
		return obj.Name.GetHashCode();
	}
}

/// <summary>
/// Compare two FileData objects based on hashes
/// </summary>
public class FileDataHashComparer : IEqualityComparer<FileData>
{
	public bool Equals(FileData? x, FileData? y)
	{
		if (x is null && y is null)
			return true;
		if (x is null || y is null)
			return false;

		return x.Hash == y.Hash && x.Size == y.Size;
	}

	public int GetHashCode(FileData obj)
	{
		// Generate a hash code based on the Name and Size of the file data object
		return obj.Hash.GetHashCode() ^ obj.Size.GetHashCode();
	}
}
