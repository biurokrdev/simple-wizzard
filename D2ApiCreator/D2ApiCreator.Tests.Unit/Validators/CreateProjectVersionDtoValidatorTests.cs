using D2ApiCreator.Application.DTOs.Projects;
using D2ApiCreator.Application.Validators.Projects;
using FluentAssertions;

namespace D2ApiCreator.Tests.Unit.Validators;

/// <summary>
/// Dane biznesowe wersji (ProcessName, ProcessOwner, ProcessVersion, ProcessStep, StatusId) są opcjonalne
/// na etapie pracy nad projektem pisma — kreator zapisuje szkic bez nich. Walidator ogranicza tylko długość.
/// </summary>
public class CreateProjectVersionDtoValidatorTests
{
    private readonly CreateProjectVersionDtoValidator _validator = new();

    [Fact]
    public async Task Validate_WithEmptyBusinessData_ShouldPass()
    {
        var dto = new CreateProjectVersionDto
        {
            ProjectId = Guid.NewGuid(),
            ProcessName = string.Empty,
            ProcessOwner = string.Empty,
            ProcessVersion = string.Empty,
            ProcessStep = string.Empty,
            StatusId = string.Empty
        };

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithFullBusinessData_ShouldPass()
    {
        var dto = new CreateProjectVersionDto
        {
            ProjectId = Guid.NewGuid(),
            ProcessName = "Wysyłka umowy",
            ProcessOwner = "Jan Kowalski",
            ProcessVersion = "1.0",
            ProcessStep = "Podpis",
            StatusId = "DRAFT",
            TenantIdentifier = "team-a"
        };

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyProjectId_ShouldFail()
    {
        var dto = new CreateProjectVersionDto { ProjectId = Guid.Empty };

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProjectVersionDto.ProjectId));
    }

    [Theory]
    [InlineData(nameof(CreateProjectVersionDto.ProcessName), 501)]
    [InlineData(nameof(CreateProjectVersionDto.ProcessOwner), 201)]
    [InlineData(nameof(CreateProjectVersionDto.ProcessVersion), 101)]
    [InlineData(nameof(CreateProjectVersionDto.ProcessStep), 201)]
    [InlineData(nameof(CreateProjectVersionDto.StatusId), 101)]
    public async Task Validate_WithTooLongBusinessField_ShouldFail(string property, int length)
    {
        var dto = new CreateProjectVersionDto { ProjectId = Guid.NewGuid() };
        typeof(CreateProjectVersionDto).GetProperty(property)!.SetValue(dto, new string('x', length));

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == property);
    }

    [Fact]
    public async Task Validate_WithNegativeVersionNumbers_ShouldFail()
    {
        var dto = new CreateProjectVersionDto { ProjectId = Guid.NewGuid(), Major = -1, Minor = -1 };

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.PropertyName).Should().Contain(new[] { "Major", "Minor" });
    }
}
