[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![Actions Status](https://github.com/DDTH/Ddth.Signum/workflows/ci/badge.svg)](https://github.com/DDTH/Ddth.Signum/actions)
[![codecov](https://codecov.io/gh/DDTH/Ddth.Signum/graph/badge.svg)](https://codecov.io/gh/DDTH/Ddth.Signum)
[![Release](https://img.shields.io/github/release/DDTH/Ddth.Signum.svg?style=flat-square)](RELEASE-NOTES.md)

Lightweight .NET library for calculating checksums and object fingerprints of *any* .NET object -
from primitives and collections to arbitrary objects.

## Features

- **Works with any object.** Operates at the interface level (`IList`, `ISet`, `IDictionary`, ...)
  rather than concrete implementations, and falls back to reflection for arbitrary objects.
- **Type-aware, value-stable checksums.**
  - Integer types with the same value share a checksum (e.g. `(int)5`, `(long)5`, `(byte)5`).
  - `float` and `double` of the same value share a checksum; `Half` and `decimal` are distinct.
  - Different families with the "same" value differ (e.g. `(int)5` ≠ `(float)5` ≠ `(decimal)5`).
- **Structure-aware composition.**
  - Ordered collections (arrays, `IList`, `IEnumerable`) are order-sensitive.
  - Unordered collections (`ISet`, `IDictionary`) are order-insensitive.
- **Customizable.**
  - Plug in your own hash function via `IHasher`; the default is a fast, non-cryptographic
    [`XxHash128`](https://www.nuget.org/packages/System.IO.Hashing). `XxHash3` and `Crc32`
    hashers are also provided out of the box.
  - Implement `ISignumFingerprintable` to control how a type contributes to its fingerprint.
  - Exclude individual members with `[SignumIgnore]`.
- **Safe by default.** Reference cycles are detected and handled gracefully.

## Usage

```sh
$ dotnet add package Ddth.Signum
```

Use the static `Signum` helper for the common case:

```csharp
using Ddth.Signum;

byte[] checksum = Signum.Checksum(myObject);
string hex = Signum.ChecksumHex(myObject);
```

`Signum.Checksum` accepts anything:

```csharp
Signum.ChecksumHex(42);                                  // primitives
Signum.ChecksumHex(new[] { 1, 2, 3 });                   // ordered collections
Signum.ChecksumHex(new HashSet<int> { 1, 2, 3 });        // unordered collections
Signum.ChecksumHex(new { Name = "Alice", Age = 30 });    // arbitrary objects
```

Same value, same checksum - regardless of the concrete integer type or collection order:

```csharp
Signum.ChecksumHex(5) == Signum.ChecksumHex(5L);                 // true (integer family)

var a = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
var b = new Dictionary<string, int> { ["b"] = 2, ["a"] = 1 };
Signum.ChecksumHex(a) == Signum.ChecksumHex(b);                  // true (order-insensitive)

var list1 = new List<int> { 1, 2, 3 };
var list2 = new List<int> { 3, 2, 1 };
Signum.ChecksumHex(list1) == Signum.ChecksumHex(list2);          // false (order matters)
```

### Using a custom hash function

The library ships three hashers (all from `System.IO.Hashing`): `XxHash128Hasher` (default,
16-byte), `XxHash3Hasher` (8-byte) and `Crc32Hasher` (4-byte). Select one via
`FingerprintOptions.HasherFactory`, or via the optional parameter on the `Signum` helper:

```csharp
// Use a built-in hasher through the static helper:
string hex = Signum.ChecksumHex(myObject, () => new XxHash3Hasher());
```

Provide your own `IHasher` to use any other algorithm:

```csharp
using System.IO.Hashing;
using Ddth.Signum;

public sealed class Crc64Hasher : IHasher
{
    private readonly Crc64 _inner = new();
    public int HashLengthInBytes => _inner.HashLengthInBytes;
    public void Append(ReadOnlySpan<byte> data) => _inner.Append(data);
    public void GetHashAndReset(Span<byte> destination) => _inner.GetHashAndReset(destination);
}

var fingerprinter = new Fingerprinter(new FingerprintOptions
{
    HasherFactory = () => new Crc64Hasher()
});

byte[] checksum = fingerprinter.Compute(myObject);
```

### Controlling how objects are fingerprinted

By default, an unknown object is fingerprinted over its public properties and fields. Exclude a
member with `[SignumIgnore]`:

```csharp
public sealed class User
{
    public string Name { get; set; }

    [SignumIgnore]
    public DateTime LastAccessed { get; set; } // ignored in the fingerprint
}
```

For full control, implement `ISignumFingerprintable`:

```csharp
public sealed class Money : ISignumFingerprintable
{
    public decimal Amount { get; set; }
    public string Currency { get; set; }

    public void WriteFingerprint(IFingerprintWriter writer)
    {
        writer.Write(Amount).Write(Currency);
    }
}
```

## License

This package is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## Contributing & Support

Feel free to create [pull requests](https://github.com/DDTH/Ddth.Signum/compare/contrib_wait_to_merge...) or [issues](https://github.com/DDTH/Ddth.Signum/issues) to report bugs or suggest new features.
