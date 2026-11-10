using SchoolAdmission.Common;
using SchoolAdmission.DTO;
using SchoolAdmission.Entities;
using SchoolAdmission.Interface;
using Microsoft.EntityFrameworkCore;

namespace SchoolAdmission.Repo;

// Seat counts on Classes change in the same transaction as the hold, always after locking the
// class row (SELECT ... FOR UPDATE). Every path locks the class first and the holds second, so
// requests for one class run one at a time, also across several backend processes.
public class HoldRepo : DbRepo, IHold
{
    private const string ExpiredMessage = "The time to pay has run out and the seat was released. Please apply again.";

    public HoldRepo(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Result<HoldGetDto>> Apply(Guid classId, string studentId, TimeSpan holdDuration)
    {
        var now = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var classRow = await LockClass(classId);
        if (classRow == null)
        {
            return Result<HoldGetDto>.NotFound($"Class with id {classId} not found");
        }

        // Give back seats of holds that ran out, so they don't block this application.
        await ExpireForClass(classId, now);

        // business rule: one active application (HELD or CONFIRMED) per student per class
        bool alreadyApplied = await _context.Holds.AnyAsync(h =>
            h.ClassId == classId && h.StudentId == studentId &&
            (h.Status == HoldStatus.Held || h.Status == HoldStatus.Confirmed));
        if (alreadyApplied)
        {
            return Result<HoldGetDto>.Conflict(
                $"This student already has an application for {classRow.Name}. Open it from My applications.");
        }

        // Take a seat only if one is free: check and increment in one statement.
        int taken = await _context.Classes
            .Where(c => c.Id == classId && c.HeldSeats + c.ConfirmedSeats < c.TotalSeats)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.HeldSeats, c => c.HeldSeats + 1));
        if (taken == 0)
        {
            return Result<HoldGetDto>.Conflict($"No seats left in {classRow.Name}.");
        }

        Hold newValue = new Hold
        {
            ClassId = classId,
            StudentId = studentId,
            Status = HoldStatus.Held,
            CreatedAt = now,
            ExpiresAt = now.Add(holdDuration)
        };
        try
        {
            await _context.Holds.AddAsync(newValue);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // The filtered unique index caught a double click; the transaction rolls back.
            return Result<HoldGetDto>.Conflict(
                $"This student already has an application for {classRow.Name}. Open it from My applications.");
        }

        await transaction.CommitAsync();

