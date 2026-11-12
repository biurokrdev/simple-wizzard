import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { D2GridComponent } from './grid.component';
import { D2GridCellDef } from './grid-cell-def';
import { D2GridDetailDef } from './grid-detail-def';
import {
  GridCollapseMode,
  GridColumn,
  GridFilterField,
  GridLabels,
  GridSelectionMode,
  GridSort,
  GridState,
} from './grid.model';

interface Osoba {
  readonly id: string;
  readonly imie: string;
  readonly miasto: string;
  readonly wiek: number;
}

/** Imiona celowo w kolejności innej niż alfabetyczna - widać efekt sortowania. */
const IMIONA = [
  'Halina',
  'Anna',
  'Celina',
  'Bartek',
  'Zofia',
  'Damian',
  'Franciszek',
  'Ewa',
  'Gabriela',
  'Igor',
  'Jan',
  'Karolina',
];

const OSOBY: readonly Osoba[] = IMIONA.map((imie, index) => ({
  id: `osoba-${index}`,
  imie,
  // Co drugi wiersz Gdańsk - razem 6 wierszy.
  miasto: index % 2 === 0 ? 'Łódź' : 'Gdańsk',
  wiek: 20 + index,
}));

const KOLUMNY: readonly GridColumn<Osoba>[] = [
  { key: 'imie', header: 'Imię', sortable: true, filterable: true },
  // Kolumna chowana do szczegółów wiersza przy wąskim ekranie.
  { key: 'miasto', header: 'Miasto', sortable: true, filterable: true, collapsible: true },
  { key: 'wiek', header: 'Wiek', sortable: true, align: 'right' },
  { key: 'akcje', header: 'Akcje', headerHidden: true, searchable: false },
];

const POLA_FILTROW: readonly GridFilterField[] = [
  {
    key: 'miasto',
    label: 'Miasto',
    options: [
      { value: 'Łódź', label: 'Łódź' },
      { value: 'Gdańsk', label: 'Gdańsk' },
    ],
  },
];

@Component({
  imports: [D2GridComponent, D2GridCellDef, D2GridDetailDef],
  template: `
    <d2-grid
      [columns]="KOLUMNY"
      [rows]="dane()"
      [trackBy]="trackOsoba"
      [pageSize]="rozmiarStrony()"
      [pageSizeOptions]="[5, 10]"
      [searchable]="wyszukiwanie()"
      [showColumnFilters]="filtryKolumn()"
      [filterFields]="polaFiltrow()"
      [initialSort]="sortowanie()"
      [loading]="ladowanie()"
      [skeletonRows]="liczbaSzkieletow()"
      [collapse]="trybZwijania()"
      [selection]="trybZaznaczania()"
      [labels]="etykiety()"
      [rowClickable]="klikalne()"
      caption="Lista osób"
      (stateChange)="stany.push($event)"
      (selectionChange)="zaznaczenia.push($event)"
      (rowClick)="klikniete.push($event)"
    >
      <button d2GridActions type="button" class="akcja-listy">Pobierz listę</button>

      <ng-template d2GridCell="akcje" let-osoba>
        <button type="button" class="akcja-wiersza">{{ osoba.imie }}</button>
      </ng-template>

      @if (szczegoly()) {
        <ng-template d2GridDetail let-osoba let-index="index">
          <p class="szczegoly-wiersza">{{ index }}: {{ osoba.imie }} ma {{ osoba.wiek }} lat</p>
        </ng-template>
      }
    </d2-grid>
  `,
})
class HostGrida {
  protected readonly KOLUMNY = KOLUMNY;
  readonly dane = signal<readonly Osoba[]>(OSOBY);
  readonly rozmiarStrony = signal(5);
  readonly wyszukiwanie = signal(true);
  readonly filtryKolumn = signal(true);
  readonly polaFiltrow = signal<readonly GridFilterField[]>([]);
  readonly sortowanie = signal<GridSort | null>(null);
  readonly ladowanie = signal(false);
  readonly liczbaSzkieletow = signal<number | null>(null);
  // Domyślnie bez chowania kolumn - testy zwijania włączają je jawnie.
  readonly trybZwijania = signal<GridCollapseMode>('never');
  readonly trybZaznaczania = signal<GridSelectionMode>('none');
  readonly etykiety = signal<Partial<GridLabels>>({});
  readonly klikalne = signal(false);
  readonly szczegoly = signal(false);
  readonly stany: GridState[] = [];
  readonly zaznaczenia: (readonly Osoba[])[] = [];
  readonly klikniete: Osoba[] = [];
  protected readonly trackOsoba = (osoba: Osoba): string => osoba.id;
}

