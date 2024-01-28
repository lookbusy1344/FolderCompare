using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace FolderCompare;

internal static class HashUtils
{
	private static readonly char[] CharLookup = "0123456789abcdef".ToCharArray();

	/// <summary>
	/// Wrapper around TryWriteBytes that throws an exception if it fails, for uint
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteBytes(Span<byte> destination, uint value)
	{
		if (!BitConverter.TryWriteBytes(destination, value))
			ThrowError();
	}

	/// <summary>
	/// Wrapper around TryWriteBytes that throws an exception if it fails, for ulong
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteBytes(Span<byte> destination, ulong value)
	{
		if (!BitConverter.TryWriteBytes(destination, value))
			ThrowError();
	}

	/// <summary>
	/// Turn a byte into 2 hex chars, without heap allocations
	/// </summary>
	public static (char high, char low) ByteToHex(byte b) => (CharLookup[b >> 4], CharLookup[b & 0x0F]);

	/// <summary>
	/// An optimising routine to improving inlining of BitConverter.TryWriteBytes
	/// Framework Design Guidelines sec 7.1, page 259
	/// </summary>
	[DoesNotReturn]
	private static void ThrowError() => throw new InvalidOperationException("Could not write bytes to span");
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
		Span<byte> hash = stackalloc byte[32];
		using var stream = File.OpenRead(file);
		if (SHA256.HashData(stream, hash) != 32)
			throw new Exception("Failed to compute hash");

		return new Sha2Value(hash);
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
		return new Sha2Value(hash);
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
		return new Sha2Value(hash);
	}

	/// <summary>
	/// Compute the SHA256 hash of a string, without heap allocations
	/// </summary>
	public static Sha2Value ComputeHashOfStringNoAlloc(string text)
	{
		// convert string to bytes on the stack
		var byteCount = Encoding.UTF8.GetByteCount(text);
		if (byteCount > 4096)
			return ComputeHashOfString(text);

		Span<byte> buffer = stackalloc byte[byteCount];
		if (Encoding.UTF8.GetBytes(text, buffer) != byteCount)
			throw new Exception("Failed to convert string to bytes");

		// now hash the bytes, again without heap allocations
		Span<byte> hash = stackalloc byte[32];
		if (!SHA256.TryHashData(buffer, hash, out _))
			throw new Exception("Failed to compute hash");
		return new Sha2Value(hash);
	}

	/// <summary>
	/// Compute the SHA256 hash of a string, by processing the string in chunks without heap allocations
	/// This uses a smaller buffer than ComputeHashOfStringNoAlloc
	/// </summary>
	public static Sha2Value ComputeHashOfStringNoAlloc2(string text)
	{
		const int bufferSizeChars = 1024;

		if (string.IsNullOrEmpty(text)) return Sha2Value.Empty;

		// buffer is 1024 chars, which is 1024-3072 bytes
		Span<byte> buffer = stackalloc byte[bufferSizeChars * 3];
		Span<byte> hash = stackalloc byte[32];

		var charoffset = 0;
		while (true)
		{
			// find the total number of chars left to process
			var remainingChars = text.Length - charoffset;
			if (remainingChars <= 0) break;

			// how many chars can we process into the buffer? (up to bufferSize)
			var charsToCopy = Math.Min(bufferSizeChars, remainingChars);

			// make a slice of the chars
			var textslice = text.AsSpan(charoffset, charsToCopy);

			// convert the chars to bytes, and put them into buffer. This will be 1-3 bytes per char
			var byteslastindex = Encoding.UTF8.GetBytes(textslice, buffer);

			// buffer range is 0..byteslastindex, so hash that
			if (!SHA256.TryHashData(buffer[..byteslastindex], hash, out _))
				throw new Exception("Failed to compute hash");

			// move the offset forward
			charoffset += charsToCopy;
		}

		return new Sha2Value(hash);
	}
}

//public static class ByteUtils
//{
//	/// <summary>
//	/// High performance routine to turn UTF-16 char into UTF-8 bytes (1-3 bytes in a tuple)
//	/// </summary>
//	[MethodImpl(MethodImplOptions.AggressiveInlining)]
//	public static (byte, byte, byte) CharToUtf8(char c)
//	{
//		// a single utf-8 byte
//		if (c <= 0x7f)
//			return ((byte)c, 0, 0);

//		// two utf-8 bytes
//		if (c <= 0x7ff)
//			return ((byte)(0xc0 | (c >> 6)),
//				(byte)(0x80 | (c & 0x3f)),
//				0);

//		// three utf-8 bytes
//		return ((byte)(0xe0 | (c >> 12)),
//			(byte)(0x80 | ((c >> 6) & 0x3f)),
//			(byte)(0x80 | (c & 0x3f)));
//	}

//	/// <summary>
//	/// Convert a span of chars into a span of UTF8 encoded bytes.
//	/// Resulting span must be pre-allocated to be at least input.Length * 3 bytes
//	/// </summary>
//	[MethodImpl(MethodImplOptions.AggressiveInlining)]
//	public static int CharSpanToUtf8Span(ReadOnlySpan<char> input, Span<byte> result)
//	{
//		// each char can be 1-3 bytes
//		if (result.Length < input.Length * 3) ThrowError();

//		var p = 0;
//		for (var i = 0; i < input.Length; i++)
//		{
//			var (b1, b2, b3) = CharToUtf8(input[i]);
//			result[p++] = b1;

//			if (b2 != 0)
//				result[p++] = b2;
//			if (b3 != 0)
//				result[p++] = b3;
//		}

//		return p;
//	}

//	/// <summary>
//	/// An optimising routine to improving inlining of CharSpanToUtf8Span
//	/// Framework Design Guidelines sec 7.1, page 259
//	/// </summary>
//	[DoesNotReturn]
//	private static void ThrowError() => throw new Exception("Output buffer is too small");
//}
