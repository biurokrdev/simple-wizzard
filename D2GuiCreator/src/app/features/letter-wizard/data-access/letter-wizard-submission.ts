import { LetterWizardData, LetterWizardStepKey } from './letter-wizard.model';

/**
 * Reguły „wymagane przed wysłaniem do weryfikacji".
 *
 * W trakcie pracy nad projektem pisma sekcje „Dane biznesowe" i „Retencja danych" w kroku 1 są opcjonalne
 * (można zapisać szkic i iść dalej). Dopiero finalizacja — przycisk „Wyślij do weryfikacji" w podsumowaniu —
 * wymaga kompletu. Jedno źródło prawdy dla blokady w `LetterWizardPageComponent` i panelu braków w `SummaryStep`.
 */
export interface SubmissionIssue {
  /** Klucz kontrolki w formularzu (`business.processName`, `general.retentionDays`). */
  field: string;
  /** Etykieta pola jak w kroku 1. */
  label: string;
  /** Sekcja kroku 1, w której pole się znajduje. */
  section: 'Dane biznesowe' | 'Retencja danych';
  /** Krok, do którego należy przejść, żeby uzupełnić pole. */
  step: LetterWizardStepKey;
  /** Komunikat (gdy inny niż „pole jest puste"). */
  message?: string;
}

export const RETENTION_DAYS_MIN = 1;
export const RETENTION_DAYS_MAX = 3650; // BR-005 / CHECK chk_retention_days

const isBlank = (value: unknown): boolean =>
  value === null || value === undefined || (typeof value === 'string' && value.trim() === '');

export function getSubmissionIssues(data: LetterWizardData | null | undefined): SubmissionIssue[] {
  if (!data) {
    return [];
  }

  const issues: SubmissionIssue[] = [];
  const business = data.business ?? ({} as LetterWizardData['business']);
  const general = data.general ?? ({} as LetterWizardData['general']);

  const businessFields: Array<[keyof LetterWizardData['business'], string]> = [
    ['processName', 'Nazwa procesu lub pod-procesu'],
    ['processOwner', 'Właściciel procesu'],
    ['businessLine', 'Linia biznesowa'],
    ['processVersion', 'Wersja procesu'],
    ['processStep', 'Krok procesu'],
    ['status', 'Stan'],
  ];

  for (const [key, label] of businessFields) {
    if (isBlank(business[key])) {
      issues.push({ field: `business.${key}`, label, section: 'Dane biznesowe', step: 'documentDescription' });
    }
  }

  if (isBlank(general.recipient)) {
    issues.push({ field: 'general.recipient', label: 'Odbiorca', section: 'Dane biznesowe', step: 'documentDescription' });
  }

  const retention = general.retentionDays;
  if (isBlank(retention) || Number.isNaN(Number(retention))) {
    issues.push({ field: 'general.retentionDays', label: 'Ilość dni przechowywania', section: 'Retencja danych', step: 'documentDescription' });
  } else if (Number(retention) < RETENTION_DAYS_MIN || Number(retention) > RETENTION_DAYS_MAX) {
    issues.push({
      field: 'general.retentionDays',
      label: 'Ilość dni przechowywania',
      section: 'Retencja danych',
      step: 'documentDescription',
      message: `Podaj wartość od ${RETENTION_DAYS_MIN} do ${RETENTION_DAYS_MAX} dni`,
    });
  }

  return issues;
}

export function isReadyForSubmission(data: LetterWizardData | null | undefined): boolean {
  return getSubmissionIssues(data).length === 0;
}