describe('D2GridComponent (d2-grid)', () => {
  let fixture: ComponentFixture<HostGrida>;
  let host: HostGrida;
  let element: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [HostGrida] }).compileComponents();

    fixture = TestBed.createComponent(HostGrida);
    host = fixture.componentInstance;
    element = fixture.nativeElement as HTMLElement;
    fixture.detectChanges();
  });

  // --- Pomocnicze odczyty DOM ----------------------------------------------

  function naglowki(): string[] {
    return zapytajWszystkie('.d2-grid__head-cell').map((th) => th.textContent?.trim() ?? '');
  }

  function wiersze(): HTMLElement[] {
    return zapytajWszystkie('.d2-grid__row');
  }

  function komorki(indeksWiersza: number): string[] {
    return Array.from(wiersze()[indeksWiersza].querySelectorAll('td')).map(
      (td) => td.textContent?.trim() ?? '',
    );
  }

  function kolumnaTekstow(indeksKolumny: number): string[] {
    return wiersze().map(
      (wiersz) => wiersz.querySelectorAll('td')[indeksKolumny].textContent?.trim() ?? '',
    );
  }

  function podsumowanie(): string {
    return zapytaj('.d2-grid__summary').textContent?.trim() ?? '';
  }

  function komunikat(): string {
    return zapytaj('.d2-grid__message').textContent?.trim() ?? '';
  }

  function zapytaj<T extends HTMLElement>(selektor: string): T {
    const znaleziony = element.querySelector<T>(selektor);
    if (!znaleziony) {
      throw new Error(`Nie znaleziono elementu: ${selektor}`);
    }
    return znaleziony;
  }

  function zapytajWszystkie<T extends HTMLElement>(selektor: string): T[] {
    return Array.from(element.querySelectorAll<T>(selektor));
  }

  // --- Pomocnicze akcje ----------------------------------------------------

  function wpisz(selektor: string, tekst: string): void {
    const pole = zapytaj<HTMLInputElement>(selektor);
    pole.value = tekst;
    pole.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  function wybierz(selektor: string, wartosc: string): void {
    const pole = zapytaj<HTMLSelectElement>(selektor);
    pole.value = wartosc;
    pole.dispatchEvent(new Event('change'));
    fixture.detectChanges();
  }

  function klik(selektor: string): void {
    zapytaj<HTMLElement>(selektor).click();
    fixture.detectChanges();
  }

  function klikPoTekscie(selektor: string, tekst: string): void {
    const znaleziony = zapytajWszystkie<HTMLElement>(selektor).find(
      (kandydat) => kandydat.textContent?.replace(/\s+/g, ' ').trim() === tekst,
    );
    if (!znaleziony) {
      throw new Error(`Nie znaleziono elementu "${tekst}" (${selektor})`);
    }
    znaleziony.click();
    fixture.detectChanges();
  }

  function klikNaglowek(naglowek: string): void {
    klikPoTekscie('.d2-grid__sort', naglowek);
  }

  function ustawWHoscie(zmiana: () => void): void {
    zmiana();
    fixture.detectChanges();
  }

  // --- Testy ---------------------------------------------------------------

  describe('renderowanie', () => {
    it('pokazuje nagłówki wszystkich kolumn', () => {
      expect(naglowki()).toEqual(['Imię', 'Miasto', 'Wiek', 'Akcje']);
    });

    it('renderuje opis tabeli dla czytników ekranu', () => {
      expect(zapytaj('caption').textContent?.trim()).toBe('Lista osób');
    });

    it('pokazuje tylko wiersze bieżącej strony', () => {
      expect(wiersze().length).toBe(5);
      expect(komorki(0)[0]).toBe('Halina');
      expect(podsumowanie()).toBe('Wyświetlono 1-5 z 12');
    });

    it('rzutuje wartości komórek na tekst zgodnie z definicją kolumny', () => {
      expect(komorki(0)[2]).toBe('20');
    });

    it('renderuje własny szablon komórki zamiast domyślnej wartości', () => {
      const przyciski = zapytajWszystkie<HTMLButtonElement>('.akcja-wiersza');

      expect(przyciski.length).toBe(5);
      expect(przyciski[0].textContent?.trim()).toBe('Halina');
    });

    it('rzutuje akcje aplikacji do paska narzędzi', () => {
      expect(zapytaj('.d2-grid__toolbar-actions .akcja-listy').textContent?.trim()).toBe(
        'Pobierz listę',
      );
    });

    it('bez wyszukiwarki nadal pokazuje akcje aplikacji w pasku narzędzi', () => {
      ustawWHoscie(() => host.wyszukiwanie.set(false));

      expect(element.querySelector('.d2-grid__search-input')).toBeNull();
      expect(zapytaj('.d2-grid__toolbar-actions .akcja-listy').textContent?.trim()).toBe(
        'Pobierz listę',
      );
    });

    it('ukrywa wyszukiwarkę i filtry, gdy są wyłączone', () => {
      ustawWHoscie(() => {
        host.wyszukiwanie.set(false);
        host.filtryKolumn.set(false);
      });

      expect(element.querySelector('.d2-grid__search-input')).toBeNull();
      expect(element.querySelector('.d2-grid__filter-input')).toBeNull();
    });
  });

  describe('ładowanie danych', () => {
    beforeEach(() => ustawWHoscie(() => host.ladowanie.set(true)));

    it('pokazuje szkielet zamiast wierszy danych', () => {
      expect(wiersze().length).toBe(0);
      expect(zapytajWszystkie('.d2-grid__skeleton-row').length).toBe(5);
    });

    it('zachowuje układ kolumn, żeby tabela nie skakała po wczytaniu', () => {
      expect(zapytajWszystkie('.d2-grid__head-cell').length).toBe(KOLUMNY.length);
      expect(zapytajWszystkie('.d2-grid__skeleton--header').length).toBe(KOLUMNY.length);
    });

    it('zastępuje szkieletem także wyszukiwarkę, filtry i stopkę', () => {
      expect(element.querySelector('.d2-grid__search-input')).toBeNull();
      expect(element.querySelector('.d2-grid__filter-input')).toBeNull();
      expect(element.querySelector('.d2-grid__page-size-select')).toBeNull();

      expect(element.querySelector('.d2-grid__skeleton--search')).not.toBeNull();
      expect(zapytajWszystkie('.d2-grid__skeleton--filter').length).toBe(2);
      expect(element.querySelector('.d2-grid__skeleton--page-size')).not.toBeNull();
      expect(element.querySelector('.d2-grid__skeleton--pagination')).not.toBeNull();
    });

    it('ukrywa akcje aplikacji na czas ładowania', () => {
      expect(zapytaj('.d2-grid__projected').classList.contains('d2-grid__projected--hidden')).toBe(true);
      expect(zapytajWszystkie('.d2-grid__skeleton--action').length).toBe(1);
    });

    it('pokazuje szkielet pól panelu filtrów', () => {
      ustawWHoscie(() => host.polaFiltrow.set(POLA_FILTROW));

      expect(element.querySelector('.d2-grid__filters-select')).toBeNull();
      expect(zapytajWszystkie('.d2-grid__skeleton--field').length).toBe(POLA_FILTROW.length);
    });

    it('zgłasza ładowanie czytnikom ekranu', () => {
      expect(zapytaj('[role="status"]').textContent?.trim()).toBe('Trwa ładowanie danych...');
    });

    it('pozwala ustawić własną liczbę wierszy szkieletu', () => {
      ustawWHoscie(() => host.liczbaSzkieletow.set(3));

      expect(zapytajWszystkie('.d2-grid__skeleton-row').length).toBe(3);
    });

    it('po zakończeniu ładowania pokazuje dane', () => {
      ustawWHoscie(() => host.ladowanie.set(false));

      expect(element.querySelector('.d2-grid__skeleton-row')).toBeNull();
      expect(wiersze().length).toBe(5);
    });
  });

  describe('sortowanie', () => {
    it('pierwsze kliknięcie sortuje rosnąco', () => {
      klikNaglowek('Imię');

      expect(kolumnaTekstow(0)).toEqual(['Anna', 'Bartek', 'Celina', 'Damian', 'Ewa']);
    });

    it('drugie kliknięcie sortuje malejąco, trzecie wyłącza sortowanie', () => {
      klikNaglowek('Imię');
      klikNaglowek('Imię');
      expect(komorki(0)[0]).toBe('Zofia');

      klikNaglowek('Imię');
      expect(komorki(0)[0]).toBe('Halina');
    });

    it('opisuje stan sortowania atrybutem aria-sort', () => {
      const naglowekImie = () => zapytajWszystkie('.d2-grid__head-cell')[0];
      expect(naglowekImie().getAttribute('aria-sort')).toBe('none');

      klikNaglowek('Imię');
      expect(naglowekImie().getAttribute('aria-sort')).toBe('ascending');

      klikNaglowek('Imię');
      expect(naglowekImie().getAttribute('aria-sort')).toBe('descending');
    });

    it('kolumny bez sortowania nie mają przycisku ani atrybutu aria-sort', () => {
      const naglowekAkcje = zapytajWszystkie('.d2-grid__head-cell')[3];

      expect(naglowekAkcje.querySelector('button')).toBeNull();
      expect(naglowekAkcje.getAttribute('aria-sort')).toBeNull();
    });

    it('respektuje sortowanie początkowe', () => {
      ustawWHoscie(() => host.sortowanie.set({ key: 'wiek', direction: 'desc' }));

      expect(komorki(0)[2]).toBe('31');
    });

    it('sortowanie po innej kolumnie zaczyna od porządku rosnącego', () => {
      klikNaglowek('Imię');
      klikNaglowek('Miasto');

      expect(kolumnaTekstow(1).every((miasto) => miasto === 'Gdańsk')).toBe(true);
    });
  });

  describe('wyszukiwanie i filtry kolumnowe', () => {
    it('wyszukiwanie globalne zawęża listę, ignorując diakrytyki', () => {
      wpisz('.d2-grid__search-input', 'gdansk');

      expect(podsumowanie()).toBe('Wyświetlono 1-5 z 6');
      expect(kolumnaTekstow(1).every((miasto) => miasto === 'Gdańsk')).toBe(true);
    });

    it('filtr kolumnowy zawęża listę tylko po swojej kolumnie', () => {
      wpisz('input[aria-label="Filtruj kolumnę Imię"]', 'an');

      expect(kolumnaTekstow(0)).toEqual(['Anna', 'Damian', 'Franciszek', 'Jan']);
    });

    it('filtry z różnych kolumn łączą się warunkiem "i"', () => {
      wpisz('input[aria-label="Filtruj kolumnę Imię"]', 'an');
      wpisz('input[aria-label="Filtruj kolumnę Miasto"]', 'gda');

      expect(kolumnaTekstow(0)).toEqual(['Anna', 'Damian']);
    });

    it('zmiana filtra wraca na pierwszą stronę', () => {
      klik('button[aria-label="Strona 3"]');
      expect(podsumowanie()).toBe('Wyświetlono 11-12 z 12');

      wpisz('input[aria-label="Filtruj kolumnę Miasto"]', 'gda');

      expect(podsumowanie()).toBe('Wyświetlono 1-5 z 6');
    });

    it('pokazuje komunikat, gdy nic nie pasuje', () => {
      wpisz('.d2-grid__search-input', 'xyz');

      expect(wiersze().length).toBe(0);
      expect(komunikat()).toBe('Brak danych do wyświetlenia');
      expect(podsumowanie()).toBe('Wyświetlono 0-0 z 0');
    });

    it('fraza wyszukiwania blokuje pola filtrów kolumnowych', () => {
      wpisz('.d2-grid__search-input', 'gda');

      const filtry = zapytajWszystkie<HTMLInputElement>('.d2-grid__filter-input');

      expect(filtry.length).toBeGreaterThan(0);
      expect(filtry.every((pole) => pole.disabled)).toBe(true);
    });

    it('filtr kolumnowy blokuje wyszukiwarkę globalną', () => {
      wpisz('input[aria-label="Filtruj kolumnę Imię"]', 'an');

      expect(zapytaj<HTMLInputElement>('.d2-grid__search-input').disabled).toBe(true);
    });

    it('wyczyszczenie filtra kolumny odblokowuje wyszukiwarkę', () => {
      wpisz('input[aria-label="Filtruj kolumnę Imię"]', 'an');

      klik('button[aria-label="Wyczyść filtr kolumny Imię"]');

      expect(zapytaj<HTMLInputElement>('.d2-grid__search-input').disabled).toBe(false);
      expect(podsumowanie()).toBe('Wyświetlono 1-5 z 12');
    });

    it('pozwala nadpisać teksty interfejsu', () => {
      ustawWHoscie(() => host.etykiety.set({ empty: 'Nic nie znaleziono' }));
      wpisz('.d2-grid__search-input', 'xyz');

      expect(komunikat()).toBe('Nic nie znaleziono');
    });
  });

  describe('panel filtrów', () => {
    beforeEach(() => ustawWHoscie(() => host.polaFiltrow.set(POLA_FILTROW)));

    it('nie pokazuje panelu, gdy aplikacja nie poda pól', () => {
      ustawWHoscie(() => host.polaFiltrow.set([]));

      expect(element.querySelector('.d2-grid__filters')).toBeNull();
    });

    it('buduje listę z opcji podanych przez aplikację i pustym wyborem', () => {
      const opcje = zapytajWszystkie('.d2-grid__filters-select option').map((opcja) =>
        opcja.textContent?.trim(),
      );

      expect(opcje).toEqual(['Wszystkie', 'Łódź', 'Gdańsk']);
    });

    it('wybór wartości zawęża listę dokładnym dopasowaniem', () => {
      wybierz('.d2-grid__filters-select', 'Gdańsk');

      expect(podsumowanie()).toBe('Wyświetlono 1-5 z 6');
      expect(kolumnaTekstow(1).every((miasto) => miasto === 'Gdańsk')).toBe(true);
    });

    it('zwija i rozwija pola filtrów', () => {
      expect(element.querySelector('.d2-grid__filters-select')).not.toBeNull();

      klik('.d2-grid__filters-toggle');
      expect(element.querySelector('.d2-grid__filters-select')).toBeNull();
      expect(zapytaj('.d2-grid__filters-toggle').textContent).toContain('Pokaż filtry');

      klik('.d2-grid__filters-toggle');
      expect(element.querySelector('.d2-grid__filters-select')).not.toBeNull();
    });

    it('pokazuje licznik aktywnych filtrów i czyści je jednym kliknięciem', () => {
      expect(element.querySelector('.d2-grid__filters-clear')).toBeNull();

      wybierz('.d2-grid__filters-select', 'Gdańsk');
      wpisz('input[aria-label="Filtruj kolumnę Imię"]', 'an');

      expect(zapytaj('.d2-grid__filters-clear').textContent).toContain('Wyczyść filtry (2)');

      klik('.d2-grid__filters-clear');

      expect(podsumowanie()).toBe('Wyświetlono 1-5 z 12');
      expect(element.querySelector('.d2-grid__filters-clear')).toBeNull();
    });

    it('czyszczenie filtrów nie kasuje frazy wyszukiwania', () => {
      wpisz('.d2-grid__search-input', 'gdansk');
      wybierz('.d2-grid__filters-select', 'Gdańsk');

      klik('.d2-grid__filters-clear');

      expect(zapytaj<HTMLInputElement>('.d2-grid__search-input').value).toBe('gdansk');
      expect(podsumowanie()).toBe('Wyświetlono 1-5 z 6');
    });
  });

  describe('chowanie kolumn i szczegóły wiersza', () => {
    beforeEach(() => ustawWHoscie(() => host.trybZwijania.set('always')));

    it('chowa kolumny oznaczone jako collapsible', () => {
      // Ostatni nagłówek to kolumna rozwijania - opisana tylko dla czytników ekranu.
      expect(naglowki()).toEqual(['Imię', 'Wiek', 'Akcje', 'Pokaż szczegóły wiersza']);
    });

    it('nie chowa kolumn w trybie "never"', () => {
      ustawWHoscie(() => host.trybZwijania.set('never'));

      expect(naglowki()).toEqual(['Imię', 'Miasto', 'Wiek', 'Akcje']);
      expect(element.querySelector('.d2-grid__expander')).toBeNull();
    });

    it('rozwija panel szczegółów z ukrytymi kolumnami', () => {
      expect(element.querySelector('.d2-grid__detail-row')).toBeNull();

      klik('.d2-grid__expander');

      const szczegoly = zapytaj('.d2-grid__detail-row');
      expect(szczegoly.textContent).toContain('Miasto');
      expect(szczegoly.textContent).toContain('Łódź');
    });

    it('ponowne kliknięcie zwija panel szczegółów', () => {
      klik('.d2-grid__expander');
      klik('.d2-grid__expander');

      expect(element.querySelector('.d2-grid__detail-row')).toBeNull();
    });

    it('opisuje stan rozwinięcia dla czytników ekranu', () => {
      const przycisk = () => zapytaj<HTMLButtonElement>('.d2-grid__expander');
      expect(przycisk().getAttribute('aria-expanded')).toBe('false');
      expect(przycisk().getAttribute('aria-label')).toBe('Pokaż szczegóły wiersza');

      klik('.d2-grid__expander');

      expect(przycisk().getAttribute('aria-expanded')).toBe('true');
      expect(przycisk().getAttribute('aria-label')).toBe('Ukryj szczegóły wiersza');
    });

    it('rozwija tylko wskazany wiersz', () => {
      zapytajWszystkie<HTMLButtonElement>('.d2-grid__expander')[1].click();
      fixture.detectChanges();

      expect(zapytajWszystkie('.d2-grid__detail-row').length).toBe(1);
      expect(zapytaj('.d2-grid__detail-row').textContent).toContain('Gdańsk');
    });
  });

  describe('własny szablon szczegółów wiersza (d2GridDetail)', () => {
    it('bez szablonu i bez schowanych kolumn nie pokazuje przycisku rozwijania', () => {
      expect(element.querySelector('.d2-grid__expander')).toBeNull();
    });

    it('z szablonem pokazuje przycisk rozwijania mimo widocznych wszystkich kolumn', () => {
      ustawWHoscie(() => host.szczegoly.set(true));

      expect(zapytajWszystkie('.d2-grid__expander').length).toBe(5);
      expect(naglowki()).toEqual(['Imię', 'Miasto', 'Wiek', 'Akcje', 'Pokaż szczegóły wiersza']);
    });

    it('rozwija treść szablonu dla klikniętego wiersza', () => {
      ustawWHoscie(() => host.szczegoly.set(true));

      zapytajWszystkie<HTMLButtonElement>('.d2-grid__expander')[1].click();
      fixture.detectChanges();

      expect(zapytajWszystkie('.d2-grid__detail-row').length).toBe(1);
      expect(zapytaj('.szczegoly-wiersza').textContent?.trim()).toBe('1: Anna ma 21 lat');
      expect(element.querySelector('.d2-grid__detail-list')).toBeNull();
    });

    it('po schowaniu kolumn pokazuje schowane kolumny i szablon razem', () => {
      ustawWHoscie(() => {
        host.szczegoly.set(true);
        host.trybZwijania.set('always');
      });

      klik('.d2-grid__expander');

      const szczegoly = zapytaj('.d2-grid__detail-row');
      expect(szczegoly.querySelector('.d2-grid__detail-list')?.textContent).toContain('Łódź');
      expect(szczegoly.querySelector('.szczegoly-wiersza')?.textContent).toContain('Halina');
    });
  });

  describe('kliknięcie w wiersz', () => {
    it('domyślnie nie zgłasza kliknięć i nie oznacza wierszy jako klikalne', () => {
      wiersze()[0].click();
      fixture.detectChanges();

      expect(host.klikniete.length).toBe(0);
      expect(wiersze()[0].classList.contains('d2-grid__row--clickable')).toBe(false);
    });

    it('po włączeniu zgłasza kliknięty wiersz', () => {
      ustawWHoscie(() => host.klikalne.set(true));

      wiersze()[1].querySelector('td')!.click();
      fixture.detectChanges();

      expect(host.klikniete.map((osoba) => osoba.imie)).toEqual(['Anna']);
      expect(wiersze()[0].classList.contains('d2-grid__row--clickable')).toBe(true);
    });

    it('kliknięcie w przycisk akcji wiersza nie jest kliknięciem w wiersz', () => {
      ustawWHoscie(() => host.klikalne.set(true));

      zapytajWszystkie<HTMLButtonElement>('.akcja-wiersza')[0].click();
      fixture.detectChanges();

      expect(host.klikniete.length).toBe(0);
    });
  });

  describe('zaznaczanie wierszy', () => {
    function pola(): HTMLInputElement[] {
      return zapytajWszystkie<HTMLInputElement>('.d2-grid__row .d2-grid__checkbox');
    }

    it('domyślnie nie pokazuje pól zaznaczania', () => {
      expect(element.querySelector('.d2-grid__checkbox')).toBeNull();
    });

    it('w trybie "multiple" dodaje kolumnę z polami wyboru', () => {
      ustawWHoscie(() => host.trybZaznaczania.set('multiple'));

      expect(pola().length).toBe(5);
      expect(pola()[0].type).toBe('checkbox');
    });

    it('zaznacza wiersz i zgłasza go aplikacji', () => {
      ustawWHoscie(() => host.trybZaznaczania.set('multiple'));

      pola()[0].click();
      fixture.detectChanges();

      expect(host.zaznaczenia.at(-1)?.map((osoba) => osoba.imie)).toEqual(['Halina']);
      expect(zapytaj('.d2-grid__selection-count').textContent?.trim()).toBe('Zaznaczono: 1');
    });

    it('w trybie "single" zaznaczenie przechodzi na kolejny wiersz', () => {
      ustawWHoscie(() => host.trybZaznaczania.set('single'));

      pola()[0].click();
      fixture.detectChanges();
      pola()[1].click();
      fixture.detectChanges();

      expect(host.zaznaczenia.at(-1)?.map((osoba) => osoba.imie)).toEqual(['Anna']);
      expect(pola()[0].type).toBe('radio');
    });

    it('nagłówek zaznacza i odznacza całą stronę', () => {
      ustawWHoscie(() => host.trybZaznaczania.set('multiple'));

      klik('input[aria-label="Zaznacz wszystkie wiersze na stronie"]');
      expect(host.zaznaczenia.at(-1)?.length).toBe(5);

      klik('input[aria-label="Zaznacz wszystkie wiersze na stronie"]');
      expect(host.zaznaczenia.at(-1)?.length).toBe(0);
    });

    it('zaznaczenie przeżywa zmianę strony', () => {
      ustawWHoscie(() => host.trybZaznaczania.set('multiple'));
      pola()[0].click();
      fixture.detectChanges();

      klik('button[aria-label="Strona 2"]');

      expect(zapytaj('.d2-grid__selection-count').textContent?.trim()).toBe('Zaznaczono: 1');
      expect(pola().every((pole) => !pole.checked)).toBe(true);
    });

    it('pasek zaznaczenia znika po odznaczeniu wszystkiego', () => {
      ustawWHoscie(() => host.trybZaznaczania.set('multiple'));

      pola()[0].click();
      fixture.detectChanges();
      expect(element.querySelector('.d2-grid__selection-bar')).not.toBeNull();

      pola()[0].click();
      fixture.detectChanges();

      expect(element.querySelector('.d2-grid__selection-bar')).toBeNull();
    });
  });

  describe('stronicowanie', () => {
    it('przechodzi na następną i poprzednią stronę', () => {
      klik('button[aria-label="Następna strona"]');
      expect(podsumowanie()).toBe('Wyświetlono 6-10 z 12');

      klik('button[aria-label="Poprzednia strona"]');
      expect(podsumowanie()).toBe('Wyświetlono 1-5 z 12');
    });

    it('blokuje strzałki na skrajnych stronach', () => {
      expect(
        zapytaj<HTMLButtonElement>('button[aria-label="Poprzednia strona"]').disabled,
      ).toBe(true);

      klik('button[aria-label="Strona 3"]');

      expect(zapytaj<HTMLButtonElement>('button[aria-label="Następna strona"]').disabled).toBe(true);
    });

    it('oznacza bieżącą stronę atrybutem aria-current', () => {
      klik('button[aria-label="Strona 2"]');

      expect(zapytaj('button[aria-label="Strona 2"]').getAttribute('aria-current')).toBe('page');
      expect(zapytaj('button[aria-label="Strona 1"]').getAttribute('aria-current')).toBeNull();
    });

    it('zmiana rozmiaru strony wraca na pierwszą stronę', () => {
      klik('button[aria-label="Strona 3"]');

      wybierz('.d2-grid__page-size-select', '10');

      expect(wiersze().length).toBe(10);
      expect(podsumowanie()).toBe('Wyświetlono 1-10 z 12');
    });

    it('po zmniejszeniu zbioru danych nie zostawia pustej strony', () => {
      klik('button[aria-label="Strona 3"]');

      ustawWHoscie(() => host.dane.set(OSOBY.slice(0, 3)));

      expect(wiersze().length).toBe(3);
      expect(podsumowanie()).toBe('Wyświetlono 1-3 z 3');
    });

    it('respektuje rozmiar strony podany przez aplikację', () => {
      ustawWHoscie(() => host.rozmiarStrony.set(10));

      expect(wiersze().length).toBe(10);
    });
  });

  describe('zdarzenie stateChange', () => {
    it('nie emituje niczego przed pierwszą interakcją', () => {
      expect(host.stany.length).toBe(0);
    });

    it('zgłasza pełny stan po zmianie sortowania', () => {
      klikNaglowek('Imię');

      expect(host.stany.at(-1)).toEqual({
        search: '',
        filters: {},
        fieldFilters: {},
        sort: { key: 'imie', direction: 'asc' },
        page: 1,
        pageSize: 5,
      });
    });

    it('zgłasza frazę wyszukiwania i numer strony', () => {
      wpisz('.d2-grid__search-input', 'gda');
      klik('button[aria-label="Strona 2"]');

      expect(host.stany.at(-1)).toEqual({
        search: 'gda',
        filters: {},
        fieldFilters: {},
        sort: null,
        page: 2,
        pageSize: 5,
      });
    });

    it('zgłasza filtr kolumnowy i numer strony', () => {
      wpisz('input[aria-label="Filtruj kolumnę Imię"]', 'a');
      klik('button[aria-label="Strona 2"]');

      expect(host.stany.at(-1)).toEqual({
        search: '',
        filters: { imie: 'a' },
        fieldFilters: {},
        sort: null,
        page: 2,
        pageSize: 5,
      });
    });

    it('zgłasza wybory z panelu filtrów', () => {
      ustawWHoscie(() => host.polaFiltrow.set(POLA_FILTROW));

      wybierz('.d2-grid__filters-select', 'Gdańsk');

      expect(host.stany.at(-1)?.fieldFilters).toEqual({ miasto: 'Gdańsk' });
    });
  });
});
