using System.Numerics;

namespace Ddth.Signum.Tests;

public class FingerprinterTests
{
    private static readonly Fingerprinter Fp = new();

    private static string Hex(object? value) => Fp.ComputeHex(value);

    private static void AssertDifferent(object? a, object? b) => Assert.NotEqual(Hex(a), Hex(b));

    // ----- basics -----

    [Fact]
    public void Null_IsStable()
    {
        Assert.Equal(Hex(null), Hex(null));
    }

    [Fact]
    public void SameValue_IsDeterministic()
    {
        Assert.Equal(Hex("hello"), Hex("hello"));
        Assert.Equal(Hex(42), Hex(42));
    }

    [Fact]
    public void Null_DiffersFromEmptyString()
    {
        AssertDifferent(null, string.Empty);
    }

    // ----- integer family: same value across types => same checksum -----

    [Fact]
    public void IntegerFamily_SameValue_SameChecksum()
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
    }

    [Fact]
    public void IntegerFamily_DifferentValues_DifferentChecksum()
    {
        AssertDifferent(5, 6);
        AssertDifferent(0, -1);
    }

    [Fact]
    public void LargeUnsignedValue_RoundTrips()
    {
        Assert.Equal(Hex(ulong.MaxValue), Hex(new BigInteger(ulong.MaxValue)));
    }

    // ----- boxing: a boxed primitive equals the primitive -----

    [Fact]
    public void BoxedPrimitive_EqualsPrimitive()
    {
        object boxed = 5;
        Assert.Equal(Hex(5), Hex(boxed));
    }

    // ----- different families with the same value differ -----

    [Fact]
    public void IntVsFloat_Differ()
    {
        AssertDifferent(5, 5.0f);
        AssertDifferent(5, 5.0d);
    }

    [Fact]
    public void FloatFamilyVsDecimalAndHalf_Differ()
    {
        AssertDifferent(5.0d, 5.0m);
        AssertDifferent(5.0d, (Half)5.0);
        AssertDifferent(5.0m, (Half)5.0);
    }

    [Fact]
    public void CharVsInteger_Differ()
    {
        AssertDifferent('A', (int)'A');
    }

    // ----- float family: float and double of the same value match -----

    [Fact]
    public void FloatAndDouble_SameValue_SameChecksum()
    {
        Assert.Equal(Hex(0.5d), Hex(0.5f));
        Assert.Equal(Hex(5.0d), Hex(5.0f));
    }

    [Fact]
    public void NegativeZero_EqualsPositiveZero()
    {
        Assert.Equal(Hex(0.0d), Hex(-0.0d));
    }

    // ----- decimal: trailing zeros do not matter -----

    [Fact]
    public void Decimal_TrailingZeros_Ignored()
    {
        Assert.Equal(Hex(1.0m), Hex(1.00m));
        Assert.Equal(Hex(1.0m), Hex(1m));
    }

    // ----- enums -----

    private enum Color { Red = 0, Green = 1 }

    private enum Size { Small = 0, Large = 1 }

    [Fact]
    public void Enum_DiffersFromUnderlyingInteger()
    {
        AssertDifferent(Color.Red, 0);
    }

    [Fact]
    public void DifferentEnumTypes_SameUnderlyingValue_Differ()
    {
        AssertDifferent(Color.Red, Size.Small);
    }

    [Fact]
    public void SameEnumValue_SameChecksum()
    {
        Assert.Equal(Hex(Color.Green), Hex(Color.Green));
    }

    // ----- ordered collections: order matters -----

    [Fact]
    public void Lists_DifferentOrder_DifferentChecksum()
    {
        var a = new List<int> { 1, 2, 3 };
        var b = new List<int> { 3, 2, 1 };
        AssertDifferent(a, b);
    }

    [Fact]
    public void Arrays_SameContentSameOrder_SameChecksum()
    {
        var a = new[] { 1, 2, 3 };
        var b = new[] { 1, 2, 3 };
        Assert.Equal(Hex(a), Hex(b));
    }

    [Fact]
    public void Array_EqualsList_OfSameItems()
    {
        // both are ordered sequences at the interface level
        var array = new[] { 1, 2, 3 };
        var list = new List<int> { 1, 2, 3 };
        Assert.Equal(Hex(array), Hex(list));
    }

    [Fact]
    public void IEnumerable_TreatedAsOrdered()
    {
        IEnumerable<int> Ascending() { yield return 1; yield return 2; }
        IEnumerable<int> Descending() { yield return 2; yield return 1; }
        AssertDifferent(Ascending(), Descending());
    }

    // ----- unordered collections: order does not matter -----

    [Fact]
    public void Sets_DifferentInsertionOrder_SameChecksum()
    {
        var a = new HashSet<int> { 1, 2, 3 };
        var b = new HashSet<int> { 3, 1, 2 };
        Assert.Equal(Hex(a), Hex(b));
    }

    [Fact]
    public void Set_DiffersFromList_OfSameItems()
    {
        var set = new HashSet<int> { 1, 2, 3 };
        var list = new List<int> { 1, 2, 3 };
        AssertDifferent(set, list);
    }

    [Fact]
    public void Dictionaries_DifferentInsertionOrder_SameChecksum()
    {
        var a = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
        var b = new Dictionary<string, int> { ["c"] = 3, ["a"] = 1, ["b"] = 2 };
        Assert.Equal(Hex(a), Hex(b));
    }

    [Fact]
    public void Dictionaries_DifferentValues_DifferentChecksum()
    {
        var a = new Dictionary<string, int> { ["a"] = 1 };
        var b = new Dictionary<string, int> { ["a"] = 2 };
        AssertDifferent(a, b);
    }

    [Fact]
    public void IReadOnlyDictionary_MatchesDictionary()
    {
        var dict = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
        IReadOnlyDictionary<string, int> ro = dict;
        Assert.Equal(Hex(dict), Hex(ro));
    }

    // ----- custom hash function -----

    private sealed class FixedZeroHasher : IHasher
    {
        public int HashLengthInBytes => 8;
        public void Append(ReadOnlySpan<byte> data) { }
        public void GetHashAndReset(Span<byte> destination) => destination.Slice(0, HashLengthInBytes).Clear();
    }

    [Fact]
    public void CustomHasher_IsUsed()
    {
        var fp = new Fingerprinter(new FingerprintOptions { HasherFactory = () => new FixedZeroHasher() });
        // The degenerate hasher always returns zeros, so any two inputs collide.
        Assert.Equal(fp.ComputeHex("a"), fp.ComputeHex("totally different"));
        // ...and the output width matches the custom hasher.
        Assert.Equal(8, fp.Compute("a").Length);
    }

    [Fact]
    public void DefaultHasher_ProducesSixteenBytes()
    {
        Assert.Equal(16, Fp.Compute("a").Length);
    }

    // ----- reflection fallback -----

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
    public void Poco_SameMemberValues_SameChecksum()
    {
        var a = new Point { X = 1, Y = 2 };
        var b = new Point { X = 1, Y = 2 };
        Assert.Equal(Hex(a), Hex(b));
    }

    [Fact]
    public void Poco_DifferentMemberValues_DifferentChecksum()
    {
        var a = new Point { X = 1, Y = 2 };
        var b = new Point { X = 1, Y = 3 };
        AssertDifferent(a, b);
    }

    [Fact]
    public void Poco_DifferentTypes_SameShape_Differ()
    {
        var a = new Point { X = 1, Y = 2 };
        var b = new Point3D { X = 1, Y = 2 };
        AssertDifferent(a, b);
    }

    private sealed class WithSecret
    {
        public int Visible { get; set; }

        [SignumIgnore]
        public int Secret { get; set; }
    }

    [Fact]
    public void SignumIgnore_ExcludesMember()
    {
        var a = new WithSecret { Visible = 1, Secret = 100 };
        var b = new WithSecret { Visible = 1, Secret = 999 };
        Assert.Equal(Hex(a), Hex(b));
    }

    // ----- ISignumFingerprintable -----

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
        Assert.Equal(Hex(a), Hex(b));

        var c = new CustomFingerprint { Relevant = 8, Ignored = 1 };
        AssertDifferent(a, c);
    }

    // ----- cycle handling -----

    private sealed class Node
    {
        public int Value { get; set; }
        public Node? Next { get; set; }
    }

    [Fact]
    public void Cycle_DoesNotThrow_AndIsStable()
    {
        var a = new Node { Value = 1 };
        a.Next = a; // self-cycle

        string first = Hex(a);
        string second = Hex(a);
        Assert.Equal(first, second);
    }

    [Fact]
    public void SharedReference_IsNotTreatedAsCycle()
    {
        var shared = new Point { X = 1, Y = 1 };
        var graphA = new List<Point> { shared, shared };
        var separate = new Point { X = 1, Y = 1 };
        var graphB = new List<Point> { separate, separate };
        // Two equal Point instances used twice should match the same shared instance used twice.
        Assert.Equal(Hex(graphA), Hex(graphB));
    }

    // ----- nested structures -----

    [Fact]
    public void NestedStructures_AreStable()
    {
        var a = new Dictionary<string, List<int>>
        {
            ["x"] = new List<int> { 1, 2 },
            ["y"] = new List<int> { 3, 4 },
        };
        var b = new Dictionary<string, List<int>>
        {
            ["y"] = new List<int> { 3, 4 },
            ["x"] = new List<int> { 1, 2 },
        };
        Assert.Equal(Hex(a), Hex(b));
    }

    [Fact]
    public void NestedList_OrderStillMatters()
    {
        var a = new Dictionary<string, List<int>> { ["x"] = new List<int> { 1, 2 } };
        var b = new Dictionary<string, List<int>> { ["x"] = new List<int> { 2, 1 } };
        AssertDifferent(a, b);
    }

    // ----- common value types -----

    [Fact]
    public void Guid_SameValue_SameChecksum()
    {
        var g = Guid.NewGuid();
        Assert.Equal(Hex(g), Hex(g));
    }

    [Fact]
    public void DateTime_DiffersByValue()
    {
        var a = new DateTime(2020, 1, 1);
        var b = new DateTime(2020, 1, 2);
        AssertDifferent(a, b);
    }

    [Fact]
    public void ByteArray_DiffersFromIntArray()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var ints = new[] { 1, 2, 3 };
        AssertDifferent(bytes, ints);
    }
}
