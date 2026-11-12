import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { D2GridCellDef, D2GridComponent, D2GridDetailDef, GridColumn, GridLabels } from 'd2-gui-shared';
import { Router } from '@angular/router';
import { ButtonComponent } from '../../../../shared/ui/button/button.component';
import { ProjectResponse, WizardDataService } from '../../../letter-wizard/services/wizard-data.service';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { PageLayoutComponent } from '../../../../shared/ui/page-layout/page-layout.component';
import { AdminTenant, AdminTenantMember, AdminTenantService, TenantMemberAccess } from '../../services/admin-tenant.service';

/**
 * Administracja → Zespoły (tenanci), BR-018: przegląd wszystkich zespołów, tworzenie zespołu z listą
 * użytkowników, blokada zespołu, a w szczegółach wiersza: blokada użytkownika, dostęp tylko do odczytu,
 * usunięcie użytkownika i dodanie nowego.
 */
@Component({
  selector: 'app-admin-tenants-page',
  standalone: true,
  imports: [DatePipe, FormsModule, PageLayoutComponent, ButtonComponent, D2GridComponent, D2GridCellDef, D2GridDetailDef, EmptyStateComponent],
  templateUrl: './admin-tenants-page.component.html',
  styleUrl: './admin-tenants-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminTenantsPageComponent implements OnInit {
  private readonly service = inject(AdminTenantService);
  private readonly projectsApi = inject(WizardDataService);
  private readonly router = inject(Router);

  readonly tenants = signal<AdminTenant[]>([]);

  /** Zakładka w szczegółach wiersza (per zespół): użytkownicy albo projekty pism zespołu. */
  readonly detailTab = signal<Record<string, 'members' | 'projects'>>({});
  /** Projekty zespołu wczytywane przy pierwszym wejściu na zakładkę „Projekty pism”. */
  readonly tenantProjects = signal<Record<string, ProjectResponse[]>>({});
  readonly projectsLoading = signal<Record<string, boolean>>({});
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly busyId = signal<string | null>(null);
  readonly blockedCount = computed(() => this.tenants().filter((t) => !t.isActive).length);

  /** Dialog nowego zespołu. */
  readonly isCreateOpen = signal(false);
  readonly isSaving = signal(false);
  newName = '';
  newDescription = '';
  newMembers = '';

  /** Pole „dodaj użytkownika” w szczegółach wiersza (per zespół). */
  readonly newMemberKey = signal<Record<string, string>>({});

  protected readonly columns: readonly GridColumn<AdminTenant>[] = [
    { key: 'isActive', header: 'Stan', width: '120px', align: 'center', sortable: true, searchable: false,
      value: (t) => (t.isActive ? 1 : 0), format: (v) => (v ? 'aktywny' : 'zablokowany') },
    { key: 'name', header: 'Zespół', sortable: true, filterable: true },
    { key: 'description', header: 'Opis', filterable: true, value: (t) => t.description ?? '' },
    { key: 'memberCount', header: 'Użytkownicy', sortable: true, width: '130px', align: 'center', searchable: false },
    { key: 'members', header: 'Loginy', filterable: true, collapsible: true, value: (t) => t.members.map((m) => m.corporateKey).join(', ') },
    { key: 'createdAt', header: 'Utworzono', sortable: true, width: '150px', searchable: false },
    { key: 'actions', header: 'Akcje', headerHidden: true, searchable: false, width: '150px', align: 'right' },
  ];

  protected readonly gridLabels: Partial<GridLabels> = {
    expandRow: 'Pokaż użytkowników zespołu',
    collapseRow: 'Ukryj użytkowników zespołu',
    empty: 'Brak zespołów — dodaj pierwszy.',
  };

  protected readonly trackTenant = (t: AdminTenant): string => t.id;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.service.getAll().subscribe({
      next: (tenants) => {
        this.tenants.set(tenants);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Błąd wczytywania zespołów:', err);
        this.error.set('Nie udało się wczytać zespołów.');
        this.isLoading.set(false);
      },
    });
  }

  // --- zespół -------------------------------------------------------------

  toggleTenantActive(tenant: AdminTenant): void {
    const target = !tenant.isActive;
    if (!target && !confirm(`Zablokować zespół „${tenant.name}”? Jego użytkownicy stracą dostęp do projektów zespołu.`)) {
      return;
    }
    this.run(tenant.id, this.service.setActive(tenant.id, target));
  }

  openCreate(): void {
    this.newName = '';
    this.newDescription = '';
    this.newMembers = '';
    this.isCreateOpen.set(true);
  }

  closeCreate(): void {
    this.isCreateOpen.set(false);
  }

  parsedNewMembers(): string[] {
    return [...new Set(this.newMembers.split(/[\n,;]+/).map((k) => k.trim()).filter(Boolean))];
  }

  create(): void {
    const name = this.newName.trim();
    if (!name) {
      return;
    }
    this.isSaving.set(true);
    this.service.create(name, this.newDescription.trim() || null, this.parsedNewMembers()).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.closeCreate();
        this.load();
      },
      error: (err) => {
        console.error('Błąd tworzenia zespołu:', err);
        this.isSaving.set(false);
        this.error.set(err?.error?.message ?? 'Nie udało się utworzyć zespołu.');
      },
    });
  }

  // --- użytkownicy w zespole -------------------------------------------------

  toggleBlocked(tenant: AdminTenant, member: AdminTenantMember): void {
    this.run(tenant.id, this.service.updateMember(tenant.id, member.id, { isBlocked: !member.isBlocked }));
  }

  toggleReadOnly(tenant: AdminTenant, member: AdminTenantMember): void {
    const access: TenantMemberAccess = member.access === 'ReadOnly' ? 'ReadWrite' : 'ReadOnly';
    this.run(tenant.id, this.service.updateMember(tenant.id, member.id, { access }));
  }

  removeMember(tenant: AdminTenant, member: AdminTenantMember): void {
    if (!confirm(`Usunąć użytkownika „${member.corporateKey}” z zespołu „${tenant.name}”?`)) {
      return;
    }
    this.run(tenant.id, this.service.removeMember(tenant.id, member.id));
  }

  setNewMemberKey(tenantId: string, value: string): void {
    this.newMemberKey.update((m) => ({ ...m, [tenantId]: value }));
  }

  addMember(tenant: AdminTenant): void {
    const key = (this.newMemberKey()[tenant.id] ?? '').trim();
    if (!key) {
      return;
    }
    this.run(tenant.id, this.service.addMember(tenant.id, key, 'ReadWrite'), () => this.setNewMemberKey(tenant.id, ''));
  }

  // --- zakładki szczegółów: użytkownicy / projekty pism -------------------------------------------

  tabOf(tenantId: string): 'members' | 'projects' {
    return this.detailTab()[tenantId] ?? 'members';
  }

  selectTab(tenant: AdminTenant, tab: 'members' | 'projects'): void {
    this.detailTab.update((m) => ({ ...m, [tenant.id]: tab }));
    if (tab === 'projects' && !(tenant.id in this.tenantProjects())) {
      this.loadTenantProjects(tenant.id);
    }
  }

  loadTenantProjects(tenantId: string): void {
    this.projectsLoading.update((m) => ({ ...m, [tenantId]: true }));
    this.projectsApi.getProjects(tenantId, undefined, 1, 500).subscribe({
      next: (page) => {
        this.tenantProjects.update((m) => ({ ...m, [tenantId]: page.items }));
        this.projectsLoading.update((m) => ({ ...m, [tenantId]: false }));
      },
      error: (err) => {
        console.error('Błąd wczytywania projektów zespołu:', err);
        this.tenantProjects.update((m) => ({ ...m, [tenantId]: [] }));
        this.projectsLoading.update((m) => ({ ...m, [tenantId]: false }));
        this.error.set('Nie udało się wczytać projektów zespołu.');
      },
    });
  }

  openProject(project: ProjectResponse): void {
    this.router.navigate(['/projects', project.id], { queryParams: { from: 'admin-projects' } });
  }

  accessLabel(member: AdminTenantMember): string {
    return member.access === 'ReadOnly' ? 'tylko odczyt' : 'edycja';
  }

  /** Wywołanie API dla zespołu: blokada wiersza na czas żądania, podmiana zespołu w liście z odpowiedzi. */
  private run(tenantId: string, request$: ReturnType<AdminTenantService['setActive']>, after?: () => void): void {
    this.busyId.set(tenantId);
    this.error.set(null);
    request$.subscribe({
      next: (updated) => {
        this.tenants.update((list) => list.map((t) => (t.id === updated.id ? updated : t)));
        this.busyId.set(null);
        after?.();
      },
      error: (err) => {
        console.error('Błąd operacji na zespole:', err);
        this.error.set(err?.error?.message ?? 'Operacja nie powiodła się.');
        this.busyId.set(null);
      },
    });
  }
}
