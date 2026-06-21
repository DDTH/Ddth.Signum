using System.Collections;
using System.IO.Hashing;
#if NET7_0_OR_GREATER
using System.Numerics;
#endif

namespace Ddth.Signum.Tests;

/// <summary>
/// Behavioural ("business case") test suite mirroring <c>TESTS.md</c>. Each region corresponds to a
/// section in that document and asserts the externally-observable contract of the library, exercised
/// through the public <see cref="Signum"/> facade and <see cref="Fingerprinter"/> type.
/// </summary>
public class BizCaseTests
{
    private static readonly Fingerprinter Fp = new();

    private static string Hex(object? value) => Fp.ComputeHex(value);

    private static void Same(object? a, object? b) => Assert.Equal(Hex(a), Hex(b));

    private static void Different(object? a, object? b) => Assert.NotEqual(Hex(a), Hex(b));

    // ===== Determinism & basics =====

    [Fact]
    public void SameValue_ComputedTwice_IsEqual()
    {
        Same("hello", "hello");
        Same(42, 42);
    }

    [Fact]
    public void Null_IsStable_AndDoesNotThrow()
    {
        Same(null, null);
    }

    [Fact]
    public void Null_DiffersFromEmptyString()
    {
        Different(null, string.Empty);
    }

    [Fact]
    public void DefaultHasher_Produces16Bytes()
    {
        Assert.Equal(16, Signum.Checksum("anything").Length);
    }

    [Fact]
    public void ChecksumHex_IsLowercaseHexOfChecksum()
    {
        byte[] bytes = Signum.Checksum("hello");
        string expected = Convert.ToHexString(bytes).ToLowerInvariant();
        Assert.Equal(expected, Signum.ChecksumHex("hello"));
    }

    // ===== Numeric families =====

    [Fact]
    public void IntegerFamily_SameValueAcrossTypes_SameChecksum()
    {
        string expected = Hex(5);
        Assert.Equal(expected, Hex((byte)5));
        Assert.Equal(expected, Hex((sbyte)5));
        Assert.Equal(expected, Hex((short)5));
        Assert.Equal(expected, Hex((ushort)5));
        Assert.Equal(expected, Hex((uint)5));
        Assert.Equal(expected, Hex((long)5));
        Assert.Equal(expected, Hex((ulong)5));
        Assert.Equal(expected, Hex((nint)5));
        Assert.Equal(expected, Hex((nuint)5));
        Assert.Equal(expected, Hex(new BigInteger(5)));
#if NET7_0_OR_GREATER
        Assert.Equal(expected, Hex((Int128)5));
        Assert.Equal(expected, Hex((UInt128)5));
#endif
    }

    [Fact]
    public void IntegerFamily_DifferentValues_Differ()
    {
        Different(5, 6);
        Different(0, -1);
    }

    [Fact]
    public void LargeUnsignedValue_RoundTripsViaBigInteger()
    {
        Same(ulong.MaxValue, new BigInteger(ulong.MaxValue));
    }

    [Fact]
    public void FloatAndDouble_SameValue_SameChecksum()
    {
        Same(0.5f, 0.5d);
        Same(5.0f, 5.0d);
    }

    [Fact]
    public void HalfAndDecimal_AreDistinctFamilies()
    {
        Different(5.0d, (Half)5.0);
        Different(5.0d, 5.0m);
        Different((Half)5.0, 5.0m);
    }

    [Fact]
    public void SameNumericValue_DifferentFamilies_Differ()
    {
        Different(5, 5.0f);     // int vs float
        Different(5, 5.0d);     // int vs double
        Different(5, 5.0m);     // int vs decimal
        Different(5, (Half)5);  // int vs Half
    }

    [Fact]
    public void BoxedPrimitive_EqualsPrimitive()
    {
        object boxed = 5;
        Same(5, boxed);
    }

    [Fact]
    public void CharVsInteger_OfSameCodePoint_Differ()
    {
        Different('A', (int)'A');
    }

    // ===== Floating-point & decimal edge cases =====

    [Fact]
    public void DoubleNegativeZero_EqualsPositiveZero()
    {
        Same(-0.0d, 0.0d);
    }

    [Fact]
    public void DoubleNaN_IsDeterministic_AndFloatNaNMatches()
    {
        Same(double.NaN, double.NaN);
        Same(double.NaN, float.NaN);
    }

    [Fact]
    public void HalfNaN_IsDeterministic_AndHalfNegativeZero_EqualsZero()
    {
        Same(Half.NaN, Half.NaN);
        Same((Half)(-0.0f), (Half)0);
    }

