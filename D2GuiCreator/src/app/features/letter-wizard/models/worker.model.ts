import { ContentControlDetail, PlaceholderDetail } from './content-control-kind';

/**
 * Worker interface for field mapping configuration
 */
export interface Worker {
  id: string;
  projectVersionId: string;
  name: string;
  workerType: string;
  configJson: string;
  createdAt: string;
  updatedAt?: string;
}

/**
 * DTO for generating workers from document analysis
 */
export interface GenerateWorkersRequest {
  projectVersionId: string;
  placeholders: string[];
  contentControls: Record<string, string>;
  /** Rodzaj formantów z analizy szablonu — API dobiera typ workera („Stan” / „Powtórz blok”) i zakres powielania. */
  contentControlDetails?: ContentControlDetail[];
  /** XLSX: arkusz / komórka / etykieta luźnych znaczników — do konfiguracji „Podstaw wartość”. */
  placeholderDetails?: PlaceholderDetail[];
}

/**
 * Parsed worker configuration
 */
export interface WorkerConfig {
  placeholder?: string;
  controlName?: string;
  controlType?: string;
  source: string;
  type: string;
  defaultValue: string;
  description: string;
}
