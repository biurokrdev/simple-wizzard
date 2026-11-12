import { Component, Input, Output, EventEmitter, signal, OnInit, inject, forwardRef, ChangeDetectionStrategy } from '@angular/core';

import { FormsModule, NG_VALUE_ACCESSOR, ControlValueAccessor } from '@angular/forms';
import { TenantService, Tenant } from '../../services/tenant.service';
import { ButtonComponent } from '../../../../shared/ui/button/button.component';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-tenant-selector',
  standalone: true,
  imports: [FormsModule, ButtonComponent],
  templateUrl: './tenant-selector.component.html',
  styleUrls: ['./tenant-selector.component.scss'],
  changeDetection: ChangeDetectionStrategy.Eager,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => TenantSelectorComponent),
      multi: true
    }
  ]
})
export class TenantSelectorComponent implements ControlValueAccessor, OnInit {
  private readonly tenantService = inject(TenantService);
  private readonly auth = inject(AuthService);

  @Input() corporateKey = this.auth.corporateKey();
  @Input() userId = this.auth.userId();
  @Output() createNewTenant = new EventEmitter<void>();

  readonly tenants = signal<Tenant[]>([]);
  readonly filteredTenants = signal<Tenant[]>([]);
  readonly isLoading = signal(false);
  readonly isOpen = signal(false);
  readonly searchTerm = signal('');
  
  selectedTenant: Tenant | null = null;
  /** Pole wyłączone (podgląd / przegląd) — bez rozwijania listy i czyszczenia. */
  readonly disabled = signal(false);
  /** Wartość z formularza, gdy lista zespołów jeszcze się nie wczytała — rozwiązywana po `loadTenants()`. */
  private pendingValue: string | null = null;
  
  private onChange: (value: string | null) => void = () => {};
  private onTouched: () => void = () => {};

  ngOnInit(): void {
    this.loadTenants();
  }

  writeValue(value: string | null): void {
    this.pendingValue = value || null;
    this.resolveSelection();
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
    if (isDisabled) {
      this.isOpen.set(false);
    }
  }

  /**
   * Dopasowuje wybrany zespół do wartości formularza. Gdy lista jeszcze się nie wczytała (podgląd wersji,
   * edycja istniejącego projektu), zapamiętana wartość jest rozwiązywana po jej wczytaniu — wcześniej selektor
   * pokazywał „Wyszukaj lub dodaj zespół…” mimo przypisanego zespołu.
   */
  private resolveSelection(): void {
    const value = this.pendingValue;
    if (!value) {
      this.selectedTenant = null;
      return;
    }
    const tenant = this.tenants().find(t => t.id === value);
    if (tenant) {
      this.selectedTenant = tenant;
    } else if (!this.isLoading() && this.tenants().length > 0) {
      // Zespół spoza listy użytkownika (np. zablokowany albo użytkownik już nie jest członkiem) — pokaż identyfikator
      this.selectedTenant = { id: value, name: 'Zespół niedostępny', memberCount: 0, members: [], createdAt: '' } as Tenant;
    }
  }

  registerOnChange(fn: (value: string | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  loadTenants(): void {
    this.isLoading.set(true);
    this.tenantService.getTenants(this.corporateKey).subscribe({
      next: (tenants) => {
        this.tenants.set(tenants);
        this.filteredTenants.set(tenants);
        this.isLoading.set(false);
        this.resolveSelection();
      },
      error: (error) => {
        console.error('Error loading tenants:', error);
        this.isLoading.set(false);
      }
    });
  }

  onSearchChange(value: string): void {
    this.searchTerm.set(value);
    
    if (!value.trim()) {
      this.filteredTenants.set(this.tenants());
      return;
    }

    const filtered = this.tenants().filter(t =>
      t.name.toLowerCase().includes(value.toLowerCase())
    );
    this.filteredTenants.set(filtered);
  }

  toggleDropdown(): void {
    if (this.disabled()) {
      return;
    }
    this.isOpen.set(!this.isOpen());
    if (this.isOpen()) {
      this.onTouched();
    }
  }

  selectTenant(tenant: Tenant): void {
    this.selectedTenant = tenant;
    this.pendingValue = tenant.id;
    this.onChange(tenant.id);
    this.isOpen.set(false);
    this.searchTerm.set('');
    this.filteredTenants.set(this.tenants());
  }

  clearSelection(): void {
    if (this.disabled()) {
      return;
    }
    this.selectedTenant = null;
    this.pendingValue = null;
    this.onChange(null);
  }

  onCreateNew(): void {
    this.createNewTenant.emit();
    this.isOpen.set(false);
  }

  getDisplayText(): string {
    if (this.selectedTenant) {
      return `${this.selectedTenant.name} (${this.selectedTenant.memberCount})`;
    }
    return 'Wybierz zespół projektowy...';
  }
}
