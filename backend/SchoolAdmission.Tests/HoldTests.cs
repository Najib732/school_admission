using System.Net;
using System.Net.Http.Json;

namespace SchoolAdmission.Tests;

public class HoldTests
{
    [Fact]
    public async Task Apply_Returns201_WithHeldStatus()
    {
        await using var app = TestApp.Create();
        var client = app.StartServer(holdDurationSeconds: 600);
        var cls = await client.CreateClass("Class 6 - Morning Shift", 120);

        var response = await client.Apply(cls.Id, ApiExtensions.Student(1));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var hold = await response.ReadHold();
        Assert.Equal("HELD", hold.Status);
        Assert.Equal(cls.Id, hold.ClassId);
        Assert.Equal(ApiExtensions.Student(1), hold.StudentId);
        Assert.InRange(hold.ExpiresAt, DateTime.UtcNow.AddSeconds(590), DateTime.UtcNow.AddSeconds(610));
    }

    [Fact]
    public async Task SameStudent_CannotApplyTwiceToSameClass_ButCanToAnotherClass()
    {
        await using var app = TestApp.Create();
        var client = app.StartServer();
        var morning = await client.CreateClass("Class 6 - Morning Shift", 10);
        var day = await client.CreateClass("Class 6 - Day Shift", 10);
        var student = ApiExtensions.Student(1);

        Assert.Equal(HttpStatusCode.Created, (await client.Apply(morning.Id, student)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.Apply(morning.Id, student)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.Apply(day.Id, student)).StatusCode);

        Assert.Equal(1, (await client.GetClass(morning.Id)).HeldSeats);
    }

    [Fact]
    public async Task Confirm_Twice_CountsTheSeatOnlyOnce()
    {
        await using var app = TestApp.Create();
        var client = app.StartServer();
        var cls = await client.CreateClass("Class 8 - Morning Shift", 5);
        var student = ApiExtensions.Student(1);
        var hold = await (await client.Apply(cls.Id, student)).ReadHold();

        // Several payments for the same application at the same moment, then one more.
        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => client.Confirm(hold.HoldId, student)));
        var again = await client.Confirm(hold.HoldId, student);

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Equal("CONFIRMED", (await again.ReadHold()).Status);

        var after = await client.GetClass(cls.Id);
        Assert.Equal(1, after.ConfirmedSeats);
        Assert.Equal(0, after.HeldSeats);
    }

    [Fact]
    public async Task OnlyTheOwnStudent_CanConfirmOrCancel()
    {
        await using var app = TestApp.Create();
        var client = app.StartServer();
        var cls = await client.CreateClass("Class 9 - Day Shift", 5);
        var hold = await (await client.Apply(cls.Id, ApiExtensions.Student(1))).ReadHold();
        var stranger = ApiExtensions.Student(2);

        Assert.Equal(HttpStatusCode.NotFound, (await client.Confirm(hold.HoldId, stranger)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.Cancel(hold.HoldId, stranger)).StatusCode);
        Assert.Equal(1, (await client.GetClass(cls.Id)).HeldSeats);
    }

    [Fact]
    public async Task Cancel_Returns204_FreesTheSeat_AndAllowsApplyingAgain()
    {
        await using var app = TestApp.Create();
        var client = app.StartServer();
        var cls = await client.CreateClass("Class 6 - Day Shift", 1);
        var student = ApiExtensions.Student(1);
        var hold = await (await client.Apply(cls.Id, student)).ReadHold();

        Assert.Equal(HttpStatusCode.NoContent, (await client.Cancel(hold.HoldId, student)).StatusCode);
        Assert.Equal(0, (await client.GetClass(cls.Id)).HeldSeats);
        Assert.Equal("CANCELLED", (await client.GetFromJsonAsync<HoldResponse>($"/holds/{hold.HoldId}", TestApp.Json))!.Status);
        Assert.Equal(HttpStatusCode.Conflict, (await client.Confirm(hold.HoldId, student)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.Apply(cls.Id, student)).StatusCode);
    }

    [Fact]
    public async Task ExpiredHold_ShowsExpired_CannotBePaid_AndTheSeatIsReleasedByItself()
    {
        await using var app = TestApp.Create();
        var client = app.StartServer(holdDurationSeconds: 1, expiryIntervalSeconds: 1);
        var cls = await client.CreateClass("Class 7 - Morning Shift", 1);
        var student = ApiExtensions.Student(1);
        var hold = await (await client.Apply(cls.Id, student)).ReadHold();
        Assert.Equal(0, (await client.GetClass(cls.Id)).AvailableSeats);

        // No API call in between: the background worker must release the seat.
        await Task.Delay(TimeSpan.FromSeconds(4));

        var status = await client.GetFromJsonAsync<HoldResponse>($"/holds/{hold.HoldId}", TestApp.Json);
        Assert.Equal("EXPIRED", status!.Status);
        var after = await client.GetClass(cls.Id);
        Assert.Equal(0, after.HeldSeats);
        Assert.Equal(1, after.AvailableSeats);

        Assert.Equal(HttpStatusCode.Conflict, (await client.Confirm(hold.HoldId, student)).StatusCode);
        Assert.Equal(0, (await client.GetClass(cls.Id)).ConfirmedSeats);

        // The same student may apply again after expiry.
        Assert.Equal(HttpStatusCode.Created, (await client.Apply(cls.Id, student)).StatusCode);
    }

    [Fact]
    public async Task MyHolds_ListsOnlyThisStudentsApplications()
    {
        await using var app = TestApp.Create();
        var client = app.StartServer();
        var a = await client.CreateClass("Class 6 - Morning Shift", 5);
        var b = await client.CreateClass("Class 7 - Morning Shift", 5);
        await client.Apply(a.Id, ApiExtensions.Student(1));
        await client.Apply(b.Id, ApiExtensions.Student(1));
        await client.Apply(a.Id, ApiExtensions.Student(2));

        var response = await client.SendWithStudent(HttpMethod.Get, "/me/holds", ApiExtensions.Student(1));
        var mine = await response.Content.ReadFromJsonAsync<List<HoldResponse>>(TestApp.Json);

        Assert.Equal(2, mine!.Count);
        Assert.All(mine, h => Assert.Equal(ApiExtensions.Student(1), h.StudentId));
        Assert.Contains(mine, h => h.ClassName == "Class 7 - Morning Shift");
    }
}
