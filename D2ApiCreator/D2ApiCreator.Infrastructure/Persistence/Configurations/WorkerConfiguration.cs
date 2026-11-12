using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using D2ApiCreator.Domain.Entities;

namespace D2ApiCreator.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Worker entity
/// </summary>
public class WorkerConfiguration : IEntityTypeConfiguration<Worker>
{
    public void Configure(EntityTypeBuilder<Worker> builder)
    {
        builder.ToTable("workers");
        
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasColumnName("id");
        
        builder.Property(w => w.ProjectVersionId)
            .HasColumnName("project_version_id")
            .IsRequired();
        
        builder.Property(w => w.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();
        
        builder.Property(w => w.WorkerType)
            .HasColumnName("worker_type")
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(w => w.ConfigJson)
            .HasColumnName("config_json")
            .HasColumnType("jsonb")
            .IsRequired();
        
        builder.Property(w => w.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
        
        builder.Property(w => w.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(w => w.ProjectVersionId)
            .HasDatabaseName("idx_workers_project_version");

        // Relationships
        builder.HasOne(w => w.ProjectVersion)
            .WithMany(pv => pv.Workers)
            .HasForeignKey(w => w.ProjectVersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
