using Microsoft.EntityFrameworkCore;  
using Microsoft.EntityFrameworkCore.Metadata.Builders;  
using Restaurante.AuthService.Domain.Entities;

namespace Restaurante.AuthService.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>  
{  
    public void Configure(EntityTypeBuilder<User> builder)  
    {  
        builder.ToTable("users");  
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)  
            .HasColumnName("id")  
            .ValueGeneratedNever(); // GUID controlado por el dominio en la inicialización

        builder.Property(u => u.Email)  
            .HasColumnName("email")  
            .IsRequired()  
            .HasMaxLength(256);

        builder.Property(u => u.PasswordHash)  
            .HasColumnName("password_hash")  
            .IsRequired()  
            .HasMaxLength(512);

        builder.Property(u => u.Role)  
            .HasColumnName("role")  
            .IsRequired()  
            .HasMaxLength(64);

        builder.Property(u => u.IsActive)  
            .HasColumnName("is_active")  
            .IsRequired()  
            .HasDefaultValue(true);

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(u => u.MongoId)
            .HasColumnName("mongo_id")
            .HasMaxLength(64);

        builder.Property(u => u.Username)
            .HasColumnName("username")
            .HasMaxLength(50);

        builder.Property(u => u.Name)
            .HasColumnName("name")
            .HasMaxLength(100);

        builder.Property(u => u.Surname)
            .HasColumnName("surname")
            .HasMaxLength(100);

        builder.Property(u => u.Phone)
            .HasColumnName("phone")
            .HasMaxLength(20);

        builder.Property(u => u.EmailVerified)
            .HasColumnName("email_verified")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.EmailVerificationToken)
            .HasColumnName("email_verification_token")
            .HasMaxLength(256);

        builder.Property(u => u.EmailVerificationTokenExpiry)
            .HasColumnName("email_verification_token_expiry");

        builder.Property(u => u.PasswordResetToken)
            .HasColumnName("password_reset_token")
            .HasMaxLength(256);

        builder.Property(u => u.PasswordResetTokenExpiry)
            .HasColumnName("password_reset_token_expiry");

        builder.Property(u => u.RefreshToken)
            .HasColumnName("refresh_token")
            .HasMaxLength(256);

        builder.Property(u => u.RefreshTokenExpiry)
            .HasColumnName("refresh_token_expiry");

        builder.Property(u => u.CompanyMongoId)
            .HasColumnName("company_mongo_id")
            .HasMaxLength(64);

        builder.Property(u => u.BranchMongoId)
            .HasColumnName("branch_mongo_id")
            .HasMaxLength(64);

        builder.Property(u => u.VerificationOtp)
            .HasColumnName("verification_otp")
            .HasMaxLength(6);

        builder.Property(u => u.VerificationOtpExpiry)
            .HasColumnName("verification_otp_expiry");

        builder.HasIndex(u => u.Email)  
            .IsUnique()  
            .HasDatabaseName("ix_users_email_unique");  

        builder.HasIndex(u => u.Username)
            .IsUnique()
            .HasFilter("username IS NOT NULL")
            .HasDatabaseName("ix_users_username_unique");

        builder.HasIndex(u => u.EmailVerificationToken)
            .IsUnique()
            .HasFilter("email_verification_token IS NOT NULL")
            .HasDatabaseName("ix_users_email_verification_token_unique");

        builder.HasIndex(u => u.PasswordResetToken)
            .IsUnique()
            .HasFilter("password_reset_token IS NOT NULL")
            .HasDatabaseName("ix_users_password_reset_token_unique");

        builder.HasIndex(u => u.RefreshToken)
            .IsUnique()
            .HasFilter("refresh_token IS NOT NULL")
            .HasDatabaseName("ix_users_refresh_token_unique");

        builder.HasIndex(u => u.VerificationOtp)
            .IsUnique()
            .HasFilter("verification_otp IS NOT NULL")
            .HasDatabaseName("ix_users_verification_otp_unique");
    }  
}
