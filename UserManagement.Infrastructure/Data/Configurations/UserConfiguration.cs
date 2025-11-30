using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManagement.Domain.Entities;
using UserManagement.Domain.Enums;

namespace UserManagement.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Id)
                .ValueGeneratedOnAdd();

            builder.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(u => u.Email)
                .IsUnique();

            builder.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(u => u.Role)
                .IsRequired()
                .HasConversion(
                    v => v.ToString(),
                    v => (UserRole)Enum.Parse(typeof(UserRole), v))
                .HasMaxLength(20);

            builder.Property(u => u.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(u => u.EmailConfirmed)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(u => u.EmailConfirmationToken)
                .HasMaxLength(100);

            builder.Property(u => u.PasswordResetToken)
                .HasMaxLength(100);

            builder.Property(u => u.ResetTokenExpires);

            builder.Property(u => u.CreatedAt)
                .IsRequired();

            builder.Property(u => u.UpdatedAt);

            builder.HasIndex(u => u.EmailConfirmed);
            builder.HasIndex(u => u.IsActive);
            builder.HasIndex(u => u.Role);
            builder.HasIndex(u => u.CreatedAt);

            builder.HasIndex(u => u.EmailConfirmationToken)
                .HasFilter("\"EmailConfirmationToken\" IS NOT NULL");

            builder.HasIndex(u => u.PasswordResetToken)
                .HasFilter("\"PasswordResetToken\" IS NOT NULL");
        }
    }
}