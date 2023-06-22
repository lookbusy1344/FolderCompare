using System.Diagnostics;
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

	/// <summary>
	/// Unfinished attempt to compute the SHA256 hash of a string, by processing the string in chunks
	/// </summary>
	public static Sha2Value ComputeHashOfStringNoAllocUNFINISHED(string text)
	{
		const int bufferSize = 4096;
		int byteCount = Encoding.UTF8.GetByteCount(text);

		Span<byte> buffer = stackalloc byte[bufferSize];
		Span<byte> hash = stackalloc byte[32];

		int offset = 0;
		while (offset < byteCount)
		{
			int remainingBytes = byteCount - offset;
			int bytesToCopy = Math.Min(bufferSize, remainingBytes);

			// *** i think this is flawed. A span<char,10> might generate 10 bytes, or up to 30 bytes.
			// so buffer needs to be x3 the size of the char span
			if (Encoding.UTF8.GetBytes(text.AsSpan(offset, bytesToCopy), buffer) != bytesToCopy)
				throw new Exception("Failed to convert string to bytes");

			if (!SHA256.TryHashData(buffer[..bytesToCopy], hash, out _))
				throw new Exception("Failed to compute hash");

			offset += bytesToCopy;
		}

		throw new NotImplementedException();
		//return Sha2Value.Create(hash);
	}
}

public static class ByteUtils
{
	/// <summary>
	/// High performance routine to turn UTF-16 char into UTF-8 bytes (1-3 bytes)
	/// </summary>
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	public static (byte, byte, byte) CharToUtf8(char c)
	{
		// a single utf-8 byte
		if (c <= 0x7f)
			return ((byte)c, 0, 0);

		// two utf-8 bytes
		if (c <= 0x7ff)
			return ((byte)(0xc0 | (c >> 6)),
				(byte)(0x80 | (c & 0x3f)),
				0);

		// three utf-8 bytes
		return ((byte)(0xe0 | (c >> 12)),
			(byte)(0x80 | ((c >> 6) & 0x3f)),
			(byte)(0x80 | (c & 0x3f)));
	}

	/// <summary>
	/// Turn a string into a sequence of UTF-8 bytes, without heap allocations
	/// </summary>
	public static IEnumerable<byte> StringToUtf8(string s)
	{
		foreach (var c in s)
		{
			var (b1, b2, b3) = CharToUtf8(c);
			yield return b1;

			if (b2 != 0)
				yield return b2;
			if (b3 != 0)
				yield return b3;
		}
	}

	public static IEnumerable<byte> StringToUtf8(ReadOnlySpan<char> cspan)
	{
		Span<byte> result = stackalloc byte[cspan.Length * 3];
		int p = 0;
		for(int i = 0; i < cspan.Length; i++)
		{
			var (b1, b2, b3) = CharToUtf8(cspan[i]);
			result[p++] = b1;

			if (b2 != 0)
				result[p++] = b2;
			if (b3 != 0)
				result[p++] = b3;
		}

		// *** maybe take a ref to an existing output buffer

		throw new NotImplementedException();
		//foreach (var b in result[..p])
		//	yield return b;
	}
}
