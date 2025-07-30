using Domain.Aggregates.Customer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfiguration;

internal class CustomerEntityConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever()
            .IsRequired();

        // ???? EF Core 9 - ??????? ?? Property ?? ??? ComplexProperty ???? Value Objects
        builder.Property(c => c.FirstName)
            .HasConversion(
                v => v.Value,
                v => Domain.Aggregates.Customer.ValueObjects.FirstName.Create(v))
            .HasColumnName("FirstName")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.LastName)
            .HasConversion(
                v => v.Value,
                v => Domain.Aggregates.Customer.ValueObjects.LastName.Create(v))
            .HasColumnName("LastName")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.DateOfBirth)
            .HasConversion(
                v => v.Value,
                v => Domain.Aggregates.Customer.ValueObjects.DateOfBirth.Create(v))
            .HasColumnName("DateOfBirth")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(c => c.Email)
            .HasConversion(
                v => v.Value,
                v => Domain.Aggregates.Customer.ValueObjects.Email.Create(v))
            .HasColumnName("Email")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(c => c.BankAccountNumber)
            .HasConversion(
                v => v.Value,
                v => Domain.Aggregates.Customer.ValueObjects.BankAccountNumber.Create(v))
            .HasColumnName("BankAccountNumber")
            .HasMaxLength(34)
            .IsRequired();

        // PhoneNumber ???? ?? Owned Type ???? ??? ??? property ????
        builder.OwnsOne(c => c.PhoneNumber, pn =>
        {
            pn.Property(p => p.CountryCode)
                .HasColumnName("PhoneCountryCode")
                .HasMaxLength(5)
                .IsRequired();

            pn.Property(p => p.Number)
                .HasColumnName("PhoneNumber")
                .HasMaxLength(15)
                .IsRequired();
                
            // ??? FullNumber computed property ???? ignore ????
            pn.Ignore(p => p.FullNumber);
        });

        // Audit Properties
        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt);

        // Soft Delete Properties
        builder.Property(c => c.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(c => c.DeletedAt);

        // Global Query Filter for Soft Delete
        builder.HasQueryFilter(c => !c.IsDeleted);

        // Indexes
        builder.HasIndex(c => c.CreatedAt);
        builder.HasIndex(c => c.IsDeleted);
        
        // Email unique index
        builder.HasIndex(c => c.Email)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Composite index for personal info uniqueness
        builder.HasIndex(c => new { c.FirstName, c.LastName, c.DateOfBirth })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Ignore Domain Events and dependencies
        builder.Ignore(c => c.DomainEvents);
        builder.Ignore("_dateTimeProvider");
    }
}