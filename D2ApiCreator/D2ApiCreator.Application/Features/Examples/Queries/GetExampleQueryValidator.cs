using FluentValidation;

namespace D2ApiCreator.Application.Features.Examples.Queries;

/// <summary>
/// Validator for GetExampleQuery
/// </summary>
public class GetExampleQueryValidator : AbstractValidator<GetExampleQuery>
{
    public GetExampleQueryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(2).WithMessage("Name must be at least 2 characters")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");
    }
}

