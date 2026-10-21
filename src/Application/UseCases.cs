using EduCore.Domain;
namespace EduCore.Application;

public interface ICourseStore
{
    Task<Course> CreateAsync(string? title, int capacity, CancellationToken cancellationToken);
    Task<Course?> FindAsync(Guid id, CancellationToken cancellationToken);
    Task<Enrollment> EnrollAsync(Guid courseId, Guid studentId, CancellationToken cancellationToken);
}
public sealed class CreateCourse(ICourseStore store)
{
    public Task<Course> Handle(string? title, int capacity, CancellationToken ct) => store.CreateAsync(title, capacity, ct);
}
public sealed class EnrollStudent(ICourseStore store)
{
    public Task<Enrollment> Handle(Guid courseId, Guid studentId, CancellationToken ct) => store.EnrollAsync(courseId, studentId, ct);
}
