using System.IO.Hashing;

namespace Ddth.Signum.Tests;

public class HasherTests
{
    public static IEnumerable<object[]> Hashers()
    {
        yield return new object[] { (Func<IHasher>)(() => new XxHash128Hasher()), 16 };
        yield return new object[] { (Func<IHasher>)(() => new XxHash3Hasher()), 8 };
        yield return new object[] { (Func<IHasher>)(() => new Crc32Hasher()), 4 };
    }

    [Fact]
    public void Factory_CreatesNewInstanceOfExpectedType()
    {
        Assert.IsType<XxHash128Hasher>(XxHash128Hasher.Factory());
        Assert.IsType<XxHash3Hasher>(XxHash3Hasher.Factory());
        Assert.IsType<Crc32Hasher>(Crc32Hasher.Factory());

        // Each invocation returns a distinct instance.
        Assert.NotSame(XxHash3Hasher.Factory(), XxHash3Hasher.Factory());
    }

    [Fact]
    public void Factory_MatchesEquivalentInlineFactory()
    {
        Assert.Equal(
            Signum.ChecksumHex("hello", () => new XxHash3Hasher()),
            Signum.ChecksumHex("hello", XxHash3Hasher.Factory));
    }

    [Fact]
    public void XxHash128Factory_MatchesDefault()
    {
        Assert.Equal(Signum.ChecksumHex("hello"), Signum.ChecksumHex("hello", XxHash128Hasher.Factory));
    }

    [Theory]
    [MemberData(nameof(Hashers))]
    public void Hasher_ProducesExpectedWidth(Func<IHasher> factory, int expectedWidth)
    {
        Assert.Equal(expectedWidth, Signum.Checksum("hello", factory).Length);
    }

    [Theory]
    [MemberData(nameof(Hashers))]
    public void Hasher_IsDeterministic(Func<IHasher> factory, int expectedWidth)
    {
        _ = expectedWidth;
        Assert.Equal(Signum.ChecksumHex("hello", factory), Signum.ChecksumHex("hello", factory));
    }

    [Theory]
    [MemberData(nameof(Hashers))]
    public void Hasher_DistinguishesValues(Func<IHasher> factory, int expectedWidth)
    {
        _ = expectedWidth;
        Assert.NotEqual(Signum.ChecksumHex("hello", factory), Signum.ChecksumHex("world", factory));
    }

    [Fact]
    public void XxHash3Hasher_MatchesUnderlyingAlgorithm()
    {
        var hasher = new XxHash3Hasher();
        Assert.Equal(new XxHash3().HashLengthInBytes, hasher.HashLengthInBytes);

        byte[] data = { 1, 2, 3, 4 };
        hasher.Append(data);
        var actual = new byte[hasher.HashLengthInBytes];
        hasher.GetHashAndReset(actual);
        Assert.Equal(XxHash3.Hash(data), actual);
    }

    [Fact]
    public void Crc32Hasher_MatchesUnderlyingAlgorithm()
    {
        var hasher = new Crc32Hasher();
        Assert.Equal(new Crc32().HashLengthInBytes, hasher.HashLengthInBytes);

        byte[] data = { 1, 2, 3, 4 };
        hasher.Append(data);
        var actual = new byte[hasher.HashLengthInBytes];
        hasher.GetHashAndReset(actual);
        Assert.Equal(Crc32.Hash(data), actual);
    }

    [Fact]
    public void GetHashAndReset_AllowsReuse()
    {
        var hasher = new XxHash3Hasher();
        var buffer = new byte[hasher.HashLengthInBytes];

        hasher.Append(new byte[] { 1 });
        hasher.GetHashAndReset(buffer);
        byte[] first = (byte[])buffer.Clone();

        hasher.Append(new byte[] { 1 });
        hasher.GetHashAndReset(buffer);

        Assert.Equal(first, buffer); // reset cleared prior state
    }
}
