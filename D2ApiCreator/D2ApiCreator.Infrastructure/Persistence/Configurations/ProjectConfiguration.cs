
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.ValueObjects;

namespace D2ApiCreator.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects", ApplicationDbContext.Schema);
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(p => p.TenantId).HasColumnName("tenant_id");
        builder.Property(p => p.Source).HasColumnName("source").IsRequired();
        builder.Property(p => p.Name).HasColumnName("name").IsRequired();
        builder.Property(p => p.RetentionDays).HasColumnName("retention_days");
        builder.Property(p => p.DefaultTemplateName).HasColumnName("default_template_name");
        builder.Property(p => p.CreatedBy).HasColumnName("created_by");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.Description).HasColumnName("description");
        builder.Property(p => p.CurrentActiveVersionId).HasColumnName("current_active_version_id");

        // Map Metadata as JSONB column
        builder.OwnsOne(p => p.Metadata, metadata =>
        {
            metadata.ToJson("metadata");
        });

        builder.HasIndex(p => new { p.TenantId, p.Source }).HasDatabaseName("idx_tenant_source");

        // Relationships
        builder.HasOne(p => p.Tenant)
            .WithMany(t => t.Projects)
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(p => p.Creator)
            .WithMany()
            .HasForeignKey(p => p.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(p => p.Versions)
            .WithOne(pv => pv.Project)
            .HasForeignKey(pv => pv.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

