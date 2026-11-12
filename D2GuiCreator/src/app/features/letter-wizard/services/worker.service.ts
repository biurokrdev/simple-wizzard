import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Worker, GenerateWorkersRequest } from '../models/worker.model';
import { WorkerFormData } from '../models/worker-form.model';

/**
 * Service for worker management (field mapping configuration)
 */
@Injectable({
  providedIn: 'root'
})
export class WorkerService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/v1/workers`;

  /**
   * Generate workers from document placeholders and content controls
   */
  generateWorkers(request: GenerateWorkersRequest): Observable<Worker[]> {
    console.log('📤 Generating workers for project version:', request.projectVersionId);
    console.log('   Placeholders:', request.placeholders.length);
    console.log('   Content Controls:', Object.keys(request.contentControls).length);
    
    return this.http.post<Worker[]>(`${this.baseUrl}/generate`, request);
  }

  /**
   * Get all workers for a project version
   */
  getWorkersByProjectVersion(projectVersionId: string): Observable<Worker[]> {
    console.log('📥 Fetching workers for project version:', projectVersionId);
    
    return this.http.get<Worker[]>(`${this.baseUrl}/project-version/${projectVersionId}`);
  }

  /**
   * Create a new worker manually
   */
  createWorker(projectVersionId: string, formData: WorkerFormData): Observable<Worker> {
    console.log('➥ Creating new worker:', formData.name);
    
    let configJson: string;
    
    if (formData.description && formData.description.trim()) {
      // Jeśli użytkownik podał konfigurację JSON, użyj jej
      configJson = formData.description;
    } else {
      // Generuj domyślną konfigurację — klucze z podkreśleniami, jak backend (GenerateWorkersCommandHandler, BR-010/BR-011)
      if (formData.workerType === 'Podstaw wartość') {
        configJson = JSON.stringify({
          "replacement_property_expression": `formModel.${formData.name}`,
          "search_for": `<%${formData.name}%>`
        });
      } else if (formData.workerType === 'Stan') {
        configJson = JSON.stringify({
          "tag_name": formData.name,
          "conditional_expression": `formModel.${formData.name}`
        });
      } else if (formData.workerType === 'Powtórz blok') {
        configJson = JSON.stringify({
          "tag_name": formData.name,
          "collection_expression": `formModel.${formData.name}`,
          "repeat_scope": 'block'
        });
      } else {
        configJson = JSON.stringify({
          type: 'contentControl',
          source: 'document',
          controlName: formData.name,
          controlType: 'text',
          description: formData.description || '',
          defaultValue: formData.defaultValue || ''
        });
      }
    }

    const dto = {
      projectVersionId,
      name: formData.name,
      workerType: formData.workerType,
      configJson
    };

    return this.http.post<Worker>(this.baseUrl, dto);
  }

  /**
   * Update an existing worker
   */
  updateWorker(workerId: string, formData: WorkerFormData): Observable<Worker> {
    console.log('✏️ Updating worker:', workerId);
    
    let configJson: string;
    
    if (formData.description && formData.description.trim()) {
      // Jeśli użytkownik podał konfigurację JSON, użyj jej
      configJson = formData.description;
    } else {
      // Generuj domyślną konfigurację — klucze z podkreśleniami, jak backend (GenerateWorkersCommandHandler, BR-010/BR-011)
      if (formData.workerType === 'Podstaw wartość') {
        configJson = JSON.stringify({
          "replacement_property_expression": `formModel.${formData.name}`,
          "search_for": `<%${formData.name}%>`
        });
      } else if (formData.workerType === 'Stan') {
        configJson = JSON.stringify({
          "tag_name": formData.name,
          "conditional_expression": `formModel.${formData.name}`
        });
      } else if (formData.workerType === 'Powtórz blok') {
        configJson = JSON.stringify({
          "tag_name": formData.name,
          "collection_expression": `formModel.${formData.name}`,
          "repeat_scope": 'block'
        });
      } else {
        configJson = JSON.stringify({
          type: 'contentControl',
          source: 'document',
          controlName: formData.name,
          controlType: 'text',
          description: formData.description || '',
          defaultValue: formData.defaultValue || ''
        });
      }
    }

    const dto = {
      name: formData.name,
      workerType: formData.workerType,
      configJson
    };

    return this.http.put<Worker>(`${this.baseUrl}/${workerId}`, dto);
  }

  /**
   * Delete a worker
   */
  deleteWorker(workerId: string): Observable<void> {
    console.log('🗑️ Deleting worker:', workerId);
    
    return this.http.delete<void>(`${this.baseUrl}/${workerId}`);
  }
}
