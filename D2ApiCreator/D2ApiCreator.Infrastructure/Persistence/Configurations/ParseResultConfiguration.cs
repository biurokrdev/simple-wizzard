
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using D2ApiCreator.Domain.Entities;
using System.Text.Json;

namespace D2ApiCreator.Infrastructure.Persistence.Configurations;

public class ParseResultConfiguration : IEntityTypeConfiguration<ParseResult>
{
    public void Configure(EntityTypeBuilder<ParseResult> builder)
    {
        builder.ToTable("parse_results");
        builder.HasKey(pr => pr.Id);
        builder.Property(pr => pr.Id).HasColumnName("id");
        builder.Property(pr => pr.FileId).HasColumnName("file_id");
        builder.Property(pr => pr.ParsedAt).HasColumnName("parsed_at");

        var jsonConv = new ValueConverter<JsonDocument?, string?>(
            v => v == null ? null : v.RootElement.GetRawText(),
            v => string.IsNullOrEmpty(v) ? null : JsonDocument.Parse(v, new JsonDocumentOptions())
        );

        builder.Property(pr => pr.ParseJson).HasColumnName("parse_json").HasConversion(jsonConv).HasColumnType("jsonb");
        builder.Property(pr => pr.SampleRequest).HasColumnName("sample_request").HasConversion(jsonConv).HasColumnType("jsonb");
        builder.Property(pr => pr.ValidationErrors).HasColumnName("validation_errors").HasConversion(jsonConv).HasColumnType("jsonb");

        builder.HasIndex(pr => pr.FileId).HasDatabaseName("idx_parse_results_file_id");

        // Relationships
        builder.HasOne(pr => pr.File)
            .WithMany(f => f.ParseResults)
            .HasForeignKey(pr => pr.FileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
