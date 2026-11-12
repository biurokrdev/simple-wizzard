import { Injectable } from '@angular/core';

export interface AppUser {
  /** Identyfikator techniczny (GUID) — do pól `createdBy`/`addedBy` w API. */
  id: string;
  /** Login korporacyjny (corporate key) — klucz członkostwa w zespołach i właściciel projektów prywatnych. */
  corporateKey: string;
  /** Nazwa wyświetlana — podpis zgłoszeń i decyzji w przeglądzie. */
  name: string;
  roles: string[];
}

/**
 * Mock użytkownika (R-03: brak uwierzytelniania). Jedno źródło tożsamości dla całego GUI:
 * lista projektów (filtr członkostwa), zespoły, kreator (właściciel projektu), przegląd (recenzent).
 * Rola `Admin` odblokowuje część administracyjną (`/admin`, `adminGuard`) — do zastąpienia tożsamością z IdP.
 */
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly mockUser: AppUser = {
    id: '00000000-0000-0000-0000-000000000000',
    corporateKey: 'TEMP_USER',
    name: 'User',
    roles: ['User', 'Admin']
  };

  getCurrentUser(): AppUser | null {
    return this.mockUser;
  }

  /** Login bieżącego użytkownika — parametr `corporateKey` w API zespołów i projektów. */
  corporateKey(): string {
    return this.mockUser.corporateKey;
  }

  userId(): string {
    return this.mockUser.id;
  }

  isAdmin(): boolean {
    return this.mockUser.roles.includes('Admin');
  }
}
