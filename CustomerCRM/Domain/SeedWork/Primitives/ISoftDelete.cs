namespace Domain.SeedWork.Primitives;

public interface ISoftDelete
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
    void Delete();
    void Restore();
}