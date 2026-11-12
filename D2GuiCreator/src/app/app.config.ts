import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors, HTTP_INTERCEPTORS, withXhr } from '@angular/common/http';
import { provideD2Gui, withBuildInfo, withConnectionStatus } from 'd2-gui-shared';

import { environment } from '../environments/environment';
import { ApiConnectionService } from './core/services/api-connection.service';

import { APP_ROUTES } from './app.routes';
import { HttpErrorInterceptor } from './core/interceptors/http-error.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(APP_ROUTES),
    provideHttpClient(withXhr()),
    {
      provide: HTTP_INTERCEPTORS,
      useClass: HttpErrorInterceptor,
      multi: true
    },
    // Wspólny shell GUI (D2GuiShared): nagłówek/stopka 1:1 z D2 ViewerEditor, cienki baner środowiska,
    // baner offline sterowany przez ApiConnectionService (health-check /api/v1/health).
    provideD2Gui(
      {
        appName: 'Doc2',
        logoSrc: 'logo.svg',
        homeLink: '/projects',
        homeLinkTitle: 'Wróć do listy projektów',
        // Motyw (paleta) — patrz D2ThemeService; przełącznik w nagłówku wyłączony (showThemeSwitcher domyślnie false)
        theme: 'orange',
        footerTagline: 'Kreator parametryzacji szablonów',
        environmentName: environment.name,
        offlineMessage: 'Brak połączenia z API — aplikacja nie może nawiązać połączenia z serwerem'
      },
      withConnectionStatus(ApiConnectionService),
      withBuildInfo(ApiConnectionService)
    )
  ]
};
