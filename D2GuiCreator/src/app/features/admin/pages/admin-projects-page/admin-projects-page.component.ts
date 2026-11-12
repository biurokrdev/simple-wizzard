import { formatDate } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { D2GridCellDef, D2GridComponent, GridColumn } from 'd2-gui-shared';
import { forkJoin } from 'rxjs';
import { PageLayoutComponent } from '../../../../shared/ui/page-layout/page-layout.component';
import { ProjectResponse, WizardDataService } from '../../../letter-wizard/services/wizard-data.service';
import { AdminTenant, AdminTenantService } from '../../services/admin-tenant.service';

interface AdminProjectRow {
  id: string;
  name: string;
  source: string;
  tenantId: string | null;
  tenantName: string;
  templateName: string | null;
  templateVersionTag: string | null;
  templateVersionIsActive: boolean;
  createdBy: string | null;
  createdAt: string;
  hasActiveVersion: boolean;
}

/** Administracja → Projekty pism: wszystkie projekty w Kreatorze (bez filtra członkostwa) z kolumną „Tenant”. */
@Component({
  selector: 'app-admin-projects-page',
  standalone: true,
  imports: [PageLayoutComponent, D2GridComponent, D2GridCellDef],
  templateUrl: './admin-projects-page.component.html',
  styleUrl: './admin-projects-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminProjectsPageComponent implements OnInit {
  private readonly projectsApi = inject(WizardDataService);
  private readonly tenantsApi = inject(AdminTenantService);
  private readonly router = inject(Router);

  readonly rows = signal<AdminProjectRow[]>([]);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly tenantCount = computed(() => new Set(this.rows().map((r) => r.tenantId).filter(Boolean)).size);

  protected readonly columns: readonly GridColumn<AdminProjectRow>[] = [
    { key: 'name', header: 'Nazwa dokumentu', sortable: true, filterable: true },
    { key: 'source', header: 'Source', sortable: true, filterable: true },
    { key: 'tenantName', header: 'Tenant', sortable: true, filterable: true, width: '170px' },
    { key: 'templateName', header: 'Szablon', sortable: true, filterable: true, collapsible: true, value: (r) => r.templateName ?? '' },
    { key: 'hasActiveVersion', header: 'Stan', sortable: true, width: '130px', searchable: false,
      value: (r) => (r.hasActiveVersion ? 1 : 0), format: (v) => (v ? 'aktywny' : 'roboczy') },
    { key: 'createdBy', header: 'Autor', sortable: true, filterable: true, collapsible: true, value: (r) => r.createdBy ?? '' },
    { key: 'createdAt', header: 'Utworzono', sortable: true, width: '150px', format: (v) => (v ? formatDate(String(v), 'dd.MM.yyyy HH:mm', 'en-US') : '') },
    { key: 'actions', header: 'Akcje', headerHidden: true, searchable: false, width: '110px', align: 'right' },
  ];

  protected readonly trackRow = (r: AdminProjectRow): string => r.id;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.error.set(null);
    forkJoin({
      projects: this.projectsApi.getProjects(undefined, undefined, 1, 500),
      tenants: this.tenantsApi.getAll(),
    }).subscribe({
      next: ({ projects, tenants }) => {
        const byId = new Map<string, AdminTenant>(tenants.map((t) => [t.id, t]));
        this.rows.set(projects.items.map((p: ProjectResponse) => ({
          id: p.id,
          name: p.name,
          source: p.source,
          tenantId: p.tenantId,
          tenantName: p.tenantId ? (p.tenantName ?? byId.get(p.tenantId)?.name ?? '(nieznany)') : 'prywatny',
          templateName: p.templateName,
          templateVersionTag: p.templateVersionTag,
          templateVersionIsActive: p.templateVersionIsActive,
          createdBy: p.createdBy,
          createdAt: p.createdAt,
          hasActiveVersion: !!p.currentActiveVersionId,
        })));
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Błąd wczytywania projektów (admin):', err);
        this.error.set('Nie udało się wczytać projektów.');
        this.isLoading.set(false);
      },
    });
  }

  /** Szczegóły projektu z kontekstem administracyjnym — „Wróć” prowadzi z powrotem tutaj. */
  open(row: AdminProjectRow): void {
    this.router.navigate(['/projects', row.id], { queryParams: { from: 'admin-projects' } });
  }
}
