using System.Net;
using System.Net.Http.Json;

namespace SchoolAdmission.Tests;

public class ClassTests
{
    [Fact]
    public async Task CreateClass_Returns201_AndListShowsSeatCounts()
    {
        await using var app = TestApp.Create();
        var client = app.StartServer();

        var response = await client.PostAsJsonAsync("/classes", new { name = "Class 6 - Morning Shift", totalSeats = 120 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ClassResponse>(TestApp.Json);
        Assert.Equal("Class 6 - Morning Shift", created!.Name);
        Assert.Equal(120, created.TotalSeats);

        await client.Apply(created.Id, ApiExtensions.Student(1));

        var list = await client.GetFromJsonAsync<List<ClassResponse>>("/classes", TestApp.Json);
        var row = Assert.Single(list!);
        Assert.Equal(1, row.HeldSeats);
        Assert.Equal(0, row.ConfirmedSeats);
        Assert.Equal(119, row.AvailableSeats);
    }

    [Fact]
    public async Task BadInput_Returns4xx_NeverServerError()
    {
        await using var app = TestApp.Create();
        var client = app.StartServer();
        var cls = await client.CreateClass("Class 7 - Day Shift", 10);

        var cases = new List<(string Name, HttpResponseMessage Response, HttpStatusCode Expected)>
        {
            ("empty name", await client.PostAsJsonAsync("/classes", new { name = "", totalSeats = 10 }), HttpStatusCode.BadRequest),
            ("zero seats", await client.PostAsJsonAsync("/classes", new { name = "X", totalSeats = 0 }), HttpStatusCode.BadRequest),
            ("seats as text", await client.PostAsync("/classes", JsonBody("{\"name\":\"X\",\"totalSeats\":\"many\"}")), HttpStatusCode.BadRequest),
            ("broken json", await client.PostAsync("/classes", JsonBody("{not json")), HttpStatusCode.BadRequest),
            ("duplicate name", await client.PostAsJsonAsync("/classes", new { name = "class 7 - day shift", totalSeats = 5 }), HttpStatusCode.Conflict),
            ("class id not a guid", await client.GetAsync("/classes/abc"), HttpStatusCode.BadRequest),
            ("unknown class", await client.GetAsync($"/classes/{Guid.NewGuid()}"), HttpStatusCode.NotFound),
            ("no student header", await client.PostAsync($"/classes/{cls.Id}/holds", null), HttpStatusCode.BadRequest),
            ("student id too short", await client.Apply(cls.Id, "12345"), HttpStatusCode.BadRequest),
            ("student id with letters", await client.Apply(cls.Id, "2015abc1234567890"), HttpStatusCode.BadRequest),
            ("apply to unknown class", await client.Apply(Guid.NewGuid(), ApiExtensions.Student(1)), HttpStatusCode.NotFound),
            ("unknown hold", await client.GetAsync($"/holds/{Guid.NewGuid()}"), HttpStatusCode.NotFound),
            ("hold id not a guid", await client.GetAsync("/holds/h_789"), HttpStatusCode.BadRequest),
            ("me without header", await client.GetAsync("/me/holds"), HttpStatusCode.BadRequest)
        };

        foreach (var (name, response, expected) in cases)
        {
            Assert.True(expected == response.StatusCode, $"{name}: expected {(int)expected}, got {(int)response.StatusCode}");
        }
    }

    private static StringContent JsonBody(string json)
    {
        return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
    }
}
