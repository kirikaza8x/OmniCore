using OmniCore.Shared.Domain.Queries;
using OmniCore.Shared.Infrastructure.Extensions;
using OmniCore.Shared.Infrastructure.Middleware;

namespace OmniCore.Shared.Tests;

public class QueryableExtensionsTests
{
    private sealed class Address { public string City { get; set; } = ""; }
    private sealed class Row
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string PasswordHash { get; set; } = "secret";
        public Address Home { get; set; } = new();
    }

    private static IQueryable<Row> Data() => new[]
    {
        new Row { Id = 1, Name = "alice", Home = new Address { City = "Hanoi" } },
        new Row { Id = 2, Name = "bob", Home = new Address { City = "Hue" } },
        new Row { Id = 3, Name = "alan", Home = new Address { City = "Hanoi" } },
    }.AsQueryable();

    private static Filter Leaf(string field, string op, object value, string? logic = null) =>
        new() { Field = field, Operator = op, Value = value, Logic = logic };

    [Fact]
    public void A_plain_property_filter_works()
    {
        var result = Data().ApplyDynamicFilters(Leaf("Name", "startswith", "al")).Select(r => r.Id).ToList();
        Assert.Equal([1, 3], result);
    }

    [Fact]
    public void A_nested_property_path_works()
    {
        var result = Data().ApplyDynamicFilters(Leaf("Home.City", "eq", "Hue")).Select(r => r.Id).ToList();
        Assert.Equal([2], result);
    }

    [Theory]
    [InlineData("PasswordHash.StartsWith(\"s\") or Id")]
    [InlineData("Name) or (1 == 1")]
    [InlineData("Name.ToString()")]
    [InlineData("Missing")]
    [InlineData("Home..City")]
    [InlineData("Name; drop")]
    [InlineData("")]
    public void Unsafe_field_names_are_rejected(string field)
    {
        Assert.False(QueryableExtensions.IsSafeFieldPath(typeof(Row), field));
    }

    [Fact]
    public void An_injected_field_is_ignored_and_never_widens_the_valid_part()
    {
        var filter = new Filter
        {
            Logic = "and",
            Filters =
            [
                Leaf("Name", "eq", "alice"),
                Leaf("Name) or (1 == 1", "eq", "x"),
            ],
        };

        var result = Data().ApplyDynamicFilters(filter).Select(r => r.Id).ToList();

        Assert.Equal([1], result);
    }

    [Fact]
    public void An_unknown_logic_word_becomes_and()
    {
        var filter = new Filter
        {
            Logic = "1==1 or",
            Filters = [Leaf("Name", "eq", "alice"), Leaf("Name", "eq", "bob")],
        };

        Assert.Empty(Data().ApplyDynamicFilters(filter));
    }

    [Fact]
    public void Sorting_accepts_real_properties_and_ignores_the_rest()
    {
        var sorted = Data().ApplyDynamicSorting([new Sort("Name", SortOrder.Descending)]).Select(r => r.Name).ToList();
        Assert.Equal(["bob", "alice", "alan"], sorted);

        var unchanged = Data().ApplyDynamicSorting([new Sort("Id; select", SortOrder.Descending)]).Select(r => r.Id).ToList();
        Assert.Equal([1, 2, 3], unchanged);
    }

    [Theory]
    [InlineData(1, 10, 1, 10, 0)]
    [InlineData(0, 0, 1, 10, 0)]
    [InlineData(3, 1_000_000, 3, 100, 200)]
    [InlineData(int.MaxValue, 100, int.MaxValue, 100, int.MaxValue)]
    public void Paging_is_clamped_and_cannot_overflow(int page, int size, int expectedPage, int expectedSize, int expectedSkip)
    {
        var (p, s, skip) = QueryableExtensions.NormalizePaging(page, size);
        Assert.Equal((expectedPage, expectedSize, expectedSkip), (p, s, skip));
    }

    [Theory]
    [InlineData("0f8fad5b-d9cb-469f-a165-70867728950e", true)]
    [InlineData("web_12345678", true)]
    [InlineData("", false)]
    [InlineData("has space", false)]
    [InlineData("<script>", false)]
    [InlineData("line\r\nbreak", false)]
    public void Device_ids_must_be_short_plain_tokens(string value, bool valid)
    {
        Assert.Equal(valid, DeviceIdMiddleware.IsValid(value));
        Assert.False(DeviceIdMiddleware.IsValid(new string('a', 65)));
    }
}
