export type ProjectStatus = 'Draft' | 'InProgress' | 'Completed';

export interface Project {
  id: string;
  name: string;
  ownerName: string;
  status: ProjectStatus;
  createdAt: string;
  source?: string;
  activeVersionTemplateName?: string;
  /** Nazwa pliku szablonu z wersji aktywnej albo ostatniej roboczej (API: `templateName`). */
  templateName?: string;
  templateVersionTag?: string;
  templateVersionIsActive?: boolean;
}
