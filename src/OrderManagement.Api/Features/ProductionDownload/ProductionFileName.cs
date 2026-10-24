using System.Text;

namespace OrderManagement.Api.Features.ProductionDownload;

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
        StringBuilder slug = new();
        foreach (char character in orderName.ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                slug.Append(character);
            }
            else if (slug.Length > 0 && slug[^1] != '-')
            {
                slug.Append('-');
            }
        }

        string name = slug.ToString(0, Math.Min(slug.Length, NameMaxLength)).Trim('-');
        return name.Length == 0 ? $"order-{orderId}.json" : $"order-{orderId}-{name}.json";
    }
}
