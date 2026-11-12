using D2ApiCreator.Application.Features.Projects.Queries.GetProjects;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using D2ApiCreator.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace D2ApiCreator.Tests.Unit.Features.Projects;

/// <summary>
/// Template shown on the project list: the active version's file when there is one, otherwise the
/// newest draft's file — so several letters edited in parallel can be told apart before activation.
/// </summary>
public class GetProjectsQueryHandlerTests
{
    private readonly IRepository<Project, Guid> _projectRepository = Substitute.For<IRepository<Project, Guid>>();
    private readonly IProjectVersionRepository _versionRepository = Substitute.For<IProjectVersionRepository>();
    private readonly IRepository<TenantMember, Guid> _tenantMemberRepository = Substitute.For<IRepository<TenantMember, Guid>>();
    private readonly IRepository<Tenant, Guid> _tenantRepository = Substitute.For<IRepository<Tenant, Guid>>();
    private readonly GetProjectsQueryHandler _sut;

    public GetProjectsQueryHandlerTests()
    {
        _sut = new GetProjectsQueryHandler(
            _projectRepository,
            _versionRepository,
            _tenantMemberRepository,
            _tenantRepository,
            Substitute.For<ILogger<GetProjectsQueryHandler>>());
        _tenantRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Tenant, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Tenant>());
    }

    private static ProjectVersionTemplateInfo Version(Project project, int major, int minor, bool isActive, string? file, int ageDays = 0)
        => new(Guid.NewGuid(), project.Id, $"{major}.{minor}", major, minor, isActive, DateTime.UtcNow.AddDays(-ageDays), file);

    private void Setup(Project project, params ProjectVersionTemplateInfo[] versions)
    {
        _projectRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Project> { project });
        _versionRepository
            .GetTemplateInfoByProjectIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(versions.ToList());
    }

    [Fact]
    public async Task Handle_WithoutActiveVersion_ShowsTemplateOfNewestDraft()
    {
        var project = new Project(Guid.NewGuid(), null, "PRJ-A", "Umowa");
        Setup(project,
            Version(project, 1, 0, false, "szablon_v1.docx", ageDays: 2),
            Version(project, 1, 1, false, "szablon_v1_1.docx", ageDays: 1));

        var result = await _sut.Handle(new GetProjectsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value!.Items.Single();
        dto.TemplateName.Should().Be("szablon_v1_1.docx");
        dto.TemplateVersionTag.Should().Be("1.1");
        dto.TemplateVersionIsActive.Should().BeFalse();
        dto.ActiveVersionTemplateName.Should().BeNull();
        dto.ActiveVersionTag.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithActiveVersion_PrefersActiveOverNewerDraft()
    {
        var project = new Project(Guid.NewGuid(), null, "PRJ-B", "Pismo");
        var active = Version(project, 1, 0, true, "aktywny.docx", ageDays: 5);
        project.SetActiveVersion(active.VersionId);
        Setup(project, active, Version(project, 2, 0, false, "roboczy.docx"));

        var result = await _sut.Handle(new GetProjectsQuery(), CancellationToken.None);

        var dto = result.Value!.Items.Single();
        dto.TemplateName.Should().Be("aktywny.docx");
        dto.TemplateVersionTag.Should().Be("1.0");
        dto.TemplateVersionIsActive.Should().BeTrue();
        dto.ActiveVersionTemplateName.Should().Be("aktywny.docx");
        dto.ActiveVersionTag.Should().Be("1.0");
    }

    [Fact]
    public async Task Handle_WithoutVersions_FallsBackToProjectDefaultTemplateName()
    {
        var project = new Project(Guid.NewGuid(), null, "PRJ-C", "Bez wersji");
        project.UpdateTemplateName("domyslny.docx");
        Setup(project);

        var result = await _sut.Handle(new GetProjectsQuery(), CancellationToken.None);

        var dto = result.Value!.Items.Single();
        dto.TemplateName.Should().Be("domyslny.docx");
        dto.TemplateVersionTag.Should().BeNull();
        dto.TemplateVersionIsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DraftWithoutUploadedFile_ReturnsNullTemplateName()
    {
        var project = new Project(Guid.NewGuid(), null, "PRJ-D", "Szkic bez pliku");
        Setup(project, Version(project, 1, 0, false, null));

        var result = await _sut.Handle(new GetProjectsQuery(), CancellationToken.None);

        var dto = result.Value!.Items.Single();
        dto.TemplateName.Should().BeNull();
        dto.TemplateVersionTag.Should().Be("1.0");
    }

    [Fact]
    public async Task Handle_WithCorporateKey_ShowsOnlyOwnPrivateAndActiveTeamProjects()
    {
        var team = new Tenant(Guid.NewGuid(), "Kredyty");
        var blockedTeam = new Tenant(Guid.NewGuid(), "Zablokowany");
        blockedTeam.Deactivate();
        var foreignTeam = new Tenant(Guid.NewGuid(), "Obcy");

        var own = new Project(Guid.NewGuid(), null, "OWN", "Mój prywatny", new ProjectMetadata(ownerCorporateKey: "jan"));
        var someoneElses = new Project(Guid.NewGuid(), null, "OTHER", "Cudzy prywatny", new ProjectMetadata(ownerCorporateKey: "anna"));
        var legacy = new Project(Guid.NewGuid(), null, "LEGACY", "Bez właściciela");
        var teamProject = new Project(Guid.NewGuid(), team.Id, "TEAM", "Zespołowy");
        var blockedTeamProject = new Project(Guid.NewGuid(), blockedTeam.Id, "BLOCKED", "Zablokowany zespół");
        var foreignProject = new Project(Guid.NewGuid(), foreignTeam.Id, "FOREIGN", "Obcy zespół");

        _projectRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Project> { own, someoneElses, legacy, teamProject, blockedTeamProject, foreignProject });
        _tenantMemberRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMember, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<TenantMember> { new(Guid.NewGuid(), team.Id, "jan", Guid.NewGuid()), new(Guid.NewGuid(), blockedTeam.Id, "jan", Guid.NewGuid()) });
        _tenantRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Tenant, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(ci => new List<Tenant> { team, blockedTeam, foreignTeam }.AsQueryable().Where(ci.Arg<System.Linq.Expressions.Expression<Func<Tenant, bool>>>()).ToList());
        _versionRepository.GetTemplateInfoByProjectIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProjectVersionTemplateInfo>());

        var result = await _sut.Handle(new GetProjectsQuery { CurrentUserCorporateKey = "jan", PageSize = 50 }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.Items.Select(i => i.Source).Should().BeEquivalentTo("OWN", "TEAM");
        result.Value.Items.Single(i => i.Source == "TEAM").TenantName.Should().Be("Kredyty");
        result.Value.Items.Single(i => i.Source == "OWN").OwnerCorporateKey.Should().Be("jan");
    }

    [Fact]
    public async Task Handle_WithoutCorporateKey_AdminSeesEverything()
    {
        var legacy = new Project(Guid.NewGuid(), null, "LEGACY", "Bez właściciela");
        var foreignProject = new Project(Guid.NewGuid(), Guid.NewGuid(), "FOREIGN", "Obcy zespół");
        _projectRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Project> { legacy, foreignProject });
        _versionRepository.GetTemplateInfoByProjectIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProjectVersionTemplateInfo>());

        var result = await _sut.Handle(new GetProjectsQuery { PageSize = 50 }, CancellationToken.None);

        result.Value!.Items.Should().HaveCount(2);
    }

    [Fact]
    public void ResolveTemplateVersion_ActivePointerWinsOverIsActiveFlag()
    {
        var project = new Project(Guid.NewGuid(), null, "PRJ-E", "Wskaźnik");
        var pointed = Version(project, 1, 0, false, "pointed.docx");
        var flagged = Version(project, 2, 0, true, "flagged.docx");
        project.SetActiveVersion(pointed.VersionId);

        var chosen = GetProjectsQueryHandler.ResolveTemplateVersion(project, new[] { flagged, pointed });

        chosen.Should().BeSameAs(pointed);
    }
}
