using Domain.Aggregates.Customer;
using Domain.Aggregates.Customer.Events;
using Domain.Aggregates.Customer.Services;
using Domain.Aggregates.Customer.ValueObjects;
using Domain.SeedWork.Exceptions;
using FluentAssertions;
using Moq;
using Xunit;
using CustomerAggregate = Domain.Aggregates.Customer.Customer;

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

        _mockUniquenessChecker.Setup(x => x.IsEmailTaken(email.Value, It.IsAny<Guid?>()))
            .ReturnsAsync(false); // false means email is NOT taken (unique)
        _mockUniquenessChecker.Setup(x => x.IsPersonalInfoTaken(firstName.Value, lastName.Value, dateOfBirth.Value, It.IsAny<Guid?>()))
            .ReturnsAsync(false); // false means personal info is NOT taken (unique)

        // Act
        var customer = CustomerAggregate.Create(
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
    public void Create_WithDuplicateEmail_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var firstName = FirstName.Create("John");
        var lastName = LastName.Create("Doe");
        var dateOfBirth = DateOfBirth.Create(new DateTime(1990, 1, 1));
        var phoneNumber = PhoneNumber.Create("+1234567890");
        var email = Email.Create("john.doe@example.com");
        var bankAccountNumber = BankAccountNumber.Create("GB82WEST12345698765432");

        _mockUniquenessChecker.Setup(x => x.IsEmailTaken(email.Value, It.IsAny<Guid?>()))
            .ReturnsAsync(true); // true means email IS taken (not unique)

        // Act & Assert
        var action = () => CustomerAggregate.Create(
            firstName,
            lastName,
            dateOfBirth,
            phoneNumber,
            email,
            bankAccountNumber,
            _mockUniquenessChecker.Object,
            _mockDateTimeProvider.Object);

        action.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("*email*unique*");
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

    [Fact]
    public void ChangeEmail_WithValidEmail_ShouldUpdateEmailAndRaiseDomainEvent()
    {
        // Arrange
        var customer = CreateValidCustomer();
        var newEmail = Email.Create("newemail@example.com");

        _mockUniquenessChecker.Setup(x => x.IsEmailTaken(newEmail.Value, customer.Id))
            .ReturnsAsync(false); // New email is unique

        // Act
        customer.ChangeEmail(newEmail, _mockUniquenessChecker.Object);

        // Assert
        customer.Email.Should().Be(newEmail);
        customer.UpdatedAt.Should().Be(_fixedDateTime);
        customer.DomainEvents.Should().HaveCount(2); // Create + Update events
        customer.DomainEvents.Last().Should().BeOfType<CustomerUpdatedDomainEvent>();
    }

    [Fact]
    public void Delete_WhenAlreadyDeleted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var customer = CreateValidCustomer();
        customer.Delete(); // First deletion

        // Act & Assert
        var action = () => customer.Delete();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Customer is already deleted");
    }

    private CustomerAggregate CreateValidCustomer()
    {
        _mockUniquenessChecker.Setup(x => x.IsEmailTaken(It.IsAny<string>(), It.IsAny<Guid?>()))
            .ReturnsAsync(false); // Email is unique
        _mockUniquenessChecker.Setup(x => x.IsPersonalInfoTaken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<Guid?>()))
            .ReturnsAsync(false); // Personal info is unique

        return CustomerAggregate.Create(
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