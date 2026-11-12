import {
  cellText,
  cellValue,
  clamp,
  compareValues,
  countActiveFilters,
  filterRows,
  formatValue,
  includesText,
  matchesFieldFilters,
  matchesFilters,
  matchesSearch,
  normalizeText,
  pageTokens,
  paginate,
  sortRows,
} from './grid-data';
import { GridColumn } from './grid.model';

interface Osoba {
  readonly imie: string;
  readonly miasto: string;
  readonly wiek: number | null;
  readonly notatka: string;
}

const KOLUMNY: readonly GridColumn<Osoba>[] = [
  { key: 'imie', header: 'Imię' },
  { key: 'miasto', header: 'Miasto' },
  { key: 'wiek', header: 'Wiek' },
  // Kolumna wyłączona z wyszukiwania globalnego.
  { key: 'notatka', header: 'Notatka', searchable: false },
];

const OSOBY: readonly Osoba[] = [
  { imie: 'Zofia', miasto: 'Łódź', wiek: 30, notatka: 'tajne' },
  { imie: 'Anna', miasto: 'Gdańsk', wiek: 41, notatka: 'nic' },
  { imie: 'Bartek', miasto: 'Łomża', wiek: null, notatka: 'nic' },
  { imie: 'Celina', miasto: 'Zakopane', wiek: 25, notatka: 'nic' },
];

describe('grid-data: odczyt komórek', () => {
  it('czyta wartość po kluczu kolumny', () => {
    expect(cellValue({ key: 'imie', header: 'Imię' }, OSOBY[0])).toBe('Zofia');
  });

  it('używa własnego gettera, gdy kolumna go definiuje', () => {
    const kolumna: GridColumn<Osoba> = {
      key: 'opis',
      header: 'Opis',
      value: (osoba) => `${osoba.imie} z ${osoba.miasto}`,
    };

    expect(cellValue(kolumna, OSOBY[0])).toBe('Zofia z Łódź');
  });

  it('formatuje tekst komórki własnym formaterem', () => {
    const kolumna: GridColumn<Osoba> = {
      key: 'wiek',
      header: 'Wiek',
      format: (wartosc) => (wartosc === null ? 'brak' : `${wartosc} lat`),
    };

    expect(cellText(kolumna, OSOBY[0])).toBe('30 lat');
    expect(cellText(kolumna, OSOBY[2])).toBe('brak');
  });

  it('domyślnie zamienia wartości puste na pusty tekst, a daty na YYYY-MM-DD', () => {
    expect(formatValue(null)).toBe('');
    expect(formatValue(undefined)).toBe('');
    expect(formatValue(0)).toBe('0');
    expect(formatValue(false)).toBe('false');
    expect(formatValue(new Date(Date.UTC(2026, 4, 15)))).toBe('2026-05-15');
  });
});

describe('grid-data: normalizacja tekstu', () => {
  it('usuwa znaki diakrytyczne, wielkość liter i białe znaki', () => {
    expect(normalizeText('  Zakład  ')).toBe('zaklad');
    expect(normalizeText('ŁÓDŹ')).toBe('lodz');
    expect(normalizeText('Gdańsk')).toBe('gdansk');
  });

  it('porównuje teksty z pominięciem diakrytyków', () => {
    expect(includesText('Zakład Produkcyjny', 'zaklad')).toBe(true);
    expect(includesText('Zakład Produkcyjny', 'ZAKŁAD')).toBe(true);
    expect(includesText('Zakład Produkcyjny', 'fabryka')).toBe(false);
  });

  it('traktuje pustą frazę jako brak warunku', () => {
    expect(includesText('cokolwiek', '')).toBe(true);
    expect(includesText('cokolwiek', '   ')).toBe(true);
  });
});

