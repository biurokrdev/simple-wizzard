using D2ApiCreator.Application.DTOs.Projects;
using FluentValidation;

namespace D2ApiCreator.Application.Validators.Projects;

/// <summary>
/// Validator for CreateProjectDto
/// </summary>
public class CreateProjectDtoValidator : AbstractValidator<CreateProjectDto>
{
    public CreateProjectDtoValidator()
    {
        RuleFor(x => x.Source)
            .NotEmpty()
            .WithMessage("Source identifier is required")
            .MaximumLength(200)
            .WithMessage("Source identifier cannot exceed 200 characters");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Project name is required")
            .MaximumLength(500)
            .WithMessage("Project name cannot exceed 500 characters");

        RuleFor(x => x.ApplicationName)
            .NotEmpty()
            .WithMessage("Application name is required")
            .MaximumLength(200)
            .WithMessage("Application name cannot exceed 200 characters");

        RuleFor(x => x.RetentionDays)
            .GreaterThan(0)
            .WithMessage("Retention days must be greater than 0")
            .LessThanOrEqualTo(3650)
            .WithMessage("Retention days cannot exceed 10 years (3650 days)");

        RuleFor(x => x.Environments)
            .NotNull()
            .WithMessage("Environments list cannot be null");
    }
}
