using System.Text.Json;

namespace LashesWebsite.Services;

/// Razor's `System.Text.Json.JsonSerializer.Serialize(x)` uses the library's raw default options
/// (PascalCase property names) -- it does NOT inherit the camelCase policy MVC configures for
/// controller responses. Client-side JS (book.js) reads the SAME shape GessFlow's real API
/// returns (camelCase), so any C# object serialized into a <script> tag for it must use this
/// helper, not the JsonSerializer static methods directly.
public static class JsonHelper
{
    private static readonly JsonSerializerOptions CamelCase = new(JsonSerializerDefaults.Web);

    public static string ToCamelJson(object? value) => JsonSerializer.Serialize(value, CamelCase);
}
