using Domain.Aggregates.Customer.Services;
using System.Reflection;

namespace Domain.Aggregates.Customer;

public static class CustomerFactory
{
    public static void HydrateFromPersistence(Customer customer, IDateTimeProvider dateTimeProvider)
    {
        var field = typeof(Customer)
            .GetField("_dateTimeProvider", BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(customer, dateTimeProvider);
    }
}