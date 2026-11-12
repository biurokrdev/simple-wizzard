
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using D2ApiCreator.Domain.Entities;
using System.Text.Json;

namespace D2ApiCreator.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_log");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.Entity).HasColumnName("entity");
        builder.Property(a => a.EntityId).HasColumnName("entity_id");
        builder.Property(a => a.Action).HasColumnName("action");
        builder.Property(a => a.ActorId).HasColumnName("actor_id");
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");

        var jsonConv = new ValueConverter<JsonDocument?, string?>(
            v => v == null ? null : v.RootElement.GetRawText(),
            v => string.IsNullOrEmpty(v) ? null : JsonDocument.Parse(v, new JsonDocumentOptions())
        );

        builder.Property(a => a.Payload).HasColumnName("payload").HasConversion(jsonConv).HasColumnType("jsonb");
    }
}
