
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using D2ApiCreator.Domain.Entities;
using System.Text.Json;

namespace D2ApiCreator.Infrastructure.Persistence.Configurations;

public class ProjectVersionConfiguration : IEntityTypeConfiguration<ProjectVersion>
{
    public void Configure(EntityTypeBuilder<ProjectVersion> builder)
    {
        builder.ToTable("project_versions", ApplicationDbContext.Schema);
        builder.HasKey(pv => pv.Id);
        builder.Property(pv => pv.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(pv => pv.ProjectId).HasColumnName("project_id");
        builder.Property(pv => pv.Major).HasColumnName("major");
        builder.Property(pv => pv.Minor).HasColumnName("minor");
        builder.Property(pv => pv.VersionTag).HasColumnName("version_tag").IsRequired();
        builder.Property(pv => pv.IsActive).HasColumnName("is_active");
        builder.Property(pv => pv.Status)
            .HasColumnName("status")
            .HasConversion<string>();
        builder.Property(pv => pv.CreatedBy).HasColumnName("created_by");
        builder.Property(pv => pv.CreatedAt).HasColumnName("created_at");
        builder.Property(pv => pv.UpdatedAt).HasColumnName("updated_at");
        builder.Property(pv => pv.Notes).HasColumnName("notes");
        builder.Property(pv => pv.SubmittedAt).HasColumnName("submitted_at");
        builder.Property(pv => pv.ReviewedAt).HasColumnName("reviewed_at");
        builder.Property(pv => pv.ReviewedBy).HasColumnName("reviewed_by").HasMaxLength(255);
        builder.Property(pv => pv.ReviewComment).HasColumnName("review_comment");
        builder.Property(pv => pv.DeliveredAt).HasColumnName("delivered_at");
        builder.Property(pv => pv.DeliveredTo).HasColumnName("delivered_to").HasMaxLength(500);
        builder.Property(pv => pv.DeliveredPackageSha).HasColumnName("delivered_package_sha").HasMaxLength(64);

        // step_data as jsonb using JsonDocument converter
        var jsonDocConverter = new ValueConverter<JsonDocument?, string?>(
            v => v == null ? null : v.RootElement.GetRawText(),
            v => string.IsNullOrEmpty(v) ? null : JsonDocument.Parse(v, new JsonDocumentOptions())
        );
        builder.Property(pv => pv.StepData).HasColumnName("step_data").HasConversion(jsonDocConverter).HasColumnType("jsonb");
        builder.Property(pv => pv.ReviewNotes).HasColumnName("review_notes").HasConversion(jsonDocConverter).HasColumnType("jsonb");

        builder.HasIndex(pv => new { pv.ProjectId, pv.CreatedAt }).HasDatabaseName("idx_project_versions_project_id_created_at");

        // Relationships
        builder.HasOne(pv => pv.Project)
            .WithMany(p => p.Versions)
            .HasForeignKey(pv => pv.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pv => pv.Creator)
            .WithMany()
            .HasForeignKey(pv => pv.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(pv => pv.Files)
            .WithOne(f => f.ProjectVersion)
            .HasForeignKey(f => f.ProjectVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(pv => pv.Mappings)
            .WithOne(m => m.ProjectVersion)
            .HasForeignKey(m => m.ProjectVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(pv => pv.Approvals)
            .WithOne(a => a.ProjectVersion)
            .HasForeignKey(a => a.ProjectVersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
