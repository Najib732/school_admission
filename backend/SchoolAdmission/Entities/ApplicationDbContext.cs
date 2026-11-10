using Microsoft.EntityFrameworkCore;

namespace SchoolAdmission.Entities;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Class> Classes { get; set; }
    public DbSet<Hold> Holds { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Class>(e =>
        {
            e.Property(c => c.Name).IsRequired().HasMaxLength(200);
            e.HasIndex(c => c.Name).IsUnique();

            // Safety net for rule 1. The real check is the one-step UPDATE in HoldRepo.Apply.
            e.ToTable(t => t.HasCheckConstraint(
                "CK_Classes_Seats",
                "\"TotalSeats\" > 0 AND \"HeldSeats\" >= 0 AND \"ConfirmedSeats\" >= 0 " +
                "AND \"HeldSeats\" + \"ConfirmedSeats\" <= \"TotalSeats\""));
        });

        modelBuilder.Entity<Hold>(e =>
        {
            e.Property(h => h.Status)
                .HasConversion(
                    s => s.ToString().ToUpperInvariant(),
                    s => Enum.Parse<HoldStatus>(s, true))
                .HasMaxLength(20);
            e.Property(h => h.StudentId).HasMaxLength(17).IsFixedLength();
            e.HasOne<Class>().WithMany().HasForeignKey(h => h.ClassId).OnDelete(DeleteBehavior.Restrict);

            // Safety net for rule 3. The real check is the AnyAsync in HoldRepo.Apply.
            e.HasIndex(h => new { h.ClassId, h.StudentId })
                .IsUnique()
                .HasFilter("\"Status\" IN ('HELD','CONFIRMED')");
            e.HasIndex(h => new { h.ClassId, h.Status, h.ExpiresAt });
            e.HasIndex(h => h.StudentId);
        });
    }
}
