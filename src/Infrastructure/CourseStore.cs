using EduCore.Application;
using EduCore.Domain;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Infrastructure;

public sealed class CoursesDb(DbContextOptions<CoursesDb> options) : DbContext(options)
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Course>(e =>
        {
            e.ToTable("courses");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.Title).HasColumnName("title").HasMaxLength(120);
            e.Property(c => c.Capacity).HasColumnName("capacity");
            e.HasMany(c => c.Enrollments).WithOne().HasForeignKey(c => c.CourseId);
            e.Navigation(c => c.Enrollments).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        model.Entity<Enrollment>(e =>
        {
            e.ToTable("enrollments");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.CourseId, x.StudentId }).IsUnique();
        });
    }
}

public sealed class CourseStore(CoursesDb db) : ICourseStore
{
    public async Task<Course> CreateAsync(string? title, int capacity, CancellationToken ct)
    {
        var course = new Course(title, capacity);
        db.Courses.Add(course);
        await db.SaveChangesAsync(ct);
        return course;
    }

    public Task<Course?> FindAsync(Guid id, CancellationToken ct) =>
        db.Courses.Include(c => c.Enrollments).SingleOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Enrollment> EnrollAsync(Guid courseId, Guid studentId, CancellationToken ct)
    {
        // PostgreSQL row lock serialises enrolments across application replicas.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var query = db.Database.IsNpgsql()
            ? db.Courses.FromSqlInterpolated(
                $"SELECT * FROM courses WHERE id = {courseId} FOR UPDATE"
            )
            : db.Courses.Where(c => c.Id == courseId);
        var course =
            await query.SingleOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Course not found.");
        await db.Entry(course).Collection(c => c.Enrollments).LoadAsync(ct);
        var enrollment = course.Enroll(studentId);
        if (db.Entry(enrollment).State == EntityState.Detached)
            db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return enrollment;
    }
}
