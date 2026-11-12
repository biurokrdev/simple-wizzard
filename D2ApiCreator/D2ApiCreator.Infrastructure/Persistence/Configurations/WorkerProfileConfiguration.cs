using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace D2ApiCreator.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="WorkerProfile"/> (table <c>worker_profiles</c>, SQL 009).</summary>
public class WorkerProfileConfiguration : IEntityTypeConfiguration<WorkerProfile>
{
    public void Configure(EntityTypeBuilder<WorkerProfile> builder)
    {
        builder.ToTable("worker_profiles");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.Key).HasColumnName("key").HasMaxLength(100).IsRequired();
        builder.HasIndex(p => p.Key).IsUnique().HasDatabaseName("uq_worker_profiles_key");
        builder.Property(p => p.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasColumnName("description");
        builder.Property(p => p.DefaultConfigJson).HasColumnName("default_config_json").HasColumnType("jsonb").IsRequired();
        builder.Property(p => p.IsVisible).HasColumnName("is_visible").IsRequired();
        builder.Property(p => p.SortOrder).HasColumnName("sort_order").IsRequired();
        builder.Property(p => p.Source)
            .HasColumnName("source")
            .HasMaxLength(20)
            .HasConversion(v => v.ToString(), v => Enum.Parse<WorkerProfileSource>(v))
            .IsRequired();
        builder.Property(p => p.IsMissingInSource).HasColumnName("is_missing_in_source").IsRequired();
        builder.Property(p => p.LastSyncedAt).HasColumnName("last_synced_at");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
    }
}
