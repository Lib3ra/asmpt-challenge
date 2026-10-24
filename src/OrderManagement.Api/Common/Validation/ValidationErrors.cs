namespace OrderManagement.Api.Common.Validation;

/// <summary>
/// Collects validation messages per field. Keys follow the JSON request: <c>name</c> for a single object,
/// <c>[1].name</c> for the second item of a list. Converts to the dictionary used by <c>ValidationProblem</c>.
/// </summary>
public sealed class ValidationErrors
{
    private readonly Dictionary<string, List<string>> _messagesByField = [];

    public bool HasErrors => _messagesByField.Count > 0;

    public void Add(string field, string message)
    {
        if (!_messagesByField.TryGetValue(field, out List<string>? messages))
        {
            messages = [];
            _messagesByField[field] = messages;
        }

        messages.Add(message);
    }

    /// <summary>Adds the errors of one list item under the prefix <c>[index].</c>.</summary>
    public void AddItem(int index, ValidationErrors itemErrors)
    {
        foreach ((string field, List<string> messages) in itemErrors._messagesByField)
        {
            foreach (string message in messages)
            {
                Add($"[{index}].{field}", message);
            }
        }
    }

    public Dictionary<string, string[]> ToDictionary() =>
        _messagesByField.ToDictionary(entry => entry.Key, entry => entry.Value.ToArray());
}
