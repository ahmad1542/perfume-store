using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PerfumeStore.Domain.Exceptions;
using Xunit;

namespace PerfumeStore.API.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program> {
    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        builder.UseEnvironment("Production");
        builder.ConfigureServices(services => {
            services.AddControllers().AddApplicationPart(typeof(ErrorProbeController).Assembly);
        });
    }
}

public sealed class ErrorResponseTests(ApiFactory factory) : IClassFixture<ApiFactory> {
    private readonly HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions {
        BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
    });

    [Theory]
    [InlineData("missing", HttpStatusCode.NotFound, "Product with id: 42 doesn't exist")]
    [InlineData("business", HttpStatusCode.BadRequest, "Not enough stock.")]
    [InlineData("unexpected", HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.")]
    public async Task ExceptionsProduceSafeProblemDetails(string kind, HttpStatusCode status, string detail) {
        var response = await client.GetAsync($"/__test/errors/{kind}");
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal((int)status, json.GetProperty("status").GetInt32());
        Assert.Equal(detail, json.GetProperty("detail").GetString());
        Assert.Equal($"/__test/errors/{kind}", json.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("traceId").GetString()));
        Assert.DoesNotContain("private database password", json.ToString());
        Assert.DoesNotContain("stackTrace", json.ToString());
    }

    [Fact]
    public async Task ExistingSalesInvoiceValidationStillReturnsFieldErrors() {
        var response = await client.PostAsJsonAsync("/api/SalesInvoices", new { amountPaid = -1 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Amount paid must be greater than or equal to 0.",
            json.GetProperty("errors").GetProperty("AmountPaid")[0].GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task SuccessfulRequestsAreUnaffected() {
        var response = await client.GetAsync("/__test/errors/success");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"ok\":true}", await response.Content.ReadAsStringAsync());
    }
}

// These routes are loaded only by the test host, never by the deployed API.
[ApiController]
[Route("__test/errors")]
public sealed class ErrorProbeController : ControllerBase {
    [HttpGet("{kind}")]
    public IActionResult Get(string kind) => kind switch {
        "missing" => throw new NotFoundException("Product", "42"),
        "business" => throw new BusinessRuleException("Not enough stock."),
        "unexpected" => throw new InvalidOperationException("private database password"),
        _ => Ok(new { ok = true })
    };
}
