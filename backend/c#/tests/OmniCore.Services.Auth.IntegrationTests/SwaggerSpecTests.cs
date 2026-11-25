using System.Net;
using System.Text.Json;

namespace OmniCore.Services.Auth.IntegrationTests;

/// <summary>The generated OpenAPI document must exist, parse, list the endpoints and have no dangling schema references
/// (a broken spec makes Swagger UI show nothing or an error instead of the endpoints).</summary>
[Collection(AuthApiCollection.Name)]
public class SwaggerSpecTests(AuthApiFactory factory)
{
    [Fact]
    public async Task The_swagger_document_is_served_complete_and_consistent()
    {
        var response = await factory.CreateClient().GetAsync("/swagger/v1/swagger.json");
        var text = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK, $"{(int)response.StatusCode}: {text[..Math.Min(text.Length, 1500)]}");

        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;

        var paths = root.GetProperty("paths").EnumerateObject().ToList();
        Assert.NotEmpty(paths);
        Assert.Contains(paths, p => p.Name.StartsWith("/api/v1/auth", StringComparison.OrdinalIgnoreCase));

        var missing = new SortedSet<string>();
        var schemas = root.TryGetProperty("components", out var c) && c.TryGetProperty("schemas", out var s)
            ? s.EnumerateObject().Select(x => x.Name).ToHashSet()
            : [];
        Walk(root, schemas, missing);

        Assert.True(missing.Count == 0, "Dangling schema references: " + string.Join(", ", missing));
    }

    private static void Walk(JsonElement element, HashSet<string> schemas, SortedSet<string> missing)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    const string prefix = "#/components/schemas/";
                    if (property.Name == "$ref" && property.Value.ValueKind == JsonValueKind.String
                        && property.Value.GetString() is { } reference && reference.StartsWith(prefix, StringComparison.Ordinal)
                        && !schemas.Contains(reference[prefix.Length..]))
                    {
                        missing.Add(reference[prefix.Length..]);
                    }

                    Walk(property.Value, schemas, missing);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray()) Walk(item, schemas, missing);
                break;
        }
    }
}
