
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using D2ApiCreator.Domain.Entities;

namespace D2ApiCreator.Infrastructure.Persistence.Configurations;

public class ApprovalConfiguration : IEntityTypeConfiguration<Approval>
{
    public void Configure(EntityTypeBuilder<Approval> builder)
    {
        builder.ToTable("approvals", ApplicationDbContext.Schema);
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(a => a.ProjectVersionId).HasColumnName("project_version_id");
        builder.Property(a => a.ApproverId).HasColumnName("approver_id");
        builder.Property(a => a.Status)
            .HasColumnName("status")
            .HasConversion<string>();
        builder.Property(a => a.Comment).HasColumnName("comment");
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");

        // Relationships
        builder.HasOne(a => a.ProjectVersion)
            .WithMany(pv => pv.Approvals)
            .HasForeignKey(a => a.ProjectVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Approver)
            .WithMany()
            .HasForeignKey(a => a.ApproverId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

