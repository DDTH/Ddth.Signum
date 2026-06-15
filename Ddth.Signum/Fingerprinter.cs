using System.Buffers.Binary;
using System.Collections;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace Ddth.Signum;

/// <summary>
/// Computes a stable checksum / fingerprint of arbitrary .NET objects.
/// </summary>
/// <remarks>
/// <para>The algorithm reduces every value to a canonical digest <c>hash(familyTag ++ payload)</c>:</para>
/// <list type="bullet">
/// <item>Values in the same numeric family with the same value share a checksum
/// (e.g. <c>(int)5</c> and <c>(long)5</c>); different families differ (e.g. <c>(int)5</c> vs <c>(float)5</c>).</item>
/// <item>Ordered collections (arrays, <see cref="IList"/>, <see cref="IEnumerable"/>) are order-sensitive.</item>
/// <item>Unordered collections (<see cref="ISet{T}"/>, <see cref="IDictionary"/>) are order-insensitive.</item>
/// <item>Unknown objects are reflected over their public properties and fields (an unordered set of
/// name/value pairs); members marked <see cref="SignumIgnoreAttribute"/> are skipped, and types implementing
/// <see cref="ISignumFingerprintable"/> control their own contribution.</item>
/// <item>Reference cycles are detected and replaced with a sentinel marker.</item>
/// </list>
/// <para>Instances are immutable and safe to reuse across threads.</para>
/// </remarks>
public sealed class Fingerprinter
{
    private readonly Func<IHasher> _hasherFactory;

    /// <summary>Creates a fingerprinter using the supplied <paramref name="options"/> (or defaults when <c>null</c>).</summary>
    public Fingerprinter(FingerprintOptions? options = null)
    {
        _hasherFactory = options?.HasherFactory ?? DefaultFactory;
    }

    private static IHasher DefaultFactory() => new XxHash128Hasher();

    /// <summary>Computes the fingerprint of <paramref name="value"/> as a byte array.</summary>
    public byte[] Compute(object? value) => new Worker(_hasherFactory).Digest(value);

