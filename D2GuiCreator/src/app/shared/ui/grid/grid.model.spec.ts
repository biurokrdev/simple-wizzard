import { DEFAULT_GRID_LABELS, formatLabel } from './grid.model';

describe('grid.model: etykiety', () => {
  it('podstawia parametry w tekście', () => {
    expect(formatLabel('Wyświetlono {from}-{to} z {total}', { from: 1, to: 10, total: 21 })).toBe(
      'Wyświetlono 1-10 z 21',
    );
  });

  it('zostawia nieznane parametry bez zmian', () => {
    expect(formatLabel('Strona {page} z {czegos}', { page: 2 })).toBe('Strona 2 z {czegos}');
  });

  it('zwraca tekst bez parametrów w niezmienionej postaci', () => {
    expect(formatLabel('Brak danych', {})).toBe('Brak danych');
  });

  it('domyślne etykiety są kompletne', () => {
    for (const [klucz, wartosc] of Object.entries(DEFAULT_GRID_LABELS)) {
      expect(wartosc).withContext(`etykieta ${klucz}`).toBeTruthy();
    }
  });
});
