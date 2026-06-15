using System.IO.Hashing;

namespace Ddth.Signum;

/// <summary>
/// Default <see cref="IHasher"/> implementation backed by the non-cryptographic
/// <see cref="XxHash128"/> algorithm from the <c>System.IO.Hashing</c> package.
/// </summary>
public sealed class XxHash128Hasher : IHasher
{
    private readonly XxHash128 _inner = new();

    /// <inheritdoc/>
    public int HashLengthInBytes => _inner.HashLengthInBytes;

    /// <inheritdoc/>
    public void Append(ReadOnlySpan<byte> data) => _inner.Append(data);

    /// <inheritdoc/>
    public void GetHashAndReset(Span<byte> destination) => _inner.GetHashAndReset(destination);
}
