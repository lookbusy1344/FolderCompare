namespace FolderCompare;

/// <summary>
/// A struct record holding the SHA-2 hash of a file. This is a value type for speed
/// </summary>
public readonly record struct Sha2Value(ulong a, ulong b, ulong c, ulong d)
{
	//public static Sha2Value Create(byte[] bytes)
	//{
	//	if (bytes.Length != 32)
	//		throw new ArgumentException("The byte array must contain exactly 32 bytes.", nameof(bytes));

	//	return new(
	//		a: BitConverter.ToUInt64(bytes, 0),
	//		b: BitConverter.ToUInt64(bytes, 8),
	//		c: BitConverter.ToUInt64(bytes, 16),
	//		d: BitConverter.ToUInt64(bytes, 24));
	//}

	/// <summary>
	/// Create a Sha2Value from a byte span
	/// </summary>
	public static Sha2Value Create(ReadOnlySpan<byte> bytes)
	{
		if (bytes.Length != 32)
			throw new ArgumentException("The byte span must contain exactly 32 bytes.", nameof(bytes));

		return new(
			a: BitConverter.ToUInt64(bytes.Slice(0, 8)),
			b: BitConverter.ToUInt64(bytes.Slice(8, 8)),
			c: BitConverter.ToUInt64(bytes.Slice(16, 8)),
			d: BitConverter.ToUInt64(bytes.Slice(24, 8)));
	}

	/// <summary>
	/// Turn the Sha2Value into a byte array
	/// </summary>
	public byte[] ToBytes()
	{
		var bytes = new byte[32];
		HashUtils.WriteBytes(bytes.AsSpan(), a);
		HashUtils.WriteBytes(bytes.AsSpan(sizeof(ulong)), b);
		HashUtils.WriteBytes(bytes.AsSpan(2 * sizeof(ulong)), c);
		HashUtils.WriteBytes(bytes.AsSpan(3 * sizeof(ulong)), d);

		return bytes;
	}
}

/// <summary>
/// A struct record holding the SHA-1 hash of a file. This is a value type for speed
/// </summary>
public readonly record struct Sha1Value(uint a, uint b, uint c, uint d, uint e)
{
	//public static Sha1Value Create(byte[] bytes)
	//{
	//	if (bytes.Length != 20)
	//		throw new ArgumentException("The byte array must contain exactly 20 bytes.", nameof(bytes));

	//	return new(
	//		a: BitConverter.ToUInt32(bytes, 0),
	//		b: BitConverter.ToUInt32(bytes, 4),
	//		c: BitConverter.ToUInt32(bytes, 8),
	//		d: BitConverter.ToUInt32(bytes, 12),
	//		e: BitConverter.ToUInt32(bytes, 16));
	//}

	/// <summary>
	/// Create a Sha1Value from a byte span
	/// </summary>
	public static Sha1Value Create(ReadOnlySpan<byte> bytes)
	{
		if (bytes.Length != 20)
			throw new ArgumentException("The byte array must contain exactly 20 bytes.", nameof(bytes));

		return new(
			a: BitConverter.ToUInt32(bytes.Slice(0, 4)),
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
		//BitConverter.TryWriteBytes(bytes.AsSpan(), a);
		//BitConverter.TryWriteBytes(bytes.AsSpan(sizeof(uint)), b);
		//BitConverter.TryWriteBytes(bytes.AsSpan(2 * sizeof(uint)), c);
		//BitConverter.TryWriteBytes(bytes.AsSpan(3 * sizeof(uint)), d);
		//BitConverter.TryWriteBytes(bytes.AsSpan(4 * sizeof(uint)), e);
		HashUtils.WriteBytes(bytes.AsSpan(), a);
		HashUtils.WriteBytes(bytes.AsSpan(sizeof(uint)), b);
		HashUtils.WriteBytes(bytes.AsSpan(2 * sizeof(uint)), c);
		HashUtils.WriteBytes(bytes.AsSpan(3 * sizeof(uint)), d);
		HashUtils.WriteBytes(bytes.AsSpan(4 * sizeof(uint)), e);

		return bytes;
	}
}

public static class HashUtils
{
	/// <summary>
	/// Wrapper around TryWriteBytes that throws an exception if it fails, for uint
	/// </summary>
	public static void WriteBytes(Span<byte> destination, uint value)
	{
		if (!BitConverter.TryWriteBytes(destination, value))
			throw new InvalidOperationException("Could not write bytes.");
	}

	/// <summary>
	/// Wrapper around TryWriteBytes that throws an exception if it fails, for ulong
	/// </summary>
	public static void WriteBytes(Span<byte> destination, ulong value)
	{
		if (!BitConverter.TryWriteBytes(destination, value))
			throw new InvalidOperationException("Could not write bytes.");
	}
}
