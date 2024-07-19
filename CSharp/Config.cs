namespace FolderCompare;

internal record class Config(DirectoryInfo FolderA, DirectoryInfo FolderB, ComparisonType Compare, bool Raw, bool OneThread, bool FirstOnly);
