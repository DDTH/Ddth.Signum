# Ddth.Signum release notes

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
