import { Component, Output, EventEmitter, signal, inject, ChangeDetectionStrategy } from '@angular/core';

import { FormsModule } from '@angular/forms';
import { TenantService, Tenant } from '../../services/tenant.service';
import { ButtonComponent } from '../../../../shared/ui/button/button.component';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-create-tenant-modal',
  standalone: true,
  imports: [FormsModule, ButtonComponent],
  templateUrl: './create-tenant-modal.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./create-tenant-modal.component.scss']
})
export class CreateTenantModalComponent {
  private readonly tenantService = inject(TenantService);
  private readonly auth = inject(AuthService);

  @Output() close = new EventEmitter<void>();
  @Output() created = new EventEmitter<Tenant>();

  readonly isVisible = signal(true);
  readonly isSaving = signal(false);
  
  name = '';
  description = '';
  newMemberKey = '';
  members = signal<string[]>([]);

  corporateKey = this.auth.corporateKey();
  userId = this.auth.userId();

  onClose(): void {
    this.isVisible.set(false);
    setTimeout(() => this.close.emit(), 200);
  }

  addMember(): void {
    const key = this.newMemberKey.trim();
    if (!key) return;

    const currentMembers = this.members();
    if (currentMembers.includes(key)) {
      alert('Ten członek został już dodany');
      return;
    }

    this.members.set([...currentMembers, key]);
    this.newMemberKey = '';
  }

  removeMember(key: string): void {
    this.members.set(this.members().filter(m => m !== key));
  }

  onSubmit(): void {
    if (!this.name.trim()) {
      alert('Nazwa zespołu jest wymagana');
      return;
    }

    this.isSaving.set(true);

    const request = {
      name: this.name.trim(),
      description: this.description.trim() || undefined,
      memberCorporateKeys: this.members()
    };

    console.log('🚀 Creating tenant with request:', request);
    console.log('   User ID:', this.userId);
    console.log('   Corporate Key:', this.corporateKey);

    this.tenantService.createTenant(request, this.userId, this.corporateKey).subscribe({
      next: (tenant) => {
        console.log('✅ Tenant created successfully:', tenant);
        this.created.emit(tenant);
        this.onClose();
      },
      error: (error) => {
        console.error('❌ Error creating tenant:', error);
        console.error('   Status:', error.status);
        console.error('   Message:', error.message);
        console.error('   Error details:', error.error);
        this.isSaving.set(false);
        alert(`Błąd podczas tworzenia zespołu: ${error.error?.message || error.message}`);
      }
    });
  }
}
