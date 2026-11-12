using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.Features.Approvals;
using D2ApiCreator.Application.Features.Tenants.Admin;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Enums;
using D2ApiCreator.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Linq.Expressions;

namespace D2ApiCreator.Tests.Unit.Features.Approvals;

/// <summary>BR-017: Draft → PendingApproval → Approved | RequiresChanges; BR-018: blokada członka zespołu.</summary>
public class ApprovalCommandsTests
{
    private readonly IRepository<ProjectVersion, Guid> _versions = Substitute.For<IRepository<ProjectVersion, Guid>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private ProjectVersion Version(ProjectStatus status = ProjectStatus.Draft)
    {
        var version = new ProjectVersion(Guid.NewGuid(), Guid.NewGuid(), 1, 0);
        if (status != ProjectStatus.Draft) version.UpdateStatus(status);
        _versions.GetByIdAsync(version.Id, Arg.Any<CancellationToken>()).Returns(version);
        return version;
    }

    [Fact]
    public async Task Submit_Draft_BecomesPendingApproval()
    {
        var version = Version();
        var result = await new SubmitVersionForReviewCommandHandler(_versions, _unitOfWork)
            .Handle(new SubmitVersionForReviewCommand(version.Id, "jan"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Error);
        version.Status.Should().Be(ProjectStatus.PendingApproval);
        version.SubmittedAt.Should().NotBeNull();
        result.Value!.SubmittedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Submit_ActiveVersion_IsRefused()
    {
        var version = Version(ProjectStatus.Active);
        var result = await new SubmitVersionForReviewCommandHandler(_versions, _unitOfWork)
            .Handle(new SubmitVersionForReviewCommand(version.Id, null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reject_RequiresComment_AndSetsRequiresChanges_ThenResubmitAllowed()
    {
        var version = Version(ProjectStatus.PendingApproval);
        var handler = new RejectVersionCommandHandler(_versions, _unitOfWork, Substitute.For<ILogger<RejectVersionCommandHandler>>());

        var noComment = await handler.Handle(new RejectVersionCommand(version.Id, "admin", "  "), CancellationToken.None);
        noComment.IsSuccess.Should().BeFalse();

        var rejected = await handler.Handle(new RejectVersionCommand(version.Id, "admin", "Popraw tag zgody"), CancellationToken.None);
        rejected.IsSuccess.Should().BeTrue(rejected.Error);
        version.Status.Should().Be(ProjectStatus.RequiresChanges);
        version.ReviewComment.Should().Be("Popraw tag zgody");
        version.ReviewedBy.Should().Be("admin");

        var resubmit = await new SubmitVersionForReviewCommandHandler(_versions, _unitOfWork)
            .Handle(new SubmitVersionForReviewCommand(version.Id, "jan"), CancellationToken.None);
        resubmit.IsSuccess.Should().BeTrue();
        version.Status.Should().Be(ProjectStatus.PendingApproval);
        version.ReviewComment.Should().BeNull("ponowne zgłoszenie czyści poprzednią decyzję");
    }

    [Fact]
    public async Task SaveReviewNotes_OnlyWhilePending_DropsEmptyEntries_AndAllowsRejectWithoutGeneralComment()
    {
        var version = Version(ProjectStatus.PendingApproval);
        var notesHandler = new SaveReviewNotesCommandHandler(_versions, _unitOfWork);

        var saved = await notesHandler.Handle(new SaveReviewNotesCommand(version.Id, new Dictionary<string, string>
        {
            ["templateAndSchema"] = "Brak tagu zgody RODO",
            ["configuration"] = "   "
        }), CancellationToken.None);

        saved.IsSuccess.Should().BeTrue(saved.Error);
        saved.Value!.ReviewNotes.Should().BeEquivalentTo(new Dictionary<string, string> { ["templateAndSchema"] = "Brak tagu zgody RODO" });
        version.HasReviewNotes().Should().BeTrue();

        var rejected = await new RejectVersionCommandHandler(_versions, _unitOfWork, Substitute.For<ILogger<RejectVersionCommandHandler>>())
            .Handle(new RejectVersionCommand(version.Id, "admin", null), CancellationToken.None);
        rejected.IsSuccess.Should().BeTrue("uwagi do kroków zastępują komentarz ogólny");
        version.Status.Should().Be(ProjectStatus.RequiresChanges);
        rejected.Value!.ReviewNotes.Should().ContainKey("templateAndSchema");

        var afterDecision = await notesHandler.Handle(new SaveReviewNotesCommand(version.Id, new Dictionary<string, string>()), CancellationToken.None);
        afterDecision.IsSuccess.Should().BeFalse("po decyzji uwag już się nie zmienia");

        await new SubmitVersionForReviewCommandHandler(_versions, _unitOfWork).Handle(new SubmitVersionForReviewCommand(version.Id, "jan"), CancellationToken.None);
        version.HasReviewNotes().Should().BeFalse("ponowne zgłoszenie czyści uwagi do kroków");
    }

    [Fact]
    public async Task Approve_Pending_BecomesApproved_WithoutActivating()
    {
        var version = Version(ProjectStatus.PendingApproval);
        var result = await new ApproveVersionCommandHandler(_versions, _unitOfWork, Substitute.For<ILogger<ApproveVersionCommandHandler>>())
            .Handle(new ApproveVersionCommand(version.Id, "admin", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Error);
        version.Status.Should().Be(ProjectStatus.Approved);
        version.IsActive.Should().BeFalse();
        version.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Approve_NotPending_IsRefused()
    {
        var version = Version(ProjectStatus.Draft);
        var result = await new ApproveVersionCommandHandler(_versions, _unitOfWork, Substitute.For<ILogger<ApproveVersionCommandHandler>>())
            .Handle(new ApproveVersionCommand(version.Id, "admin", null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateTenantMember_BlocksAndSetsReadOnly()
    {
        var tenants = Substitute.For<IRepository<Tenant, Guid>>();
        var members = Substitute.For<IRepository<TenantMember, Guid>>();
        var tenant = new Tenant(Guid.NewGuid(), "Zespół A", Guid.NewGuid());
        var member = new TenantMember(Guid.NewGuid(), tenant.Id, "jkowalski", Guid.NewGuid());
        tenants.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        members.GetByIdAsync(member.Id, Arg.Any<CancellationToken>()).Returns(member);
        members.FindAsync(Arg.Any<Expression<Func<TenantMember, bool>>>(), Arg.Any<CancellationToken>()).Returns(new List<TenantMember> { member });

        var result = await new UpdateTenantMemberCommandHandler(tenants, members, _unitOfWork)
            .Handle(new UpdateTenantMemberCommand(tenant.Id, member.Id, IsBlocked: true, Access: TenantMemberAccess.ReadOnly), CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Error);
        member.IsBlocked.Should().BeTrue();
        member.Access.Should().Be(TenantMemberAccess.ReadOnly);
        result.Value!.Members.Single().Access.Should().Be("ReadOnly");
        result.Value.Members.Single().IsBlocked.Should().BeTrue();
    }

    [Fact]
    public async Task AdminCreateTenant_AddsMembersWithoutCreator()
    {
        var tenants = Substitute.For<IRepository<Tenant, Guid>>();
        var members = Substitute.For<IRepository<TenantMember, Guid>>();
        tenants.FindAsync(Arg.Any<Expression<Func<Tenant, bool>>>(), Arg.Any<CancellationToken>()).Returns(new List<Tenant>());
        var added = new List<TenantMember>();
        members.AddAsync(Arg.Do<TenantMember>(m => added.Add(m)), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<TenantMember>());
        members.FindAsync(Arg.Any<Expression<Func<TenantMember, bool>>>(), Arg.Any<CancellationToken>()).Returns(ci => added);

        var result = await new AdminCreateTenantCommandHandler(tenants, members, _unitOfWork)
            .Handle(new AdminCreateTenantCommand("Zespół B", null, new[] { "anowak", " anowak ", "jkowalski" }, Guid.Empty), CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Error);
        added.Select(m => m.CorporateKey).Should().BeEquivalentTo("anowak", "jkowalski");
        result.Value!.MemberCount.Should().Be(2);
        result.Value.IsActive.Should().BeTrue();
    }
}
