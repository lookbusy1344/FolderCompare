using System.Security.Cryptography;
using System.Text;

namespace FolderCompare;

/// <summary>
/// A struct record holding the SHA-2 hash of a file. This is a value type for speed
/// </summary>
public readonly record struct Sha2Value(ulong a, ulong b, ulong c, ulong d)
{
	public static readonly Sha2Value Empty = new(0, 0, 0, 0);

	/// <summary>
	/// Create a Sha2Value from a byte span
	/// </summary>
	public static Sha2Value Create(ReadOnlySpan<byte> bytes)
	{
		if (bytes.Length != 32)
			throw new ArgumentException("The byte span must contain exactly 32 bytes.", nameof(bytes));

		return new(
			a: BitConverter.ToUInt64(bytes[..8]),
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

	//public override string ToString() => BitConverter.ToString(ToBytes()).Replace("-", "");

	public override string ToString()
	{
		var builder = new StringBuilder(64);
		foreach (var b in ToBytes())
			builder.AppendFormat("{0:x2}", b);
		return builder.ToString();
	}
}

/// <summary>
/// A struct record holding the SHA-1 hash of a file. This is a value type for speed
/// </summary>
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

	//public override string ToString() => BitConverter.ToString(ToBytes()).Replace("-", "");

	public override string ToString()
	{
		var builder = new StringBuilder(40);
		foreach (var b in ToBytes())
			builder.AppendFormat("{0:x2}", b);
		return builder.ToString();
	}
}

internal static class HashUtils
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

/// <summary>
/// Functions for computing hashes based on files and strings
/// </summary>
public static class HashBuilder
{
	/// <summary>
	/// Compute the SHA256 hash of a file. This uses heap allocation
	/// </summary>
	public static Sha2Value ComputeHashOfFile(string file)
	{
		using var stream = File.OpenRead(file);
		var hash = SHA256.HashData(stream);
		return Sha2Value.Create(hash);
	}

	/// <summary>
	/// Compute the SHA256 hash of a file, without heap allocations
	/// </summary>
	public static Sha2Value ComputeHashOfFileNoAlloc(string file)
	{
		using var stream = File.OpenRead(file);
		Span<byte> buffer = stackalloc byte[4096];
		Span<byte> hash = stackalloc byte[32];

		//while (stream.Read(MemoryMarshal.AsBytes(buffer)) is int bytesRead && bytesRead > 0)
		while (stream.Read(buffer) is int bytesRead && bytesRead > 0)
		{
			if (!SHA256.TryHashData(buffer, hash, out _))
				throw new Exception("Failed to compute hash");
		}
		return Sha2Value.Create(hash);
	}

	/// <summary>
	/// Compute the SHA256 hash of a string.
	/// Converting string to bytes is a heap allocation
	/// </summary>
	public static Sha2Value ComputeHashOfString(string text)
	{
		Span<byte> hash = stackalloc byte[32];
		if (!SHA256.TryHashData(Encoding.UTF8.GetBytes(text), hash, out _))
			throw new Exception("Failed to compute hash");
		return Sha2Value.Create(hash);
	}

	/// <summary>
	/// Compute the SHA256 hash of a string, without heap allocations
	/// </summary>
	public static Sha2Value ComputeHashOfStringNoAlloc(string text)
	{
		// convert string to bytes on the stack
		int byteCount = Encoding.UTF8.GetByteCount(text);
		if (byteCount > 4096)
			return ComputeHashOfString(text);

		Span<byte> buffer = stackalloc byte[byteCount];
		if (Encoding.UTF8.GetBytes(text, buffer) != byteCount)
			throw new Exception("Failed to convert string to bytes");

		// now hash the bytes, again without heap allocations
		Span<byte> hash = stackalloc byte[32];
		if (!SHA256.TryHashData(buffer, hash, out _))
			throw new Exception("Failed to compute hash");
		return Sha2Value.Create(hash);
	}
}
