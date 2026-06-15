namespace Ddth.Signum;

/// <summary>
/// Stable family tag constants mixed into every digest so that values of different families
/// never collide. These byte values are part of the on-the-wire format: never reorder or reuse
/// them, as doing so changes all computed checksums.
/// </summary>
internal static class FamilyTag
{
    public const byte Null = 0;
    public const byte Bool = 1;
    public const byte Char = 2;
    public const byte Integer = 3;       // byte/sbyte/short/ushort/int/uint/long/ulong/nint/nuint/BigInteger/Int128/UInt128
    public const byte Double = 4;        // float + double (float widened to double)
    public const byte Half = 5;
    public const byte Decimal = 6;
    public const byte String = 7;
    public const byte Bytes = 8;
    public const byte Guid = 9;
    public const byte DateTime = 10;
    public const byte DateTimeOffset = 11;
    public const byte TimeSpan = 12;
    public const byte DateOnly = 13;
    public const byte TimeOnly = 14;
    public const byte List = 15;         // ordered
    public const byte Set = 16;          // unordered
    public const byte Dict = 17;         // unordered
    public const byte KeyValue = 18;
    public const byte Enum = 19;
    public const byte Object = 20;       // reflection fallback
    public const byte Custom = 21;       // ISignumFingerprintable
    public const byte Sentinel = 22;     // cycle marker
}
