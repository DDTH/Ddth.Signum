namespace Ddth.Signum;

/// <summary>
/// Convenience entry point for computing fingerprints with the default configuration.
/// For custom hashers or options, construct a <see cref="Fingerprinter"/> directly.
/// </summary>
public static class Signum
{
    private static readonly Fingerprinter Default = new();

    /// <summary>Computes the checksum of <paramref name="value"/> as a byte array.</summary>
    public static byte[] Checksum(object? value) => Default.Compute(value);

    /// <summary>Computes the checksum of <paramref name="value"/> as a lowercase hexadecimal string.</summary>
    public static string ChecksumHex(object? value) => Default.ComputeHex(value);
}
