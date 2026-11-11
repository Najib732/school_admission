using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace SchoolAdmission.Tests;

// Starts the real API in memory against a fresh PostgreSQL database, and drops it afterwards.
// The server comes from TEST_DB_CONNECTION (see README); the database name is made per test.
public sealed class TestApp : IAsyncDisposable
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper) }
    };

    private readonly List<WebApplicationFactory<Program>> _factories = [];

    public string ConnectionString { get; }

    private TestApp(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public static TestApp Create()
    {
        var server = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
            ?? throw new InvalidOperationException("TEST_DB_CONNECTION is not set.");
        var builder = new NpgsqlConnectionStringBuilder(server)
        {
            Database = $"school_admission_test_{Guid.NewGuid():N}"
        };

        return new TestApp(builder.ConnectionString);
    }

    public HttpClient StartServer(int holdDurationSeconds = 600, int expiryIntervalSeconds = 60)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
            builder.UseSetting("HOLD_DURATION_SECONDS", holdDurationSeconds.ToString());
            builder.UseSetting("HOLD_EXPIRY_INTERVAL_SECONDS", expiryIntervalSeconds.ToString());
        });
        _factories.Add(factory);

        return factory.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var factory in _factories)
        {
            await factory.DisposeAsync();
        }

        NpgsqlConnection.ClearAllPools();
        var builder = new NpgsqlConnectionStringBuilder(ConnectionString);
        var database = builder.Database;
        builder.Database = "postgres";

        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)", connection);
        await command.ExecuteNonQueryAsync();
    }
}

public record ClassResponse(Guid Id, string Name, int TotalSeats, int HeldSeats, int ConfirmedSeats, int AvailableSeats);

public record HoldResponse(Guid HoldId, Guid ClassId, string ClassName, string StudentId, string Status, DateTime ExpiresAt);

public static class ApiExtensions
{
    public static async Task<ClassResponse> CreateClass(this HttpClient client, string name, int totalSeats)
    {
        var response = await client.PostAsJsonAsync("/classes", new { name, totalSeats });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClassResponse>(TestApp.Json))!;
    }

    public static async Task<ClassResponse> GetClass(this HttpClient client, Guid classId)
    {
        return (await client.GetFromJsonAsync<ClassResponse>($"/classes/{classId}", TestApp.Json))!;
    }

    public static Task<HttpResponseMessage> Apply(this HttpClient client, Guid classId, string studentId)
    {
        return client.SendWithStudent(HttpMethod.Post, $"/classes/{classId}/holds", studentId);
    }

    public static Task<HttpResponseMessage> Confirm(this HttpClient client, Guid holdId, string studentId)
    {
        return client.SendWithStudent(HttpMethod.Post, $"/holds/{holdId}/confirm", studentId);
    }

    public static Task<HttpResponseMessage> Cancel(this HttpClient client, Guid holdId, string studentId)
    {
        return client.SendWithStudent(HttpMethod.Delete, $"/holds/{holdId}", studentId);
    }

    public static async Task<HoldResponse> ReadHold(this HttpResponseMessage response)
    {
        return (await response.Content.ReadFromJsonAsync<HoldResponse>(TestApp.Json))!;
    }

    public static Task<HttpResponseMessage> SendWithStudent(this HttpClient client, HttpMethod method, string url, string studentId)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Student-Id", studentId);
        return client.SendAsync(request);
    }

    // 17-digit birth registration numbers: 20150000000000001, 20150000000000002, ...
    public static string Student(int n) => (20150000000000000L + n).ToString();
}
