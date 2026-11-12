using D2ApiCreator.Application.DTOs.Projects;
using FluentValidation;

namespace D2ApiCreator.Application.Validators.Projects;

public class UpdateProjectVersionDtoValidator : AbstractValidator<UpdateProjectVersionDto>
{
    public UpdateProjectVersionDtoValidator()
    {
        RuleFor(x => x.StepData)
            .NotNull().WithMessage("Step data is required");
    }
}
