using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class ServiceRegistrationConfiguration : IEntityTypeConfiguration<ServiceRegistration>
{
    public void Configure(EntityTypeBuilder<ServiceRegistration> builder)
    {
        builder.ToTable("service_registrations");

        builder.HasKey(x => x.Id);
        
        #region Relacionamentos

        builder.Property(x => x.RetreatId)
            .HasColumnName("retreat_id")
            .IsRequired();

        builder.HasOne<Retreat>()
            .WithMany()
            .HasForeignKey(x => x.RetreatId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.PreferredSpaceId)
            .HasColumnName("preferred_space_id");

        builder.HasOne<ServiceSpace>()
            .WithMany()
            .HasForeignKey(x => x.PreferredSpaceId)
            .OnDelete(DeleteBehavior.Restrict);

        #endregion

        #region Informações Básicas

        builder.OwnsOne(x => x.Name, fn =>
        {
            fn.Property(p => p.Value)
              .HasColumnName("full_name")
              .HasMaxLength(160)
              .IsRequired();
        });

        builder.Property(x => x.Cpf)
            .HasConversion(
                toProvider => toProvider.Value,
                fromProvider => new CPF(fromProvider)
            )
            .HasColumnName("cpf")
            .HasMaxLength(11)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasConversion(
                toProvider => toProvider.Value,
                fromProvider => new EmailAddress(fromProvider)
            )
            .HasColumnName("email")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(x => x.Phone)
            .HasColumnName("phone")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.BirthDate)
            .HasColumnName("birth_date")
            .IsRequired();

        builder.Property(x => x.Gender)
            .HasColumnName("gender")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.City)
            .HasColumnName("city")
            .HasMaxLength(120)
            .IsRequired();

        #endregion

        #region Controle e Status

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.Enabled)
            .HasColumnName("enabled")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.RegistrationDate)
            .HasColumnName("registration_date")
            .IsRequired();

        #endregion

        #region Termos e LGPD

        builder.Property(x => x.TermsAccepted)
            .HasColumnName("terms_accepted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.TermsAcceptedAt)
            .HasColumnName("terms_accepted_at");

        builder.Property(x => x.TermsVersion)
            .HasColumnName("terms_version")
            .HasMaxLength(50);

        builder.Property(x => x.MarketingOptIn)
            .HasColumnName("marketing_opt_in")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.MarketingOptInAt)
            .HasColumnName("marketing_opt_in_at");

        builder.Property(x => x.ClientIp)
            .HasColumnName("client_ip")
            .HasMaxLength(45);

        builder.Property(x => x.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(512);

        #endregion

        #region Dados Complementares

        builder.Property(x => x.MaritalStatus)
            .HasColumnName("marital_status")
            .HasConversion<string>();

        builder.Property(x => x.Pregnancy)
            .HasColumnName("pregnancy")
            .HasConversion<string>()
            .HasDefaultValue(PregnancyStatus.None)
            .IsRequired();

        builder.Property(x => x.ShirtSize)
            .HasColumnName("shirt_size")
            .HasConversion<string>();

        builder.Property(x => x.WeightKg)
            .HasColumnName("weight_kg")
            .HasPrecision(5, 2);

        builder.Property(x => x.HeightCm)
            .HasColumnName("height_cm")
            .HasPrecision(5, 2);

        builder.Property(x => x.Profession)
            .HasColumnName("profession")
            .HasMaxLength(120);

        builder.Property(x => x.EducationLevel)
            .HasColumnName("education_level")
            .HasConversion<string>();

        #endregion

        #region Endereço e Contato

        builder.Property(x => x.StreetAndNumber)
            .HasColumnName("street_and_number")
            .HasMaxLength(160);

        builder.Property(x => x.Neighborhood)
            .HasColumnName("neighborhood")
            .HasMaxLength(120);

        builder.Property(x => x.State)
            .HasColumnName("state")
            .HasConversion<string>();

        builder.Property(x => x.PostalCode)
            .HasColumnName("postal_code")
            .HasMaxLength(10);

        builder.Property(x => x.Whatsapp)
            .HasColumnName("whatsapp")
            .HasMaxLength(20);

        #endregion

        #region Experiência Rahamim

        builder.Property(x => x.RahaminVidaCompleted)
            .HasColumnName("rahamin_vida_completed")
            .HasConversion<string>()
            .HasDefaultValue(RahaminVidaEdition.None)
            .IsRequired();

        builder.Property(x => x.PreviousUncalledApplications)
            .HasColumnName("prev_uncalled_applications")
            .HasConversion<string>()
            .HasDefaultValue(RahaminAttempt.None)
            .IsRequired();

        builder.Property(x => x.PostRetreatLifeSummary)
            .HasColumnName("post_retreat_life_summary")
            .HasMaxLength(1000);

        #endregion

        #region Vida Pessoal e Espiritual

        builder.Property(x => x.ChurchLifeDescription)
            .HasColumnName("church_life_description")
            .HasMaxLength(1000);

        builder.Property(x => x.PrayerLifeDescription)
            .HasColumnName("prayer_life_description")
            .HasMaxLength(1000);

        builder.Property(x => x.FamilyRelationshipDescription)
            .HasColumnName("family_relationship_description")
            .HasMaxLength(1000);

        builder.Property(x => x.SelfRelationshipDescription)
            .HasColumnName("self_relationship_description")
            .HasMaxLength(1000);

        #endregion

        #region Foto

        builder.OwnsOne(x => x.PhotoUrl, p =>
        {
            p.Property(u => u.Value)
             .HasColumnName("photo_url")
             .HasMaxLength(512);
        });

        builder.Property(x => x.PhotoStorageKey)
            .HasColumnName("photo_storage_key")
            .HasMaxLength(300);

        builder.Property(x => x.PhotoContentType)
            .HasColumnName("photo_content_type")
            .HasMaxLength(60);

        builder.Property(x => x.PhotoSizeBytes)
            .HasColumnName("photo_size_bytes");

        builder.Property(x => x.PhotoUploadedAt)
            .HasColumnName("photo_uploaded_at");

        #endregion

        #region Índices

        builder.HasIndex(e => new { e.RetreatId, e.Cpf }).IsUnique();
        builder.HasIndex(e => new { e.RetreatId, e.Email }).IsUnique();
        builder.HasIndex(e => new { e.RetreatId, e.Status, e.Gender });
        builder.HasIndex(e => e.PreferredSpaceId);
        builder.HasIndex(e => e.Cpf);
        builder.HasIndex(e => e.Email);

        #endregion
    }
}
