using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;
using Sudoku;
using System.Reflection;

namespace Sudoku.tests;

public class UnitTest1 : IClassFixture<WebApplicationFactory<Program>>
{
    private WebApplicationFactory<Program>? _factory = null;

    public UnitTest1(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/online", "sim!")]
    [InlineData("/new", null)]
    public async Task Sudoku_ReturnsJsonString(string end, string? resp)
    {
        HttpClient client = _factory!.CreateClient();

        HttpResponseMessage response = await client.GetAsync(end);

        response.EnsureSuccessStatusCode();

        Assert.Equal(
            "application/json",
            response.Content.Headers.ContentType!.MediaType
        );
        if (resp != null)
        {
            Assert.Contains(resp!, await response.Content.ReadAsStringAsync());
        }

    }
}
