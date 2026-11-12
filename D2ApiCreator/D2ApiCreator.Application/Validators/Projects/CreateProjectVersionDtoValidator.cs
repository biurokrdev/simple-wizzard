using D2ApiCreator.Application.DTOs.Projects;
using FluentValidation;

namespace D2ApiCreator.Application.Validators.Projects;

/// <summary>
/// Validator for CreateProjectVersionDto
/// </summary>
public class CreateProjectVersionDtoValidator : AbstractValidator<CreateProjectVersionDto>
{
    public CreateProjectVersionDtoValidator()
    {
        // Dane biznesowe (ProcessName, ProcessOwner, ProcessVersion, ProcessStep, StatusId) są OPCJONALNE
        // na etapie pracy nad projektem pisma — kreator pozwala zapisać szkic bez nich; komplet jest wymagany
        // dopiero przed wysłaniem parametryzacji do weryfikacji (reguła w GUI, letter-wizard-submission.ts).
        // Encja ProjectVersion i tak ich nie przechowuje osobno — żyją w step_data (JSONB). 2026-11-08.
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("Project ID is required");

        RuleFor(x => x.Major)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Major version must be non-negative");

        RuleFor(x => x.Minor)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Minor version must be non-negative");

        RuleFor(x => x.ProcessName)
            .MaximumLength(500)
            .WithMessage("Process name cannot exceed 500 characters");

        // TenantIdentifier is optional - projects can be personal or team-based
        RuleFor(x => x.TenantIdentifier)
            .MaximumLength(200)
            .WithMessage("Tenant identifier cannot exceed 200 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.TenantIdentifier));

        RuleFor(x => x.ProcessOwner)
            .MaximumLength(200)
            .WithMessage("Process owner cannot exceed 200 characters");

        RuleFor(x => x.ProcessVersion)
            .MaximumLength(100)
            .WithMessage("Process version cannot exceed 100 characters");

        RuleFor(x => x.ProcessStep)
            .MaximumLength(200)
            .WithMessage("Process step cannot exceed 200 characters");

        RuleFor(x => x.StatusId)
            .MaximumLength(100)
            .WithMessage("Status ID cannot exceed 100 characters");
    }
}
