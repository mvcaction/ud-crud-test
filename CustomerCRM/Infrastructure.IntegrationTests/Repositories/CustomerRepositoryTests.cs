using Application.Features.Customer.Abstractions;
using Domain.Aggregates.Customer.Services;
using Domain.Aggregates.Customer.ValueObjects;
using FluentAssertions;
using Infrastructure.IntegrationTests.Common;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CustomerAggregate = Domain.Aggregates.Customer.Customer;

namespace Infrastructure.IntegrationTests.Repositories;

public class CustomerRepositoryTests : IClassFixture<PostgreSqlTestContainer>, IDisposable
{
    private readonly PostgreSqlTestContainer _testContainer;
    private readonly CrmDbContext _context;
    private readonly ICustomerRepository _repository;
    private readonly ServiceProvider _serviceProvider;
    private static int _customerCounter = 0;

    public CustomerRepositoryTests(PostgreSqlTestContainer testContainer)
    {
        _testContainer = testContainer;
        
        var services = new ServiceCollection();

        // Create a configuration with the test container connection string
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", _testContainer.ConnectionString)
            })
            .Build();

        // Register the configuration as a singleton service
        services.AddSingleton<IConfiguration>(configuration);

        // Use the Infrastructure extension method which registers all services correctly
        services.AddInfrastructure(configuration);
        
        // Override the DbContext to use the test container
        services.Remove(services.First(s => s.ServiceType == typeof(DbContextOptions<CrmDbContext>)));
        services.AddDbContext<CrmDbContext>(options =>
            options.UseNpgsql(_testContainer.ConnectionString));

        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<CrmDbContext>();
        _repository = _serviceProvider.GetRequiredService<ICustomerRepository>();
    }

    [Fact]
    public async Task AddAsync_ShouldPersistCustomer()
    {
        // Arrange
        var customer = await CreateTestCustomerAsync();

        // Act
        await _repository.AddAsync(customer);
        await _context.SaveChangesAsync();

        // Assert
        var savedCustomer = await _repository.GetByIdAsync(customer.Id);
        savedCustomer.Should().NotBeNull();
        savedCustomer!.Email.Value.Should().Be(customer.Email.Value);
        
        // Verify in database directly
        var dbCustomer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == customer.Id);
        dbCustomer.Should().NotBeNull();
        dbCustomer!.Email.Value.Should().Be(customer.Email.Value);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnCustomerByEmail()
    {
        // Arrange
        var customer = await CreateTestCustomerAsync();
        await _repository.AddAsync(customer);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEmailAsync(customer.Email.Value);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(customer.Id);
        result.Email.Value.Should().Be(customer.Email.Value);
    }

    [Fact]
    public async Task Update_ShouldUpdateCustomer()
    {
        // Arrange
        var customer = await CreateTestCustomerAsync();
        await _repository.AddAsync(customer);
        await _context.SaveChangesAsync();

        var uniquenessChecker = _serviceProvider.GetRequiredService<ICustomerUniquenessCheckerService>();
        var newEmail = Email.Create($"updated{Guid.NewGuid():N}@example.com");

        // Act
        customer.ChangeEmail(newEmail, uniquenessChecker);
        _repository.Update(customer);
        await _context.SaveChangesAsync();

        // Assert
        var updatedCustomer = await _repository.GetByIdAsync(customer.Id);
        updatedCustomer.Should().NotBeNull();
        updatedCustomer!.Email.Value.Should().Be(newEmail.Value);
        
        // Verify domain events were raised
        customer.DomainEvents.Should().Contain(e => e.GetType().Name == "CustomerUpdatedDomainEvent");
    }

    [Fact]
    public async Task GetByIdAsync_WithIncludeDeleted_ShouldReturnDeletedCustomer()
    {
        // Arrange
        var customer = await CreateTestCustomerAsync();
        await _repository.AddAsync(customer);
        await _context.SaveChangesAsync();

        // Delete the customer
        customer.Delete();
        _repository.Update(customer);
        await _context.SaveChangesAsync();

        // Act - Get without including deleted (should return null)
        var resultExcludeDeleted = await _repository.GetByIdAsync(customer.Id, includeDeleted: false);

        // Act - Get with including deleted (should return customer)
        var resultIncludeDeleted = await _repository.GetByIdAsync(customer.Id, includeDeleted: true);

        // Assert
        resultExcludeDeleted.Should().BeNull(); // Soft-deleted customer not returned by default
        resultIncludeDeleted.Should().NotBeNull(); // Soft-deleted customer returned when explicitly requested
        resultIncludeDeleted!.IsDeleted.Should().BeTrue();
        resultIncludeDeleted.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_And_Restore_ShouldWorkCorrectly()
    {
        // Arrange
        var customer = await CreateTestCustomerAsync();
        await _repository.AddAsync(customer);
        await _context.SaveChangesAsync();

        // Act - Delete
        customer.Delete();
        _repository.Update(customer);
        await _context.SaveChangesAsync();

        // Assert - Customer is deleted
        var deletedCustomer = await _repository.GetByIdAsync(customer.Id, includeDeleted: true);
        deletedCustomer.Should().NotBeNull();
        deletedCustomer!.IsDeleted.Should().BeTrue();

        // Act - Restore
        deletedCustomer.Restore();
        _repository.Update(deletedCustomer);
        await _context.SaveChangesAsync();

        // Assert - Customer is restored
        var restoredCustomer = await _repository.GetByIdAsync(customer.Id);
        restoredCustomer.Should().NotBeNull();
        restoredCustomer!.IsDeleted.Should().BeFalse();
        restoredCustomer.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task Multiple_Customers_With_Different_PersonalInfo_ShouldAllPersist()
    {
        // Arrange
        var customer1 = await CreateTestCustomerAsync();
        var customer2 = await CreateTestCustomerAsync();
        var customer3 = await CreateTestCustomerAsync();

        // Act
        await _repository.AddAsync(customer1);
        await _repository.AddAsync(customer2);
        await _repository.AddAsync(customer3);
        await _context.SaveChangesAsync();

        // Assert
        var savedCustomer1 = await _repository.GetByIdAsync(customer1.Id);
        var savedCustomer2 = await _repository.GetByIdAsync(customer2.Id);
        var savedCustomer3 = await _repository.GetByIdAsync(customer3.Id);

        savedCustomer1.Should().NotBeNull();
        savedCustomer2.Should().NotBeNull();
        savedCustomer3.Should().NotBeNull();

        // Verify they all have different personal info
        var allCustomers = new[] { savedCustomer1!, savedCustomer2!, savedCustomer3! };
        allCustomers.Select(c => c.Email.Value).Should().OnlyHaveUniqueItems();
        allCustomers.Select(c => $"{c.FirstName.Value}|{c.LastName.Value}|{c.DateOfBirth.Value:yyyy-MM-dd}").Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task BankAccountNumber_ShouldBeValidIban()
    {
        // Arrange
        var customer = await CreateTestCustomerAsync();

        // Act
        await _repository.AddAsync(customer);
        await _context.SaveChangesAsync();

        // Assert
        var savedCustomer = await _repository.GetByIdAsync(customer.Id);
        savedCustomer.Should().NotBeNull();
        
        // Verify the bank account number is a valid IBAN
        savedCustomer!.BankAccountNumber.Value.Should().MatchRegex(@"^[A-Z]{2}\d{2}[A-Z0-9]{4,30}$");
        savedCustomer.BankAccountNumber.Value.Length.Should().BeGreaterThanOrEqualTo(15);
    }

    [Fact]
    public async Task Repository_ShouldHandleHighVolumeOperations()
    {
        // Fix: Create customers sequentially to avoid DbContext concurrency issues
        var customers = new List<CustomerAggregate>();
        
        // Create customers sequentially
        for (int i = 0; i < 10; i++)
        {
            var customer = await CreateTestCustomerAsync($"volume{i}@example.com");
            customers.Add(customer);
        }

        // Save customers in batches to avoid concurrency issues
        foreach (var customer in customers)
        {
            using var scope = _serviceProvider.CreateScope();
            var scopedRepository = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();
            var scopedContext = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
            
            await scopedRepository.AddAsync(customer);
            await scopedContext.SaveChangesAsync();
        }

        // Assert
        var allCustomers = await _context.Customers.CountAsync();
        allCustomers.Should().BeGreaterThanOrEqualTo(10);
    }

    [Fact]
    public async Task Repository_ShouldHandleConcurrentOperations_WithSeparateScopes()
    {
        // This test demonstrates proper concurrent operations using separate DbContext instances
        var tasks = Enumerable.Range(0, 5) // Reduced number for stability
            .Select(async i => await CreateAndSaveCustomerWithSeparateScopeAsync($"concurrent{i}@example.com"))
            .ToArray();

        // Act - This should work because each operation uses its own DbContext
        await Task.WhenAll(tasks);

        // Assert
        var allCustomers = await _context.Customers.CountAsync();
        allCustomers.Should().BeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public async Task Database_ShouldEnforceEmailUniqueness()
    {
        // Arrange
        var duplicateEmail = $"duplicate{Guid.NewGuid():N}@example.com";
        var customer1 = await CreateTestCustomerAsync(duplicateEmail);
        var customer2 = await CreateTestCustomerAsync(duplicateEmail);

        // Act & Assert
        await _repository.AddAsync(customer1);
        // First customer should save successfully since Dapper executes immediately

        // This should throw due to database constraint when trying to add the second customer
        // Since we're using Dapper repository, expect PostgresException instead of DbUpdateException
        var action = async () => await _repository.AddAsync(customer2);
        await action.Should().ThrowAsync<Npgsql.PostgresException>()
            .Where(ex => ex.SqlState == "23505"); // 23505 is the PostgreSQL error code for unique violation
    }

    private async Task<CustomerAggregate> CreateTestCustomerAsync(string? specificEmail = null)
    {
        var uniquenessChecker = _serviceProvider.GetRequiredService<ICustomerUniquenessCheckerService>();
        var dateTimeProvider = _serviceProvider.GetRequiredService<IDateTimeProvider>();

        // Generate unique data for each customer to avoid constraint violations
        var counter = Interlocked.Increment(ref _customerCounter);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var uniqueId = $"{counter}_{timestamp}";

        var uniqueEmail = specificEmail ?? $"test{uniqueId}@example.com";
        var uniqueFirstName = $"John{counter}";
        var uniqueLastName = $"Doe{counter}";
        var uniqueDate = new DateTime(1990, 1, 1).AddDays(counter); // Different birth dates

        // Generate a valid IBAN for testing
        var validIban = GenerateValidTestIban(counter);

        return CustomerAggregate.Create(
            FirstName.Create(uniqueFirstName),
            LastName.Create(uniqueLastName),
            DateOfBirth.Create(uniqueDate),
            PhoneNumber.Create($"+123456789{counter:D2}"), // Unique phone numbers
            Email.Create(uniqueEmail),
            BankAccountNumber.Create(validIban),
            uniquenessChecker,
            dateTimeProvider);
    }

    private async Task<CustomerAggregate> CreateAndSaveCustomerAsync(string email)
    {
        var customer = await CreateTestCustomerAsync(email);
        await _repository.AddAsync(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    private async Task<CustomerAggregate> CreateAndSaveCustomerWithSeparateScopeAsync(string email)
    {
        // Create a separate scope for each concurrent operation
        using var scope = _serviceProvider.CreateScope();
        var scopedRepository = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();
        var scopedContext = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var scopedUniquenessChecker = scope.ServiceProvider.GetRequiredService<ICustomerUniquenessCheckerService>();
        var scopedDateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        // Generate unique data for each customer to avoid constraint violations
        var counter = Interlocked.Increment(ref _customerCounter);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var uniqueId = $"{counter}_{timestamp}";

        var uniqueFirstName = $"John{counter}";
        var uniqueLastName = $"Doe{counter}";
        var uniqueDate = new DateTime(1990, 1, 1).AddDays(counter);
        var validIban = GenerateValidTestIban(counter);

        var customer = CustomerAggregate.Create(
            FirstName.Create(uniqueFirstName),
            LastName.Create(uniqueLastName),
            DateOfBirth.Create(uniqueDate),
            PhoneNumber.Create($"+123456789{counter:D2}"),
            Email.Create(email),
            BankAccountNumber.Create(validIban),
            scopedUniquenessChecker,
            scopedDateTimeProvider);

        await scopedRepository.AddAsync(customer);
        await scopedContext.SaveChangesAsync();
        return customer;
    }

    private static string GenerateValidTestIban(int counter)
    {
        // Use a pool of pre-calculated valid IBANs for different countries
        // These are test IBANs that pass checksum validation
        var validTestIbans = new[]
        {
            "GB82WEST12345698765432", // UK
            "DE89370400440532013000", // Germany
            "FR1420041010050500013M02606", // France
            "IT60X0542811101000000123456", // Italy
            "ES9121000418450200051332", // Spain
            "NL91ABNA0417164300", // Netherlands
            "BE68539007547034", // Belgium
            "AT611904300234573201", // Austria
            "CH9300762011623852957", // Switzerland
            "IE29AIBK93115212345678", // Ireland
            "SE4550000000058398257466", // Sweden
            "NO9386011117947", // Norway
            "DK5000400440116243", // Denmark
            "FI2112345600000785", // Finland
            "LU280019400644750000", // Luxembourg
            "PT50000201231234567890154", // Portugal
            "GR1601101250000000012300695", // Greece
            "CZ6508000000192000145399", // Czech Republic
            "PL61109010140000071219812874", // Poland
            "HU42117730161111101800000000" // Hungary
        };

        // Use modulo to cycle through available IBANs
        return validTestIbans[counter % validTestIbans.Length];
    }

    // Add this test to verify the fix works
    [Fact]
    public async Task GetByEmailAsync_WithValueConversion_ShouldWork()
    {
        // Arrange
        var customer = await CreateTestCustomerAsync();
        await _repository.AddAsync(customer);
        await _context.SaveChangesAsync();

        // Act - Test the repository method
        var result = await _repository.GetByEmailAsync(customer.Email.Value);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(customer.Id);
        result.Email.Value.Should().Be(customer.Email.Value);
    }

    [Fact]
    public async Task GetByEmailAsync_WithDirectDbContextQuery_ShouldWork()
    {
        // Arrange
        var customer = await CreateTestCustomerAsync();
        await _repository.AddAsync(customer);
        await _context.SaveChangesAsync();

        // Act - Test direct query with the fixed approach
        var result = await _context.Customers
            .Where(c => c.Email == Domain.Aggregates.Customer.ValueObjects.Email.Create(customer.Email.Value))
            .FirstOrDefaultAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(customer.Id);
    }

    public void Dispose()
    {
        _context.Dispose();
        _serviceProvider.Dispose();
    }
}