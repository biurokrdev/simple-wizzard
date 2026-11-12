using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.DTOs.Projects;
using D2ApiCreator.Application.Features.Projects.Commands.ActivateProjectVersion;
using D2ApiCreator.Application.Features.Projects.Commands.DeactivateProjectVersion;
using D2ApiCreator.Application.Features.Projects.Queries.GetProject;
using D2ApiCreator.Application.Features.Projects.Queries.GetProjectVersions;
using D2ApiCreator.Domain.Enums;
using D2ServicesCreator.Api.Contracts;
using D2ServicesCreator.Api.Controllers;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace D2ServicesCreator.Tests.Unit.Controllers;

public class ProjectsControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly ProjectsController _controller;

    public ProjectsControllerTests()
    {
        _controller = new ProjectsController(_sender, NullLogger<ProjectsController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    // ── GET /api/v1/Projects/{projectId} ──────────────────────────────────

    [Fact]
    public async Task GetProjectStatus_WhenProjectNotFound_Returns404Problem()
    {
        var projectId = Guid.NewGuid();
        _sender.Send(Arg.Any<GetProjectQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProjectDto>.Failure($"Project with ID {projectId} not found"));

        var result = await _controller.GetProjectStatus(projectId, CancellationToken.None);

        var problem = result.Should().BeOfType<ObjectResult>().Subject;
        problem.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        problem.ContentTypes.Should().Contain("application/problem+json");
        problem.Value.Should().BeOfType<ProblemDetails>().Which.Status.Should().Be(404);
        await _sender.DidNotReceive().Send(Arg.Any<GetProjectVersionsQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProjectStatus_WhenActiveVersionExists_ReturnsNarrowedStatus()
    {
        var projectId = Guid.NewGuid();
        var activeId = Guid.NewGuid();
        var draftId = Guid.NewGuid();
        _sender.Send(Arg.Any<GetProjectQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProjectDto>.Success(BuildProject(projectId, activeId)));
        _sender.Send(Arg.Any<GetProjectVersionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<ProjectVersionDto>>.Success(new List<ProjectVersionDto>
            {
                BuildVersion(projectId, draftId, "1.1", isActive: false, ProjectStatus.Draft),
                BuildVersion(projectId, activeId, "1.0", isActive: true, ProjectStatus.Active)
            }));

        var result = await _controller.GetProjectStatus(projectId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ProjectStatusResponse>().Subject;
        response.ProjectId.Should().Be(projectId);
        response.Source.Should().Be("PRJ-TEST");
        response.IsActive.Should().BeTrue();
        response.ActiveVersionId.Should().Be(activeId);
        response.ActiveVersionTag.Should().Be("1.0");
        response.Versions.Should().HaveCount(2);
        response.Versions.Select(v => v.VersionId).Should().ContainInOrder(draftId, activeId);
        response.Versions.Single(v => v.VersionId == activeId).Status.Should().Be(ProjectStatus.Active);
    }

    [Fact]
    public async Task GetProjectStatus_WhenNoActiveVersion_ReturnsIsActiveFalse()
    {
        var projectId = Guid.NewGuid();
        _sender.Send(Arg.Any<GetProjectQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProjectDto>.Success(BuildProject(projectId, currentActiveVersionId: null)));
        _sender.Send(Arg.Any<GetProjectVersionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<ProjectVersionDto>>.Success(new List<ProjectVersionDto>
            {
                BuildVersion(projectId, Guid.NewGuid(), "1.0", isActive: false, ProjectStatus.Draft)
            }));

        var result = await _controller.GetProjectStatus(projectId, CancellationToken.None);

        var response = result.Should().BeOfType<OkObjectResult>().Subject
            .Value.Should().BeOfType<ProjectStatusResponse>().Subject;
        response.IsActive.Should().BeFalse();
        response.ActiveVersionId.Should().BeNull();
        response.ActiveVersionTag.Should().BeNull();
        response.Versions.Should().ContainSingle();
    }

    [Fact]
    public async Task GetProjectStatus_WhenPointerSetButVersionFlagFalse_ReportsInactiveButKeepsPointer()
    {
        // Inconsistent data (pointer without flag) must not be reported as "active".
        var projectId = Guid.NewGuid();
        var pointedId = Guid.NewGuid();
        _sender.Send(Arg.Any<GetProjectQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProjectDto>.Success(BuildProject(projectId, pointedId)));
        _sender.Send(Arg.Any<GetProjectVersionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<ProjectVersionDto>>.Success(new List<ProjectVersionDto>
            {
                BuildVersion(projectId, pointedId, "1.0", isActive: false, ProjectStatus.Inactive)
            }));

        var result = await _controller.GetProjectStatus(projectId, CancellationToken.None);

        var response = result.Should().BeOfType<OkObjectResult>().Subject
            .Value.Should().BeOfType<ProjectStatusResponse>().Subject;
        response.IsActive.Should().BeFalse();
        response.ActiveVersionId.Should().Be(pointedId);
    }

    [Fact]
    public async Task GetProjectStatus_WhenVersionsQueryFails_Returns500WithoutInternalMessage()
    {
        var projectId = Guid.NewGuid();
        _sender.Send(Arg.Any<GetProjectQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProjectDto>.Success(BuildProject(projectId, null)));
        _sender.Send(Arg.Any<GetProjectVersionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<ProjectVersionDto>>.Failure("Npgsql: connection refused"));

        var result = await _controller.GetProjectStatus(projectId, CancellationToken.None);

        var problem = result.Should().BeOfType<ObjectResult>().Subject;
        problem.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        problem.Value.Should().BeOfType<ProblemDetails>().Which.Detail.Should().NotContain("Npgsql");
    }

    // ── POST .../versions/{versionId}/activate ────────────────────────────

    [Fact]
    public async Task ActivateVersion_WhenSuccess_Returns200WithVersionState()
    {
        var projectId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        _sender.Send(Arg.Any<ActivateProjectVersionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProjectVersionDto>.Success(
                BuildVersion(projectId, versionId, "2.0", isActive: true, ProjectStatus.Active)));

        var result = await _controller.ActivateVersion(projectId, versionId, CancellationToken.None);

        var state = result.Should().BeOfType<OkObjectResult>().Subject
            .Value.Should().BeOfType<ProjectVersionStateResponse>().Subject;
        state.Should().Be(new ProjectVersionStateResponse(projectId, versionId, "2.0", true, ProjectStatus.Active));
        await _sender.Received(1).Send(
            Arg.Is<ActivateProjectVersionCommand>(c => c.ProjectId == projectId && c.VersionId == versionId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActivateVersion_WhenNotFound_Returns404()
    {
        _sender.Send(Arg.Any<ActivateProjectVersionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProjectVersionDto>.Failure("Version with ID x not found"));

        var result = await _controller.ActivateVersion(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task ActivateVersion_WhenVersionBelongsToOtherProject_Returns404()
    {
        _sender.Send(Arg.Any<ActivateProjectVersionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProjectVersionDto>.Failure("Version does not belong to the specified project"));

        var result = await _controller.ActivateVersion(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task ActivateVersion_WhenOtherFailure_Returns400()
    {
        _sender.Send(Arg.Any<ActivateProjectVersionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProjectVersionDto>.Failure("Failed to activate version: boom"));

        var result = await _controller.ActivateVersion(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        var problem = result.Should().BeOfType<ObjectResult>().Subject;
        problem.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        problem.Value.Should().BeOfType<ProblemDetails>().Which.Detail.Should().Contain("Failed to activate");
    }

    // ── POST .../versions/{versionId}/deactivate ──────────────────────────

    [Fact]
    public async Task DeactivateVersion_WhenSuccess_Returns200WithInactiveState()
    {
        var projectId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        _sender.Send(Arg.Any<DeactivateProjectVersionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProjectVersionDto>.Success(
                BuildVersion(projectId, versionId, "1.0", isActive: false, ProjectStatus.Inactive)));

        var result = await _controller.DeactivateVersion(projectId, versionId, CancellationToken.None);

        var state = result.Should().BeOfType<OkObjectResult>().Subject
            .Value.Should().BeOfType<ProjectVersionStateResponse>().Subject;
        state.IsActive.Should().BeFalse();
        state.Status.Should().Be(ProjectStatus.Inactive);
        await _sender.Received(1).Send(
            Arg.Is<DeactivateProjectVersionCommand>(c => c.ProjectId == projectId && c.VersionId == versionId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeactivateVersion_WhenProjectNotFound_Returns404()
    {
        _sender.Send(Arg.Any<DeactivateProjectVersionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProjectVersionDto>.Failure("Project with ID x not found"));

        var result = await _controller.DeactivateVersion(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private static ProjectDto BuildProject(Guid projectId, Guid? currentActiveVersionId) => new()
    {
        Id = projectId,
        Source = "PRJ-TEST",
        Name = "Pismo testowe",
        RetentionDays = 90,
        CreatedAt = DateTime.UtcNow,
        CurrentActiveVersionId = currentActiveVersionId
    };

    private static ProjectVersionDto BuildVersion(Guid projectId, Guid versionId, string tag, bool isActive, ProjectStatus status)
    {
        var parts = tag.Split('.');
        return new ProjectVersionDto
        {
            Id = versionId,
            ProjectId = projectId,
            Major = int.Parse(parts[0]),
            Minor = int.Parse(parts[1]),
            VersionTag = tag,
            IsActive = isActive,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
    }
}