        return Result<HoldGetDto>.Ok(ToGetDto(newValue, classRow.Name, now));
    }

    public async Task<Result<HoldGetDto>> GetById(Guid holdId)
    {
        var value = await (
            from h in _context.Holds.AsNoTracking()
            join c in _context.Classes on h.ClassId equals c.Id
            where h.Id == holdId
            select new { h, c.Name }).FirstOrDefaultAsync();

        if (value == null)
        {
            return Result<HoldGetDto>.NotFound($"Application with id {holdId} not found");
        }

        return Result<HoldGetDto>.Ok(ToGetDto(value.h, value.Name, DateTime.UtcNow));
    }

    public async Task<Result<HoldGetDto>> Confirm(Guid holdId, string studentId)
    {
        var now = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var hold = await _context.Holds.AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == holdId && h.StudentId == studentId);
        if (hold == null)
        {
            return Result<HoldGetDto>.NotFound($"Application with id {holdId} not found");
        }

        // Paying twice is fine: the seat was counted the first time.
        if (hold.Status == HoldStatus.Confirmed)
        {
            return await GetById(holdId);
        }

        await LockClass(hold.ClassId);

        int confirmed = await _context.Holds
            .Where(h => h.Id == holdId && h.Status == HoldStatus.Held && h.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(h => h.Status, HoldStatus.Confirmed)
                .SetProperty(h => h.ConfirmedAt, now));

        if (confirmed == 1)
        {
            await _context.Classes
                .Where(c => c.Id == hold.ClassId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.HeldSeats, c => c.HeldSeats - 1)
                    .SetProperty(c => c.ConfirmedSeats, c => c.ConfirmedSeats + 1));
            await transaction.CommitAsync();

            return await GetById(holdId);
        }

        // Nothing changed: find out why.
        var status = await _context.Holds.Where(h => h.Id == holdId).Select(h => h.Status).FirstAsync();

        if (status == HoldStatus.Confirmed)
        {
            // Another request confirmed it a moment ago.
            await transaction.CommitAsync();
            return await GetById(holdId);
        }

        if (status == HoldStatus.Cancelled)
        {
            return Result<HoldGetDto>.Conflict("This application was cancelled, so it can't be paid.");
        }

        // HELD but out of time, or already EXPIRED: release the seat now and say so.
        await ExpireForClass(hold.ClassId, now);
        await transaction.CommitAsync();

        return Result<HoldGetDto>.Conflict(ExpiredMessage);
    }

    public async Task<Result<bool>> Cancel(Guid holdId, string studentId)
    {
        var now = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var hold = await _context.Holds.AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == holdId && h.StudentId == studentId);
        if (hold == null)
        {
            return Result<bool>.NotFound($"Application with id {holdId} not found");
        }

        if (hold.Status == HoldStatus.Cancelled)
        {
            return Result<bool>.Ok(true);
        }

        if (hold.Status == HoldStatus.Confirmed)
        {
            return Result<bool>.Conflict("This admission is already confirmed and can't be cancelled here.");
        }

        await LockClass(hold.ClassId);
        await ExpireForClass(hold.ClassId, now);

        int cancelled = await _context.Holds
            .Where(h => h.Id == holdId && h.Status == HoldStatus.Held)
            .ExecuteUpdateAsync(s => s
                .SetProperty(h => h.Status, HoldStatus.Cancelled)
                .SetProperty(h => h.CancelledAt, now));

        if (cancelled == 1)
        {
            await _context.Classes
                .Where(c => c.Id == hold.ClassId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.HeldSeats, c => c.HeldSeats - 1));
            await transaction.CommitAsync();

            return Result<bool>.Ok(true);
        }

        var status = await _context.Holds.Where(h => h.Id == holdId).Select(h => h.Status).FirstAsync();
        await transaction.CommitAsync();

        return status switch
        {
            HoldStatus.Cancelled => Result<bool>.Ok(true),
            HoldStatus.Confirmed => Result<bool>.Conflict("This admission is already confirmed and can't be cancelled here."),
            _ => Result<bool>.Conflict("This application has already expired, so there is nothing to cancel.")
        };
    }

    public async Task<Result<List<HoldGetDto>>> GetByStudentId(string studentId)
    {
        var now = DateTime.UtcNow;

        var rows = await (
            from h in _context.Holds.AsNoTracking()
            join c in _context.Classes on h.ClassId equals c.Id
            where h.StudentId == studentId
            orderby h.CreatedAt descending
            select new { h, c.Name }).ToListAsync();

        var value = rows.Select(x => ToGetDto(x.h, x.Name, now)).ToList();

        return Result<List<HoldGetDto>>.Ok(value);
    }

    // Used by the background worker: expires every overdue hold, one class at a time.
    public async Task<int> ExpireOverdue()
    {
        var now = DateTime.UtcNow;

        var classIds = await _context.Holds
            .Where(h => h.Status == HoldStatus.Held && h.ExpiresAt <= now)
            .Select(h => h.ClassId)
            .Distinct()
            .ToListAsync();

        int total = 0;
        foreach (var classId in classIds)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await LockClass(classId);
            total += await ExpireForClass(classId, now);
            await transaction.CommitAsync();
        }

        return total;
    }

    // Locks the class row until the transaction ends; concurrent requests for this class wait here.
    private async Task<Class?> LockClass(Guid classId)
    {
        var rows = await _context.Classes
            .FromSqlInterpolated($"SELECT * FROM \"Classes\" WHERE \"Id\" = {classId} AND \"IsDelete\" = false FOR UPDATE")
            .AsNoTracking()
            .ToListAsync();

        return rows.FirstOrDefault();
    }

    // Call only while the class row is locked.
    private async Task<int> ExpireForClass(Guid classId, DateTime now)
    {
        int expired = await _context.Holds
            .Where(h => h.ClassId == classId && h.Status == HoldStatus.Held && h.ExpiresAt <= now)
            .ExecuteUpdateAsync(s => s.SetProperty(h => h.Status, HoldStatus.Expired));

        if (expired > 0)
        {
            await _context.Classes
                .Where(c => c.Id == classId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.HeldSeats, c => c.HeldSeats - expired));
        }

        return expired;
    }

    // A HELD hold whose time has passed is reported as EXPIRED, even before the worker marks it.
    private static HoldGetDto ToGetDto(Hold h, string className, DateTime now)
    {
        var status = h.Status == HoldStatus.Held && h.ExpiresAt <= now ? HoldStatus.Expired : h.Status;

        return new HoldGetDto
        {
            HoldId = h.Id,
            ClassId = h.ClassId,
            ClassName = className,
            StudentId = h.StudentId,
            Status = status,
            CreatedAt = h.CreatedAt,
            ExpiresAt = h.ExpiresAt,
            ConfirmedAt = h.ConfirmedAt,
            CancelledAt = h.CancelledAt
        };
    }
}
