import { LetterWizardStepKey } from '../../data-access/letter-wizard.model';

export type WizardStepStatus = 'todo' | 'in-progress' | 'accepted';

export interface WizardStepperItem {
  key: LetterWizardStepKey;
  index: number;
  label: string;
  status: WizardStepStatus;
}
