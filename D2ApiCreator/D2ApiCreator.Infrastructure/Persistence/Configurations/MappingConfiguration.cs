
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Enums;
using System.Text.Json;

namespace D2ApiCreator.Infrastructure.Persistence.Configurations;

public class MappingConfiguration : IEntityTypeConfiguration<Mapping>
{
    public void Configure(EntityTypeBuilder<Mapping> builder)
    {
        builder.ToTable("mappings", ApplicationDbContext.Schema);
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(m => m.ProjectVersionId).HasColumnName("project_version_id");
        builder.Property(m => m.TagName).HasColumnName("tag_name");
        builder.Property(m => m.MappingType).HasConversion<string>().HasColumnName("mapping_type");
        builder.Property(m => m.SortOrder).HasColumnName("sort_order");
        builder.Property(m => m.CreatedBy).HasColumnName("created_by");
        builder.Property(m => m.CreatedAt).HasColumnName("created_at");

        var jsonConv = new ValueConverter<JsonDocument, string?>(
            v => v.RootElement.GetRawText(),
            v => string.IsNullOrEmpty(v) ? JsonDocument.Parse("null", new JsonDocumentOptions()) : JsonDocument.Parse(v, new JsonDocumentOptions())
        );

        builder.Property(m => m.MappingJson).HasColumnName("mapping_json").HasConversion(jsonConv).HasColumnType("jsonb");

        builder.HasIndex(m => new { m.ProjectVersionId, m.TagName }).HasDatabaseName("idx_mappings_project_version_tag");

        // Relationships
        builder.HasOne(m => m.ProjectVersion)
            .WithMany(pv => pv.Mappings)
            .HasForeignKey(m => m.ProjectVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Creator)
            .WithMany()
            .HasForeignKey(m => m.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
