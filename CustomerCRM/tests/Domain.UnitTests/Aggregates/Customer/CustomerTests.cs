using Domain.Aggregates.Customer;
using Domain.Aggregates.Customer.Events;
using Domain.Aggregates.Customer.Services;
using Domain.Aggregates.Customer.ValueObjects;
using Domain.SeedWork.Exceptions;
using FluentAssertions;
using Moq;
using Xunit;

namespace Domain.UnitTests.Aggregates.Customer;

public class CustomerTests
{
    private readonly Mock<ICustomerUniquenessCheckerService> _mockUniquenessChecker;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly DateTime _fixedDateTime = new(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

    public CustomerTests()
    {
        _mockUniquenessChecker = new Mock<ICustomerUniquenessCheckerService>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();
        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(_fixedDateTime);
    }

    [Fact]
    public void Create_WithValidData_ShouldCreateCustomer()
    {
        // Arrange
        var firstName = FirstName.Create("John");
        var lastName = LastName.Create("Doe");
        var dateOfBirth = DateOfBirth.Create(new DateTime(1990, 1, 1));
        var phoneNumber = PhoneNumber.Create("+1234567890");
        var email = Email.Create("john.doe@example.com");
        var bankAccountNumber = BankAccountNumber.Create("GB82WEST12345698765432");

        _mockUniquenessChecker.Setup(x => x.IsEmailUniqueAsync(It.IsAny<Email>(), It.IsAny<Guid?>()))
            .ReturnsAsync(true);
        _mockUniquenessChecker.Setup(x => x.IsPersonalInfoUniqueAsync(It.IsAny<FirstName>(), It.IsAny<LastName>(), It.IsAny<DateOfBirth>(), It.IsAny<Guid?>()))
            .ReturnsAsync(true);

        // Act
        var customer = Customer.Create(
            firstName,
            lastName,
            dateOfBirth,
            phoneNumber,
            email,
            bankAccountNumber,
            _mockUniquenessChecker.Object,
            _mockDateTimeProvider.Object);

        // Assert
        customer.Should().NotBeNull();
        customer.Id.Should().NotBe(Guid.Empty);
        customer.FirstName.Should().Be(firstName);
        customer.LastName.Should().Be(lastName);
        customer.DateOfBirth.Should().Be(dateOfBirth);
        customer.PhoneNumber.Should().Be(phoneNumber);
        customer.Email.Should().Be(email);
        customer.BankAccountNumber.Should().Be(bankAccountNumber);
        customer.CreatedAt.Should().Be(_fixedDateTime);
        customer.IsDeleted.Should().BeFalse();
        customer.DomainEvents.Should().HaveCount(1);
        customer.DomainEvents.First().Should().BeOfType<CustomerCreatedDomainEvent>();
    }

    [Fact]
    public void Delete_WhenNotDeleted_ShouldMarkAsDeletedAndRaiseDomainEvent()
    {
        // Arrange
        var customer = CreateValidCustomer();

        // Act
        customer.Delete();

        // Assert
        customer.IsDeleted.Should().BeTrue();
        customer.DeletedAt.Should().Be(_fixedDateTime);
        customer.DomainEvents.Should().HaveCount(2); // Create + Delete events
        customer.DomainEvents.Last().Should().BeOfType<CustomerDeletedDomainEvent>();
    }

    [Fact]
    public void Restore_WhenDeleted_ShouldRestoreCustomerAndRaiseDomainEvent()
    {
        // Arrange
        var customer = CreateValidCustomer();
        customer.Delete();

        // Act
        customer.Restore();

        // Assert
        customer.IsDeleted.Should().BeFalse();
        customer.DeletedAt.Should().BeNull();
        customer.UpdatedAt.Should().Be(_fixedDateTime);
        customer.DomainEvents.Should().HaveCount(3); // Create + Delete + Restore events
        customer.DomainEvents.Last().Should().BeOfType<CustomerRestoredDomainEvent>();
    }

    private Customer CreateValidCustomer()
    {
        _mockUniquenessChecker.Setup(x => x.IsEmailUniqueAsync(It.IsAny<Email>(), It.IsAny<Guid?>()))
            .ReturnsAsync(true);
        _mockUniquenessChecker.Setup(x => x.IsPersonalInfoUniqueAsync(It.IsAny<FirstName>(), It.IsAny<LastName>(), It.IsAny<DateOfBirth>(), It.IsAny<Guid?>()))
            .ReturnsAsync(true);

        return Customer.Create(
            FirstName.Create("John"),
            LastName.Create("Doe"),
            DateOfBirth.Create(new DateTime(1990, 1, 1)),
            PhoneNumber.Create("+1234567890"),
            Email.Create("john.doe@example.com"),
            BankAccountNumber.Create("GB82WEST12345698765432"),
            _mockUniquenessChecker.Object,
            _mockDateTimeProvider.Object);
    }
}