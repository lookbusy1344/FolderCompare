using System.Text;

namespace FolderCompare;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE1006 // Upper case first letter for public members

/// <summary>
/// A struct record holding the SHA-1 hash of a file. This is a value type for speed
/// </summary>
[System.Diagnostics.DebuggerDisplay("a: {a}, b: {b}, c: {c}, d: {d}, e: {e}")]
public readonly record struct Sha1Value(uint a, uint b, uint c, uint d, uint e)
{
	public static readonly Sha1Value Empty = new(0, 0, 0, 0, 0);

	/// <summary>
	/// Create a Sha1Value from a byte span
	/// </summary>
	public static Sha1Value Create(ReadOnlySpan<byte> bytes)
	{
		if (bytes.Length != 20)
			throw new ArgumentException("The byte array must contain exactly 20 bytes.", nameof(bytes));

		return new(
			a: BitConverter.ToUInt32(bytes[..4]),
			b: BitConverter.ToUInt32(bytes.Slice(4, 4)),
			c: BitConverter.ToUInt32(bytes.Slice(8, 4)),
			d: BitConverter.ToUInt32(bytes.Slice(12, 4)),
			e: BitConverter.ToUInt32(bytes.Slice(16, 4)));
	}

	/// <summary>
	/// Turn the Sha1Value into a byte array
	/// </summary>
	public byte[] ToBytes()
	{
		var bytes = new byte[20];
		HashUtils.WriteBytes(bytes.AsSpan(), a);
		HashUtils.WriteBytes(bytes.AsSpan(sizeof(uint)), b);
		HashUtils.WriteBytes(bytes.AsSpan(2 * sizeof(uint)), c);
		HashUtils.WriteBytes(bytes.AsSpan(3 * sizeof(uint)), d);
		HashUtils.WriteBytes(bytes.AsSpan(4 * sizeof(uint)), e);

		return bytes;
	}

	/// <summary>
	/// Turn the Sha1Value into a byte array, without allocating a new array
	/// </summary>
	public void ToBytes(Span<byte> bytes)
	{
		//if (bytes.Length != 20)
		//	throw new ArgumentException("The byte span must contain exactly 20 bytes.", nameof(bytes));

		HashUtils.WriteBytes(bytes, a);
		HashUtils.WriteBytes(bytes[sizeof(uint)..], b);
		HashUtils.WriteBytes(bytes[(2 * sizeof(uint))..], c);
		HashUtils.WriteBytes(bytes[(3 * sizeof(uint))..], d);
		HashUtils.WriteBytes(bytes[(4 * sizeof(uint))..], e);
	}

	public override string ToString()
	{
		Span<byte> buff = stackalloc byte[20];
		var builder = new StringBuilder(40);

		ToBytes(buff);
		foreach (var b in buff)
			_ = builder.AppendFormat("{0:x2}", b);

		return builder.ToString();
	}
}
