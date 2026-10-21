namespace AppBackendBasic.Core.Attributes;

/// <summary>
/// Marks a property for automatic encryption at rest
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class EncryptedAttribute : Attribute
{
}
