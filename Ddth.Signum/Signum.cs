using System.Collections.Concurrent;

namespace Ddth.Signum;

/// <summary>
/// Convenience entry point for computing fingerprints. When no hasher factory is supplied the
/// default configuration is used; otherwise a <see cref="Fingerprinter"/> built around the
/// supplied factory is used.
/// </summary>
/// <remarks>
/// <see cref="Fingerprinter"/> instances are immutable and thread-safe, so they are cached and
/// reused per hasher factory to avoid allocating a new instance on every call.
/// </remarks>
public static class Signum
{
    private static readonly Fingerprinter Default = new();

    private static readonly ConcurrentDictionary<Func<IHasher>, Fingerprinter> Cache = new();

    /// <summary>Computes the checksum of <paramref name="value"/> as a byte array.</summary>
    /// <param name="value">The value to fingerprint.</param>
    /// <param name="hasherFactory">
    /// Optional factory producing the <see cref="IHasher"/> to use. When <c>null</c>, the default
    /// hasher (<see cref="XxHash128Hasher"/>) is used.
    /// </param>
    public static byte[] Checksum(object? value, Func<IHasher>? hasherFactory = null)
        => Resolve(hasherFactory).Compute(value);

    /// <summary>Computes the checksum of <paramref name="value"/> as a lowercase hexadecimal string.</summary>
    /// <param name="value">The value to fingerprint.</param>
    /// <param name="hasherFactory">
    /// Optional factory producing the <see cref="IHasher"/> to use. When <c>null</c>, the default
    /// hasher (<see cref="XxHash128Hasher"/>) is used.
    /// </param>
    public static string ChecksumHex(object? value, Func<IHasher>? hasherFactory = null)
        => Resolve(hasherFactory).ComputeHex(value);

    private static Fingerprinter Resolve(Func<IHasher>? hasherFactory)
    {
        if (hasherFactory is null)
        {
            return Default;
        }
        return Cache.GetOrAdd(hasherFactory, static factory => new Fingerprinter(new FingerprintOptions { HasherFactory = factory }));
    }
}
