# Ddth.Signum — Business Cases to Test

This document lists the behavioural (business) cases the library is expected to satisfy. They map
to the requirements behind the fingerprinting algorithm and serve as a checklist for the unit
tests under this directory.

## Determinism & basics

- Computing the checksum of the same value twice yields the same result.
- A `null` input produces a stable checksum and does not throw.
- `null` and an empty string produce different checksums.
- The default hasher produces a 16-byte (`XxHash128`) checksum.
- `ChecksumHex` returns the lowercase hexadecimal form of `Checksum`.

## Numeric families

- Integer types with the same value share a checksum (`byte`, `sbyte`, `short`, `ushort`, `int`,
  `uint`, `long`, `ulong`, `nint`, `nuint`, `BigInteger`, and `Int128`/`UInt128` on .NET 7+).
- Different integer values produce different checksums (including `0` vs `-1`).
- Large unsigned values round-trip correctly (e.g. `ulong.MaxValue` equals its `BigInteger`).
- `float` and `double` of the same value share a checksum.
- `Half` and `decimal` are each treated as their own distinct family.
- Different families with the "same" value differ (`int 5` ≠ `float 5` ≠ `decimal 5` ≠ `Half 5`).
- A boxed primitive equals the unboxed primitive (`(object)5` == `5`).
- `char` and the integer of the same code point produce different checksums.

## Floating-point & decimal edge cases

- Negative zero equals positive zero for `double` (and `Half`).
- `double.NaN` is deterministic and matches `float.NaN`.
- `Half.NaN` is deterministic; `Half` negative zero equals positive zero.
- Decimal trailing zeros do not matter (`1.0m` == `1.00m` == `1m`).
- Decimal negative zero equals zero.

## Booleans, dates & common value types

- `true`/`false` are deterministic, distinct from each other, and distinct from integers.
- `Guid`, `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, and `TimeOnly` differ by value.
- A `byte[]` blob differs from an `int[]` with the same numeric content.

## Ordered collections (order matters)

- Two lists with the same items in a different order produce different checksums.
- Two arrays with the same items in the same order produce the same checksum.
- An array equals a list of the same items (interface-level equivalence).
- A bare `IEnumerable` is treated as ordered (different yield order → different checksum).

## Unordered collections (order does not matter)

- Sets with the same items in different insertion order produce the same checksum.
- A set differs from a list of the same items.
- Dictionaries with the same entries in different insertion order produce the same checksum.
- Dictionaries with different values produce different checksums.
- An `IReadOnlyDictionary` matches an equivalent `Dictionary`.
- A generic-only dictionary (no non-generic `IDictionary`) matches an equivalent `Dictionary`.
- A standalone `KeyValuePair` is deterministic and distinct from a single-entry dictionary.

## Object reflection fallback

- Two objects of the same type with the same public member values share a checksum.
- Two objects of the same type with different member values produce different checksums.
- Two different types with the same shape/values produce different checksums.
- Public fields are included in the fingerprint.
- Members marked `[SignumIgnore]` are excluded (properties and fields).
- A property whose getter throws is skipped rather than failing the computation.

## Custom fingerprinting

- A type implementing `ISignumFingerprintable` contributes only the state it writes;
  unwritten members do not affect the checksum, written members do.

## Cycles & shared references

- A self-referencing/cyclic object graph does not throw and is stable.
- A shared (non-cyclic) reference used twice is not mistaken for a cycle and matches an
  equivalent graph using two equal instances.

## Nested structures

- Nested structures combine the rules (e.g. dictionary of lists): outer order is ignored while
  inner list order still matters.

## Pluggable hashers

- Each built-in hasher produces its expected digest width: `XxHash128` (16), `XxHash3` (8),
  `Crc32` (4).
- Each built-in hasher is deterministic and distinguishes different values.
- Each wrapper matches its underlying `System.IO.Hashing` algorithm output.
- `GetHashAndReset` clears prior state, allowing the hasher instance to be reused.
- A custom `IHasher` supplied via `FingerprintOptions.HasherFactory` is actually used, and its
  digest width is reflected in the output.

## `Signum` static helper

- `Checksum`/`ChecksumHex` are deterministic and distinguish different values.
- A `null` hasher factory falls back to the default hasher.
- A supplied hasher factory is used (and matches an equivalent `Fingerprinter`).
- The same factory delegate reuses a cached `Fingerprinter` (no per-call allocation).
- Each hasher's shared static `Factory` creates a new instance of the expected type.
- A hasher's `Factory` yields the same result as an equivalent inline lambda.
- `XxHash128Hasher.Factory` produces the same result as the default path.
