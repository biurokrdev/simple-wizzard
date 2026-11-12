/**
 * Kontrolka listy `d2-grid` (D2GuiShared).
 *
 * Pochodzi z projektu `grid-forever` (`ui-grid`); aplikacje D2 importują ją
 * wyłącznie przez ten plik (`shared/ui/grid`).
 */
export { D2GridComponent } from './grid.component';
export { D2GridCellDef } from './grid-cell-def';
export type { GridCellContext } from './grid-cell-def';
export { D2GridDetailDef } from './grid-detail-def';
export type { GridDetailContext } from './grid-detail-def';
export { DEFAULT_GRID_LABELS, formatLabel } from './grid.model';
export type {
  GridAlign,
  GridCollapseMode,
  GridColumn,
  GridFilterCriteria,
  GridFilterField,
  GridFilterOption,
  GridLabels,
  GridPage,
  GridSelectionMode,
  GridSort,
  GridState,
  PageToken,
  SortDirection,
} from './grid.model';
export {
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
