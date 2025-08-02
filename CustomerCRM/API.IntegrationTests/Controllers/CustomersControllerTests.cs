using Application.Features.Customer.Commands.CreateCustomer;
using Application.Features.Customer.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
namespace API.IntegrationTests.Controllers;

public class CustomersControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CustomersControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateCustomer_WithValidData_ShouldReturnCreatedResult()
    {
        // Arrange
        var randomId = Guid.NewGuid().ToString().Substring(0, 8);
        var command = new CreateCustomerCommand(
            FirstName: $"John-{randomId}",  
            LastName: "Doe",
            DateOfBirth: new DateTime(1990, 1, 1),
            PhoneNumber: "+1234567890",
            Email: $"john.doe.{Guid.NewGuid()}@example.com",
            BankAccountNumber: "GB82WEST12345698765432"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/customers", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var customerId = await response.Content.ReadFromJsonAsync<Guid>();
        customerId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task GetCustomer_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/customers/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]  
    public async Task CreateCustomer_WithInvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var randomId = Guid.NewGuid().ToString().Substring(0, 8);
        var command = new CreateCustomerCommand(
            FirstName: $"John-{randomId}",  // Make FirstName unique for each test run
            LastName: "Doe",
            DateOfBirth: new DateTime(1990, 1, 1),
            PhoneNumber: "+1234567890",
            Email: "invalid-email", // Invalid email format
            BankAccountNumber: "GB82WEST12345698765432"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/customers", command);

        // Assert - This will work when the API properly returns BadRequest instead of throwing exception
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCustomer_WithEmptyFirstName_ShouldReturnBadRequest()
    {
        // Arrange
        var randomId = Guid.NewGuid().ToString().Substring(0, 8);
        var command = new CreateCustomerCommand(
            FirstName: "", // Empty first name
            LastName: "Doe",
            DateOfBirth: new DateTime(1990, 1, 1),
            PhoneNumber: "+1234567890",
            Email: $"test.{randomId}@example.com",  // Make email unique for each test run
            BankAccountNumber: "GB82WEST12345698765432"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/customers", command);

        // Assert - This will work when the API properly returns BadRequest instead of throwing exception
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCustomer_ThenUpdate_ShouldReturnUpdatedData()
    {
        // Arrange - Create a customer first
        var randomId = Guid.NewGuid().ToString().Substring(0, 8);
        var createCommand = new CreateCustomerCommand(
            FirstName: $"Jane-{randomId}",  // Make FirstName unique for each test run
            LastName: "Smith",
            DateOfBirth: new DateTime(1985, 5, 15),
            PhoneNumber: "+1987654321",
            Email: $"jane.smith.{Guid.NewGuid()}@example.com",
            BankAccountNumber: "GB82WEST12345698765432"
        );

        var createResponse = await _client.PostAsJsonAsync("/api/customers", createCommand);
        createResponse.EnsureSuccessStatusCode();
        var customerId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Act - Get the created customer
        var getResponse = await _client.GetAsync($"/api/customers/{customerId}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var raw = await getResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"Raw response ({raw.Length} chars): {raw}");
        Console.WriteLine($"Content type: {getResponse.Content.Headers.ContentType}");

        if (!raw.TrimStart().StartsWith("{") && !raw.TrimStart().StartsWith("["))
        {
            Assert.Fail($"Expected JSON response but got: {raw}");
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var customer = await getResponse.Content.ReadFromJsonAsync<CustomerDto>(options);
            customer.Should().NotBeNull();
            customer!.FirstName.Should().Be(createCommand.FirstName);  // Updated to match the randomized name
            customer.LastName.Should().Be("Smith");
            customer.Email.Should().Be(createCommand.Email);
        }
        catch (JsonException ex)
        {
            Assert.Fail($"Failed to deserialize JSON: {ex.Message}\nRaw content: {raw}");
        }
    }

    [Fact]
    public async Task DeleteCustomer_ThenGet_ShouldReturnNotFound()
    {
        // Arrange - Create a customer first with unique data
        var randomId = Guid.NewGuid().ToString().Substring(0, 8);
        var createCommand = new CreateCustomerCommand(
            FirstName: $"ToDelete-{randomId}",  // Make FirstName unique for each test run
            LastName: "User",
            DateOfBirth: new DateTime(1980, 1, 1),
            PhoneNumber: "+1111111111",
            Email: $"todelete.{Guid.NewGuid()}@example.com",
            BankAccountNumber: "GB82WEST12345698765432"
        );

        var createResponse = await _client.PostAsJsonAsync("/api/customers", createCommand);

        // Ensure creation was successful before proceeding
        createResponse.EnsureSuccessStatusCode();
        var customerId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Act - Delete the customer
        var deleteResponse = await _client.DeleteAsync($"/api/customers/{customerId}");

        // Assert - Delete should succeed
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act - Try to get the deleted customer
        var getResponse = await _client.GetAsync($"/api/customers/{customerId}");

        // Assert - Should return NotFound due to soft delete
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RestoreCustomer_AfterDeletion_ShouldMakeCustomerAccessibleAgain()
    {
        // Create a customer with same format as the working test
        var randomId = Guid.NewGuid().ToString().Substring(0, 8);
        var createCommand = new CreateCustomerCommand(
            FirstName: $"ToRestore-{randomId}",  // Make FirstName unique for each test run
            LastName: "User",
            DateOfBirth: new DateTime(1980, 1, 1),
            PhoneNumber: "+1111111111",
            Email: $"torestore.{Guid.NewGuid()}@example.com",
            BankAccountNumber: "GB82WEST12345698765432"
        );

        var createResponse = await _client.PostAsJsonAsync("/api/customers", createCommand);
        Console.WriteLine($"Create status: {createResponse.StatusCode}");

        if (!createResponse.IsSuccessStatusCode)
        {
            var content = await createResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"Create response: {content}");
            Assert.Fail($"PENDING FEATURE: Customer creation failed: {content} - Fix the database entity model first");
        }

        var customerId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Delete the customer
        var deleteResponse = await _client.DeleteAsync($"/api/customers/{customerId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getDeletedResponse = await _client.GetAsync($"/api/customers/{customerId}");
        getDeletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Try to restore the customer
        HttpResponseMessage restoreResponse;
        restoreResponse = await _client.PostAsync($"/api/customers/{customerId}/restore", null);
        Console.WriteLine($"POST restore status: {restoreResponse.StatusCode}");

        if (restoreResponse.StatusCode == HttpStatusCode.NotFound ||
            restoreResponse.StatusCode == HttpStatusCode.MethodNotAllowed)
        {
            var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
            restoreResponse = await _client.PatchAsync($"/api/customers/{customerId}/restore", content);
            Console.WriteLine($"PATCH restore status: {restoreResponse.StatusCode}");
        }

        if (restoreResponse.StatusCode == HttpStatusCode.NotFound)
        {
            Console.WriteLine("NOTICE: Restore endpoint not implemented yet");
            Assert.Fail("PENDING FEATURE: Restore endpoint not implemented yet - this test will pass when the feature is ready");
        }

        restoreResponse.IsSuccessStatusCode.Should().BeTrue(
            $"Restore operation should succeed. Got status code: {restoreResponse.StatusCode}");

        var getResponse = await _client.GetAsync($"/api/customers/{customerId}");
        var responseContent = await getResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"Get after restore: {getResponse.StatusCode}, Content: {responseContent}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "Customer should be accessible after restoration");
    }

    [Fact]
    public async Task DeleteCustomer_DebugTest_ShouldShowDeletedState()
    {
        // Arrange - Create a customer first with unique data
        var randomId = Guid.NewGuid().ToString().Substring(0, 8);
        var createCommand = new CreateCustomerCommand(
            FirstName: $"ToDelete-{randomId}",
            LastName: "User",
            DateOfBirth: new DateTime(1980, 1, 1),
            PhoneNumber: "+1111111111",
            Email: $"todelete.{Guid.NewGuid()}@example.com",
            BankAccountNumber: "GB82WEST12345698765432"
        );

        var createResponse = await _client.PostAsJsonAsync("/api/customers", createCommand);
        createResponse.EnsureSuccessStatusCode();
        var customerId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Get the customer before deletion to confirm it exists
        var getBeforeDelete = await _client.GetAsync($"/api/customers/{customerId}");
        getBeforeDelete.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var customerBeforeDelete = await getBeforeDelete.Content.ReadFromJsonAsync<CustomerDto>();
        customerBeforeDelete.Should().NotBeNull();
        customerBeforeDelete!.IsDeleted.Should().BeFalse();

        // Act - Delete the customer
        var deleteResponse = await _client.DeleteAsync($"/api/customers/{customerId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act - Try to get the deleted customer
        var getResponse = await _client.GetAsync($"/api/customers/{customerId}");
        
        // Debug: Log the actual response
        var responseContent = await getResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"After delete - Status: {getResponse.StatusCode}");
        Console.WriteLine($"After delete - Content: {responseContent}");
        
        if (getResponse.StatusCode == HttpStatusCode.OK)
        {
            var customerAfterDelete = await getResponse.Content.ReadFromJsonAsync<CustomerDto>();
            Console.WriteLine($"IsDeleted flag: {customerAfterDelete?.IsDeleted}");
            Console.WriteLine($"DeletedAt: {customerAfterDelete?.DeletedAt}");
        }

        // This should be NotFound, but let's see what we actually get
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

// Enhanced CustomWebApplicationFactory that completely bypasses deps.json issues
public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Override logging to reduce noise during tests
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        });

        builder.UseEnvironment("Testing");

        // Use the API project's content root instead of the test project's
        var apiProjectPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "API"));
        if (Directory.Exists(apiProjectPath))
        {
            builder.UseContentRoot(apiProjectPath);
        }

        // Disable host configuration validation that might require deps.json
        builder.UseSetting("hostBuilder:reloadConfigOnChange", "false");
    }
}