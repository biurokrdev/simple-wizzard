import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface TenantMember {
  id: string;
  tenantId: string;
  corporateKey: string;
  addedBy: string;
  addedAt: string;
}

export interface Tenant {
  id: string;
  name: string;
  description?: string;
  createdBy?: string;
  createdAt: string;
  memberCount: number;
  members: TenantMember[];
}

export interface CreateTenantRequest {
  name: string;
  description?: string;
  memberCorporateKeys: string[];
}

export interface AddTenantMemberRequest {
  corporateKey: string;
}

@Injectable({
  providedIn: 'root'
})
export class TenantService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/v1/tenants`;

  /**
   * Get all tenants where the current user is a member
   */
  getTenants(corporateKey: string): Observable<Tenant[]> {
    const params = new HttpParams().set('corporateKey', corporateKey);
    return this.http.get<Tenant[]>(this.apiUrl, { params });
  }

  /**
   * Create a new tenant/team
   */
  createTenant(request: CreateTenantRequest, createdBy: string, corporateKey: string): Observable<Tenant> {
    const params = new HttpParams()
      .set('createdBy', createdBy)
      .set('corporateKey', corporateKey);
    
    return this.http.post<Tenant>(this.apiUrl, request, { params });
  }

  /**
   * Add a member to tenant
   */
  addMember(tenantId: string, request: AddTenantMemberRequest, addedBy: string, requesterCorporateKey: string): Observable<{ success: boolean }> {
    const params = new HttpParams()
      .set('addedBy', addedBy)
      .set('requesterCorporateKey', requesterCorporateKey);
    
    return this.http.post<{ success: boolean }>(`${this.apiUrl}/${tenantId}/members`, request, { params });
  }

  /**
   * Remove a member from tenant
   */
  removeMember(tenantId: string, memberId: string, requesterId: string, requesterCorporateKey: string): Observable<{ success: boolean }> {
    const params = new HttpParams()
      .set('requesterId', requesterId)
      .set('requesterCorporateKey', requesterCorporateKey);
    
    return this.http.delete<{ success: boolean }>(`${this.apiUrl}/${tenantId}/members/${memberId}`, { params });
  }

  /**
   * Delete a tenant (only if no projects associated)
   */
  deleteTenant(tenantId: string, requesterId: string): Observable<{ success: boolean }> {
    const params = new HttpParams().set('requesterId', requesterId);
    return this.http.delete<{ success: boolean }>(`${this.apiUrl}/${tenantId}`, { params });
  }
}
