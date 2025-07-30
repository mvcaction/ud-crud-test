namespace Domain.Aggregates.Customer.Services;

public interface ICustomerUniquenessCheckerService
{
    Task<bool> IsEmailTaken(string email, Guid? excludeCustomerId = null);
    Task<bool> IsPersonalInfoTaken(string firstName, string lastName, DateTime dateOfBirth, Guid? excludeCustomerId = null);
}