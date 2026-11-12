export interface RecipientDto {
  id: string;
  code: string;
  name: string;
  description?: string;
  isActive: boolean;
}

export interface BusinessLineDto {
  id: string;
  code: string;
  name: string;
  description?: string;
  isActive: boolean;
}

export interface TenantDto {
  id: string;
  code: string;
  name: string;
  description?: string;
  isActive: boolean;
}

export interface StatusDto {
  id: string;
  code: string;
  name: string;
  description?: string;
  isActive: boolean;
}

export interface WorkerTypeDto {
  id: string;
  code: string;
  name: string;
  description?: string;
  configSchema?: string; // JSON schema dla konfiguracji tego typu workera
  isActive: boolean;
}

/**
 * Wynik odświeżenia pojedynczego słownika
 */
export interface RefreshResult {
  dictionaryName: string;
  itemCount: number;
  totalCount: number;
  refreshedAt: string;
}

/**
 * Wynik odświeżenia wszystkich słowników
 */
export interface RefreshAllResult {
  recipients: RefreshResult;
  businessLines: RefreshResult;
  tenants: RefreshResult;
  statuses: RefreshResult;
  workerTypes: RefreshResult;
  refreshedAt: string;
}
