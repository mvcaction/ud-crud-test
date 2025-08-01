using Infrastructure.Resilience;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Services;

public interface IExternalCustomerValidationService
{
    Task<bool> ValidateCustomerAsync(string email, CancellationToken cancellationToken = default);
    Task<CustomerCreditScore> GetCreditScoreAsync(string customerId, CancellationToken cancellationToken = default);
}

public record CustomerCreditScore(int Score, string Rating, DateTime ValidUntil);

internal class ExternalCustomerValidationService : IExternalCustomerValidationService
{
    private readonly IResilienceService _resilienceService;
    private readonly ILogger<ExternalCustomerValidationService> _logger;

    public ExternalCustomerValidationService(
        IResilienceService resilienceService,
        ILogger<ExternalCustomerValidationService> logger)
    {
        _resilienceService = resilienceService;
        _logger = logger;
    }

    public async Task<bool> ValidateCustomerAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _resilienceService.ExecuteWithResilienceAsync(async () =>
        {
            // Simulate external API call
            var httpClient = _resilienceService.GetResilientHttpClient("external-api");
            
            _logger.LogInformation("Validating customer email with external service: {Email}", email);
            
            // Example external API call
            var response = await httpClient.GetAsync(
                $"/api/validate?email={Uri.EscapeDataString(email)}", 
                cancellationToken);
            
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ExternalValidationResult>(content);
            
            return result?.IsValid ?? false;
            
        }, $"ValidateCustomer-{email}");
    }

    public async Task<CustomerCreditScore> GetCreditScoreAsync(string customerId, CancellationToken cancellationToken = default)
    {
        return await _resilienceService.ExecuteWithResilienceAsync(async () =>
        {
            var httpClient = _resilienceService.GetResilientHttpClient("external-api");
            
            _logger.LogInformation("Getting credit score for customer: {CustomerId}", customerId);
            
            var response = await httpClient.GetAsync(
                $"/api/credit-score/{customerId}", 
                cancellationToken);
            
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<CustomerCreditScore>(content);
            
            return result ?? throw new InvalidOperationException("Invalid credit score response");
            
        }, $"GetCreditScore-{customerId}");
    }
}

internal record ExternalValidationResult(bool IsValid, string Reason);