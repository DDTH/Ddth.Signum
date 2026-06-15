namespace Ddth.Signum;

/// <summary>
/// Abstraction over an incremental hash function used to compute fingerprints.
/// Implementations need not be thread-safe; a fresh instance is created for every digest.
/// </summary>
public interface IHasher
{
    /// <summary>Appends bytes to the running hash.</summary>
    void Append(ReadOnlySpan<byte> data);

    /// <summary>Length, in bytes, of the hash produced by <see cref="GetHashAndReset"/>.</summary>
    int HashLengthInBytes { get; }

    /// <summary>
    /// Writes the current hash into <paramref name="destination"/> and resets the instance
    /// so it can be reused. <paramref name="destination"/> must be at least
    /// <see cref="HashLengthInBytes"/> bytes long.
    /// </summary>
    void GetHashAndReset(Span<byte> destination);
}
