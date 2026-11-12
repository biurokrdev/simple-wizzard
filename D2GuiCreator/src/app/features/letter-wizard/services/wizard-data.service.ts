import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { LetterWizardData } from '../data-access/letter-wizard.model';
import { AuthService } from '../../../core/services/auth.service';

/**
 * DTO for creating a new project
 */
export interface CreateProjectRequest {
  tenantId: string;
  source: string;
  name: string;
  applicationName: string;
  recipientId: string | null;
  retentionDays: number;
  environments: string[];
  processName?: string;
  processOwner?: string;
  businessLineId?: string | null;
  processVersion?: string;
  processStep?: string;
  statusId?: string;
  description?: string;
  createdBy?: string;
  /** Login autora — właściciel projektu prywatnego (bez zespołu). */
  ownerCorporateKey?: string;
}

/**
 * DTO for creating a new project version
 */
export interface CreateProjectVersionRequest {
  projectId: string;
  major: number;
  minor: number;
  processName: string;
  businessLineId: string | null;
  tenantIdentifier: string;
  processOwner: string;
  processVersion: string;
  processStep: string;
  statusId: string;
  stepData: any; // JSON object with complete wizard data
  notes?: string;
  createdBy?: string;
}

/**
 * Response for created project
 */
export interface ProjectResponse {
  id: string;
  tenantId: string | null;
  /** Nazwa zespołu (API dokleja; null dla projektu prywatnego). */
  tenantName: string | null;
  /** Login właściciela projektu prywatnego. */
  ownerCorporateKey: string | null;
  source: string;
  name: string;
  applicationName: string | null;
  recipientId: string | null;
  retentionDays: number;
  processName: string | null;
  processOwner: string | null;
  businessLineId: string | null;
  processVersion: string | null;
  processStep: string | null;
  statusId: string | null;
  defaultTemplateName: string | null;
  createdBy: string | null;
  createdAt: string;
  description: string | null;
  currentActiveVersionId: string | null;
  activeVersionTag: string | null;
  activeVersionTemplateName: string | null;
  /** Szablon do pokazania na liście: z wersji aktywnej, a gdy jej nie ma — z ostatniej roboczej. */
  templateName: string | null;
  templateVersionTag: string | null;
  templateVersionIsActive: boolean;
}

/**
 * Response for created project version
 */
export interface ProjectVersionResponse {
  id: string;
  projectId: string;
  major: number;
  minor: number;
  versionTag: string;
  isActive: boolean;
  status: string | number;
  createdBy: string | null;
  createdAt: string;
  stepData: any;
  notes: string | null;
  /** Przegląd/akceptacja (BR-017) */
  submittedAt?: string | null;
  reviewedAt?: string | null;
  reviewedBy?: string | null;
  reviewComment?: string | null;
  /** Uwagi recenzenta do kroków kreatora: klucz kroku → treść. */
  reviewNotes?: Record<string, string> | null;
  /** Paczka wdrożeniowa (BR-022): ostatnia wysyłka do aplikacji docelowej. */
  deliveredAt?: string | null;
  deliveredTo?: string | null;
  deliveredPackageSha?: string | null;
}

/** Wynik wysyłki paczki do aplikacji docelowej (`POST .../package/publish`). */
export interface PackageDeliveryResponse {
  projectId: string;
  versionId: string;
  versionTag: string;
  fileName: string;
  size: number;
  sha256: string;
  workerCount: number;
  hasTemplate: boolean;
  deliveredAt: string;
  deliveredTo: string;
  targetResponse: string | null;
}

/**
 * Response for uploaded file
 */
export interface FileUploadResponse {
  fileId: string;
  fileName: string;
  size: number;
  contentType: string | null;
  uploadedAt: string;
}

/**
 * Paged response wrapper
 */
export interface PagedResponse<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

/**
 * Service for saving wizard data to the backend
 */
