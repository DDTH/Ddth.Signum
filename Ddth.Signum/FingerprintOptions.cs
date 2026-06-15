namespace Ddth.Signum;

/// <summary>
/// Options controlling how a <see cref="Fingerprinter"/> computes checksums.
/// </summary>
public sealed class FingerprintOptions
{
    /// <summary>
    /// Factory that produces the <see cref="IHasher"/> used for each digest. When <c>null</c>,
    /// a default <see cref="XxHash128Hasher"/> is used. The factory must return a new instance
    /// on every call and may be invoked many times (and concurrently) for a single computation.
    /// </summary>
    public Func<IHasher>? HasherFactory { get; init; }
}
