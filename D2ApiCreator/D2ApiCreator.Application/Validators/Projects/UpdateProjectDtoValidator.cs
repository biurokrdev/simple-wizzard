using D2ApiCreator.Application.DTOs.Projects;
using FluentValidation;

namespace D2ApiCreator.Application.Validators.Projects;

public class UpdateProjectDtoValidator : AbstractValidator<UpdateProjectDto>
{
    public UpdateProjectDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required")
            .MaximumLength(255).WithMessage("Project name cannot exceed 255 characters");

        RuleFor(x => x.RetentionDays)
            .GreaterThanOrEqualTo(0).When(x => x.RetentionDays.HasValue)
            .WithMessage("Retention days must be a positive number");
    }
}
