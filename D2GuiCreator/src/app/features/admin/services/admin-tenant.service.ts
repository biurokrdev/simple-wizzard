import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export type TenantMemberAccess = 'ReadWrite' | 'ReadOnly';

export interface AdminTenantMember {
  id: string;
  tenantId: string;
  corporateKey: string;
  addedBy: string;
  addedAt: string;
  isBlocked: boolean;
  access: TenantMemberAccess;
}

export interface AdminTenant {
  id: string;
  name: string;
  description: string | null;
  createdBy: string | null;
  createdAt: string;
  memberCount: number;
  members: AdminTenantMember[];
  /** `false` = zespół zablokowany. */
  isActive: boolean;
}

/** Administracja zespołów (BR-018): `/api/v1/admin/tenants`. Bez autoryzacji API do czasu R-03. */
@Injectable({ providedIn: 'root' })
export class AdminTenantService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/v1/admin/tenants`;

  getAll(): Observable<AdminTenant[]> {
    return this.http.get<AdminTenant[]>(this.baseUrl);
  }

  create(name: string, description: string | null, memberCorporateKeys: string[]): Observable<AdminTenant> {
    return this.http.post<AdminTenant>(this.baseUrl, { name, description, memberCorporateKeys });
  }

  setActive(tenantId: string, isActive: boolean): Observable<AdminTenant> {
    return this.http.put<AdminTenant>(`${this.baseUrl}/${tenantId}/status`, { isActive });
  }

  addMember(tenantId: string, corporateKey: string, access: TenantMemberAccess): Observable<AdminTenant> {
    return this.http.post<AdminTenant>(`${this.baseUrl}/${tenantId}/members`, { corporateKey, access });
  }

  updateMember(tenantId: string, memberId: string, change: { isBlocked?: boolean; access?: TenantMemberAccess }): Observable<AdminTenant> {
    return this.http.put<AdminTenant>(`${this.baseUrl}/${tenantId}/members/${memberId}`, change);
  }

  removeMember(tenantId: string, memberId: string): Observable<AdminTenant> {
    return this.http.delete<AdminTenant>(`${this.baseUrl}/${tenantId}/members/${memberId}`);
  }
}
