using System.Net;

namespace SchoolAdmission.Tests;

public class ConcurrencyTests
{
    [Fact]
    public async Task ManyStudentsApplyingAtOnce_NeverHoldMoreSeatsThanTheClassHas()
    {
        await using var app = TestApp.Create();
        var client = app.StartServer();
        var cls = await client.CreateClass("Class 6 - Morning Shift", 5);

        // 60 different students press "Apply" at the same moment for 5 seats.
        var responses = await Task.WhenAll(
            Enumerable.Range(1, 60).Select(n => client.Apply(cls.Id, ApiExtensions.Student(n))));

        Assert.Equal(5, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(55, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));

        var after = await client.GetClass(cls.Id);
        Assert.Equal(5, after.HeldSeats);
        Assert.Equal(0, after.AvailableSeats);
    }

    [Fact]
    public async Task SameStudentDoubleClicking_GetsOnlyOneHold()
    {
        await using var app = TestApp.Create();
        var client = app.StartServer();
        var cls = await client.CreateClass("Class 7 - Day Shift", 10);
        var student = ApiExtensions.Student(1);

        var responses = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => client.Apply(cls.Id, student)));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(1, (await client.GetClass(cls.Id)).HeldSeats);
    }
}
