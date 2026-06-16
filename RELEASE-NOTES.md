# Ddth.Signum release notes

## 2026-06-16 - v0.0.3

### Fixed/Improvements

- Impr: Expose shared static Factory on XxHash128Hasher, XxHash3Hasher and Crc32Hasher for cached reuse via Signum.

## 2026-06-16 - v0.0.2

### Added/Refactoring/Deprecation

- Feat: Add XxHash3Hasher and Crc32Hasher pre-implemented hashers.

### Fixed/Improvements

- Impr: Signum.Checksum/ChecksumHex accept an optional cached hasherFactory.

## 2026-06-15 - v0.0.1

### Added/Refactoring/Deprecation

- Feat: compute checksum/fingerprint of any .NET object at the interface level
- Feat: add reflection fallback for arbitrary objects (public properties + fields)
- Feat: unify integer family so (int)5, (long)5, (byte)5 share a checksum
- Feat: share checksum between float and double of the same value
- Feat: treat Half and decimal as distinct numeric families
- Feat: distinguish families with the same value, e.g. (int)5 != (float)5 != (decimal)5
- Feat: make ordered collections (arrays, IList, IEnumerable) order-sensitive
- Feat: make unordered collections (ISet, IDictionary) order-insensitive
- Feat: support pluggable hash functions via IHasher
- Feat: default to fast non-cryptographic XxHash128 (System.IO.Hashing)
- Feat: add ISignumFingerprintable for custom per-type fingerprint contribution
- Feat: add [SignumIgnore] to exclude individual members
- Feat: detect and gracefully handle reference cycles