describe('grid-data: wyszukiwanie i filtrowanie', () => {
  it('pusta fraza dopuszcza każdy wiersz', () => {
    expect(matchesSearch(OSOBY[0], KOLUMNY, '')).toBe(true);
  });

  it('szuka we wszystkich kolumnach oprócz wyłączonych z wyszukiwania', () => {
    expect(matchesSearch(OSOBY[0], KOLUMNY, 'lodz')).toBe(true);
    expect(matchesSearch(OSOBY[0], KOLUMNY, 'tajne')).toBe(false);
  });

  it('filtry kolumnowe łączy warunkiem "i"', () => {
    expect(matchesFilters(OSOBY[1], KOLUMNY, { imie: 'an', miasto: 'gda' })).toBe(true);
    expect(matchesFilters(OSOBY[1], KOLUMNY, { imie: 'an', miasto: 'lodz' })).toBe(false);
  });

  it('pomija puste filtry i nieznane kolumny', () => {
    expect(matchesFilters(OSOBY[1], KOLUMNY, { imie: '  ', nieistnieje: 'xyz' })).toBe(true);
  });

  it('panel filtrów wymaga dokładnego dopasowania wartości', () => {
    expect(matchesFieldFilters(OSOBY[1], KOLUMNY, { miasto: 'Gdańsk' })).toBe(true);
    // "Gda" pasowałoby do filtra kolumnowego, ale nie do listy rozwijanej.
    expect(matchesFieldFilters(OSOBY[1], KOLUMNY, { miasto: 'Gda' })).toBe(false);
  });

  it('pusty wybór w panelu filtrów nie zawęża listy', () => {
    expect(matchesFieldFilters(OSOBY[1], KOLUMNY, { miasto: '' })).toBe(true);
  });

  it('filtruje po frazie i filtrach jednocześnie, nie zmieniając wejścia', () => {
    const wejscie = [...OSOBY];

    const wynik = filterRows(wejscie, KOLUMNY, {
      search: 'bar',
      filters: { miasto: 'ł' },
      fieldFilters: {},
    });

    expect(wynik.map((osoba) => osoba.imie)).toEqual(['Bartek']);
    expect(wejscie).toEqual([...OSOBY]);
  });

  it('łączy panel filtrów z pozostałymi warunkami', () => {
    const wynik = filterRows(OSOBY, KOLUMNY, {
      search: '',
      filters: { imie: 'a' },
      fieldFilters: { miasto: 'Łódź' },
    });

    expect(wynik.map((osoba) => osoba.imie)).toEqual(['Zofia']);
  });

  it('liczy aktywne filtry z pominięciem pustych i frazy', () => {
    expect(
      countActiveFilters({
        search: 'szukana fraza',
        filters: { imie: 'an', miasto: '  ' },
        fieldFilters: { wiek: '30' },
      }),
    ).toBe(2);
  });
});

describe('grid-data: porównywanie wartości', () => {
  it('porównuje liczby numerycznie', () => {
    expect(compareValues(2, 10)).toBeLessThan(0);
    expect(compareValues(10, 2)).toBeGreaterThan(0);
    expect(compareValues(5, 5)).toBe(0);
  });

  it('porównuje teksty według polskiej kolejności', () => {
    expect(compareValues('Łódź', 'Zakopane')).toBeLessThan(0);
    expect(compareValues('anna', 'Anna')).toBe(0);
  });

  it('porównuje daty i wartości logiczne', () => {
    expect(compareValues(new Date(2026, 0, 1), new Date(2026, 0, 2))).toBeLessThan(0);
    expect(compareValues(false, true)).toBeLessThan(0);
  });

  it('wartości puste stawia na końcu', () => {
    expect(compareValues(null, 1)).toBeGreaterThan(0);
    expect(compareValues(1, undefined)).toBeLessThan(0);
    expect(compareValues('', null)).toBe(0);
  });
});

