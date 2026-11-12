
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ProjectListTableComponent } from '../../components/project-list-table/project-list-table.component';
import { PageLayoutComponent } from '../../../../shared/ui/page-layout/page-layout.component';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { Project } from '../../../../shared/models/project.model';
import { createGuid } from '../../../../core/utils/guid.util';
import { WizardDataService, ProjectResponse, PagedResponse } from '../../../letter-wizard/services/wizard-data.service';
import { TenantService, Tenant } from '../../../tenants/services/tenant.service';
import { FormsModule } from '@angular/forms';
import { ButtonComponent } from '../../../../shared/ui/button/button.component';
import { AuthService } from '../../../../core/services/auth.service';

/**
 * Liczba projektów pobierana jednym żądaniem. Stronicowanie, sortowanie i filtry
 * kolumnowe wykonuje `d2-grid` po stronie klienta; filtry zaawansowane nadal
 * zawężają zbiór po stronie serwera (R-27: przy większej liczbie projektów
 * wrócić do stronicowania serwerowego).
 */
const PROJECTS_FETCH_SIZE = 200;

@Component({
  selector: 'app-project-list-page',
  standalone: true,
  imports: [
    FormsModule,
    PageLayoutComponent,
    EmptyStateComponent,
    ProjectListTableComponent,
    ButtonComponent
],
  templateUrl: './project-list-page.component.html',
  styleUrls: ['./project-list-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectListPageComponent implements OnInit {
  private readonly wizardDataService = inject(WizardDataService);
  private readonly tenantService = inject(TenantService);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);

  readonly projects = signal<Project[]>([]);
  readonly isLoading = signal(false);
  readonly tenants = signal<Tenant[]>([]);
  readonly isLoadingTenants = signal(false);
  readonly selectedTenantId = signal<string | null>(null);
  readonly showAdvancedFilters = signal(false);
  
  // Advanced filters
  readonly searchTerm = signal<string>('');
  readonly selectedStatus = signal<string | null>(null);
  readonly dateFrom = signal<string | null>(null);
  readonly dateTo = signal<string | null>(null);

  /** Login zalogowanego użytkownika (mock do czasu R-03) — filtr członkostwa po stronie API. */
  readonly corporateKey = this.auth.corporateKey();

  ngOnInit(): void {
    this.loadTenants();
    this.loadProjects();
  }

  private loadTenants(): void {
    this.isLoadingTenants.set(true);
    this.tenantService.getTenants(this.corporateKey).subscribe({
      next: (tenants) => {
        this.tenants.set(tenants);
        this.isLoadingTenants.set(false);
      },
      error: (error) => {
        console.error('❌ Error loading tenants:', error);
        this.isLoadingTenants.set(false);
      }
    });
  }

  private loadProjects(): void {
    this.isLoading.set(true);
    
    const tenantId = this.selectedTenantId();
    const searchTerm = this.searchTerm();
    const status = this.selectedStatus();
    const dateFrom = this.dateFrom();
    const dateTo = this.dateTo();

    this.wizardDataService.getProjects(
      tenantId || undefined,
      undefined, // source
      1,
      PROJECTS_FETCH_SIZE,
      searchTerm || undefined,
      status || undefined,
      dateFrom || undefined,
      dateTo || undefined,
      this.corporateKey
    ).subscribe({
      next: (response: PagedResponse<ProjectResponse>) => {
        // Map API response to UI model
        const mappedProjects: Project[] = response.items.map(p => ({
          id: p.id,
          name: p.name,
          ownerName: p.createdBy || 'Unknown',
          status: this.mapStatus(p.currentActiveVersionId),
          createdAt: p.createdAt,
          source: p.source,
          activeVersionTemplateName: p.activeVersionTemplateName ?? undefined,
          templateName: p.templateName ?? undefined,
          templateVersionTag: p.templateVersionTag ?? undefined,
          templateVersionIsActive: p.templateVersionIsActive
        }));

        this.projects.set(mappedProjects);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('❌ Error loading projects:', error);
        this.isLoading.set(false);
        this.projects.set([]);
      }
    });
  }

  private mapStatus(currentActiveVersionId: string | null): 'Draft' | 'InProgress' | 'Completed' {
    // Simple status mapping - you can enhance this based on your needs
    if (!currentActiveVersionId) {
      return 'Draft';
    }
    return 'InProgress';
  }

  onCreateProject(): void {
    const newProjectId = createGuid();

    this.router.navigate(['/projects', newProjectId, 'wizard'], {
      state: { isNewProject: true }
    });
  }

  onOpenProject(project: Project): void {
    this.router.navigate(['/projects', project.id]);
  }

  onTenantFilterChange(tenantId: string): void {
    this.selectedTenantId.set(tenantId || null);
    this.applyFilters();
  }

  toggleAdvancedFilters(): void {
    this.showAdvancedFilters.set(!this.showAdvancedFilters());
  }

  clearAllFilters(): void {
    this.selectedTenantId.set(null);
    this.searchTerm.set('');
    this.selectedStatus.set(null);
    this.dateFrom.set(null);
    this.dateTo.set(null);
    this.applyFilters();
  }

  applyFilters(): void {
    this.loadProjects();
  }

  onSearchChange(value: string): void {
    this.searchTerm.set(value);
    this.applyFilters();
  }

  onStatusChange(status: string): void {
    this.selectedStatus.set(status || null);
    this.applyFilters();
  }

  onDateFromChange(date: string): void {
    this.dateFrom.set(date || null);
    this.applyFilters();
  }

  onDateToChange(date: string): void {
    this.dateTo.set(date || null);
    this.applyFilters();
  }

  hasActiveFilters(): boolean {
    return !!(
      this.selectedTenantId() ||
      this.searchTerm() ||
      this.selectedStatus() ||
      this.dateFrom() ||
      this.dateTo()
    );
  }

  getActiveFiltersCount(): number {
    let count = 0;
    if (this.selectedTenantId()) count++;
    if (this.searchTerm()) count++;
    if (this.selectedStatus()) count++;
    if (this.dateFrom()) count++;
    if (this.dateTo()) count++;
    return count;
  }

  getSelectedTenantName(): string {
    const tenantId = this.selectedTenantId();
    if (!tenantId) return '';
    const tenant = this.tenants().find(t => t.id === tenantId);
    return tenant?.name || '';
  }

}
