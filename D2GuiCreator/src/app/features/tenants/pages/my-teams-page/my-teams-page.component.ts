import { Component, OnInit, signal, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TenantService, Tenant } from '../../services/tenant.service';
import { CreateTenantModalComponent } from '../../components/create-tenant-modal/create-tenant-modal.component';
import { PageLayoutComponent } from '../../../../shared/ui/page-layout/page-layout.component';
import { ButtonComponent } from '../../../../shared/ui/button/button.component';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-my-teams-page',
  standalone: true,
  imports: [CommonModule, FormsModule, CreateTenantModalComponent, PageLayoutComponent, ButtonComponent],
  templateUrl: './my-teams-page.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./my-teams-page.component.scss']
})
export class MyTeamsPageComponent implements OnInit {
  private readonly tenantService = inject(TenantService);
  private readonly auth = inject(AuthService);

  readonly tenants = signal<Tenant[]>([]);
  readonly isLoading = signal(false);
  readonly showCreateModal = signal(false);
  readonly selectedTenant = signal<Tenant | null>(null);
  readonly newMemberKey = signal('');

  corporateKey = this.auth.corporateKey();
  userId = this.auth.userId();

  ngOnInit(): void {
    this.loadTenants();
  }

  loadTenants(): void {
    this.isLoading.set(true);
    this.tenantService.getTenants(this.corporateKey).subscribe({
      next: (tenants) => {
        this.tenants.set(tenants);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading tenants:', error);
        this.isLoading.set(false);
      }
    });
  }

  onCreateTenant(): void {
    this.showCreateModal.set(true);
  }

  onTenantCreated(tenant: Tenant): void {
    this.tenants.set([...this.tenants(), tenant]);
    this.showCreateModal.set(false);
  }

  onCloseModal(): void {
    this.showCreateModal.set(false);
  }

  selectTenant(tenant: Tenant): void {
    this.selectedTenant.set(this.selectedTenant()?.id === tenant.id ? null : tenant);
  }

  isCreator(tenant: Tenant): boolean {
    return tenant.createdBy === this.userId;
  }

  canRemoveMember(tenant: Tenant, memberKey: string): boolean {
    return this.isCreator(tenant) || memberKey === this.corporateKey;
  }

  addMember(tenant: Tenant): void {
    const key = this.newMemberKey().trim();
    if (!key) {
      alert('Wprowadź Corporate Key członka');
      return;
    }

    if (tenant.members.some(m => m.corporateKey === key)) {
      alert('Ten członek już należy do zespołu');
      return;
    }

    this.tenantService.addMember(tenant.id, { corporateKey: key }, this.userId, this.corporateKey).subscribe({
      next: () => {
        console.log('✅ Member added');
        this.newMemberKey.set('');
        this.loadTenants(); // Reload to get updated data
      },
      error: (error) => {
        console.error('❌ Error adding member:', error);
        alert('Błąd podczas dodawania członka');
      }
    });
  }

  removeMember(tenant: Tenant, memberId: string): void {
    if (!confirm('Czy na pewno chcesz usunąć tego członka?')) {
      return;
    }

    this.tenantService.removeMember(tenant.id, memberId, this.userId, this.corporateKey).subscribe({
      next: () => {
        console.log('✅ Member removed');
        this.loadTenants();
      },
      error: (error) => {
        console.error('❌ Error removing member:', error);
        alert('Błąd podczas usuwania członka');
      }
    });
  }

  deleteTenant(tenant: Tenant): void {
    if (!this.isCreator(tenant)) {
      alert('Tylko twórca zespołu może go usunąć');
      return;
    }

    if (!confirm(`Czy na pewno chcesz usunąć zespół "${tenant.name}"?\n\nUwaga: Możesz usunąć tylko zespół bez przypisanych projektów.`)) {
      return;
    }

    this.tenantService.deleteTenant(tenant.id, this.userId).subscribe({
      next: () => {
        console.log('✅ Tenant deleted');
        this.tenants.set(this.tenants().filter(t => t.id !== tenant.id));
        if (this.selectedTenant()?.id === tenant.id) {
          this.selectedTenant.set(null);
        }
      },
      error: (error) => {
        console.error('❌ Error deleting tenant:', error);
        alert('Błąd podczas usuwania zespołu. Upewnij się, że nie ma przypisanych projektów.');
      }
    });
  }
}
