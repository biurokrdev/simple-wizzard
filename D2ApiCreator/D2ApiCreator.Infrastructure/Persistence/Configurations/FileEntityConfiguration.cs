
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using D2ApiCreator.Domain.Entities;

namespace D2ApiCreator.Infrastructure.Persistence.Configurations;

public class FileEntityConfiguration : IEntityTypeConfiguration<FileEntity>
{
    public void Configure(EntityTypeBuilder<FileEntity> builder)
    {
        builder.ToTable("files");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id");
        builder.Property(f => f.ProjectVersionId).HasColumnName("project_version_id");
        builder.Property(f => f.Filename).HasColumnName("filename").IsRequired();
        builder.Property(f => f.Content).HasColumnName("content"); // bytea
        builder.Property(f => f.StorageRef).HasColumnName("storage_ref");
        builder.Property(f => f.ContentType).HasColumnName("content_type");
        builder.Property(f => f.Size).HasColumnName("size");
        builder.Property(f => f.Sha256).HasColumnName("sha256");
        builder.Property(f => f.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(f => f.ProjectVersionId).HasDatabaseName("idx_files_project_version_id");

        // Relationships
        builder.HasOne(f => f.ProjectVersion)
            .WithMany(pv => pv.Files)
            .HasForeignKey(f => f.ProjectVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(f => f.ParseResults)
            .WithOne(pr => pr.File)
            .HasForeignKey(pr => pr.FileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

