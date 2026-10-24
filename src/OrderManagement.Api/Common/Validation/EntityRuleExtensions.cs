using FluentValidation;

namespace OrderManagement.Api.Common.Validation;

/// <summary>
/// FluentValidation rules shared by components, boards and orders (see "Domain rules" in the handbook).
/// Used as <c>RuleFor(r => r.Name).EntityName(256)</c>.
/// </summary>
public static class EntityRuleExtensions
{
    /// <summary>Name is required, must not be blank and is limited to <paramref name="maxLength"/> characters after trimming.</summary>
    public static IRuleBuilderOptions<T, string?> EntityName<T>(this IRuleBuilder<T, string?> ruleBuilder, int maxLength) =>
        ruleBuilder
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Name is required.")
            .Must(name => (name?.Trim().Length ?? 0) <= maxLength)
            .WithMessage($"Name must be at most {maxLength} characters.");

    /// <summary>Description is optional and limited to <paramref name="maxLength"/> characters after trimming.</summary>
    public static IRuleBuilderOptions<T, string?> EntityDescription<T>(this IRuleBuilder<T, string?> ruleBuilder, int maxLength) =>
        ruleBuilder
            .Must(description => (description?.Trim().Length ?? 0) <= maxLength)
            .WithMessage($"Description must be at most {maxLength} characters.");

    /// <summary>
    /// Reports names that occur more than once in the same list (case-insensitive, trimmed), on every occurrence
    /// after the first, under <c>{listName}[index].Name</c>.
    /// </summary>
    public static IRuleBuilderOptionsConditions<T, List<TItem>> UniqueNames<T, TItem>(
        this IRuleBuilder<T, List<TItem>> ruleBuilder, string listName, Func<TItem, string?> nameOf) =>
        ruleBuilder.Custom((items, context) =>
        {
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < items.Count; index++)
            {
                // Missing items (null in the JSON array) are reported by the item rules.
                if (items[index] is null)
                {
                    continue;
                }

                string name = nameOf(items[index])?.Trim() ?? string.Empty;
                if (name.Length > 0 && !seen.Add(name))
                {
                    context.AddFailure($"{listName}[{index}].Name", $"The name '{name}' appears more than once in this request.");
                }
            }
        });
}
