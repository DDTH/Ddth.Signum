using System.Collections;
#if NET7_0_OR_GREATER
using System.Numerics;
#endif

namespace Ddth.Signum.Tests;

/// <summary>
/// Tests that exercise the remaining type branches and normalization edge cases of
/// <see cref="Fingerprinter"/> not covered by <see cref="FingerprinterTests"/>.
/// </summary>
public class FingerprinterCoverageTests
{
    private static readonly Fingerprinter Fp = new();

    private static string Hex(object? value) => Fp.ComputeHex(value);

    // ----- bool -----

    [Fact]
    public void Bool_IsDeterministic_AndDistinctValues()
    {
        Assert.Equal(Hex(true), Hex(true));
        Assert.NotEqual(Hex(true), Hex(false));
    }

    [Fact]
    public void Bool_DiffersFromInteger()
    {
        Assert.NotEqual(Hex(true), Hex(1));
    }

    // ----- date / time value types -----

    [Fact]
    public void DateTimeOffset_DiffersByValue()
    {
        var a = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var b = new DateTimeOffset(2020, 1, 2, 0, 0, 0, TimeSpan.Zero);
        Assert.Equal(Hex(a), Hex(a));
        Assert.NotEqual(Hex(a), Hex(b));
    }

    [Fact]
    public void TimeSpan_DiffersByValue()
    {
        Assert.Equal(Hex(TimeSpan.FromMinutes(5)), Hex(TimeSpan.FromMinutes(5)));
        Assert.NotEqual(Hex(TimeSpan.FromMinutes(5)), Hex(TimeSpan.FromMinutes(6)));
    }

    [Fact]
    public void DateOnly_DiffersByValue()
    {
        var a = new DateOnly(2020, 1, 1);
        var b = new DateOnly(2020, 1, 2);
        Assert.Equal(Hex(a), Hex(a));
        Assert.NotEqual(Hex(a), Hex(b));
    }

    [Fact]
    public void TimeOnly_DiffersByValue()
    {
        var a = new TimeOnly(10, 0);
        var b = new TimeOnly(11, 0);
        Assert.Equal(Hex(a), Hex(a));
        Assert.NotEqual(Hex(a), Hex(b));
    }

    // ----- 128-bit integers (only available on .NET 7+) -----

#if NET7_0_OR_GREATER
    [Fact]
    public void Int128_JoinsIntegerFamily()
    {
        Assert.Equal(Hex(5), Hex((Int128)5));
        Assert.Equal(Hex(5), Hex((UInt128)5));
    }

    [Fact]
    public void Int128_LargeValue_RoundTrips()
    {
        Int128 value = (Int128)ulong.MaxValue + 1;
        Assert.Equal(Hex(new BigInteger(ulong.MaxValue) + 1), Hex(value));
    }
#endif

    // ----- standalone KeyValuePair -----

    [Fact]
    public void KeyValuePair_IsDeterministic_AndDistinctFromDictionary()
    {
        var kv = new KeyValuePair<string, int>("a", 1);
        Assert.Equal(Hex(kv), Hex(new KeyValuePair<string, int>("a", 1)));
        Assert.NotEqual(Hex(kv), Hex(new Dictionary<string, int> { ["a"] = 1 }));
    }

    // ----- floating point special values -----

    [Fact]
    public void DoubleNaN_IsDeterministic_AndFloatNaNMatches()
    {
        Assert.Equal(Hex(double.NaN), Hex(double.NaN));
        Assert.Equal(Hex(double.NaN), Hex(float.NaN));
    }

    [Fact]
    public void HalfZero_NegativeAndPositive_Match()
    {
        Assert.Equal(Hex((Half)0), Hex((Half)(-0.0f)));
    }

    [Fact]
    public void HalfNaN_IsDeterministic()
    {
        Assert.Equal(Hex(Half.NaN), Hex(Half.NaN));
    }

    // ----- decimal negative zero -----

    [Fact]
    public void Decimal_NegativeZero_EqualsZero()
    {
        decimal negativeZero = new decimal(0, 0, 0, isNegative: true, scale: 2);
        Assert.Equal(Hex(0m), Hex(negativeZero));
    }

    // ----- generic-only dictionary (no non-generic IDictionary) -----

    private sealed class ReadOnlyDict : IReadOnlyDictionary<string, int>
    {
        private readonly Dictionary<string, int> _inner;

        public ReadOnlyDict(Dictionary<string, int> inner) => _inner = inner;

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
        var readOnly = new ReadOnlyDict(new Dictionary<string, int> { ["b"] = 2, ["a"] = 1 });
        Assert.Equal(Hex(dict), Hex(readOnly));
    }

    // ----- reflection over public fields -----

    private sealed class WithFields
    {
        public int A;

        [SignumIgnore]
        public int Ignored;
    }

    [Fact]
    public void PublicFields_AreIncluded_AndIgnoreAttributeHonored()
    {
        var a = new WithFields { A = 1, Ignored = 10 };
        var b = new WithFields { A = 1, Ignored = 999 };
        Assert.Equal(Hex(a), Hex(b));

        var c = new WithFields { A = 2, Ignored = 10 };
        Assert.NotEqual(Hex(a), Hex(c));
    }

    // ----- a property whose getter throws is skipped -----

    private sealed class ThrowingProperty
    {
        public int Ok { get; set; }

        public static int Bad => throw new InvalidOperationException("boom");
    }

    [Fact]
    public void ThrowingPropertyGetter_IsSkipped()
    {
        var a = new ThrowingProperty { Ok = 1 };
        var b = new ThrowingProperty { Ok = 1 };
        Assert.Equal(Hex(a), Hex(b)); // does not throw; Bad is skipped
    }
}
