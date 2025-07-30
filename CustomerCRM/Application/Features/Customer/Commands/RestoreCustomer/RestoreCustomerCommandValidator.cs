using FluentValidation;

namespace Application.Features.Customer.Commands.RestoreCustomer;

public class RestoreCustomerCommandValidator : AbstractValidator<RestoreCustomerCommand>
{
    public RestoreCustomerCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Customer ID is required");
    }
}