using Application.Features.Customer.Abstractions;
using Application.Features.Customer.Models;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Infrastructure.Persistence.Repositories;

internal class CustomerReadRepository : ICustomerReadRepository
{
    private readonly string _connectionString;
    private readonly ILogger<CustomerReadRepository> _logger;

    public CustomerReadRepository(IConfiguration configuration, ILogger<CustomerReadRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new ArgumentNullException("Connection string not found");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
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
            WHERE ""Id"" = @Id AND ""IsDeleted"" = false";

        using var connection = new NpgsqlConnection(_connectionString);
        try
        {
            var result = await connection.QueryFirstOrDefaultAsync<CustomerDto>(sql, new { Id = id });
            
            return result;
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "23503") // Foreign key violation
        {
            _logger.LogError(ex, "Database error while retrieving customer {CustomerId}: Foreign key violation", id);
            throw new InvalidOperationException("Unable to retrieve customer due to a database integrity issue.", ex);
        }
        catch (Npgsql.PostgresException ex)
        {
            _logger.LogError(ex, "Database error while retrieving customer {CustomerId}", id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving customer {CustomerId}", id);
            throw;
        }
    }

    public async Task<CustomerDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
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
        try
        {
            var result = await connection.QueryFirstOrDefaultAsync<CustomerDto>(sql, new { Email = email });
            return result;
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "23503") // Foreign key violation
        {
            _logger.LogError(ex, "Database error while retrieving customer by email {Email}: Foreign key violation", email);
            throw new InvalidOperationException("Unable to retrieve customer due to a database integrity issue.", ex);
        }
        catch (Npgsql.PostgresException ex)
        {
            _logger.LogError(ex, "Database error while retrieving customer by email {Email}", email);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving customer by email {Email}", email);
            throw;
        }
    }

    public async Task<PagedResult<CustomerListDto>> GetPagedListAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        bool includeDeleted,
        CancellationToken cancellationToken = default)
    {
        var whereConditions = new List<string>();
        var parameters = new DynamicParameters();

        if (!includeDeleted)
        {
            whereConditions.Add(@"""IsDeleted"" = false");
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            whereConditions.Add(@"""FirstName"" ILIKE @SearchTerm OR ""LastName"" ILIKE @SearchTerm OR ""Email"" ILIKE @SearchTerm");
            parameters.Add("SearchTerm", $"%{searchTerm}%");
        }

        var whereClause = whereConditions.Count > 0 ? $"WHERE {string.Join(" AND ", whereConditions)}" : "";
        
        parameters.Add("Offset", (pageNumber - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        var countSql = $@"
            SELECT COUNT(*) 
            FROM ""Customers"" 
            {whereClause}";

        var dataSql = $@"
            SELECT 
                ""Id"",
                ""FirstName"",
                ""LastName"",
                ""Email"",
                ""CreatedAt"",
                ""IsDeleted""
            FROM ""Customers"" 
            {whereClause}
            ORDER BY ""CreatedAt"" DESC
            LIMIT @PageSize OFFSET @Offset";

        using var connection = new NpgsqlConnection(_connectionString);
        
        var totalCount = await connection.QuerySingleAsync<int>(countSql, parameters);
        var items = await connection.QueryAsync<CustomerListDto>(dataSql, parameters);

        return new PagedResult<CustomerListDto>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}