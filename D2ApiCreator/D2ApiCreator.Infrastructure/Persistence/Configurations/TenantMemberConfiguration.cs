using D2ApiCreator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace D2ApiCreator.Infrastructure.Persistence.Configurations;

public class TenantMemberConfiguration : IEntityTypeConfiguration<TenantMember>
{
    public void Configure(EntityTypeBuilder<TenantMember> builder)
    {
        builder.ToTable("tenant_members", ApplicationDbContext.Schema);

        builder.HasKey(tm => tm.Id);

        builder.Property(tm => tm.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(tm => tm.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(tm => tm.CorporateKey)
            .HasColumnName("corporate_key")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(tm => tm.AddedBy)
            .HasColumnName("added_by")
            .IsRequired();

        builder.Property(tm => tm.AddedAt)
            .HasColumnName("added_at")
            .IsRequired();

        // Relationships
        builder.HasOne(tm => tm.Tenant)
            .WithMany(t => t.Members)
            .HasForeignKey(tm => tm.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(tm => tm.TenantId)
            .HasDatabaseName("idx_tenant_members_tenant_id");

        builder.HasIndex(tm => tm.CorporateKey)
            .HasDatabaseName("idx_tenant_members_corporate_key");

        builder.HasIndex(tm => new { tm.TenantId, tm.CorporateKey })
            .IsUnique()
            .HasDatabaseName("uq_tenant_corporate_key");

        builder.Property(m => m.IsBlocked).HasColumnName("is_blocked").IsRequired();
        builder.Property(m => m.Access)
            .HasColumnName("access")
            .HasMaxLength(20)
            .HasConversion(v => v.ToString(), v => Enum.Parse<D2ApiCreator.Domain.Enums.TenantMemberAccess>(v))
            .IsRequired();
    }
}
