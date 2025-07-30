namespace Domain.SeedWork.Primitives;

public interface IBusinessRule
{
    bool IsBroken();
    string Message { get; }
}