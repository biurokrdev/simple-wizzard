using D2ApiCreator.Application.DTOs.Workers;
using FluentValidation;

namespace D2ApiCreator.Application.Validators.Workers;

/// <summary>
/// Validator for GenerateWorkersDto
/// </summary>
public class GenerateWorkersDtoValidator : AbstractValidator<GenerateWorkersDto>
{
    public GenerateWorkersDtoValidator()
    {
        RuleFor(x => x.ProjectVersionId)
            .NotEmpty().WithMessage("Project version ID is required");

        RuleFor(x => x)
            .Must(dto => dto.Placeholders.Any() || dto.ContentControls.Any())
            .WithMessage("At least one placeholder or content control is required");
    }
}
