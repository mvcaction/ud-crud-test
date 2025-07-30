using Application.Common;
using MediatR;

namespace Application.Features.Customer.Commands.DeleteCustomer;

public record DeleteCustomerCommand(Guid Id) : IRequest<Result>;