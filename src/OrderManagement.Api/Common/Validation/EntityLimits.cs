namespace OrderManagement.Api.Common.Validation;

/// <summary>Text limits that apply to components, boards and orders alike (see "Domain rules" in the handbook).</summary>
public static class EntityLimits
{
    public const int NameMaxLength = 256;
    public const int DescriptionMaxLength = 2000;
}
