namespace Ddth.Signum.Tests;

public class SignumTests
{
    [Fact]
    public void Checksum_IsDeterministic()
    {
        Assert.Equal(Signum.Checksum("hello"), Signum.Checksum("hello"));
    }

    [Fact]
    public void Checksum_DefaultHasher_ProducesSixteenBytes()
    {
        Assert.Equal(16, Signum.Checksum("hello").Length);
    }

    [Fact]
    public void Checksum_DifferentValues_Differ()
    {
        Assert.NotEqual(Signum.Checksum(1), Signum.Checksum(2));
    }

    [Fact]
    public void ChecksumHex_IsDeterministic()
    {
        Assert.Equal(Signum.ChecksumHex("hello"), Signum.ChecksumHex("hello"));
    }

    [Fact]
    public void ChecksumHex_IsLowercaseHexOfChecksum()
    {
        byte[] bytes = Signum.Checksum("hello");
        string expected = Convert.ToHexString(bytes).ToLowerInvariant();
        Assert.Equal(expected, Signum.ChecksumHex("hello"));
    }

    [Fact]
    public void ChecksumHex_MatchesDefaultFingerprinter()
    {
        var fp = new Fingerprinter();
        var value = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
        Assert.Equal(fp.ComputeHex(value), Signum.ChecksumHex(value));
    }

    [Fact]
    public void Checksum_Null_DoesNotThrow()
    {
        Assert.Equal(Signum.ChecksumHex(null), Signum.ChecksumHex(null));
    }

    [Fact]
    public void Checksum_WithNullFactory_MatchesDefault()
    {
        Assert.Equal(Signum.ChecksumHex("hello"), Signum.ChecksumHex("hello", null));
    }

    private sealed class FixedZeroHasher : IHasher
    {
        public int HashLengthInBytes => 8;
        public void Append(ReadOnlySpan<byte> data) { }
        public void GetHashAndReset(Span<byte> destination) => destination.Slice(0, HashLengthInBytes).Clear();
    }

    [Fact]
    public void Checksum_WithCustomFactory_UsesThatHasher()
    {
        Func<IHasher> factory = () => new FixedZeroHasher();
        // Degenerate hasher always returns 8 zero bytes, so output width and value are fixed.
        Assert.Equal(8, Signum.Checksum("a", factory).Length);
        Assert.Equal(Signum.ChecksumHex("a", factory), Signum.ChecksumHex("totally different", factory));
        // ...and differs from the default hasher output.
        Assert.NotEqual(Signum.ChecksumHex("a"), Signum.ChecksumHex("a", factory));
    }

    [Fact]
    public void Checksum_WithCustomFactory_MatchesEquivalentFingerprinter()
    {
        Func<IHasher> factory = () => new FixedZeroHasher();
        var fp = new Fingerprinter(new FingerprintOptions { HasherFactory = factory });
        Assert.Equal(fp.ComputeHex("a"), Signum.ChecksumHex("a", factory));
    }

    [Fact]
    public void Checksum_SameFactoryInstance_IsDeterministic()
    {
        Func<IHasher> factory = () => new XxHash128Hasher();
        Assert.Equal(Signum.ChecksumHex("hello", factory), Signum.ChecksumHex("hello", factory));
        // A custom XxHash128 factory yields the same result as the default.
        Assert.Equal(Signum.ChecksumHex("hello"), Signum.ChecksumHex("hello", factory));
    }
}
