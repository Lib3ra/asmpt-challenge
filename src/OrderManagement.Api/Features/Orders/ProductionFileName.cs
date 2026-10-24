using System.Text;

namespace OrderManagement.Api.Features.Orders;

/// <summary>Builds the download file name <c>order-&lt;id&gt;-&lt;name&gt;.json</c> with a safe, lower-case name part.</summary>
public static class ProductionFileName
{
    private const int NameMaxLength = 50;

    /// <summary>
    /// e.g. order 7 "ORD-2026-001 Prototype run" → <c>order-7-ord-2026-001-prototype-run.json</c>. Every run of
    /// characters other than ASCII letters and digits becomes one hyphen; a name with nothing left gives <c>order-7.json</c>.
    /// </summary>
    public static string For(int orderId, string orderName)
    {
        StringBuilder safeName = new();
        foreach (char character in orderName.ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                safeName.Append(character);
            }
            else if (safeName.Length > 0 && safeName[^1] != '-')
            {
                safeName.Append('-');
            }
        }

        string safeNamePart = safeName.ToString(0, Math.Min(safeName.Length, NameMaxLength)).Trim('-');
        return safeNamePart.Length == 0 ? $"order-{orderId}.json" : $"order-{orderId}-{safeNamePart}.json";
    }
}
