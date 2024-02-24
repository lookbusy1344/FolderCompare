namespace FolderCompare;

/// <summary>
/// A struct record holding the SHA-2 hash of a file. This is a value type for speed
/// </summary>
[System.Diagnostics.DebuggerDisplay("{this.ToString()}")]
public readonly record struct Sha2Value
{
#pragma warning disable CA1815, IDE0051, IDE0044    // warnings related to inline arrays, nothing significant                                                             
	/// <summary>
	/// A struct containing an inline array of 32 bytes, large enough for a SHA2 hash
	/// </summary>
	[System.Runtime.CompilerServices.InlineArray(Size)]
	private struct InnerSha2
	{
		private byte _element0;
	}
#pragma warning restore CA1815, IDE0051, IDE0044

	/// <summary>
	/// Empty value: 32 x 0 bytes
	/// </summary>
	public static readonly Sha2Value Empty;

	/// <summary>
	/// Size in bytes of the hash
	/// </summary>
	public const int Size = 32;

	/// <summary>
	/// The inline array of 32 bytes
	/// </summary>
	private readonly InnerSha2 val;

	/// <summary>
	/// Inline arrays lack automatic value semantics (like normal arrays), so we need to implement them manually
	/// </summary>
	public readonly bool Equals(Sha2Value other)
	{
		for (var i = 0; i < Size; i++)
			if (val[i] != other.val[i])
				return false;

		return true;
	}

	public override readonly int GetHashCode() => HashCode.Combine(val[0], val[2], val[10], val[12], val[20], val[22], val[31]);

	/// <summary>
	/// construct SHA2 value from a byte span
	/// </summary>
	public Sha2Value(ReadOnlySpan<byte> bytes)
	{
		if (bytes.Length != Size)
			throw new ArgumentException("The byte span must contain exactly 32 bytes.", nameof(bytes));

		bytes.CopyTo(val);
	}

#if DEBUG
	/// <summary>
	/// Just a diagnostic constructor to create a Sha2Value from 4 ulong values
	/// </summary>
	public Sha2Value(ulong a, ulong b, ulong c, ulong d)
	{
		// need to cast to byte span to be able to write to it, cannot Slice an inline array in .NET 8
		Span<byte> bytes = val;

		HashUtils.WriteBytes(bytes, a);
		HashUtils.WriteBytes(bytes.Slice(8, 8), b);
		HashUtils.WriteBytes(bytes.Slice(16, 8), c);
		HashUtils.WriteBytes(bytes.Slice(24, 8), d);
	}
#endif

	/// <summary>
	/// Turn the hash into a heap allocated byte array
	/// </summary>
	public byte[] ToBytes() => ((ReadOnlySpan<byte>)val).ToArray();

	/// <summary>
	/// Write the hash into a byte span
	/// </summary>
	public void ToBytes(Span<byte> bytes) => ((ReadOnlySpan<byte>)val).CopyTo(bytes);

	public override string ToString() => HashUtils.ToHexString(val);
}
