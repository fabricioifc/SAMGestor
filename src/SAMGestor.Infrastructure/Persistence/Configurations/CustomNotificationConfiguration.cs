using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class CustomNotificationConfiguration : IEntityTypeConfiguration<CustomNotification>
{
    public void Configure(EntityTypeBuilder<CustomNotification> b)
    {
        b.ToTable("custom_notifications");

        b.HasKey(n => n.Id);

        b.Property(n => n.RetreatId)
            .HasColumnName("retreat_id");

        b.Property(n => n.SentByUserId)
            .HasColumnName("sent_by_user_id")
            .IsRequired();

        b.Property(n => n.SentAt)
            .HasColumnName("sent_at")
            .IsRequired();

        b.Property(n => n.TargetType)
            .HasColumnName("target_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        b.Property(n => n.TargetFilterJson)
            .HasColumnName("target_filter_json")
            .HasColumnType("jsonb")
            .IsRequired();
        
        b.OwnsOne(n => n.Template, t =>
        {
            t.Property(p => p.Subject)
                .HasColumnName("template_subject")
                .HasMaxLength(200)
                .IsRequired();

            t.Property(p => p.Body)
                .HasColumnName("template_body")
                .HasMaxLength(50000)
                .IsRequired();

            t.Property(p => p.PreheaderText)
                .HasColumnName("template_preheader_text")
                .HasMaxLength(200);

            t.Property(p => p.CallToActionUrl)
                .HasColumnName("template_cta_url")
                .HasMaxLength(500);

            t.Property(p => p.CallToActionText)
                .HasColumnName("template_cta_text")
                .HasMaxLength(100);

            t.Property(p => p.SecondaryLinkUrl)
                .HasColumnName("template_secondary_url")
                .HasMaxLength(500);

            t.Property(p => p.SecondaryLinkText)
                .HasColumnName("template_secondary_text")
                .HasMaxLength(100);

            t.Property(p => p.ImageUrl)
                .HasColumnName("template_image_url")
                .HasMaxLength(500);
        });

        b.Property(n => n.TotalRecipients)
            .HasColumnName("total_recipients")
            .IsRequired();

        b.Property(n => n.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        b.Property(n => n.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(1000);

        b.HasIndex(n => n.RetreatId);
        b.HasIndex(n => n.SentByUserId);
        b.HasIndex(n => n.SentAt);
        b.HasIndex(n => n.Status);
    }
}
