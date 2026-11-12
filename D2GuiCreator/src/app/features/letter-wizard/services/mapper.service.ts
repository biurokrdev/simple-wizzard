import { Injectable, signal } from '@angular/core';
import { Observable, of } from 'rxjs';
import { MapperType } from '../data-access/letter-wizard.model';

@Injectable({
  providedIn: 'root'
})
export class MapperService {
  // Signal to cache mapper types
  private mapperTypesCache = signal<MapperType[]>([]);

  constructor() {}

  /**
   * Pobiera dostępne typy maperów z API
   * Na razie zwraca przykładowe dane - do podłączenia pod prawdziwe API
   */
  getMapperTypes(): Observable<MapperType[]> {
    // TODO: Replace with actual API call
    // return this.http.get<MapperType[]>('/api/mapper-types');
    
    const mockMapperTypes: MapperType[] = [
      {
        key: 'choice-list',
        name: 'Choice List',
        templateJson: JSON.stringify({
          value: '',
          'replacement-property-expression': 'form',
          default: ''
        }, null, 2)
      },
      {
        key: 'evaluation',
        name: 'Evaluation',
        templateJson: JSON.stringify({
          expression: '',
          'replacement-property-expression': 'form',
          default: ''
        }, null, 2)
      },
      {
        key: 'direct-mapping',
        name: 'Direct Mapping',
        templateJson: JSON.stringify({
          sourceField: '',
          targetField: '',
          transformation: 'none'
        }, null, 2)
      },
      {
        key: 'conditional',
        name: 'Conditional Mapping',
        templateJson: JSON.stringify({
          condition: '',
          trueValue: '',
          falseValue: '',
          'replacement-property-expression': 'form'
        }, null, 2)
      },
      {
        key: 'lookup',
        name: 'Lookup Table',
        templateJson: JSON.stringify({
          lookupKey: '',
          lookupTable: {},
          defaultValue: ''
        }, null, 2)
      }
    ];

    this.mapperTypesCache.set(mockMapperTypes);
    return of(mockMapperTypes);
  }

  /**
   * Zwraca template JSON dla danego typu mapera
   */
  getTemplateForMapperType(mapperKey: string): string {
    const mapperType = this.mapperTypesCache().find(mt => mt.key === mapperKey);
    return mapperType?.templateJson || '{}';
  }
}
