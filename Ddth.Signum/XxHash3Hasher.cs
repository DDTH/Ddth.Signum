using System.IO.Hashing;

namespace Ddth.Signum;

/// <summary>
/// <see cref="IHasher"/> implementation backed by the fast, non-cryptographic
/// <see cref="XxHash3"/> algorithm (64-bit output) from the <c>System.IO.Hashing</c> package.
/// </summary>
public sealed class XxHash3Hasher : IHasher
{
    /// <summary>Shared factory that creates a new <see cref="XxHash3Hasher"/> instance.</summary>
    public static readonly Func<IHasher> Factory = () => new XxHash3Hasher();

    private readonly XxHash3 _inner = new();

    /// <inheritdoc/>
    public int HashLengthInBytes => _inner.HashLengthInBytes;

    /// <inheritdoc/>
    public void Append(ReadOnlySpan<byte> data) => _inner.Append(data);

    /// <inheritdoc/>
    public void GetHashAndReset(Span<byte> destination) => _inner.GetHashAndReset(destination);
}
