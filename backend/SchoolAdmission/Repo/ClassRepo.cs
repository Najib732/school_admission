using SchoolAdmission.Common;
using SchoolAdmission.DTO;
using SchoolAdmission.Entities;
using SchoolAdmission.Interface;
using Microsoft.EntityFrameworkCore;

namespace SchoolAdmission.Repo;

public class ClassRepo : DbRepo, IClass
{
    public ClassRepo(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Result<ClassGetDto>> Add(ClassAddDto dto)
    {
        var name = dto.Name.Trim();

        bool exists = await _context.Classes.AnyAsync(c => c.Name.ToLower() == name.ToLower());
        if (exists)
        {
            return Result<ClassGetDto>.Conflict($"A class named \"{name}\" already exists.");
        }

        Class newValue = new Class
        {
            Name = name,
            TotalSeats = dto.TotalSeats
        };
        try
        {
            await _context.Classes.AddAsync(newValue);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Two admins created the same name at the same moment; the unique index caught it.
            return Result<ClassGetDto>.Conflict($"A class named \"{name}\" already exists.");
        }

        return await GetById(newValue.Id);
    }

    public async Task<Result<List<ClassGetDto>>> GetAll()
    {
        var value = await (
            from c in _context.Classes
            where !c.IsDelete
            orderby c.Name
            select new ClassGetDto
            {
                Id = c.Id,
                Name = c.Name,
                TotalSeats = c.TotalSeats,
                HeldSeats = c.HeldSeats,
                ConfirmedSeats = c.ConfirmedSeats,
                AvailableSeats = c.TotalSeats - c.HeldSeats - c.ConfirmedSeats
            }).ToListAsync();

        return Result<List<ClassGetDto>>.Ok(value);
    }

    public async Task<Result<ClassGetDto>> GetById(Guid id)
    {
        var value = await (
            from c in _context.Classes
            where c.Id == id && !c.IsDelete
            select new ClassGetDto
            {
                Id = c.Id,
                Name = c.Name,
                TotalSeats = c.TotalSeats,
                HeldSeats = c.HeldSeats,
                ConfirmedSeats = c.ConfirmedSeats,
                AvailableSeats = c.TotalSeats - c.HeldSeats - c.ConfirmedSeats
            }).FirstOrDefaultAsync();

        if (value == null)
        {
            return Result<ClassGetDto>.NotFound($"Class with id {id} not found");
        }

        return Result<ClassGetDto>.Ok(value);
    }
}
