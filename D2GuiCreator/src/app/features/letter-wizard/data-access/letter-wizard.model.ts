import { ContentControlDetail, PlaceholderDetail } from '../models/content-control-kind';

export type LetterWizardStepKey =
  | 'documentDescription'
  | 'templateAndSchema'
  | 'configuration'
  | 'summary';

export interface LetterDocumentGeneral {
  documentName: string;
  sourceIdentifier: string;
  applicationName: string;
  recipient: string | null;
  retentionDays: number;
  environments: string[];
}

export interface LetterDocumentBusinessData {
  processName: string;
  businessLine: string | null;
  tenant: string;
  processOwner: string;
  processVersion: string;
  processStep: string;
  status: string;
}

export interface LetterTemplateAndSchema {
  templateId: string;
  dataSchemaId: string;
  defaultLanguage?: string;
  outputFormat?: 'pdf' | 'docx';
  detectedElements?: DetectedDocumentElements;
}

export interface DetectedDocumentElements {
  placeholders: string[];
  contentControls: Record<string, string>;
  /** Rodzaj formantów (wariant / powtarzany / lista / tabela) — opcjonalny, starsze wersje go nie mają. */
  contentControlDetails?: ContentControlDetail[];
  /** XLSX: etykiety i komórki luźnych znaczników; `source` = `docx` | `xlsx`. */
  placeholderDetails?: PlaceholderDetail[];
  source?: string;
}

export interface MapperType {
  key: string;
  name: string;
  templateJson: string;
}

export interface FieldMapping {
  name: string;
  mapperType: string;
  mappingJson: string;
}

export interface LetterConfiguration {
  autoSendEnabled: boolean;
  requiresApproval: boolean;
  approvalRole?: string;
  visibilityScope: 'internal' | 'external' | 'both';
  fieldMappings?: FieldMapping[];
}

export interface LetterWizardData {
  general: LetterDocumentGeneral;
  business: LetterDocumentBusinessData;
  templateAndSchema: LetterTemplateAndSchema;
  configuration: LetterConfiguration;
}

export interface LetterWizardMeta {
  id?: string;
  projectId: string;
  /** Numer edytowanej wersji (np. „1.1") i czy jest aktywna — do nagłówka kreatora. */
  versionTag?: string;
  isActiveVersion?: boolean;
  /** Status wersji z API (enum ProjectStatus: liczba lub nazwa) i decyzja recenzenta (BR-017). */
  status?: string | number | null;
  reviewComment?: string | null;
  reviewedBy?: string | null;
  reviewNotes?: Record<string, string> | null;
  currentStep: LetterWizardStepKey;
  completedSteps: LetterWizardStepKey[];
  lastUpdatedAt?: string;
}

export interface LetterWizardState {
  data: LetterWizardData;
  meta: LetterWizardMeta;
}
