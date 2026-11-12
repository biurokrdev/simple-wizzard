
import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnInit, Output, inject, signal, ViewChild } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ExternalDataService } from '../../services/external-data.service';
import { RecipientDto, BusinessLineDto, TenantDto, StatusDto } from '../../data-access/external-data.model';
import { TenantSelectorComponent } from '../../../tenants/components/tenant-selector/tenant-selector.component';
import { CreateTenantModalComponent } from '../../../tenants/components/create-tenant-modal/create-tenant-modal.component';
import { Tenant } from '../../../tenants/services/tenant.service';

@Component({
  selector: 'app-document-description-step',
  standalone: true,
  imports: [ReactiveFormsModule, TenantSelectorComponent, CreateTenantModalComponent],
  templateUrl: './document-description-step.component.html',
  styleUrls: ['./document-description-step.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DocumentDescriptionStepComponent implements OnInit {
  @Input() generalGroup!: FormGroup;
  @Input() businessGroup!: FormGroup;
  /** Krok gotowy: trzy słowniki wczytane (albo odrzucone) — kreator chowa wskaźnik ładowania. */
  @Output() ready = new EventEmitter<void>();
  private pendingDictionaries = 3;

  private readonly externalDataService = inject(ExternalDataService);

  @ViewChild(TenantSelectorComponent) tenantSelector?: TenantSelectorComponent;

  readonly environmentOptions = ['DEV', 'UAT', 'PREPROD', 'PROD'];
  
  readonly recipients = signal<RecipientDto[]>([]);
  readonly businessLines = signal<BusinessLineDto[]>([]);
  readonly statuses = signal<StatusDto[]>([]);
  readonly showCreateTenantModal = signal(false);
  readonly isLoadingRecipients = signal(false);
  readonly isLoadingBusinessLines = signal(false);
  readonly isLoadingStatuses = signal(false);

  ngOnInit(): void {
    this.loadRecipients();
    this.loadBusinessLines();
    this.loadStatuses();
    this.setupSourceIdentifierGeneration();
  }

  private setupSourceIdentifierGeneration(): void {
    const documentNameControl = this.generalGroup.get('documentName');
    const sourceIdentifierControl = this.generalGroup.get('sourceIdentifier');

    if (documentNameControl && sourceIdentifierControl) {
      // Zablokuj pole sourceIdentifier przed ręczną edycją
      sourceIdentifierControl.disable();

      // Subskrybuj zmiany w polu documentName
      documentNameControl.valueChanges.subscribe((value: string) => {
        const generatedValue = this.generateSourceIdentifier(value || '');
        sourceIdentifierControl.setValue(generatedValue, { emitEvent: false });
      });
    }
  }

  private generateSourceIdentifier(input: string): string {
    if (!input) return '';

    // Zamień polskie znaki na odpowiedniki bez znaków diakrytycznych
    const polishChars: { [key: string]: string } = {
      'ą': 'a', 'ć': 'c', 'ę': 'e', 'ł': 'l', 'ń': 'n', 'ó': 'o', 'ś': 's', 'ź': 'z', 'ż': 'z',
      'Ą': 'A', 'Ć': 'C', 'Ę': 'E', 'Ł': 'L', 'Ń': 'N', 'Ó': 'O', 'Ś': 'S', 'Ź': 'Z', 'Ż': 'Z'
    };

    let result = input;
    for (const [polish, replacement] of Object.entries(polishChars)) {
      result = result.replace(new RegExp(polish, 'g'), replacement);
    }

    // Usuń znaki specjalne i podkreślenia, zostaw tylko litery, cyfry i spacje
    result = result.replace(/[^a-zA-Z0-9\s]/g, '');

    if (!result) return '';

    // Zastosuj PascalCase: każda litera po spacji, cyfrze lub na początku wielka
    let pascalCase = '';
    let shouldCapitalize = true; // Pierwsza litera wielka

    for (let i = 0; i < result.length; i++) {
      const char = result[i];
      
      if (/\s/.test(char)) {
        // Spacja - pomijamy i zaznaczamy że następna litera powinna być wielka
        shouldCapitalize = true;
      } else if (/[0-9]/.test(char)) {
        // Cyfra - dodaj bez zmian i zaznacz że następna litera powinna być wielka
        pascalCase += char;
        shouldCapitalize = true;
      } else if (/[a-zA-Z]/.test(char)) {
        // Litera - jeśli powinna być wielka (po spacji, cyfrze lub na początku)
        if (shouldCapitalize) {
          pascalCase += char.toUpperCase();
          shouldCapitalize = false;
        } else {
          pascalCase += char.toLowerCase();
        }
      }
    }

    return pascalCase;
  }

  private markDictionaryLoaded(): void {
    this.pendingDictionaries -= 1;
    if (this.pendingDictionaries === 0) {
      this.ready.emit();
    }
  }

  private loadRecipients(): void {
    console.log('🔄 Loading recipients...');
    this.isLoadingRecipients.set(true);
    this.externalDataService.getActiveRecipients().subscribe({
      next: (data: RecipientDto[]) => {
        console.log('✅ Recipients loaded:', data);
        this.recipients.set(data);
        this.isLoadingRecipients.set(false);
        this.markDictionaryLoaded();
      },
      error: (error: any) => {
        console.error('❌ Error loading recipients:', error);
        console.error('Error details:', {
          status: error.status,
          statusText: error.statusText,
          url: error.url,
          message: error.message
        });
        this.isLoadingRecipients.set(false);
        this.markDictionaryLoaded();
      }
    });
  }

  private loadBusinessLines(): void {
    console.log('🔄 Loading business lines...');
    this.isLoadingBusinessLines.set(true);
    this.externalDataService.getActiveBusinessLines().subscribe({
      next: (data: BusinessLineDto[]) => {
        console.log('✅ Business lines loaded:', data);
        this.businessLines.set(data);
        this.isLoadingBusinessLines.set(false);
        this.markDictionaryLoaded();
      },
      error: (error: any) => {
        console.error('❌ Error loading business lines:', error);
        console.error('Error details:', {
          status: error.status,
          statusText: error.statusText,
          url: error.url,
          message: error.message
        });
        this.isLoadingBusinessLines.set(false);
        this.markDictionaryLoaded();
      }
    });
  }

  private loadStatuses(): void {
    console.log('🔄 Loading statuses...');
    this.isLoadingStatuses.set(true);
    this.externalDataService.getActiveStatuses().subscribe({
      next: (data: StatusDto[]) => {
        console.log('✅ Statuses loaded:', data);
        this.statuses.set(data);
        this.isLoadingStatuses.set(false);
        this.markDictionaryLoaded();
      },
      error: (error: any) => {
        console.error('❌ Error loading statuses:', error);
        console.error('Error details:', {
          status: error.status,
          statusText: error.statusText,
          url: error.url,
          message: error.message
        });
        this.isLoadingStatuses.set(false);
        this.markDictionaryLoaded();
      }
    });
  }

  onCreateNewTenant(): void {
    this.showCreateTenantModal.set(true);
  }

  onTenantCreated(tenant: Tenant): void {
    console.log('✅ Tenant created, refreshing list:', tenant);
    this.showCreateTenantModal.set(false);
    
    // Odśwież listę tenantów w selectorze
    if (this.tenantSelector) {
      this.tenantSelector.loadTenants();
      // Ustaw nowo utworzony tenant jako wybrany
      setTimeout(() => {
        this.businessGroup.patchValue({ tenant: tenant.id });
      }, 300);
    } else {
      console.warn('⚠️ TenantSelector not available');
    }
  }

  onModalClose(): void {
    console.log('Modal closed');
    this.showCreateTenantModal.set(false);
  }

  get environmentsControl(): FormControl<string[] | null> {
    return this.generalGroup.get('environments') as FormControl<
      string[] | null
    >;
  }

  isEnvironmentSelected(env: string): boolean {
    const value = this.environmentsControl?.value ?? [];
    return value.includes(env);
  }

  onEnvironmentToggle(env: string, checked: boolean): void {
    const current = this.environmentsControl?.value ?? [];
    const set = new Set(current);

    if (checked) {
      set.add(env);
    } else {
      set.delete(env);
    }

    this.environmentsControl.setValue(Array.from(set));
    this.environmentsControl.markAsDirty();
    this.environmentsControl.markAsTouched();
  }

  copySourceIdentifier(): void {
    const sourceIdentifierControl = this.generalGroup.get('sourceIdentifier');
    const value = sourceIdentifierControl?.value;

    if (value) {
      navigator.clipboard.writeText(value).then(
        () => {
          console.log('✅ Source identifier copied to clipboard:', value);
        },
        (err) => {
          console.error('❌ Failed to copy source identifier:', err);
        }
      );
    }
  }
}
