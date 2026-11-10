namespace SchoolAdmission.Common;

public class HoldSettings
{
    // How long a seat stays held before it expires (HOLD_DURATION_SECONDS).
    public TimeSpan HoldDuration { get; init; }

    // How often the background worker looks for expired holds (HOLD_EXPIRY_INTERVAL_SECONDS).
    public TimeSpan ExpiryInterval { get; init; }

    public static HoldSettings FromConfiguration(IConfiguration configuration)
    {
        return new HoldSettings
        {
            HoldDuration = TimeSpan.FromSeconds(ReadPositive(configuration, "HOLD_DURATION_SECONDS")),
            ExpiryInterval = TimeSpan.FromSeconds(ReadPositive(configuration, "HOLD_EXPIRY_INTERVAL_SECONDS"))
        };
    }

    private static int ReadPositive(IConfiguration configuration, string key)
    {
        var raw = configuration[key];
        if (int.TryParse(raw, out var value) && value > 0)
        {
            return value;
        }

        throw new InvalidOperationException($"{key} must be set to a positive number, got '{raw}'.");
    }
}
