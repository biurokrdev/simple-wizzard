import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ContentControlDetail, PlaceholderDetail } from '../models/content-control-kind';

/**
 * Wynik walidacji dokumentu
 */
export interface DocumentValidationResult {
  isValid: boolean;
  errors: ValidationError[];
  warnings: ValidationWarning[];
  metadata?: DocumentMetadata;
}

export interface ValidationError {
  code: string;
  message: string;
  location?: string;
  severity: string;
}

export interface ValidationWarning {
  code: string;
  message: string;
  location?: string;
}

export interface DocumentMetadata {
  pageCount: number;
  characterCount: number;
  fileSizeBytes: number;
  format: string;
  containsMacros: boolean;
}

/**
 * Wynik ekstrakcji znaczników i content controls
 */
export interface PlaceholderExtractionResult {
  placeholders: string[];
  /** Nazwa formantu → typ w schemacie: `boolean` (wariant) albo `array` (powtarzany / lista / tabela). */
  contentControls: Record<string, string>;
  /** Rodzaj i kontekst każdego formantu (od 2026-11-08; starsze odpowiedzi mogą go nie mieć). */
  contentControlDetails?: ContentControlDetail[];
  /** `docx` | `xlsx` (od 2026-11-08). */
  source?: 'docx' | 'xlsx' | string;
  /** XLSX: etykieta i komórka każdego luźnego znacznika. */
  placeholderDetails?: PlaceholderDetail[];
  placeholdersCount: number;
  contentControlsCount: number;
  fileName: string;
}

/**
 * Serwis do obsługi operacji na dokumentach Word (walidacja, ekstrakcja znaczników)
 */
@Injectable({
  providedIn: 'root'
})
export class DocumentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/v1/documents`;

  /**
   * Waliduje szablon dokumentu Word
   */
  validateTemplate(file: File): Observable<DocumentValidationResult> {
    const formData = new FormData();
    formData.append('file', file, file.name);

    console.log('📤 Validating template:', file.name);
    
    return this.http.post<DocumentValidationResult>(
      `${this.baseUrl}/validate-template`,
      formData
    );
  }

  /**
   * Ekstraktuje znaczniki <%%> i content controls z dokumentu Word
   */
  extractPlaceholders(file: File): Observable<PlaceholderExtractionResult> {
    const formData = new FormData();
    formData.append('file', file, file.name);

    console.log('📤 Extracting placeholders and content controls from:', file.name);
    
    return this.http.post<PlaceholderExtractionResult>(
      `${this.baseUrl}/extract-placeholders`,
      formData
    );
  }
}
