# Ddth.Signum — Architecture Design

## Goal
A lightweight .NET library that computes a stable checksum / fingerprint of *any* .NET object,
operating at the **interface** level (e.g. `IList`, `ISet`, `IDictionary`) rather than concrete
implementations.

## Confirmed requirements & decisions
1. Work against top-level interfaces, not concrete types.
2. Multi-target `net6.0;net8.0;net10.0` with `#if` guards for version-specific types
   (Int128/UInt128/Half/DateOnly/TimeOnly/Rune...).
3. **Integer family**: byte, sbyte, short, ushort, int, uint, long, ulong, nint, nuint,
   BigInteger, Int128/UInt128 → same value ⇒ same checksum (normalize to canonical BigInteger
   sign+magnitude bytes).
4. **Float family**: `float` and `double` share normalization (widen float → double, hash the
   canonical IEEE-754 double bits). `Half` and `decimal` are each their **own distinct family**.
5. Different families with the "same" value differ: `int 5` ≠ `float 5.0` ≠ `decimal 5m` ≠ `Half`.
6. Primitive vs boxed (`int` vs `Int32`, `(object)5`) are the same CLR type ⇒ already identical.
7. Ordered collections (array, IList, IEnumerable): order **matters**.
8. Unordered collections (ISet, IDictionary): order **does not** matter.
9. Optional caller-supplied hash function via `IHasher`; default = **XxHash128** from the
   `System.IO.Hashing` NuGet package.
10. POCO fallback: reflect over **public properties + fields only** (never non-public), treat as
    an **unordered set of (name, value) pairs**. Members marked `[SignumIgnore]` are excluded.
    Objects may implement `ISignumFingerprintable` to fully customize.
11. Cycles: reference-tracked; on revisit, substitute a **sentinel marker** and continue.

## Core algorithm — canonical recursive digest
`byte[] Digest(value)` = `hash( familyTag ++ canonicalPayload )`, computed with a fresh `IHasher`.

- **Scalars**: `Digest = hash(familyTag, normalizedBytes)`.
- **Ordered sequence**: `hash(TAG_LIST, count, Digest(e0), Digest(e1), ...)` — sequential append,
  so reordering changes the result.
- **Unordered set**: fold element digests with a **commutative combiner** (modular addition over a
  fixed-width accumulator), then `hash(TAG_SET, count, accumulator)`.
- **Dictionary**: per entry `pair = hash(TAG_KV, Digest(key), Digest(value))`; fold pairs with the
  commutative combiner; then `hash(TAG_DICT, count, accumulator)`.

Modular addition (not XOR) is used for the combiner so identical element digests don't cancel.
`count` is always mixed in to distinguish e.g. empty vs all-cancelling cases.

## Type resolution order (most specific first)
`null → string → char → bool → numeric primitives (per family) → Guid/DateTime/DateTimeOffset/
TimeSpan/DateOnly/TimeOnly → byte blob (byte[], ReadOnlyMemory<byte>) → IDictionary &
IReadOnlyDictionary<,> → ISet & IReadOnlySet<> → KeyValuePair<,> → IEnumerable & IEnumerable<> →
Nullable<T> (unwrap) → enum → ISignumFingerprintable → POCO reflection fallback`.

String is special-cased **before** IEnumerable (string is `IEnumerable<char>`).

**Enum (decided):** own family keyed by `(enum type FullName, underlying integer value)`.
- `Color.Red(0)` ≠ `int 0` (different family); `Color.Red(0)` ≠ `Size.Small(0)` (different type
  identity); robust to **renaming** members, sensitive to **renumbering**.
- Undefined values `(Color)999` and `[Flags]` combinations just hash by their underlying integer —
  no special handling.
- Use `Type.FullName` (namespace + name), **not** `AssemblyQualifiedName`, so the checksum is not
  coupled to assembly version.

## Public API (sketch)
```csharp
namespace Ddth.Signum;

public interface IHasher {
    void Append(ReadOnlySpan<byte> data);
    int HashLengthInBytes { get; }
    void GetHashAndReset(Span<byte> destination);
}

public interface ISignumFingerprintable {
    void WriteFingerprint(IFingerprintWriter writer);   // user-driven canonical contribution
}

/// Applied to a public property/field to exclude it from the reflection-based fingerprint.
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
public sealed class SignumIgnoreAttribute : Attribute { }

public sealed class FingerprintOptions {
    public Func<IHasher>? HasherFactory { get; init; }  // null ⇒ XxHash128
}

public sealed class Fingerprinter {
    public Fingerprinter(FingerprintOptions? options = null);
    public byte[] Compute(object? value);
    public string ComputeHex(object? value);
}
```

## Project structure
- Multi-target both `Ddth.Signum` and `Ddth.Signum.Tests`.
- Add `System.IO.Hashing` package reference to the library.
- CI already builds/tests against .NET 6/7/8/9/10.

## Family tag registry (stable byte constants — never reorder)
Null, Bool, Char, IntegerFamily, DoubleFamily(float+double), Half, Decimal, String, Bytes,
Guid, DateTime, DateTimeOffset, TimeSpan, DateOnly, TimeOnly, List, Set, Dict, KeyValue, Enum,
Object, Sentinel(cycle).

## Resolved (formerly open) items
- Bare `IEnumerable` → treated as an **ordered** list.
- Reflection fallback uses **public members only**; non-public members are never inspected.
- `[SignumIgnore]` excludes a public property/field from the reflection-based fingerprint.
