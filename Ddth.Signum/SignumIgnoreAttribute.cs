namespace Ddth.Signum;

/// <summary>
/// Applied to a public property or field to exclude it from the reflection-based fingerprint.
/// Has no effect on types that implement <see cref="ISignumFingerprintable"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
public sealed class SignumIgnoreAttribute : Attribute
{
}
