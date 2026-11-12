import { formatDate } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { D2GridCellDef, D2GridComponent, GridColumn } from '../../../../shared/ui/grid';
import { Project } from '../../../../shared/models/project.model';

/**
 * Lista projektów dokumentów oparta o `d2-grid` (D2GuiShared): sortowanie,
 * filtry kolumnowe, wyszukiwarka i stronicowanie są po stronie grida.
 * Strona odpowiada tylko za dane (w tym filtry serwerowe) i reakcję na wybór.
 */
@Component({
  selector: 'app-project-list-table',
  standalone: true,
  imports: [D2GridComponent, D2GridCellDef],
  templateUrl: './project-list-table.component.html',
  styleUrls: ['./project-list-table.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectListTableComponent {
  readonly projects = input<readonly Project[]>([]);
  readonly loading = input(false);
  readonly projectClick = output<Project>();

  protected readonly columns: readonly GridColumn<Project>[] = [
    { key: 'id', header: 'Id', sortable: true, filterable: true, width: '150px' },
    { key: 'name', header: 'Nazwa dokumentu', sortable: true, filterable: true },
    { key: 'source', header: 'Source', sortable: true, filterable: true, value: (p) => p.source ?? '' },
    {
      // Plik z wersji aktywnej, a przed aktywacją — z ostatniej roboczej (oznaczonej w komórce).
      key: 'templateName',
      header: 'Szablon dokumentu',
      sortable: true,
      filterable: true,
      collapsible: true,
      value: (p) => p.templateName ?? ''
    },
    { key: 'ownerName', header: 'Edytor', sortable: true, filterable: true, collapsible: true },
    {
      key: 'createdAt',
      header: 'Data',
      sortable: true,
      filterable: true,
      width: '140px',
      format: (value) => (value ? formatDate(String(value), 'dd.MM.yyyy HH:mm', 'en-US') : '')
    },
    { key: 'actions', header: 'Akcje', headerHidden: true, searchable: false, width: '110px', align: 'right' }
  ];

  protected readonly trackProject = (project: Project): string => project.id;
}
