import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { D2GridCellDef, D2GridComponent, GridColumn } from '../../../../shared/ui/grid';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { ButtonComponent } from '../../../../shared/ui/button/button.component';
import { PageLayoutComponent } from '../../../../shared/ui/page-layout/page-layout.component';
import { AdminTenantService } from '../../services/admin-tenant.service';
import { ApprovalService, PendingVersion } from '../../services/approval.service';

interface PendingRow extends PendingVersion {
  tenantName: string;
}

/**
 * Administracja → Zgłoszenia do publikacji (BR-017): wersje zgłoszone przez projektantów („Wyślij do weryfikacji”).
 * Administrator ogląda wersję w kreatorze tylko do odczytu (szablon, konfiguracja, workery), potem akceptuje
 * albo odsyła do poprawy z komentarzem — projektant zobaczy status „Wymaga poprawy” i komentarz w szczegółach projektu.
 */
@Component({
  selector: 'app-admin-approvals-page',
  standalone: true,
  imports: [DatePipe, FormsModule, PageLayoutComponent, ButtonComponent, D2GridComponent, D2GridCellDef],
  templateUrl: './admin-approvals-page.component.html',
  styleUrl: './admin-approvals-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminApprovalsPageComponent implements OnInit {
  private readonly approvals = inject(ApprovalService);
  private readonly tenants = inject(AdminTenantService);
  private readonly router = inject(Router);
  private readonly reviewer = inject(AuthService).getCurrentUser()?.name ?? 'admin';

  readonly rows = signal<PendingRow[]>([]);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly notice = signal<string | null>(null);

  /** Dialog decyzji. */
  readonly decision = signal<{ row: PendingRow; kind: 'approve' | 'reject' } | null>(null);
  comment = '';
  readonly isSaving = signal(false);

  protected readonly columns: readonly GridColumn<PendingRow>[] = [
    { key: 'projectName', header: 'Projekt pisma', sortable: true, filterable: true },
    { key: 'source', header: 'Source', sortable: true, filterable: true, collapsible: true },
    { key: 'tenantName', header: 'Tenant', sortable: true, filterable: true, width: '160px' },
    { key: 'versionTag', header: 'Wersja', sortable: true, width: '100px', align: 'center' },
    { key: 'templateName', header: 'Szablon', filterable: true, collapsible: true, value: (r) => r.templateName ?? '' },
    { key: 'submittedAt', header: 'Zgłoszono', sortable: true, width: '150px', searchable: false },
    { key: 'actions', header: 'Akcje', headerHidden: true, searchable: false, width: '300px', align: 'right' },
  ];

  protected readonly trackRow = (r: PendingRow): string => r.versionId;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.error.set(null);
    forkJoin({ pending: this.approvals.getPending(), tenants: this.tenants.getAll() }).subscribe({
      next: ({ pending, tenants }) => {
        const names = new Map(tenants.map((t) => [t.id, t.name]));
        this.rows.set(pending.map((p) => ({ ...p, tenantName: p.tenantId ? (names.get(p.tenantId) ?? '(nieznany)') : 'prywatny' })));
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Błąd wczytywania zgłoszeń:', err);
        this.error.set('Nie udało się wczytać zgłoszeń.');
        this.isLoading.set(false);
      },
    });
  }

  /**
   * Przegląd wersji w kreatorze (tryb `review`): krok po kroku, z polem uwag przy każdym kroku
   * i decyzją (zaakceptuj / odeślij do poprawy) na końcu. „Zamknij” wraca na tę listę.
   */
  preview(row: PendingRow): void {
    this.router.navigate(['/projects', row.projectId, 'wizard'], { queryParams: { versionId: row.versionId, mode: 'review', from: 'admin-approvals' } });
  }

  openDetails(row: PendingRow): void {
    this.router.navigate(['/projects', row.projectId], { queryParams: { from: 'admin-approvals' } });
  }

  openDecision(row: PendingRow, kind: 'approve' | 'reject'): void {
    this.comment = '';
    this.decision.set({ row, kind });
  }

  closeDecision(): void {
    this.decision.set(null);
  }

  confirmDecision(): void {
    const current = this.decision();
    if (!current) {
      return;
    }
    const comment = this.comment.trim();
    if (current.kind === 'reject' && !comment) {
      return;
    }
    this.isSaving.set(true);
    const request$ = current.kind === 'approve'
      ? this.approvals.approve(current.row.versionId, this.reviewer, comment || null)
      : this.approvals.reject(current.row.versionId, this.reviewer, comment);
    request$.subscribe({
      next: () => {
        this.isSaving.set(false);
        this.notice.set(current.kind === 'approve'
          ? `Wersja ${current.row.versionTag} projektu „${current.row.projectName}” zaakceptowana.`
          : `Wersja ${current.row.versionTag} projektu „${current.row.projectName}” odesłana do poprawy.`);
        this.closeDecision();
        this.load();
      },
      error: (err) => {
        console.error('Błąd decyzji:', err);
        this.isSaving.set(false);
        this.error.set(err?.error?.message ?? 'Nie udało się zapisać decyzji.');
      },
    });
  }
}
