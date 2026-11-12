using D2ApiCreator.Application.DTOs.Projects;
using FluentValidation;

namespace D2ApiCreator.Application.Validators.Projects;

public class UpdateProjectVersionStepDataDtoValidator : AbstractValidator<UpdateProjectVersionStepDataDto>
{
    public UpdateProjectVersionStepDataDtoValidator()
    {
        RuleFor(x => x.ProjectVersionId)
            .NotEmpty().WithMessage("Project version ID is required");

        RuleFor(x => x.StepData)
            .NotNull().WithMessage("Step data is required");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes cannot exceed 2000 characters");
    }
}
