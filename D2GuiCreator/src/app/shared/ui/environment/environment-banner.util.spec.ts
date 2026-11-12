import { resolveD2EnvironmentBanner } from './environment-banner.util';

describe('resolveD2EnvironmentBanner', () => {
  it('ukrywa baner dla produkcji i pustej nazwy', () => {
    expect(resolveD2EnvironmentBanner('PRD')).toBeNull();
    expect(resolveD2EnvironmentBanner('prod')).toBeNull();
    expect(resolveD2EnvironmentBanner('Production')).toBeNull();
    expect(resolveD2EnvironmentBanner('')).toBeNull();
    expect(resolveD2EnvironmentBanner('   ')).toBeNull();
    expect(resolveD2EnvironmentBanner(null)).toBeNull();
    expect(resolveD2EnvironmentBanner(undefined)).toBeNull();
  });

  it('mapuje znane środowiska niezależnie od wielkości liter', () => {
    expect(resolveD2EnvironmentBanner('local')).toEqual({ label: 'Wersja Local', variant: 'local' });
    expect(resolveD2EnvironmentBanner('DEV')).toEqual({ label: 'Wersja DEV', variant: 'dev' });
    expect(resolveD2EnvironmentBanner('uat')).toEqual({ label: 'Wersja UAT', variant: 'test' });
    expect(resolveD2EnvironmentBanner('tst')).toEqual({ label: 'Wersja TST', variant: 'test' });
    expect(resolveD2EnvironmentBanner('preprd')).toEqual({ label: 'Wersja PRE', variant: 'pre' });
  });

  it('nieznaną nazwę pokazuje w neutralnym wariancie zamiast ją gubić', () => {
    expect(resolveD2EnvironmentBanner('Staging')).toEqual({
      label: 'Wersja Staging',
      variant: 'unknown',
    });
  });
});
