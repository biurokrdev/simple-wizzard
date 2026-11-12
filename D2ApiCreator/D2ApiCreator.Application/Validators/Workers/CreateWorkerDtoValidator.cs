using D2ApiCreator.Application.DTOs.Workers;
using FluentValidation;

namespace D2ApiCreator.Application.Validators.Workers;

/// <summary>
/// Validator for CreateWorkerDto
/// </summary>
public class CreateWorkerDtoValidator : AbstractValidator<CreateWorkerDto>
{
    public CreateWorkerDtoValidator()
    {
        RuleFor(x => x.ProjectVersionId)
            .NotEmpty().WithMessage("Project version ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Worker name is required")
            .MaximumLength(255).WithMessage("Worker name cannot exceed 255 characters");

        RuleFor(x => x.WorkerType)
            .NotEmpty().WithMessage("Worker type is required")
            .MaximumLength(100).WithMessage("Worker type cannot exceed 100 characters");

        RuleFor(x => x.ConfigJson)
            .NotEmpty().WithMessage("Configuration JSON is required")
            .Must(BeValidJson).WithMessage("Configuration must be valid JSON");
    }

    private bool BeValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
