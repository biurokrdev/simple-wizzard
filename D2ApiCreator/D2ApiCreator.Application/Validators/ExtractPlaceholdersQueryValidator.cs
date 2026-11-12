using D2ApiCreator.Application.Features.Documents.Queries.ExtractPlaceholders;
using FluentValidation;
namespace D2ApiCreator.Application.Validators;
/// <summary>
/// Validator for ExtractPlaceholdersQuery
/// </summary>
public class ExtractPlaceholdersQueryValidator : AbstractValidator<ExtractPlaceholdersQuery>
{
    private static readonly string[] AllowedExtensions = { ".docx", ".xlsx" };
    public ExtractPlaceholdersQueryValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("File name is required")
            .Must(HaveValidExtension)
            .WithMessage("Only .docx and .xlsx files are allowed");
        RuleFor(x => x.FileStream)
            .NotNull()
            .WithMessage("File stream is required")
            .Must(stream => stream != null && stream.Length > 0)
            .WithMessage("File cannot be empty");
    }
    private bool HaveValidExtension(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;
            
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return AllowedExtensions.Contains(extension);
    }
}
