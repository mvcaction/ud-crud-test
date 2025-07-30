using Domain.Aggregates.Customer;
using Domain.Aggregates.Customer.Services;

namespace Infrastructure.Services;

public interface ICustomerHydrationService
{
    void Hydrate(Customer customer);
}

public class CustomerHydrationService : ICustomerHydrationService
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public CustomerHydrationService(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public void Hydrate(Customer customer)
    {
        CustomerFactory.HydrateFromPersistence(customer, _dateTimeProvider);
    }
}