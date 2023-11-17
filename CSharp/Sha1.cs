using System.Text;

namespace FolderCompare;

/// <summary>
/// A struct record holding the SHA-1 hash of a file. This is a value type for speed
/// </summary>
[System.Diagnostics.DebuggerDisplay("{this.ToString()}")]
public readonly record struct Sha1Value
{
#pragma warning disable CA1815, IDE0051, IDE0044    // warnings related to inline arrays, nothing significant                                                             
	/// <summary>
	/// A struct containing an inline array of 20 bytes, large enough for a SHA1 hash
	/// </summary>
	[System.Runtime.CompilerServices.InlineArray(Size)]
	private struct InnerSha1
	{
		private byte _element0;
	}
#pragma warning restore CA1815, IDE0051, IDE0044

	/// <summary>
	/// Empty value, all 0 bytes
	/// </summary>
	public static readonly Sha1Value Empty;

	/// <summary>
	/// Size in bytes of the hash
	/// </summary>
	public const int Size = 20;

	/// <summary>
	/// The inline array of 32 bytes
	/// </summary>
	private readonly InnerSha1 val;

	/// <summary>
	/// Inline arrays lack automatic value semantics (like normal arrays), so we need to implement them manually
	/// </summary>
	public readonly bool Equals(Sha1Value other)
	{
		for (var i = 0; i < Size; i++)
			if (val[i] != other.val[i])
				return false;

		return true;
	}

	public override readonly int GetHashCode() => HashCode.Combine(val[0], val[2], val[10], val[14], val[18], val[19]);

	/// <summary>
	/// construct SHA1 value from a byte span
	/// </summary>
	public Sha1Value(ReadOnlySpan<byte> bytes)
	{
		if (bytes.Length != Size)
			throw new ArgumentException("The byte span must contain exactly 20 bytes.", nameof(bytes));

		bytes.CopyTo(val);
	}

#if DEBUG
	/// <summary>
	/// Just a diagnostic constructor to create a Sha1Value from int values
	/// </summary>
	public Sha1Value(uint a, uint b, uint c, uint d, uint e)
	{
		Span<byte> bytes = stackalloc byte[Size];

		HashUtils.WriteBytes(bytes, a);
		HashUtils.WriteBytes(bytes.Slice(4, 4), b);
		HashUtils.WriteBytes(bytes.Slice(8, 4), c);
		HashUtils.WriteBytes(bytes.Slice(12, 4), d);
		HashUtils.WriteBytes(bytes.Slice(16, 4), e);

		bytes.CopyTo(val);
	}
#endif

	/// <summary>
	/// Turn the Sha2Value into a byte array
	/// </summary>
	public byte[] ToBytes()
	{
		var bytes = new byte[Size];

		ReadOnlySpan<byte> src = val;
		src.CopyTo(bytes);

		return bytes;
	}

	/// <summary>
	/// Turn the Sha2Value into a byte array, without allocating a new array
	/// </summary>
	public void ToBytes(Span<byte> bytes)
	{
		//if (bytes.Length != Size)
		//	throw new ArgumentException("The byte span must contain exactly 32 bytes.", nameof(bytes));

		ReadOnlySpan<byte> src = val;
		src.CopyTo(bytes);
	}

	public override string ToString()
	{
		Span<byte> buff = stackalloc byte[Size];
		var builder = new StringBuilder(Size * 2);

		ToBytes(buff);
		foreach (var b in buff)
			_ = builder.AppendFormat("{0:x2}", b);

		return builder.ToString();
	}
}
