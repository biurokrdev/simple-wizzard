import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ValidationNote } from '../components/validation-notes/validation-notes.component';

export interface TemplateUploadResponse {
  validationNotes: ValidationNote[];
  dataSchema: Record<string, any>;
}

@Injectable({
  providedIn: 'root'
})
export class TemplateUploadService {
  private readonly apiUrl = '/api/templates'; // TODO: Skonfigurować właściwy URL

  constructor(private http: HttpClient) {}

  uploadTemplate(file: File): Observable<TemplateUploadResponse> {
    const formData = new FormData();
    formData.append('file', file, file.name);

    return this.http.post<TemplateUploadResponse>(
      `${this.apiUrl}/upload`,
      formData
    );
  }
}
