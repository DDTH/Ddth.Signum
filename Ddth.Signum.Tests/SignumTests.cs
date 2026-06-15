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
}
