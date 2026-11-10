using SchoolAdmission.Common;
using SchoolAdmission.Interface;

namespace SchoolAdmission.Services;

// Every few seconds, marks HELD applications whose time ran out as EXPIRED and gives their seats back.
// Safe to run in several backend processes at once: each class is locked while its holds expire.
public class HoldExpiryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly HoldSettings _settings;
    private readonly ILogger<HoldExpiryWorker> _logger;

    public HoldExpiryWorker(IServiceScopeFactory scopeFactory, HoldSettings settings, ILogger<HoldExpiryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_settings.ExpiryInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var holdRepo = scope.ServiceProvider.GetRequiredService<IHold>();

                int expired = await holdRepo.ExpireOverdue();
                if (expired > 0)
                {
                    _logger.LogInformation("Expired {Count} holds", expired);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One failed run (e.g. database restarting) must not stop the worker.
                _logger.LogError(ex, "Hold expiry run failed");
            }
        }
    }
}
