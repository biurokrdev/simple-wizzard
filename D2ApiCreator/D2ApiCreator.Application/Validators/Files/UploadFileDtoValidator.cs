using D2ApiCreator.Application.DTOs.Files;
using FluentValidation;

namespace D2ApiCreator.Application.Validators.Files;

public class UploadFileDtoValidator : AbstractValidator<UploadFileDto>
{
    public UploadFileDtoValidator()
    {
        RuleFor(x => x.ProjectVersionId)
            .NotEmpty().WithMessage("Project version ID is required");

        RuleFor(x => x.File)
            .NotNull().WithMessage("File is required")
            .Must(file => file != null && file.Length > 0).WithMessage("File cannot be empty")
            .Must(file => file == null || file.Length <= 10 * 1024 * 1024).WithMessage("File size cannot exceed 10MB")
            .Must(file => file == null || IsValidFileExtension(file.FileName)).WithMessage("Only .docx and .xlsx files are allowed");
    }

    private bool IsValidFileExtension(string filename)
    {
        if (string.IsNullOrEmpty(filename))
            return false;

        return filename.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) || filename.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase);
    }
}
