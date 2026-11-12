using System.Text.Json;
using D2ApiCreator.Application.DTOs.WorkerProfiles;
using FluentValidation;

namespace D2ApiCreator.Application.Validators.WorkerProfiles;

public class CreateWorkerProfileDtoValidator : AbstractValidator<CreateWorkerProfileDto>
{
    public CreateWorkerProfileDtoValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.DefaultConfigJson).Must(BeJsonObject).WithMessage("DefaultConfigJson must be a valid JSON object");
    }

    internal static bool BeJsonObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return true;
        }
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public class UpdateWorkerProfileDtoValidator : AbstractValidator<UpdateWorkerProfileDto>
{
    public UpdateWorkerProfileDtoValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.DefaultConfigJson).Must(CreateWorkerProfileDtoValidator.BeJsonObject).WithMessage("DefaultConfigJson must be a valid JSON object");
    }
}
