import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonComponent } from '../../../../shared/ui/button/button.component';

export type ValidationNoteType = 'info' | 'error' | 'warning' | 'success';

export interface ValidationNote {
  id: string;
  type: ValidationNoteType;
  message: string;
  details?: string[];
}

@Component({
  selector: 'app-validation-notes',
  standalone: true,
  imports: [CommonModule, ButtonComponent],
  templateUrl: './validation-notes.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './validation-notes.component.scss'
})
export class ValidationNotesComponent implements OnChanges {
  @Input() notes: ValidationNote[] = [];
  @Input() title: string = 'Uwagi do szablonu dokumentu';
  @Input() pageSize: number = 3;
  @Output() noteRemoved = new EventEmitter<string>();

  currentPage = 0;

  ngOnChanges(changes: SimpleChanges): void {
    // Resetuj do pierwszej strony gdy notes się zmienią
    if (changes['notes']) {
      const oldLength = changes['notes'].previousValue?.length || 0;
      const newLength = changes['notes'].currentValue?.length || 0;
      
      console.log('📝 Notes changed:', { oldLength, newLength, currentPage: this.currentPage });
      
      if (!changes['notes'].firstChange) {
        this.currentPage = 0;
        console.log('🔄 Reset currentPage to 0');
      }
    }
  }

  get totalPages(): number {
    return Math.ceil(this.notes.length / this.pageSize);
  }

  get paginatedNotes(): ValidationNote[] {
    const startIndex = this.currentPage * this.pageSize;
    return this.notes.slice(startIndex, startIndex + this.pageSize);
  }

  get hasPrevPage(): boolean {
    return this.currentPage > 0;
  }

  get hasNextPage(): boolean {
    return this.currentPage < this.totalPages - 1;
  }

  getTypeLabel(type: ValidationNoteType): string {
    const labels: Record<ValidationNoteType, string> = {
      info: 'Informacje',
      error: 'Błąd',
      warning: 'Ostrzeżenie',
      success: 'Sukces'
    };
    return labels[type];
  }

  removeNote(noteId: string): void {
    this.noteRemoved.emit(noteId);
  }

  nextPage(): void {
    console.log('➡️ Next page clicked. Before:', { currentPage: this.currentPage, totalPages: this.totalPages });
    if (this.hasNextPage) {
      this.currentPage++;
      console.log('➡️ After:', { currentPage: this.currentPage });
    }
  }

  prevPage(): void {
    console.log('⬅️ Prev page clicked. Before:', { currentPage: this.currentPage, totalPages: this.totalPages });
    if (this.hasPrevPage) {
      this.currentPage--;
      console.log('⬅️ After:', { currentPage: this.currentPage });
    }
  }

  goToPage(page: number): void {
    if (page >= 0 && page < this.totalPages) {
      this.currentPage = page;
    }
  }

  trackByNoteId(index: number, note: ValidationNote): string {
    return note.id;
  }
}
