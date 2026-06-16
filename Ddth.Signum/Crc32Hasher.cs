using System.IO.Hashing;

namespace Ddth.Signum;

/// <summary>
/// <see cref="IHasher"/> implementation backed by the non-cryptographic <see cref="Crc32"/>
/// algorithm (32-bit output) from the <c>System.IO.Hashing</c> package. Useful for compatibility
/// with systems that expect a CRC-32 checksum; not recommended where low collision rates matter.
/// </summary>
public sealed class Crc32Hasher : IHasher
{
    /// <summary>Shared factory that creates a new <see cref="Crc32Hasher"/> instance.</summary>
    public static readonly Func<IHasher> Factory = () => new Crc32Hasher();

    private readonly Crc32 _inner = new();

    /// <inheritdoc/>
    public int HashLengthInBytes => _inner.HashLengthInBytes;

    /// <inheritdoc/>
    public void Append(ReadOnlySpan<byte> data) => _inner.Append(data);

    /// <inheritdoc/>
    public void GetHashAndReset(Span<byte> destination) => _inner.GetHashAndReset(destination);
}
