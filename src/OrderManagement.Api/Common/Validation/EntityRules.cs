namespace OrderManagement.Api.Common.Validation;

/// <summary>Validation rules shared by components, boards and orders (see "Domain rules" in the handbook).</summary>
public static class EntityRules
{
    public const string NameField = "name";
    public const string DescriptionField = "description";

    /// <summary>Name is required, must not be blank and is limited to <paramref name="maxLength"/> characters after trimming.</summary>
    public static void ValidateName(string? name, int maxLength, ValidationErrors errors)
    {
        string trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            errors.Add(NameField, "Name is required.");
        }
        else if (trimmed.Length > maxLength)
        {
            errors.Add(NameField, $"Name must be at most {maxLength} characters.");
        }
    }

    /// <summary>Description is optional and limited to <paramref name="maxLength"/> characters after trimming.</summary>
    public static void ValidateDescription(string? description, int maxLength, ValidationErrors errors)
    {
        if ((description?.Trim().Length ?? 0) > maxLength)
        {
            errors.Add(DescriptionField, $"Description must be at most {maxLength} characters.");
        }
    }

    /// <summary>
    /// Reports names that occur more than once in the same request (case-insensitive), on every occurrence
    /// after the first.
    /// </summary>
    public static void ValidateUniqueNamesInRequest(IReadOnlyList<string?> names, ValidationErrors errors)
    {
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < names.Count; index++)
        {
            string trimmed = names[index]?.Trim() ?? string.Empty;
            if (trimmed.Length > 0 && !seen.Add(trimmed))
            {
                errors.Add($"[{index}].{NameField}", $"The name '{trimmed}' appears more than once in this request.");
            }
        }
    }
}