describe('grid-data: sortowanie', () => {
  it('zwraca kopię listy, gdy sortowanie nie jest ustawione', () => {
    const wynik = sortRows(OSOBY, KOLUMNY, null);

    expect(wynik).toEqual([...OSOBY]);
    expect(wynik).not.toBe(OSOBY as Osoba[]);
  });

  it('ignoruje sortowanie po nieznanej kolumnie', () => {
    expect(sortRows(OSOBY, KOLUMNY, { key: 'nieistnieje', direction: 'asc' })).toEqual([...OSOBY]);
  });

  it('sortuje rosnąco i malejąco', () => {
    const rosnaco = sortRows(OSOBY, KOLUMNY, { key: 'imie', direction: 'asc' });
    const malejaco = sortRows(OSOBY, KOLUMNY, { key: 'imie', direction: 'desc' });

    expect(rosnaco.map((osoba) => osoba.imie)).toEqual(['Anna', 'Bartek', 'Celina', 'Zofia']);
    expect(malejaco.map((osoba) => osoba.imie)).toEqual(['Zofia', 'Celina', 'Bartek', 'Anna']);
  });

  it('przy sortowaniu rosnącym wartości puste trafiają na koniec', () => {
    const wynik = sortRows(OSOBY, KOLUMNY, { key: 'wiek', direction: 'asc' });

    expect(wynik.map((osoba) => osoba.wiek)).toEqual([25, 30, 41, null]);
  });

  it('jest stabilne dla równych wartości', () => {
    const wynik = sortRows(OSOBY, KOLUMNY, { key: 'notatka', direction: 'asc' });

    expect(wynik.map((osoba) => osoba.imie)).toEqual(['Anna', 'Bartek', 'Celina', 'Zofia']);
  });

  it('używa własnego komparatora kolumny', () => {
    const kolumny: GridColumn<Osoba>[] = [
      // Sortowanie po długości imienia zamiast alfabetycznie.
      {
        key: 'imie',
        header: 'Imię',
        compare: (a, b) => String(a).length - String(b).length,
      },
    ];

    const wynik = sortRows(OSOBY, kolumny, { key: 'imie', direction: 'asc' });

    expect(wynik.map((osoba) => osoba.imie)).toEqual(['Anna', 'Zofia', 'Bartek', 'Celina']);
  });
});

describe('grid-data: stronicowanie', () => {
  const liczby = Array.from({ length: 21 }, (_, index) => index + 1);

  it('zwraca pierwszą stronę wraz z zakresem', () => {
    const strona = paginate(liczby, 1, 10);

    expect(strona.items).toEqual([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);
    expect(strona.page).toBe(1);
    expect(strona.pageCount).toBe(3);
    expect(strona.total).toBe(21);
    expect(strona.from).toBe(1);
    expect(strona.to).toBe(10);
  });

  it('ostatnia strona może być niepełna', () => {
    const strona = paginate(liczby, 3, 10);

    expect(strona.items).toEqual([21]);
    expect(strona.from).toBe(21);
    expect(strona.to).toBe(21);
  });

  it('przycina numer strony do dostępnego zakresu', () => {
    expect(paginate(liczby, 99, 10).page).toBe(3);
    expect(paginate(liczby, 0, 10).page).toBe(1);
    expect(paginate(liczby, -5, 10).page).toBe(1);
  });

  it('dla pustej listy zwraca jedną, pustą stronę', () => {
    const strona = paginate([], 1, 10);

    expect(strona.items).toEqual([]);
    expect(strona.pageCount).toBe(1);
    expect(strona.total).toBe(0);
    expect(strona.from).toBe(0);
    expect(strona.to).toBe(0);
  });

  it('rozmiar strony mniejszy niż 1 traktuje jak 1', () => {
    expect(paginate(liczby, 1, 0).items).toEqual([1]);
  });

  it('przycina liczbę do zakresu', () => {
    expect(clamp(5, 1, 3)).toBe(3);
    expect(clamp(-5, 1, 3)).toBe(1);
    expect(clamp(2, 1, 3)).toBe(2);
  });
});

describe('grid-data: przyciski stronicowania', () => {
  it('pokazuje wszystkie strony, gdy mieszczą się w limicie', () => {
    expect(pageTokens(1, 4)).toEqual([1, 2, 3, 4]);
  });

  it('dla pierwszych stron dodaje wielokropek na końcu', () => {
    expect(pageTokens(1, 10)).toEqual([1, 2, 3, 4, 'ellipsis', 10]);
  });

  it('dla środkowych stron dodaje wielokropki po obu stronach', () => {
    expect(pageTokens(5, 10)).toEqual([1, 'ellipsis', 4, 5, 6, 'ellipsis', 10]);
  });

  it('dla ostatnich stron dodaje wielokropek na początku', () => {
    expect(pageTokens(10, 10)).toEqual([1, 'ellipsis', 7, 8, 9, 10]);
  });

  it('zawsze pokazuje pierwszą i ostatnią stronę oraz stronę bieżącą', () => {
    const tokeny = pageTokens(7, 20);

    expect(tokeny[0]).toBe(1);
    expect(tokeny[tokeny.length - 1]).toBe(20);
    expect(tokeny).toContain(7);
  });

  it('przycina numer strony i liczbę stron do sensownych wartości', () => {
    expect(pageTokens(3, 1)).toEqual([1]);
    expect(pageTokens(0, 3)).toEqual([1, 2, 3]);
  });
});
