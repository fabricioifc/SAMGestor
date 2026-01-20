using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class RetreatConfiguration : IEntityTypeConfiguration<Retreat>
{
    public void Configure(EntityTypeBuilder<Retreat> builder)
    {
        builder.ToTable("retreats");
        builder.HasKey(r => r.Id);
        
        builder.Navigation(r => r.Images)
               .HasField("_images")
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(r => r.EmergencyCodes)
               .HasField("_emergencyCodes")
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        #region Informações Básicas

        builder.OwnsOne(r => r.Name, n =>
        {
            n.Property(p => p.Value)
             .HasColumnName("name")
             .HasMaxLength(120)
             .IsRequired();

            n.HasIndex(p => p.Value).IsUnique();
        });

        builder.Property(r => r.Edition)
               .HasColumnName("edition")
               .HasMaxLength(30)
               .IsRequired();

        builder.Property(r => r.Theme)
               .HasColumnName("theme")
               .HasMaxLength(120)
               .IsRequired();

        builder.Property(r => r.ShortDescription)
               .HasColumnName("short_description")
               .HasMaxLength(200)
               .IsRequired(false);

        builder.Property(r => r.LongDescription)
               .HasColumnName("long_description")
               .HasMaxLength(5000)
               .IsRequired(false);

        builder.Property(r => r.Location)
               .HasColumnName("location")
               .HasMaxLength(200)
               .IsRequired(false);

        #endregion

        #region Datas

        builder.Property(r => r.StartDate)
               .HasColumnName("start_date")
               .HasColumnType("date")
               .IsRequired();

        builder.Property(r => r.EndDate)
               .HasColumnName("end_date")
               .HasColumnType("date")
               .IsRequired();

        builder.Property(r => r.RegistrationStart)
               .HasColumnName("registration_start")
               .HasColumnType("date")
               .IsRequired();

        builder.Property(r => r.RegistrationEnd)
               .HasColumnName("registration_end")
               .HasColumnType("date")
               .IsRequired();

        #endregion

        #region Vagas

        builder.Property(r => r.MaleSlots)
               .HasColumnName("male_slots")
               .IsRequired();

        builder.Property(r => r.FemaleSlots)
               .HasColumnName("female_slots")
               .IsRequired();

        #endregion

        #region Taxas

        builder.OwnsOne(r => r.FeeFazer, f =>
        {
            f.Property(v => v.Amount)
             .HasColumnName("fee_fazer_amount")
             .HasColumnType("numeric(18,2)")
             .IsRequired();

            f.Property(v => v.Currency)
             .HasColumnName("fee_fazer_currency")
             .HasMaxLength(3)
             .IsRequired();
        });

        builder.OwnsOne(r => r.FeeServir, f =>
        {
            f.Property(v => v.Amount)
             .HasColumnName("fee_servir_amount")
             .HasColumnType("numeric(18,2)")
             .IsRequired();

            f.Property(v => v.Currency)
             .HasColumnName("fee_servir_currency")
             .HasMaxLength(3)
             .IsRequired();
        });

        #endregion

        #region Contato

        builder.Property(r => r.ContactEmail)
               .HasColumnName("contact_email")
               .HasMaxLength(100)
               .IsRequired(false);

        builder.Property(r => r.ContactPhone)
               .HasColumnName("contact_phone")
               .HasMaxLength(20)
               .IsRequired(false);

        #endregion

        #region Status e Controle

        builder.Property(r => r.Status)
               .HasColumnName("status")
               .HasConversion<string>()
               .HasMaxLength(30)
               .IsRequired();

        builder.Property(r => r.IsPubliclyVisible)
               .HasColumnName("is_publicly_visible")
               .HasDefaultValue(false)
               .IsRequired();

        builder.Property(r => r.PublishedAt)
               .HasColumnName("published_at")
               .HasColumnType("timestamp with time zone")
               .IsRequired(false);

        builder.HasIndex(r => new { r.IsPubliclyVisible, r.Status, r.StartDate })
               .HasDatabaseName("ix_retreats_public_listing");

        #endregion

        #region Contemplação

        builder.Property(r => r.ContemplationClosed)
               .HasColumnName("contemplation_closed")
               .IsRequired();

        #endregion

        #region Famílias

        builder.Property(r => r.FamiliesVersion)
               .HasColumnName("families_version")
               .IsRequired()
               .IsConcurrencyToken();

        builder.Property(r => r.FamiliesLocked)
               .HasColumnName("families_locked")
               .HasDefaultValue(false)
               .IsRequired();

        #endregion

        #region Serviços

        builder.Property(x => x.ServiceSpacesVersion)
               .HasColumnName("service_spaces_version")
               .HasDefaultValue(0)
               .IsRequired()
               .IsConcurrencyToken();

        builder.Property(x => x.ServiceLocked)
               .HasColumnName("service_locked")
               .HasDefaultValue(false)
               .IsRequired();

        #endregion

        #region Barracas/Tendas

        builder.Property(x => x.TentsVersion)
               .HasColumnName("tents_version")
               .HasDefaultValue(0)
               .IsRequired()
               .IsConcurrencyToken();

        builder.Property(x => x.TentsLocked)
               .HasColumnName("tents_locked")
               .HasDefaultValue(false)
               .IsRequired();

        #endregion

        #region Política de Privacidade

        builder.OwnsOne(r => r.PrivacyPolicyData, pp =>
        {
            pp.Property(p => p.Title)
              .HasColumnName("privacy_policy_title")
              .HasMaxLength(200)
              .IsRequired();

            pp.Property(p => p.Body)
              .HasColumnName("privacy_policy_body")
              .HasMaxLength(50000)
              .IsRequired();

            pp.Property(p => p.Version)
              .HasColumnName("privacy_policy_version")
              .HasMaxLength(50)
              .IsRequired();

            pp.Property(p => p.PublishedAt)
              .HasColumnName("privacy_policy_published_at")
              .HasColumnType("timestamp with time zone")
              .IsRequired();
        });

        builder.Property(r => r.RequiresPrivacyPolicyAcceptance)
               .HasColumnName("requires_privacy_policy_acceptance")
               .HasDefaultValue(true)
               .IsRequired();

        #endregion

        #region Imagens (Owned Collection)

        builder.OwnsMany(r => r.Images, img =>
        {
            img.ToTable("retreat_images");

            img.WithOwner()
               .HasForeignKey("retreat_id");

            // garante tipo da FK shadow
            img.Property<Guid>("retreat_id")
               .HasColumnName("retreat_id");

            img.Property<int>("Id")
               .HasColumnName("id")
               .ValueGeneratedOnAdd();

            img.HasKey("Id");

            img.Property(i => i.ImageUrl)
               .HasColumnName("image_url")
               .HasMaxLength(500)
               .IsRequired();

            img.Property(i => i.StorageId)
               .HasColumnName("storage_id")
               .HasMaxLength(100)
               .IsRequired();

            img.Property(i => i.Type)
               .HasColumnName("type")
               .HasConversion<string>()
               .HasMaxLength(20)
               .IsRequired();

            img.Property(i => i.Order)
               .HasColumnName("order")
               .IsRequired();

            img.Property(i => i.UploadedAt)
               .HasColumnName("uploaded_at")
               .HasColumnType("timestamp with time zone")
               .IsRequired();

            img.Property(i => i.AltText)
               .HasColumnName("alt_text")
               .HasMaxLength(200)
               .IsRequired(false);

            // índices
            img.HasIndex(i => i.StorageId)
               .IsUnique()
               .HasDatabaseName("ix_retreat_images_storage_id");

            img.HasIndex("retreat_id", nameof(RetreatImage.Type), nameof(RetreatImage.Order))
               .HasDatabaseName("ix_retreat_images_type_order");
        });

        #endregion

        #region Códigos de Emergência (Owned Collection)

        builder.OwnsMany(r => r.EmergencyCodes, code =>
        {
            code.ToTable("retreat_emergency_codes");

            code.WithOwner()
                .HasForeignKey("retreat_id");

            // garante tipo da FK shadow
            code.Property<Guid>("retreat_id")
                .HasColumnName("retreat_id");

            code.Property<int>("Id")
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            code.HasKey("Id");

            code.Property(c => c.Code)
                .HasColumnName("code")
                .HasMaxLength(50)
                .IsRequired();

            code.Property(c => c.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            code.Property(c => c.ExpiresAt)
                .HasColumnName("expires_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired(false);

            code.Property(c => c.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true)
                .IsRequired();

            code.Property(c => c.CreatedByUserId)
                .HasColumnName("created_by_user_id")
                .HasMaxLength(100)
                .IsRequired();

            code.Property(c => c.Reason)
                .HasColumnName("reason")
                .HasMaxLength(500)
                .IsRequired(false);

            code.Property(c => c.MaxUses)
                .HasColumnName("max_uses")
                .IsRequired(false);

            code.Property(c => c.UsedCount)
                .HasColumnName("used_count")
                .HasDefaultValue(0)
                .IsRequired();
            
            code.HasIndex(c => c.Code)
                .IsUnique()
                .HasDatabaseName("ix_retreat_emergency_codes_code");

            code.HasIndex("retreat_id", nameof(EmergencyRegistrationCode.IsActive), nameof(EmergencyRegistrationCode.ExpiresAt))
                .HasDatabaseName("ix_retreat_emergency_codes_active");
        });

        #endregion

        #region Auditoria

        builder.Property(r => r.CreatedAt)
               .HasColumnName("created_at")
               .HasColumnType("timestamp with time zone")
               .IsRequired();

        builder.Property(r => r.CreatedByUserId)
               .HasColumnName("created_by_user_id")
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(r => r.LastModifiedAt)
               .HasColumnName("last_modified_at")
               .HasColumnType("timestamp with time zone")
               .IsRequired(false);

        builder.Property(r => r.LastModifiedByUserId)
               .HasColumnName("last_modified_by_user_id")
               .HasMaxLength(100)
               .IsRequired(false);

        builder.HasIndex(r => r.CreatedAt)
               .HasDatabaseName("ix_retreats_created_at");

        #endregion

        #region Índices Compostos Adicionais

        builder.HasIndex(r => new { r.Edition })
               .HasDatabaseName("ix_retreats_edition");

        #endregion
    }
}
