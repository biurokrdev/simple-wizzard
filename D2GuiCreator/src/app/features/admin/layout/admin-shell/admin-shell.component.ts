import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

/**
 * Szkielet części administracyjnej — jak w D2 ViewerEditor (`pages/admin/admin-shell`): treść po lewej,
 * menu w panelu po prawej na CAŁĄ wysokość strony (host znosi padding `<main>` shella ujemnym marginesem
 * i rozciąga się na jego pełną wysokość). Dostęp tylko bezpośrednim adresem `/admin` (adminGuard) —
 * aplikacja dla projektantów nie wystawia do niego linków.
 */
@Component({
  selector: 'app-admin-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './admin-shell.component.html',
  styles: `
    :host {
      display: block;
      height: calc(100% + 3rem);
      margin: -1.5rem;
    }

    .admin-layout {
      display: flex;
      align-items: stretch;
      height: 100%;
      min-height: 100%;
    }

    .admin-content {
      flex: 1;
      min-width: 0;
      overflow-y: auto;
      padding: 1.5rem;
    }

    .admin-sidebar {
      width: 240px;
      flex-shrink: 0;
      display: flex;
      flex-direction: column;
      background: var(--d2-color-surface, #ffffff);
      border-left: 1px solid var(--d2-color-border, #e3e3e3);
    }

    .sidebar-brand {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 18px 16px;
      border-bottom: 1px solid var(--d2-color-border, #e3e3e3);
      text-decoration: none;
      color: inherit;
    }

    .sidebar-brand:hover .sidebar-title {
      color: var(--d2-color-primary, #ff6200);
    }

    .sidebar-logo {
      width: 28px;
      height: 28px;
      border-radius: 4px;
    }

    .sidebar-title {
      font-size: 15px;
      font-weight: 600;
      color: var(--d2-color-heading, #1c1c1c);
    }

    .sidebar-nav {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 2px;
      padding: 12px 8px;
      overflow-y: auto;
    }

    .nav-item {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 9px 12px;
      border-radius: 8px;
      font-size: 13.5px;
      font-weight: 500;
      color: var(--d2-color-text, #333333);
      text-decoration: none;
      transition: background 0.12s ease, color 0.12s ease;
    }

    .nav-item:hover {
      background: var(--d2-color-surface-muted, #f5f5f5);
      color: var(--d2-color-heading, #1c1c1c);
    }

    .nav-item.active {
      background: var(--d2-color-primary-light, #fde3cf);
      color: var(--d2-color-primary-hover, #cc4e00);
    }

    .sidebar-footer {
      padding: 8px;
      border-top: 1px solid var(--d2-color-border, #e3e3e3);
    }

    .back-link {
      color: var(--d2-color-muted, #6b6b6b);
    }

    .back-link:hover {
      color: var(--d2-color-text, #333333);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminShellComponent {}
