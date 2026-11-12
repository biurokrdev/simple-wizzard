import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { D2BuildInfoSource, D2GuiConfig, provideD2Gui, withBuildInfo } from '../config/d2-gui.config';
import { D2FooterComponent } from './footer.component';

class FakeBuildInfo implements D2BuildInfoSource {
  readonly buildNumber = signal<string | null>(null);
  readonly buildDate = signal<string | null>(null);
}

describe('D2FooterComponent', () => {
  let buildInfo: FakeBuildInfo;

  async function setup(config: Partial<D2GuiConfig>): Promise<void> {
    buildInfo = new FakeBuildInfo();
    await TestBed.configureTestingModule({
      imports: [D2FooterComponent],
      providers: [
        provideD2Gui(config, withBuildInfo(FakeBuildInfo)),
        { provide: FakeBuildInfo, useFactory: () => buildInfo },
      ],
    }).compileComponents();
  }

  function render() {
    const fixture = TestBed.createComponent(D2FooterComponent);
    fixture.detectChanges();
    return { fixture, el: fixture.nativeElement as HTMLElement, text: () => (fixture.nativeElement as HTMLElement).textContent ?? '' };
  }

  beforeEach(() => TestBed.resetTestingModule());

  it('pokazuje © z rokiem, tagline, środowisko, numer i datę buildu', async () => {
    await setup({ appName: 'Doc2', footerTagline: 'Kreator parametryzacji szablonów', environmentName: 'local' });
    const { fixture, el, text } = render();
    buildInfo.buildNumber.set('1.4.2');
    buildInfo.buildDate.set('08/11/2026 00:30');
    fixture.detectChanges();

    expect(text()).toContain(`Doc2 © ${new Date().getFullYear()} Doc2`);
    expect(text()).toContain('Kreator parametryzacji szablonów');
    expect(el.querySelector('.status-env')?.textContent?.trim()).toBe('LOCAL');
    expect(el.querySelector('.status-build-number')?.textContent?.trim()).toBe('Wersja: 1.4.2');
    expect(el.querySelector('.status-build-date')?.textContent?.trim()).toBe('08/11/2026 00:30');
    expect(el.querySelectorAll('.status-center .status-separator').length).toBe(2);
  });

  it('ukrywa środowisko na produkcji i dane buildu, gdy ich nie ma', async () => {
    await setup({ appName: 'Doc2', environmentName: 'PRD' });
    const { el } = render();

    expect(el.querySelector('.status-env')).toBeNull();
    expect(el.querySelector('.status-build-number')).toBeNull();
    expect(el.querySelector('.status-build-date')).toBeNull();
    expect(el.querySelectorAll('.status-center .status-separator').length).toBe(0);
    expect(el.querySelector('.status-copyright')).not.toBeNull();
  });

  it('reaguje na dane buildu przychodzące później (np. z health-checka API)', async () => {
    await setup({ environmentName: 'dev' });
    const { fixture, el } = render();
    expect(el.querySelector('.status-build-number')).toBeNull();
    expect(el.querySelectorAll('.status-center .status-separator').length).toBe(0);

    buildInfo.buildNumber.set('2.0.0');
    fixture.detectChanges();
    expect(el.querySelector('.status-build-number')?.textContent?.trim()).toBe('Wersja: 2.0.0');
    expect(el.querySelectorAll('.status-center .status-separator').length).toBe(1);
  });
});
