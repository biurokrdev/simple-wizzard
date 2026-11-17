using Microsoft.EntityFrameworkCore;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Domain.Common.BaseTypes;
using D2ApiCreator.Domain.Entities;
using System.Reflection;

namespace D2ApiCreator.Infrastructure.Persistence;

/// <summary>
/// Main Application Database Context
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext, IUnitOfWork
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets for D2ApiCreator entities
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantMember> TenantMembers => Set<TenantMember>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectVersion> ProjectVersions => Set<ProjectVersion>();
    public DbSet<FileEntity> Files => Set<FileEntity>();
    public DbSet<ParseResult> ParseResults => Set<ParseResult>();
    public DbSet<Mapping> Mappings => Set<Mapping>();
    public DbSet<Approval> Approvals => Set<Approval>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Worker> Workers => Set<Worker>();
    public DbSet<WorkerProfile> WorkerProfiles => Set<WorkerProfile>();

    /// <summary>Schemat bazy, w którym żyją wszystkie tabele aplikacji (tworzony przez infra/sql/000_create_schema.sql).</summary>
    public const string Schema = "cr";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        // Apply all configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dispatch domain events before saving
        await DispatchDomainEventsAsync(cancellationToken);

        return await base.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        await Database.CommitTransactionAsync(cancellationToken);
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        await Database.RollbackTransactionAsync(cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        var domainEntities = ChangeTracker
            .Entries<AggregateRoot<object>>()
            .Where(x => x.Entity.DomainEvents.Any())
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        domainEntities.ForEach(entity => entity.Entity.ClearDomainEvents());

        // Here you can publish domain events using MediatR or other mechanism
        // For now, we just clear them
        await Task.CompletedTask;
    }
}