@Injectable({
  providedIn: 'root'
})
export class WizardDataService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly baseUrl = `${environment.apiUrl}/v1/projects`;
  private readonly filesBaseUrl = `${environment.apiUrl}/v1/files`;

  /**
   * Creates a new project from wizard step 1 data
   */
  createProject(data: LetterWizardData, projectId: string): Observable<ProjectResponse> {
    // Validate required fields (tenant is optional - can be personal or team project)
    if (!data.general.sourceIdentifier) {
      console.error('❌ Missing required field: sourceIdentifier');
      throw new Error('Source Identifier is required');
    }
    if (!data.general.documentName) {
      console.error('❌ Missing required field: documentName');
      throw new Error('Document Name is required');
    }

    const request: CreateProjectRequest = {
      tenantId: data.business.tenant, // Tenant ID (GUID) from TenantSelectorComponent
      source: data.general.sourceIdentifier,
      name: data.general.documentName,
      applicationName: data.general.applicationName || '',
      recipientId: data.general.recipient || null,
      retentionDays: data.general.retentionDays || 365,
      environments: data.general.environments || [],
      processName: data.business.processName || '',
      processOwner: data.business.processOwner || '',
      businessLineId: data.business.businessLine || null,
      processVersion: data.business.processVersion || '1.0',
      processStep: data.business.processStep || '',
      statusId: data.business.status || '',
      description: `Process: ${data.business.processName || 'N/A'}, Owner: ${data.business.processOwner || 'N/A'}`,
      ownerCorporateKey: this.auth.corporateKey()
    };

    console.log('📤 Creating project with request:', JSON.stringify(request, null, 2));

    return this.http.post<ProjectResponse>(this.baseUrl, request);
  }

  /**
   * Creates a new project version with complete wizard data
   */
  createProjectVersion(
    projectId: string,
    data: LetterWizardData,
    versionMajor: number = 1,
    versionMinor: number = 0
  ): Observable<ProjectVersionResponse> {
    const request: CreateProjectVersionRequest = {
      projectId,
      major: versionMajor,
      minor: versionMinor,
      processName: data.business.processName,
      businessLineId: data.business.businessLine,
      tenantIdentifier: data.business.tenant,
      processOwner: data.business.processOwner,
      processVersion: data.business.processVersion,
      processStep: data.business.processStep,
      statusId: data.business.status,
      stepData: data, // Complete wizard state
      notes: undefined
    };

    console.log('📤 Creating project version:', request);

    return this.http.post<ProjectVersionResponse>(
      `${this.baseUrl}/${projectId}/versions`,
      request
    );
  }

  /**
   * Saves the first step data (creates project and initial version)
   */
  saveFirstStepData(
    projectId: string,
    data: LetterWizardData
  ): Observable<{ project: ProjectResponse; version: ProjectVersionResponse }> {
    // This would ideally be a single transaction on the backend
    // For now, we'll handle it in the facade by chaining the observables
    throw new Error('Use createProject and createProjectVersion separately');
  }

  /**
   * Uploads a template file to a project version
   */
  uploadTemplateFile(
    projectVersionId: string,
    file: File
  ): Observable<FileUploadResponse> {
    const formData = new FormData();
    formData.append('file', file, file.name);

    console.log('📤 Uploading template file:', file.name, 'to version:', projectVersionId);

    return this.http.post<FileUploadResponse>(
      `${this.filesBaseUrl}/project-versions/${projectVersionId}/upload?fileType=template`,
      formData
    );
  }

  /**
   * Updates project version step data (for step 2 and beyond)
   */
  updateProjectVersionStepData(
    projectId: string,
    versionId: string,
    data: LetterWizardData
  ): Observable<any> {
    const request = {
      projectVersionId: versionId,
      stepData: data,
      notes: undefined
    };

    console.log('📤 Updating project version step data:', request);

    return this.http.patch(
      `${this.baseUrl}/${projectId}/versions/${versionId}/step-data`,
      request
    );
  }

  /**
   * Gets all projects with pagination
   */
  getProjects(
    tenantId?: string, 
    source?: string,
    pageNumber: number = 1,
    pageSize: number = 10,
    searchTerm?: string,
    status?: string,
    dateFrom?: string,
    dateTo?: string,
    /** Login użytkownika: API zwraca tylko projekty jego zespołów i jego prywatne. Bez — wszystkie (admin). */
    corporateKey?: string
  ): Observable<PagedResponse<ProjectResponse>> {
    let url = this.baseUrl;
    const params: string[] = [];
    
    params.push(`pageNumber=${pageNumber}`);
    params.push(`pageSize=${pageSize}`);
    
    if (tenantId) {
      params.push(`tenantId=${encodeURIComponent(tenantId)}`);
    }
    if (source) {
      params.push(`source=${encodeURIComponent(source)}`);
    }
    if (searchTerm) {
      params.push(`searchTerm=${encodeURIComponent(searchTerm)}`);
    }
    if (status) {
      params.push(`status=${encodeURIComponent(status)}`);
    }
    if (dateFrom) {
      params.push(`dateFrom=${encodeURIComponent(dateFrom)}`);
    }
    if (dateTo) {
      params.push(`dateTo=${encodeURIComponent(dateTo)}`);
    }
    if (corporateKey) {
      params.push(`corporateKey=${encodeURIComponent(corporateKey)}`);
    }
    if (corporateKey) {
      params.push(`corporateKey=${encodeURIComponent(corporateKey)}`);
    }
    
    if (params.length > 0) {
      url += '?' + params.join('&');
    }

    console.log('📤 Getting projects with pagination:', { pageNumber, pageSize, tenantId, searchTerm });

    return this.http.get<PagedResponse<ProjectResponse>>(url);
  }

  /**
   * Gets a single project by ID
   */
  getProject(projectId: string): Observable<ProjectResponse> {
    console.log('📤 Getting project:', projectId);

    return this.http.get<ProjectResponse>(`${this.baseUrl}/${projectId}`);
  }

  /**
   * Gets a specific project version
   */
  getProjectVersion(projectId: string, versionId: string): Observable<ProjectVersionResponse> {
    console.log('📤 Getting project version:', projectId, versionId);

    return this.http.get<ProjectVersionResponse>(
      `${this.baseUrl}/${projectId}/versions/${versionId}`
    );
  }

  /**
   * Gets all versions for a project (latest first)
   */
  getProjectVersions(projectId: string): Observable<ProjectVersionResponse[]> {
    console.log('📤 Getting project versions for:', projectId);

    return this.http.get<ProjectVersionResponse[]>(
      `${this.baseUrl}/${projectId}/versions`
    );
  }

  /**
   * Activates a specific project version
   */
  /**
   * Nowa wersja robocza jako kopia wskazanej (step_data, bieżący plik szablonu, workery) — używana,
   * gdy edytowana wersja jest aktywna: produkcyjna zostaje nietknięta, praca idzie na kopii (BR-015).
   */
  cloneProjectVersion(projectId: string, versionId: string): Observable<ProjectVersionResponse> {
    return this.http.post<ProjectVersionResponse>(
      `${this.baseUrl}/${projectId}/versions/${versionId}/clone`,
      {}
    );
  }

  /** Paczka wdrożeniowa wersji (ZIP: manifest.json, workers.json, szablon) — do pobrania (BR-022). */
  downloadPackage(projectId: string, versionId: string, requestedBy?: string): Observable<Blob> {
    const params = requestedBy ? `?requestedBy=${encodeURIComponent(requestedBy)}` : '';
    return this.http.get(`${this.baseUrl}/${projectId}/versions/${versionId}/package${params}`, { responseType: 'blob' });
  }

  /** Buduje paczkę i wysyła do aplikacji docelowej; API zapisuje datę, cel i skrót na wersji. */
  publishPackage(projectId: string, versionId: string, sentBy: string | null): Observable<PackageDeliveryResponse> {
    return this.http.post<PackageDeliveryResponse>(`${this.baseUrl}/${projectId}/versions/${versionId}/package/publish`, { sentBy });
  }

  activateProjectVersion(projectId: string, versionId: string): Observable<ProjectVersionResponse> {
    console.log('🔄 Activating project version:', projectId, versionId);

    return this.http.post<ProjectVersionResponse>(
      `${this.baseUrl}/${projectId}/versions/${versionId}/activate`,
      {}
    );
  }
}
