import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { PageLayoutComponent } from '../../../../shared/ui/page-layout/page-layout.component';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { WizardDataService } from '../../../letter-wizard/services/wizard-data.service';
import { AdminTenantService, AdminTenant } from '../../services/admin-tenant.service';
import { ApprovalService, PendingVersion } from '../../services/approval.service';
import { WorkerProfileService } from '../../services/worker-profile.service';

interface PendingRow extends PendingVersion {
  tenantName: string;
}

/**
 * Administracja → Przegląd (`/admin/dashboard`, strona startowa modułu): kafelki z liczbami
 * (projekty, zgłoszenia, zespoły, profile workerów) i lista najstarszych zgłoszeń czekających na decyzję.
 */
@Component({
  selector: 'app-admin-dashboard-page',
  standalone: true,
  imports: [DatePipe, RouterLink, PageLayoutComponent, EmptyStateComponent],
  templateUrl: './admin-dashboard-page.component.html',
  styleUrl: './admin-dashboard-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminDashboardPageComponent implements OnInit {
  private readonly projectsApi = inject(WizardDataService);
  private readonly tenantsApi = inject(AdminTenantService);
  private readonly approvalsApi = inject(ApprovalService);
  private readonly profilesApi = inject(WorkerProfileService);
  private readonly router = inject(Router);

  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);

  readonly projectCount = signal(0);
  readonly activeProjectCount = signal(0);
  readonly pending = signal<PendingRow[]>([]);
  readonly tenants = signal<AdminTenant[]>([]);
  readonly profileCount = signal(0);
  readonly visibleProfileCount = signal(0);

  readonly blockedTenantCount = computed(() => this.tenants().filter((t) => !t.isActive).length);
  readonly memberCount = computed(() => this.tenants().reduce((sum, t) => sum + t.memberCount, 0));
  readonly oldestPending = computed(() => this.pending().slice(0, 5));

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.error.set(null);
    forkJoin({
      projects: this.projectsApi.getProjects(undefined, undefined, 1, 500),
      pending: this.approvalsApi.getPending(),
      tenants: this.tenantsApi.getAll(),
      profiles: this.profilesApi.getAll()
    }).subscribe({
      next: ({ projects, pending, tenants, profiles }) => {
        const names = new Map(tenants.map((t) => [t.id, t.name]));
        this.projectCount.set(projects.totalCount);
        this.activeProjectCount.set(projects.items.filter((p) => !!p.currentActiveVersionId).length);
        this.pending.set(pending.map((p) => ({ ...p, tenantName: p.tenantId ? (names.get(p.tenantId) ?? '(nieznany)') : 'prywatny' })));
        this.tenants.set(tenants);
        this.profileCount.set(profiles.length);
        this.visibleProfileCount.set(profiles.filter((p) => p.isVisible).length);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Błąd wczytywania przeglądu administracyjnego:', err);
        this.error.set('Nie udało się wczytać danych przeglądu.');
        this.isLoading.set(false);
      }
    });
  }

  review(row: PendingRow): void {
    this.router.navigate(['/projects', row.projectId, 'wizard'], { queryParams: { versionId: row.versionId, mode: 'review', from: 'admin-approvals' } });
  }
}
