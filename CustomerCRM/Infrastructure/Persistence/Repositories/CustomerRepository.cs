using Application.Features.Customer.Abstractions;
using Dapper;
using Domain.Aggregates.Customer;
using Domain.Aggregates.Customer.Services;
using Domain.Aggregates.Customer.ValueObjects;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Infrastructure.Persistence.Repositories;

internal class CustomerRepository : ICustomerRepository
{
    private readonly string _connectionString;
    private readonly ICustomerHydrationService _hydrationService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CustomerRepository> _logger;

    public CustomerRepository(
        IConfiguration configuration,
        ICustomerHydrationService hydrationService,
        IDateTimeProvider dateTimeProvider,
        ILogger<CustomerRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new ArgumentNullException("Connection string not found");
        _hydrationService = hydrationService ?? throw new ArgumentNullException(nameof(hydrationService));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default, bool includeDeleted = false)
    {
        try
        {
            var whereClause = includeDeleted 
                ? "WHERE \"Id\" = @Id" 
                : "WHERE \"Id\" = @Id AND \"IsDeleted\" = false";

            var sql = $@"
                SELECT 
                    ""Id"",
                    ""FirstName"",
                    ""LastName"",
                    ""DateOfBirth"",
                    ""PhoneCountryCode"",
                    ""PhoneNumber"",
                    ""Email"",
                    ""BankAccountNumber"",
                    ""CreatedAt"",
                    ""UpdatedAt"",
                    ""IsDeleted"",
                    ""DeletedAt""
                FROM ""Customers"" 
                {whereClause}";

            using var connection = new NpgsqlConnection(_connectionString);
            var customerData = await connection.QueryFirstOrDefaultAsync(sql, new { Id = id });
            
            if (customerData == null)
                return null;

            var customer = MapToCustomer(customerData);
            _hydrationService.Hydrate(customer);
            
            return customer;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Operation was cancelled while retrieving customer {CustomerId}", id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving customer {CustomerId}", id);
            throw;
        }
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            const string sql = @"
                SELECT 
                    ""Id"",
                    ""FirstName"",
                    ""LastName"",
                    ""DateOfBirth"",
                    ""PhoneCountryCode"",
                    ""PhoneNumber"",
                    ""Email"",
                    ""BankAccountNumber"",
                    ""CreatedAt"",
                    ""UpdatedAt"",
                    ""IsDeleted"",
                    ""DeletedAt""
                FROM ""Customers"" 
                WHERE ""Email"" = @Email AND ""IsDeleted"" = false";

            using var connection = new NpgsqlConnection(_connectionString);
            var customerData = await connection.QueryFirstOrDefaultAsync(sql, new { Email = email });
            
            if (customerData == null)
                return null;

            var customer = MapToCustomer(customerData);
            _hydrationService.Hydrate(customer);
            
