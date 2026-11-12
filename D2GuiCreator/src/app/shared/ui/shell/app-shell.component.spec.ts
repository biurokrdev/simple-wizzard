import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import {
  D2BuildInfoSource,
  D2ConnectionStatusSource,
  provideD2Gui,
  withBuildInfo,
  withConnectionStatus,
} from '../config/d2-gui.config';
import { D2AppShellComponent } from './app-shell.component';

@Component({ standalone: true, template: 'page' })
class PageStub {}

class FakeApiStatus implements D2ConnectionStatusSource, D2BuildInfoSource {
  readonly connected = signal(true);
  readonly isApiConnected = this.connected.asReadonly();
  readonly buildNumber = signal<string | null>('1.2.3');
  readonly buildDate = signal<string | null>('08/11/2026 00:30');
}

describe('D2AppShellComponent', () => {
  let api: FakeApiStatus;

  beforeEach(async () => {
    api = new FakeApiStatus();
    await TestBed.configureTestingModule({
      imports: [D2AppShellComponent],
      providers: [
        provideRouter([{ path: '', component: PageStub }]),
        provideD2Gui(
          {
            appName: 'Doc2',
            homeLink: '/projects',
            footerTagline: 'Kreator parametryzacji szablonów',
            environmentName: 'local',
            offlineMessage: 'Brak połączenia z API',
          },
          withConnectionStatus(FakeApiStatus),
          withBuildInfo(FakeApiStatus),
        ),
        { provide: FakeApiStatus, useFactory: () => api },
      ],
    }).compileComponents();
  });

  function render(): HTMLElement {
    const fixture = TestBed.createComponent(D2AppShellComponent);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('renderuje nagłówek z logo, nazwą i linkiem home z konfiguracji', () => {
    const el = render();
    const brand = el.querySelector<HTMLAnchorElement>('d2-header a.brand');
    expect(brand).not.toBeNull();
    expect(brand?.getAttribute('href')).toBe('/projects');
    expect(el.querySelector('d2-header .app-name')?.textContent?.trim()).toBe('Doc2');
    expect(el.querySelector('d2-header img.logo')?.getAttribute('src')).toBe('logo.svg');
  });

  it('pokazuje cienki baner środowiska w wariancie local', () => {
    const banner = render().querySelector('d2-environment-banner .env-banner');
    expect(banner?.textContent?.trim()).toBe('Wersja Local');
    expect(banner?.classList).toContain('env-banner--local');
  });

  it('układa shell jako banery + nagłówek, przewijany <main> i stopkę na końcu', () => {
    const el = render();
    const shell = el.querySelector('.d2-app-shell');
    const children = Array.from(shell?.children ?? []).map((c) => c.tagName.toLowerCase());
    expect(children).toEqual(['div', 'main', 'd2-footer']);
    expect(shell?.querySelector('.d2-app-shell__top d2-global-banners')).not.toBeNull();
    expect(shell?.querySelector('.d2-app-shell__top d2-header')).not.toBeNull();
    expect(shell?.querySelector('main.d2-app-shell__content router-outlet')).not.toBeNull();
  });

  it('renderuje stopkę-pasek statusu z ©, taglinem, środowiskiem i danymi buildu', () => {
    const footer = render().querySelector('d2-footer .status-footer');
    expect(footer?.textContent).toContain(`Doc2 © ${new Date().getFullYear()} Doc2`);
    expect(footer?.textContent).toContain('Kreator parametryzacji szablonów');
    expect(footer?.querySelector('.status-env')?.textContent?.trim()).toBe('LOCAL');
    expect(footer?.querySelector('.status-build-number')?.textContent?.trim()).toBe('Wersja: 1.2.3');
    expect(footer?.querySelector('.status-build-date')?.textContent?.trim()).toBe('08/11/2026 00:30');
  });

  it('pokazuje baner offline tylko bez połączenia z API i pozwala go zamknąć', () => {
    const fixture = TestBed.createComponent(D2AppShellComponent);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.offline-banner')).toBeNull();

    api.connected.set(false);
    fixture.detectChanges();
    expect(el.querySelector('.offline-banner')?.textContent).toContain('Brak połączenia z API');

    el.querySelector<HTMLButtonElement>('.offline-banner__close')?.click();
    fixture.detectChanges();
    expect(el.querySelector('.offline-banner')).toBeNull();
  });
});
