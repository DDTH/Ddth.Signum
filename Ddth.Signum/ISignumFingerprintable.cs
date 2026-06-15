namespace Ddth.Signum;

/// <summary>
/// Receives values contributed by a custom <see cref="ISignumFingerprintable"/> implementation.
/// Each <see cref="Write"/> call contributes one value in an ordered fashion, so the order of
/// calls is significant.
/// </summary>
public interface IFingerprintWriter
{
    /// <summary>Contributes <paramref name="value"/> to the fingerprint. Returns this writer to allow chaining.</summary>
    IFingerprintWriter Write(object? value);
}

/// <summary>
/// Implement this interface to take full control over how an object contributes to its fingerprint,
/// bypassing the default reflection-based behaviour.
/// </summary>
public interface ISignumFingerprintable
{
    /// <summary>Writes the canonical representation of this instance to <paramref name="writer"/>.</summary>
    void WriteFingerprint(IFingerprintWriter writer);
}
