using Domain.Aggregates.Customer.Services;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Infrastructure.Services;

public class CustomerUniquenessCheckerService : ICustomerUniquenessCheckerService
{
    private readonly string _connectionString;
    private readonly ILogger<CustomerUniquenessCheckerService> _logger;

    public CustomerUniquenessCheckerService(
        IConfiguration configuration,
        ILogger<CustomerUniquenessCheckerService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new ArgumentNullException("Connection string not found");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> IsEmailTaken(string email, Guid? excludeCustomerId = null)
    {
        try
        {
            var sql = @"
                SELECT COUNT(1) 
                FROM ""Customers"" 
                WHERE ""Email"" = @Email 
                AND (@ExcludeCustomerId IS NULL OR ""Id"" != @ExcludeCustomerId)";

            using var connection = new NpgsqlConnection(_connectionString);
            var count = await connection.QuerySingleAsync<int>(sql, new
            {
                Email = email,
                ExcludeCustomerId = excludeCustomerId
            });

            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking email uniqueness for {Email}", email);
            throw;
        }
    }

    public async Task<bool> IsPersonalInfoTaken(
        string firstName,
        string lastName,
        DateTime dateOfBirth,
        Guid? excludeCustomerId = null)
    {
        try
        {
            var sql = @"
                SELECT COUNT(1) 
                FROM ""Customers"" 
                WHERE ""FirstName"" = @FirstName 
                AND ""LastName"" = @LastName 
                AND ""DateOfBirth"" = @DateOfBirth
                AND (@ExcludeCustomerId IS NULL OR ""Id"" != @ExcludeCustomerId)";

            using var connection = new NpgsqlConnection(_connectionString);
            var count = await connection.QuerySingleAsync<int>(sql, new
            {
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dateOfBirth,
                ExcludeCustomerId = excludeCustomerId
            });

            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking personal info uniqueness for {FirstName} {LastName}", firstName, lastName);
            throw;
        }
    }
}