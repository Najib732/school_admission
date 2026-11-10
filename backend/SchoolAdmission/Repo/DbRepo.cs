using SchoolAdmission.Entities;

namespace SchoolAdmission.Repo;

public class DbRepo
{
    public ApplicationDbContext _context;

    public DbRepo(ApplicationDbContext context)
    {
        _context = context;
    }
}