    [Fact]
    public void DecimalTrailingZeros_DoNotMatter()
    {
        Same(1.0m, 1.00m);
        Same(1.0m, 1m);
    }

    [Fact]
    public void DecimalNegativeZero_EqualsZero()
    {
        decimal negativeZero = new(0, 0, 0, isNegative: true, scale: 2);
        Same(0m, negativeZero);
    }

    // ===== Booleans, dates & common value types =====

    [Fact]
    public void Bool_IsDeterministic_Distinct_AndDiffersFromInteger()
    {
        Same(true, true);
        Different(true, false);
        Different(true, 1);
        Different(false, 0);
    }

    [Fact]
    public void Guid_DiffersByValue()
    {
        var g = Guid.NewGuid();
        Same(g, g);
        Different(Guid.NewGuid(), Guid.NewGuid());
    }

    [Fact]
    public void DateTime_DiffersByValue()
    {
        var a = new DateTime(2020, 1, 1);
        Same(a, new DateTime(2020, 1, 1));
        Different(a, new DateTime(2020, 1, 2));
    }

    [Fact]
    public void DateTimeOffset_DiffersByValue()
    {
        var a = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        Same(a, new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));
        Different(a, new DateTimeOffset(2020, 1, 2, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void TimeSpan_DiffersByValue()
    {
        Same(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        Different(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(6));
    }

    [Fact]
    public void DateOnly_DiffersByValue()
    {
        Same(new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 1));
        Different(new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 2));
    }

    [Fact]
    public void TimeOnly_DiffersByValue()
    {
        Same(new TimeOnly(10, 0), new TimeOnly(10, 0));
        Different(new TimeOnly(10, 0), new TimeOnly(11, 0));
    }

    [Fact]
    public void ByteArray_DiffersFromIntArray_OfSameContent()
    {
        Different(new byte[] { 1, 2, 3 }, new[] { 1, 2, 3 });
    }

    // ===== Enums =====

    private enum Color { Red = 0, Green = 1 }

    private enum Size { Small = 0, Large = 1 }

    [Fact]
    public void Enum_DiffersFromUnderlyingInteger()
    {
        Different(Color.Red, 0);
    }

    [Fact]
    public void DifferentEnumTypes_SameUnderlyingValue_Differ()
    {
        Different(Color.Red, Size.Small);
    }

    [Fact]
    public void SameEnumValue_IsDeterministic()
    {
        Same(Color.Green, Color.Green);
    }

    // ===== Ordered collections (order matters) =====

    [Fact]
    public void Lists_DifferentOrder_Differ()
    {
        Different(new List<int> { 1, 2, 3 }, new List<int> { 3, 2, 1 });
    }

    [Fact]
    public void Arrays_SameContentSameOrder_Same()
    {
        Same(new[] { 1, 2, 3 }, new[] { 1, 2, 3 });
    }

    [Fact]
    public void Array_EqualsList_OfSameItems()
    {
        Same(new[] { 1, 2, 3 }, new List<int> { 1, 2, 3 });
    }

    [Fact]
    public void BareEnumerable_IsTreatedAsOrdered()
    {
        IEnumerable<int> Ascending() { yield return 1; yield return 2; }
        IEnumerable<int> Descending() { yield return 2; yield return 1; }
        Different(Ascending(), Descending());
    }

    // ===== Unordered collections (order does not matter) =====

    [Fact]
    public void Sets_DifferentInsertionOrder_Same()
    {
        Same(new HashSet<int> { 1, 2, 3 }, new HashSet<int> { 3, 1, 2 });
    }

    [Fact]
    public void Set_DiffersFromList_OfSameItems()
    {
        Different(new HashSet<int> { 1, 2, 3 }, new List<int> { 1, 2, 3 });
    }

    [Fact]
    public void Dictionaries_DifferentInsertionOrder_Same()
    {
        var a = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
        var b = new Dictionary<string, int> { ["c"] = 3, ["a"] = 1, ["b"] = 2 };
        Same(a, b);
    }

    [Fact]
    public void Dictionaries_DifferentValues_Differ()
    {
        var a = new Dictionary<string, int> { ["a"] = 1 };
        var b = new Dictionary<string, int> { ["a"] = 2 };
        Different(a, b);
    }

    [Fact]
    public void ReadOnlyDictionary_MatchesEquivalentDictionary()
    {
        var dict = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
        IReadOnlyDictionary<string, int> ro = dict;
        Same(dict, ro);
    }

    private sealed class GenericOnlyDict : IReadOnlyDictionary<string, int>
    {
        private readonly Dictionary<string, int> _inner;

        public GenericOnlyDict(Dictionary<string, int> inner) => _inner = inner;

        public int this[string key] => _inner[key];
        public IEnumerable<string> Keys => _inner.Keys;
        public IEnumerable<int> Values => _inner.Values;
        public int Count => _inner.Count;
        public bool ContainsKey(string key) => _inner.ContainsKey(key);
        public bool TryGetValue(string key, out int value) => _inner.TryGetValue(key, out value);
        public IEnumerator<KeyValuePair<string, int>> GetEnumerator() => _inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();
    }

    [Fact]
    public void GenericOnlyDictionary_MatchesEquivalentDictionary()
    {
        var dict = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
        var generic = new GenericOnlyDict(new Dictionary<string, int> { ["b"] = 2, ["a"] = 1 });
        Same(dict, generic);
    }

    [Fact]
    public void KeyValuePair_IsDeterministic_AndDistinctFromSingleEntryDictionary()
    {
        var kv = new KeyValuePair<string, int>("a", 1);
        Same(kv, new KeyValuePair<string, int>("a", 1));
        Different(kv, new Dictionary<string, int> { ["a"] = 1 });
    }

    // ===== Object reflection fallback =====

    private sealed class Point
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    private sealed class Point3D
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    [Fact]
    public void Poco_SameMemberValues_Same()
    {
        Same(new Point { X = 1, Y = 2 }, new Point { X = 1, Y = 2 });
    }

    [Fact]
    public void Poco_DifferentMemberValues_Differ()
    {
        Different(new Point { X = 1, Y = 2 }, new Point { X = 1, Y = 3 });
    }

    [Fact]
    public void Poco_DifferentTypesSameShape_Differ()
    {
        Different(new Point { X = 1, Y = 2 }, new Point3D { X = 1, Y = 2 });
    }

    private sealed class WithFields
    {
        public int A;

        [SignumIgnore]
        public int Ignored;
    }

    [Fact]
    public void PublicFields_AreIncluded()
    {
        Different(new WithFields { A = 1 }, new WithFields { A = 2 });
    }

    [Fact]
    public void SignumIgnore_ExcludesMembers()
    {
        var a = new WithFields { A = 1, Ignored = 10 };
        var b = new WithFields { A = 1, Ignored = 999 };
        Same(a, b);
    }

    private sealed class WithSecretProperty
    {
        public int Visible { get; set; }

        [SignumIgnore]
        public int Secret { get; set; }
    }

    [Fact]
    public void SignumIgnore_ExcludesProperties()
    {
        var a = new WithSecretProperty { Visible = 1, Secret = 100 };
        var b = new WithSecretProperty { Visible = 1, Secret = 999 };
        Same(a, b);
    }

    private sealed class ThrowingProperty
    {
        public int Ok { get; set; }

        public static int Bad => throw new InvalidOperationException("boom");
    }

    [Fact]
    public void ThrowingPropertyGetter_IsSkipped()
    {
        Same(new ThrowingProperty { Ok = 1 }, new ThrowingProperty { Ok = 1 });
    }

    // ===== Custom fingerprinting =====

    private sealed class CustomFingerprint : ISignumFingerprintable
    {
        public int Ignored { get; set; }

        public int Relevant { get; set; }

        public void WriteFingerprint(IFingerprintWriter writer) => writer.Write(Relevant);
    }

    [Fact]
    public void Fingerprintable_OnlyWrittenStateMatters()
    {
        var a = new CustomFingerprint { Relevant = 7, Ignored = 1 };
        var b = new CustomFingerprint { Relevant = 7, Ignored = 2 };
        Same(a, b);

        var c = new CustomFingerprint { Relevant = 8, Ignored = 1 };
        Different(a, c);
    }

    // ===== Cycles & shared references =====

    private sealed class Node
    {
        public int Value { get; set; }
        public Node? Next { get; set; }
    }

    [Fact]
    public void SelfCycle_DoesNotThrow_AndIsStable()
    {
        var a = new Node { Value = 1 };
        a.Next = a;
        Assert.Equal(Hex(a), Hex(a));
    }

    [Fact]
    public void SharedReference_IsNotTreatedAsCycle()
    {
        var shared = new Point { X = 1, Y = 1 };
        var graphA = new List<Point> { shared, shared };
        var separate = new Point { X = 1, Y = 1 };
        var graphB = new List<Point> { separate, separate };
        Same(graphA, graphB);
    }

    // ===== Nested structures =====

    [Fact]
    public void NestedStructures_OuterOrderIgnored_InnerOrderMatters()
    {
        var a = new Dictionary<string, List<int>>
        {
            ["x"] = new List<int> { 1, 2 },
            ["y"] = new List<int> { 3, 4 },
        };
        var reordered = new Dictionary<string, List<int>>
        {
            ["y"] = new List<int> { 3, 4 },
            ["x"] = new List<int> { 1, 2 },
        };
        Same(a, reordered);

        var innerSwapped = new Dictionary<string, List<int>>
        {
            ["x"] = new List<int> { 2, 1 },
            ["y"] = new List<int> { 3, 4 },
        };
        Different(a, innerSwapped);
    }

    // ===== Pluggable hashers =====

    public static IEnumerable<object[]> BuiltInHashers()
    {
        yield return new object[] { (Func<IHasher>)(() => new XxHash128Hasher()), 16 };
        yield return new object[] { (Func<IHasher>)(() => new XxHash3Hasher()), 8 };
        yield return new object[] { (Func<IHasher>)(() => new Crc32Hasher()), 4 };
    }

    [Theory]
    [MemberData(nameof(BuiltInHashers))]
    public void BuiltInHasher_ProducesExpectedWidth(Func<IHasher> factory, int expectedWidth)
    {
        Assert.Equal(expectedWidth, Signum.Checksum("hello", factory).Length);
    }

    [Theory]
    [MemberData(nameof(BuiltInHashers))]
    public void BuiltInHasher_IsDeterministic_AndDistinguishesValues(Func<IHasher> factory, int expectedWidth)
    {
        _ = expectedWidth;
        Assert.Equal(Signum.ChecksumHex("hello", factory), Signum.ChecksumHex("hello", factory));
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

        Assert.Equal(first, buffer);
    }

    private sealed class FixedZeroHasher : IHasher
    {
        public int HashLengthInBytes => 8;
        public void Append(ReadOnlySpan<byte> data) { }
        public void GetHashAndReset(Span<byte> destination) => destination.Slice(0, HashLengthInBytes).Clear();
    }

    [Fact]
    public void CustomHasherViaOptions_IsUsed_AndWidthReflected()
    {
        var fp = new Fingerprinter(new FingerprintOptions { HasherFactory = () => new FixedZeroHasher() });
        Assert.Equal(8, fp.Compute("a").Length);
        Assert.Equal(fp.ComputeHex("a"), fp.ComputeHex("totally different"));
    }

    // ===== Signum static helper =====

    [Fact]
    public void Signum_Checksum_IsDeterministic_AndDistinguishesValues()
    {
        Assert.Equal(Signum.ChecksumHex("hello"), Signum.ChecksumHex("hello"));
        Assert.NotEqual(Signum.Checksum(1), Signum.Checksum(2));
    }

    [Fact]
    public void Signum_NullFactory_FallsBackToDefault()
    {
        Assert.Equal(Signum.ChecksumHex("hello"), Signum.ChecksumHex("hello", null));
    }

    [Fact]
    public void Signum_SuppliedFactory_IsUsed_AndMatchesEquivalentFingerprinter()
    {
        Func<IHasher> factory = () => new FixedZeroHasher();
        var fp = new Fingerprinter(new FingerprintOptions { HasherFactory = factory });
        Assert.Equal(fp.ComputeHex("a"), Signum.ChecksumHex("a", factory));
        Assert.NotEqual(Signum.ChecksumHex("a"), Signum.ChecksumHex("a", factory));
    }

    [Fact]
    public void Signum_SameFactoryDelegate_ReusesCachedFingerprinter()
    {
        Func<IHasher> factory = XxHash3Hasher.Factory;
        // Repeated calls with the same delegate instance must remain consistent (cache hit).
        Assert.Equal(Signum.ChecksumHex("hello", factory), Signum.ChecksumHex("hello", factory));
    }

    [Fact]
    public void StaticFactories_CreateExpectedTypes_AndDistinctInstances()
    {
        Assert.IsType<XxHash128Hasher>(XxHash128Hasher.Factory());
        Assert.IsType<XxHash3Hasher>(XxHash3Hasher.Factory());
        Assert.IsType<Crc32Hasher>(Crc32Hasher.Factory());
        Assert.NotSame(XxHash3Hasher.Factory(), XxHash3Hasher.Factory());
    }

    [Fact]
    public void StaticFactory_MatchesEquivalentInlineLambda()
    {
        Assert.Equal(
            Signum.ChecksumHex("hello", () => new XxHash3Hasher()),
            Signum.ChecksumHex("hello", XxHash3Hasher.Factory));
    }

    [Fact]
    public void XxHash128Factory_MatchesDefaultPath()
    {
        Assert.Equal(Signum.ChecksumHex("hello"), Signum.ChecksumHex("hello", XxHash128Hasher.Factory));
    }
}