            return customer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer by email {Email}", email);
            throw;
        }
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(customer);
        
        try
        {
            const string sql = @"
                INSERT INTO ""Customers"" (
                    ""Id"",
                    ""FirstName"",
                    ""LastName"",
                    ""DateOfBirth"",
                    ""PhoneCountryCode"",
                    ""PhoneNumber"",
                    ""Email"",
                    ""BankAccountNumber"",
                    ""CreatedAt"",
                    ""UpdatedAt"",
                    ""IsDeleted"",
                    ""DeletedAt""
                ) VALUES (
                    @Id,
                    @FirstName,
                    @LastName,
                    @DateOfBirth,
                    @PhoneCountryCode,
                    @PhoneNumber,
                    @Email,
                    @BankAccountNumber,
                    @CreatedAt,
                    @UpdatedAt,
                    @IsDeleted,
                    @DeletedAt
                )";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new
            {
                customer.Id,
                FirstName = customer.FirstName.Value,
                LastName = customer.LastName.Value,
                DateOfBirth = customer.DateOfBirth.Value,
                PhoneCountryCode = customer.PhoneNumber.CountryCode,
                PhoneNumber = customer.PhoneNumber.Number,
                Email = customer.Email.Value,
                BankAccountNumber = customer.BankAccountNumber.Value,
                customer.CreatedAt,
                customer.UpdatedAt,
                customer.IsDeleted,
                customer.DeletedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding customer");
            throw;
        }
    }

    // Replace the Update method
    public void Update(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        
        try
        {
            // Execute the async update synchronously since the interface expects void
            // This is acceptable for Dapper since it executes immediately
            var task = UpdateAsync(customer);
            task.GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {CustomerId}", customer.Id);
            throw;
        }
    }

    // Helper method for actual async update
    public async Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(customer);
        
        try
        {
            const string sql = @"
                UPDATE ""Customers"" SET
                    ""FirstName"" = @FirstName,
                    ""LastName"" = @LastName,
                    ""DateOfBirth"" = @DateOfBirth,
                    ""PhoneCountryCode"" = @PhoneCountryCode,
                    ""PhoneNumber"" = @PhoneNumber,
                    ""Email"" = @Email,
                    ""BankAccountNumber"" = @BankAccountNumber,
                    ""UpdatedAt"" = @UpdatedAt,
                    ""IsDeleted"" = @IsDeleted,
                    ""DeletedAt"" = @DeletedAt
                WHERE ""Id"" = @Id";

            using var connection = new NpgsqlConnection(_connectionString);
            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                customer.Id,
                FirstName = customer.FirstName.Value,
                LastName = customer.LastName.Value,
                DateOfBirth = customer.DateOfBirth.Value,
                PhoneCountryCode = customer.PhoneNumber.CountryCode,
                PhoneNumber = customer.PhoneNumber.Number,
                Email = customer.Email.Value,
                BankAccountNumber = customer.BankAccountNumber.Value,
                customer.UpdatedAt,
                customer.IsDeleted,
                customer.DeletedAt
            });

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Customer with ID {customer.Id} was not found for update.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {CustomerId}", customer.Id);
            throw;
        }
    }

    private Customer MapToCustomer(dynamic customerData)
    {
        // Reconstruct the phone number as a single string first
        var fullPhoneNumber = $"{customerData.PhoneCountryCode}{customerData.PhoneNumber}";
        
        // Use reflection to create Customer instance without going through the factory method
        // This bypasses the business rules validation since we're reconstructing from DB
        var customerType = typeof(Customer);
        var customer = (Customer)Activator.CreateInstance(customerType, true)!;
        
        // Set the properties using reflection
        SetPrivateProperty(customer, "Id", customerData.Id);
        SetPrivateProperty(customer, "FirstName", FirstName.Create(customerData.FirstName));
        SetPrivateProperty(customer, "LastName", LastName.Create(customerData.LastName));
        SetPrivateProperty(customer, "DateOfBirth", DateOfBirth.Create(customerData.DateOfBirth));
        SetPrivateProperty(customer, "PhoneNumber", PhoneNumber.Create(fullPhoneNumber));
        SetPrivateProperty(customer, "Email", Email.Create(customerData.Email));
        SetPrivateProperty(customer, "BankAccountNumber", BankAccountNumber.Create(customerData.BankAccountNumber));
        SetPrivateProperty(customer, "CreatedAt", customerData.CreatedAt);
        SetPrivateProperty(customer, "UpdatedAt", customerData.UpdatedAt);
        SetPrivateProperty(customer, "IsDeleted", customerData.IsDeleted);
        SetPrivateProperty(customer, "DeletedAt", customerData.DeletedAt);
        
        // Set the date time provider using reflection on the private field
        SetPrivateField(customer, "_dateTimeProvider", _dateTimeProvider);

        return customer;
    }

    private static void SetPrivateProperty(object obj, string propertyName, object value)
    {
        var property = obj.GetType().GetProperty(propertyName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | 
            System.Reflection.BindingFlags.Public);
        property?.SetValue(obj, value);
    }

    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(obj, value);
    }
}