    /// <summary>Computes the fingerprint of <paramref name="value"/> as a lowercase hexadecimal string.</summary>
    public string ComputeHex(object? value)
    {
        var bytes = Compute(value);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Per-computation state. Holds the active recursion path so that genuine cycles (back-edges)
    /// are detected while shared, non-cyclic references are still hashed normally.
    /// </summary>
    private sealed class Worker
    {
        private readonly Func<IHasher> _factory;
        private readonly HashSet<object> _path = new(ReferenceEqualityComparer.Instance);

        public Worker(Func<IHasher> factory) => _factory = factory;

        public byte[] Digest(object? value)
        {
            var h = _factory();
            Write(h, value);
            return GetHash(h);
        }

        private static byte[] GetHash(IHasher h)
        {
            var buffer = new byte[h.HashLengthInBytes];
            h.GetHashAndReset(buffer);
            return buffer;
        }

        private void Write(IHasher h, object? value)
        {
            if (value is null)
            {
                Tag(h, FamilyTag.Null);
                return;
            }

            switch (value)
            {
                case string s: WriteString(h, FamilyTag.String, s); return;
                case bool b: Tag(h, FamilyTag.Bool); WriteByte(h, (byte)(b ? 1 : 0)); return;
                case char c: Tag(h, FamilyTag.Char); WriteUInt16(h, c); return;

                case byte v: WriteInteger(h, v); return;
                case sbyte v: WriteInteger(h, v); return;
                case short v: WriteInteger(h, v); return;
                case ushort v: WriteInteger(h, v); return;
                case int v: WriteInteger(h, v); return;
                case uint v: WriteInteger(h, v); return;
                case long v: WriteInteger(h, v); return;
                case ulong v: WriteInteger(h, v); return;
                case IntPtr v: WriteInteger(h, (BigInteger)(long)v); return;
                case UIntPtr v: WriteInteger(h, (BigInteger)(ulong)v); return;
                case BigInteger v: WriteInteger(h, v); return;
#if NET7_0_OR_GREATER
                case Int128 v: WriteInteger(h, (BigInteger)v); return;
                case UInt128 v: WriteInteger(h, (BigInteger)v); return;
#endif

                case float v: WriteDouble(h, v); return;
                case double v: WriteDouble(h, v); return;
                case Half v: WriteHalf(h, v); return;
                case decimal v: WriteDecimal(h, v); return;

                case Guid v: Tag(h, FamilyTag.Guid); WriteGuid(h, v); return;
                case DateTime v: Tag(h, FamilyTag.DateTime); WriteInt64(h, v.Ticks); WriteByte(h, (byte)v.Kind); return;
                case DateTimeOffset v: Tag(h, FamilyTag.DateTimeOffset); WriteInt64(h, v.UtcTicks); WriteInt64(h, v.Offset.Ticks); return;
                case TimeSpan v: Tag(h, FamilyTag.TimeSpan); WriteInt64(h, v.Ticks); return;
                case DateOnly v: Tag(h, FamilyTag.DateOnly); WriteInt32(h, v.DayNumber); return;
                case TimeOnly v: Tag(h, FamilyTag.TimeOnly); WriteInt64(h, v.Ticks); return;

                case Enum e: WriteEnum(h, e); return;
            }

            // Composite / reference handling, with cycle detection for reference types only.
            bool isReference = !value.GetType().IsValueType;
            if (isReference && !_path.Add(value))
            {
                Tag(h, FamilyTag.Sentinel);
                return;
            }

            try
            {
                WriteComposite(h, value);
            }
            finally
            {
                if (isReference)
                {
                    _path.Remove(value);
                }
            }
        }

        private void WriteComposite(IHasher h, object value)
        {
            Type type = value.GetType();

            if (value is ISignumFingerprintable custom)
            {
                WriteCustom(h, custom);
                return;
            }

            if (value is byte[] bytes)
            {
                Tag(h, FamilyTag.Bytes);
                WriteInt32(h, bytes.Length);
                h.Append(bytes);
                return;
            }

            if (TryGetKeyValuePair(value, type, out object? kvKey, out object? kvValue))
            {
                Tag(h, FamilyTag.KeyValue);
                h.Append(Digest(kvKey));
                h.Append(Digest(kvValue));
                return;
            }

            if (TryGetDictionaryEntries(value, type, out IEnumerable<KeyValuePair<object?, object?>> entries))
            {
                WriteDictionary(h, entries);
                return;
            }

            if (ImplementsSet(type) && value is IEnumerable setItems)
            {
                WriteSet(h, setItems);
                return;
            }

            if (value is IEnumerable seq)
            {
                WriteList(h, seq);
                return;
            }

            WriteObject(h, value, type);
        }

        private void WriteCustom(IHasher h, ISignumFingerprintable custom)
        {
            Tag(h, FamilyTag.Custom);
            WriteRawString(h, TypeId(custom.GetType()));
            var writer = new Writer(this, h);
            custom.WriteFingerprint(writer);
            WriteInt32(h, writer.Count);
        }

        private void WriteList(IHasher h, IEnumerable seq)
        {
            Tag(h, FamilyTag.List);
            int count = 0;
            foreach (var item in seq)
            {
                h.Append(Digest(item));
                count++;
            }
            WriteInt32(h, count);
        }

        private void WriteSet(IHasher h, IEnumerable items)
        {
            Tag(h, FamilyTag.Set);
            byte[]? accumulator = null;
            int count = 0;
            foreach (var item in items)
            {
                byte[] digest = Digest(item);
                accumulator ??= new byte[digest.Length];
                AddInto(accumulator, digest);
                count++;
            }
            if (accumulator is not null)
            {
                h.Append(accumulator);
            }
            WriteInt32(h, count);
        }

        private void WriteDictionary(IHasher h, IEnumerable<KeyValuePair<object?, object?>> entries)
        {
            Tag(h, FamilyTag.Dict);
            byte[]? accumulator = null;
            int count = 0;
            foreach (var entry in entries)
            {
                byte[] pair = PairDigest(FamilyTag.KeyValue, Digest(entry.Key), Digest(entry.Value));
                accumulator ??= new byte[pair.Length];
                AddInto(accumulator, pair);
                count++;
            }
            if (accumulator is not null)
            {
                h.Append(accumulator);
            }
            WriteInt32(h, count);
        }

        private void WriteObject(IHasher h, object value, Type type)
        {
            Tag(h, FamilyTag.Object);
            WriteRawString(h, TypeId(type));
            byte[]? accumulator = null;
            int count = 0;
            foreach (var (name, memberValue) in GetMembers(value, type))
            {
                byte[] pair = PairDigest(FamilyTag.KeyValue, Digest(name), Digest(memberValue));
                accumulator ??= new byte[pair.Length];
                AddInto(accumulator, pair);
                count++;
            }
            if (accumulator is not null)
            {
                h.Append(accumulator);
            }
            WriteInt32(h, count);
        }

        private byte[] PairDigest(byte tag, byte[] left, byte[] right)
        {
            var h = _factory();
            Tag(h, tag);
            h.Append(left);
            h.Append(right);
            return GetHash(h);
        }

        private static void WriteEnum(IHasher h, Enum e)
        {
            Tag(h, FamilyTag.Enum);
            Type type = e.GetType();
            WriteRawString(h, TypeId(type));
            Type underlying = Enum.GetUnderlyingType(type);
            BigInteger numeric = underlying == typeof(ulong)
                ? new BigInteger(Convert.ToUInt64(e, CultureInfo.InvariantCulture))
                : new BigInteger(Convert.ToInt64(e, CultureInfo.InvariantCulture));
            WriteInteger(h, numeric);
        }

        private sealed class Writer : IFingerprintWriter
        {
            private readonly Worker _worker;
            private readonly IHasher _hasher;

            public Writer(Worker worker, IHasher hasher)
            {
                _worker = worker;
                _hasher = hasher;
            }

            public int Count { get; private set; }

            public IFingerprintWriter Write(object? value)
            {
                _hasher.Append(_worker.Digest(value));
                Count++;
                return this;
            }
        }

        // ----- value normalization helpers -----

        private static void WriteInteger(IHasher h, BigInteger value)
        {
            Tag(h, FamilyTag.Integer);
            byte[] bytes = value.ToByteArray(); // canonical minimal-length little-endian two's complement
            WriteInt32(h, bytes.Length);
            h.Append(bytes);
        }

        private static void WriteDouble(IHasher h, double value)
        {
            Tag(h, FamilyTag.Double);
            if (value == 0d)
            {
                value = 0d; // collapse -0.0 to +0.0
            }
            else if (double.IsNaN(value))
            {
                value = double.NaN; // canonical NaN bit pattern
            }
            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteDoubleLittleEndian(buffer, value);
            h.Append(buffer);
        }

        private static void WriteHalf(IHasher h, Half value)
        {
            Tag(h, FamilyTag.Half);
            if (value == (Half)0)
            {
                value = (Half)0;
            }
            else if (Half.IsNaN(value))
            {
                value = Half.NaN;
            }
            WriteUInt16(h, (ushort)BitConverter.HalfToInt16Bits(value));
        }

        private static void WriteDecimal(IHasher h, decimal value)
        {
            Tag(h, FamilyTag.Decimal);
            // Normalize so trailing zeros do not matter: 1.0m == 1.00m == 1m.
            // decimal.ToString never produces exponent notation or a "-0" string, and the trim
            // only runs when a '.' is present, so the result is always a valid canonical number.
            string text = value.ToString(CultureInfo.InvariantCulture);
            if (text.IndexOf('.') >= 0)
            {
                text = text.TrimEnd('0').TrimEnd('.');
            }
            WriteRawString(h, text);
        }

        private static void WriteGuid(IHasher h, Guid value)
        {
            Span<byte> buffer = stackalloc byte[16];
            value.TryWriteBytes(buffer);
            h.Append(buffer);
        }

        // ----- collection / object inspection helpers -----

        private static bool TryGetKeyValuePair(object value, Type type, out object? key, out object? val)
        {
            key = null;
            val = null;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                key = type.GetProperty("Key")!.GetValue(value);
                val = type.GetProperty("Value")!.GetValue(value);
                return true;
            }
            return false;
        }

        private static bool TryGetDictionaryEntries(object value, Type type, out IEnumerable<KeyValuePair<object?, object?>> entries)
        {
            if (value is IDictionary nonGeneric)
            {
                entries = EnumerateNonGeneric(nonGeneric);
                return true;
            }

            foreach (Type iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType)
                {
                    continue;
                }
                Type definition = iface.GetGenericTypeDefinition();
                if (definition == typeof(IReadOnlyDictionary<,>) || definition == typeof(IDictionary<,>))
                {
                    entries = EnumerateGeneric((IEnumerable)value);
                    return true;
                }
            }

            entries = Array.Empty<KeyValuePair<object?, object?>>();
            return false;
        }

