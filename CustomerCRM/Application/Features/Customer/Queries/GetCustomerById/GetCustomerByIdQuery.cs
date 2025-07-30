using Application.Features.Customer.Models;
using MediatR;

namespace Application.Features.Customer.Queries.GetCustomerById;

public record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDto?>;