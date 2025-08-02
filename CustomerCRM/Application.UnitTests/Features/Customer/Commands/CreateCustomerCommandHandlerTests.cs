using Application.Common;
using Application.Features.Customer.Abstractions;
using Application.Features.Customer.Commands.CreateCustomer;
using Domain.Aggregates.Customer.Services;
using Domain.SeedWork.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using CustomerAggregate = Domain.Aggregates.Customer.Customer;

namespace Application.UnitTests.Features.Customer.Commands;

public class CreateCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _mockRepository;
    private readonly Mock<ICustomerUniquenessCheckerService> _mockUniquenessChecker;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<CreateCustomerCommandHandler>> _mockLogger;
    private readonly CreateCustomerCommandHandler _handler;

    public CreateCustomerCommandHandlerTests()
    {
        _mockRepository = new Mock<ICustomerRepository>();
        _mockUniquenessChecker = new Mock<ICustomerUniquenessCheckerService>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<CreateCustomerCommandHandler>>();

        _handler = new CreateCustomerCommandHandler(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockUniquenessChecker.Object,
            _mockDateTimeProvider.Object,
            _mockLogger.Object);

        _mockDateTimeProvider.Setup(x => x.UtcNow)
            .Returns(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnSuccessResult()
    {
        // Arrange
        var command = new CreateCustomerCommand(
            FirstName: "John",
            LastName: "Doe",
            DateOfBirth: new DateTime(1990, 1, 1),
            PhoneNumber: "+1234567890",
            Email: "john.doe@example.com",
            BankAccountNumber: "GB82WEST12345698765432"
        );

        _mockUniquenessChecker.Setup(x => x.IsEmailTaken("john.doe@example.com", It.IsAny<Guid?>()))
            .ReturnsAsync(false); // false = email is NOT taken (unique)
        _mockUniquenessChecker.Setup(x => x.IsPersonalInfoTaken("John", "Doe", new DateTime(1990, 1, 1), It.IsAny<Guid?>()))
            .ReturnsAsync(false); // false = personal info is NOT taken (unique)

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);

        _mockRepository.Verify(x => x.AddAsync(It.IsAny<CustomerAggregate>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ShouldReturnFailureResult()
    {
        // Arrange
        var command = new CreateCustomerCommand(
            FirstName: "John",
            LastName: "Doe",
            DateOfBirth: new DateTime(1990, 1, 1),
            PhoneNumber: "+1234567890",
            Email: "john.doe@example.com",
            BankAccountNumber: "GB82WEST12345698765432"
        );

        _mockUniquenessChecker.Setup(x => x.IsEmailTaken("john.doe@example.com", It.IsAny<Guid?>()))
            .ReturnsAsync(true); // true = email IS taken (not unique)
        _mockUniquenessChecker.Setup(x => x.IsPersonalInfoTaken("John", "Doe", new DateTime(1990, 1, 1), It.IsAny<Guid?>()))
            .ReturnsAsync(false); // Personal info is unique

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("email");

        _mockRepository.Verify(x => x.AddAsync(It.IsAny<CustomerAggregate>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithDuplicatePersonalInfo_ShouldReturnFailureResult()
    {
        // Arrange
        var command = new CreateCustomerCommand(
            FirstName: "John",
            LastName: "Doe",
            DateOfBirth: new DateTime(1990, 1, 1),
            PhoneNumber: "+1234567890",
            Email: "john.doe@example.com",
            BankAccountNumber: "GB82WEST12345698765432"
        );

        _mockUniquenessChecker.Setup(x => x.IsEmailTaken("john.doe@example.com", It.IsAny<Guid?>()))
            .ReturnsAsync(false); // Email is unique
        _mockUniquenessChecker.Setup(x => x.IsPersonalInfoTaken("John", "Doe", new DateTime(1990, 1, 1), It.IsAny<Guid?>()))
            .ReturnsAsync(true); // true = personal info IS taken (not unique)

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("personal");

        _mockRepository.Verify(x => x.AddAsync(It.IsAny<CustomerAggregate>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidValueObject_ShouldReturnFailureResult()
    {
        // Arrange
        var command = new CreateCustomerCommand(
            FirstName: "", // Invalid: empty first name
            LastName: "Doe",
            DateOfBirth: new DateTime(1990, 1, 1),
            PhoneNumber: "+1234567890",
            Email: "john.doe@example.com",
            BankAccountNumber: "GB82WEST12345698765432"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeEmpty();

        _mockRepository.Verify(x => x.AddAsync(It.IsAny<CustomerAggregate>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("", "Doe", "2000-01-01", "+1234567890", "john@example.com", "GB82WEST12345698765432")] // Empty FirstName
    [InlineData("John", "", "2000-01-01", "+1234567890", "john@example.com", "GB82WEST12345698765432")] // Empty LastName
    [InlineData("John", "Doe", "2000-01-01", "", "john@example.com", "GB82WEST12345698765432")] // Empty PhoneNumber
    [InlineData("John", "Doe", "2000-01-01", "+1234567890", "invalid-email", "GB82WEST12345698765432")] // Invalid Email
    [InlineData("John", "Doe", "2000-01-01", "+1234567890", "john@example.com", "")] // Empty BankAccountNumber
    public async Task Handle_WithInvalidData_ShouldReturnFailureResult(
        string firstName, string lastName, string dateOfBirth,
        string phoneNumber, string email, string bankAccountNumber)
    {
        // Arrange
        var command = new CreateCustomerCommand(
            FirstName: firstName,
            LastName: lastName,
            DateOfBirth: DateTime.Parse(dateOfBirth),
            PhoneNumber: phoneNumber,
            Email: email,
            BankAccountNumber: bankAccountNumber
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeEmpty();

        _mockRepository.Verify(x => x.AddAsync(It.IsAny<CustomerAggregate>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}