namespace FolderCompare;

/// <summary>
/// Command line options structure
/// </summary>
internal sealed record class Config(
	DirectoryInfo FolderA,
	DirectoryInfo FolderB,
	ComparisonType Compare,
	bool Raw,
	bool OneThread,
	bool FirstOnly)
{
	public bool Equals(Config? other) => other != null
										 && FolderA?.FullName == other.FolderA?.FullName
										 && FolderB?.FullName == other.FolderB?.FullName
										 && Compare == other.Compare
										 && OneThread == other.OneThread
										 && Raw == other.Raw
										 && FirstOnly == other.FirstOnly;

	public override int GetHashCode() => HashCode.Combine(FolderA?.FullName, FolderB?.FullName, Compare, OneThread, Raw, FirstOnly);
}
