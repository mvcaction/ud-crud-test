using Application.Common;
using MediatR;

namespace Application.Features.Customer.Commands.RestoreCustomer;

public record RestoreCustomerCommand(Guid Id) : IRequest<Result>;