        private static IEnumerable<KeyValuePair<object?, object?>> EnumerateNonGeneric(IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                yield return new KeyValuePair<object?, object?>(entry.Key, entry.Value);
            }
        }

        private static IEnumerable<KeyValuePair<object?, object?>> EnumerateGeneric(IEnumerable items)
        {
            foreach (object? item in items)
            {
                if (item is null)
                {
                    continue;
                }
                Type itemType = item.GetType();
                object? key = itemType.GetProperty("Key")!.GetValue(item);
                object? val = itemType.GetProperty("Value")!.GetValue(item);
                yield return new KeyValuePair<object?, object?>(key, val);
            }
        }

        private static bool ImplementsSet(Type type)
        {
            foreach (Type iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType)
                {
                    continue;
                }
                Type definition = iface.GetGenericTypeDefinition();
                if (definition == typeof(ISet<>) || definition == typeof(IReadOnlySet<>))
                {
                    return true;
                }
            }
            return false;
        }

        private static IEnumerable<(string Name, object? Value)> GetMembers(object value, Type type)
        {
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetIndexParameters().Length > 0 || !property.CanRead)
                {
                    continue;
                }
                if (property.IsDefined(typeof(SignumIgnoreAttribute), true))
                {
                    continue;
                }
                object? memberValue;
                try
                {
                    memberValue = property.GetValue(value);
                }
                catch
                {
                    continue;
                }
                yield return (property.Name, memberValue);
            }

            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.IsDefined(typeof(SignumIgnoreAttribute), true))
                {
                    continue;
                }
                yield return (field.Name, field.GetValue(value));
            }
        }

        private static string TypeId(Type type) => type.FullName ?? type.Name;

        // ----- low-level byte writers -----

        private static void Tag(IHasher h, byte tag)
        {
            Span<byte> buffer = stackalloc byte[1];
            buffer[0] = tag;
            h.Append(buffer);
        }

        private static void WriteByte(IHasher h, byte value)
        {
            Span<byte> buffer = stackalloc byte[1];
            buffer[0] = value;
            h.Append(buffer);
        }

        private static void WriteUInt16(IHasher h, ushort value)
        {
            Span<byte> buffer = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
            h.Append(buffer);
        }

        private static void WriteInt32(IHasher h, int value)
        {
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
            h.Append(buffer);
        }

        private static void WriteInt64(IHasher h, long value)
        {
            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
            h.Append(buffer);
        }

        private static void WriteString(IHasher h, byte tag, string value)
        {
            Tag(h, tag);
            WriteRawString(h, value);
        }

        private static void WriteRawString(IHasher h, string value)
        {
            int byteCount = Encoding.UTF8.GetByteCount(value);
            WriteInt32(h, byteCount);
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            h.Append(bytes);
        }

        /// <summary>Adds <paramref name="value"/> into <paramref name="accumulator"/> as a little-endian integer, modulo 2^(8*len).</summary>
        private static void AddInto(byte[] accumulator, byte[] value)
        {
            int carry = 0;
            for (int i = 0; i < accumulator.Length; i++)
            {
                int sum = accumulator[i] + (i < value.Length ? value[i] : 0) + carry;
                accumulator[i] = (byte)sum;
                carry = sum >> 8;
            }
        }
    }
}
