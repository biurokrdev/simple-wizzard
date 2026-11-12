import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { D2_GUI_DEFAULT_CONFIG, provideD2Gui } from '../config/d2-gui.config';
import { D2Theme, D2_THEMES, d2ThemeCssVariable, d2ThemeCssVariables } from './d2-theme.model';
import { D2ThemeService } from './d2-theme.service';

const BUSINESS_LINE: D2Theme = {
  name: 'bank-x',
  label: 'Bank X',
  palette: { ...D2_THEMES[0].palette, primary: '#123456', primaryHover: '#0f2a45', primaryLight: '#dde6ef' },
};

describe('D2 theme', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
    localStorage.removeItem('d2.theme');
    document.documentElement.removeAttribute('data-d2-theme');
    document.documentElement.style.cssText = '';
  });

  function service(config: Partial<typeof D2_GUI_DEFAULT_CONFIG> = {}): D2ThemeService {
    TestBed.configureTestingModule({ providers: [provideD2Gui(config)] });
    return TestBed.inject(D2ThemeService);
  }

  it('każda wbudowana paleta ma komplet kolorów', () => {
    const keys = Object.keys(D2_THEMES[0].palette).sort();
    for (const theme of D2_THEMES) {
      expect(Object.keys(theme.palette).sort(), theme.name).toEqual(keys);
      for (const [key, value] of Object.entries(theme.palette)) {
        expect(value, `${theme.name}.${key}`).toMatch(/^(#[0-9a-f]{6}|rgba?\()/i);
      }
    }
  });

  it('mapuje klucze palety na zmienne CSS w kebab-case', () => {
    expect(d2ThemeCssVariable('primaryHover')).toBe('--d2-color-primary-hover');
    expect(d2ThemeCssVariable('pageBg')).toBe('--d2-color-page-bg');
    expect(Object.keys(d2ThemeCssVariables(D2_THEMES[0]))).toContain('--d2-color-border-strong');
  });

  it('po starcie stosuje motyw z konfiguracji na <html>', () => {
    service({ theme: 'teal' });

    expect(document.documentElement.getAttribute('data-d2-theme')).toBe('teal');
    expect(document.documentElement.style.getPropertyValue('--d2-color-primary')).toBe('#0f766e');
  });

  it('bez konfiguracji używa palety „orange”', () => {
    const themeService = service();

    expect(themeService.theme().name).toBe('orange');
    expect(document.documentElement.style.getPropertyValue('--d2-color-primary')).toBe('#ff6200');
  });

  it('setTheme przełącza zmienne i zapamiętuje wybór; nieznana nazwa jest ignorowana', () => {
    const themeService = service();

    themeService.setTheme('violet');
    expect(themeService.theme().label).toContain('Business Banking');
    expect(document.documentElement.style.getPropertyValue('--d2-color-primary')).toBe('#4b4a9b');
    expect(localStorage.getItem('d2.theme')).toBe('violet');

    themeService.setTheme('nie-ma-takiego');
    expect(themeService.theme().name).toBe('violet');
  });

  it('zapamiętany wybór użytkownika wygrywa z konfiguracją', () => {
    localStorage.setItem('d2.theme', 'graphite');

    expect(service({ theme: 'teal' }).theme().name).toBe('graphite');
  });

  it('palety z konfiguracji dochodzą do wbudowanych i można je wybrać', () => {
    const themeService = service({ themes: [BUSINESS_LINE], theme: 'bank-x' });

    expect(themeService.themes.map((t) => t.name)).toContain('bank-x');
    expect(themeService.theme().name).toBe('bank-x');
    expect(document.documentElement.style.getPropertyValue('--d2-color-primary')).toBe('#123456');
  });
});
