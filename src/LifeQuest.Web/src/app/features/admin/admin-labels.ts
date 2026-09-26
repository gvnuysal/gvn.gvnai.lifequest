import { AdminAction, DayPart, ExperimentStatus, ExperimentVerdict, SafetyLevel, WeightGroup } from '../../core/api/models';
import { t } from '../../core/i18n/i18n';

/* Metinler core/i18n/sections/admin-labels.ts'te; burada ton ve sıra. Getter'lar dil değişince yenilenir. */

export const SAFETY_LABELS: Record<SafetyLevel, { readonly label: string; tone: 'success' | 'warning' | 'danger' }> = {
  Safe: { tone: 'success', get label() { return t().adminLabels.safety.Safe; } },
  NeedsReview: { tone: 'warning', get label() { return t().adminLabels.safety.NeedsReview; } },
  Blocked: { tone: 'danger', get label() { return t().adminLabels.safety.Blocked; } },
};

export const DAY_PART_OPTIONS: { value: DayPart; readonly label: string }[] = (['Morning', 'Afternoon', 'Evening', 'Night'] as const).map(
  (value) => ({ value, get label() { return t().adminLabels.dayParts[value]; } }),
);

export const WEIGHT_GROUPS: { value: WeightGroup; readonly label: string; readonly hint: string }[] = (
  ['Interest', 'Novelty', 'Score', 'Penalty', 'TasteGraph', 'Exploration', 'Windows'] as const
).map((value) => ({
  value,
  get label() { return t().adminLabels.weightGroups[value].label; },
  get hint() { return t().adminLabels.weightGroups[value].hint; },
}));

export const AUDIT_ACTION_LABELS: Record<AdminAction, string> = new Proxy({} as Record<AdminAction, string>, {
  get: (_, key) => t().adminLabels.auditActions[key as AdminAction] ?? String(key),
});

const VERDICT_TONES: Record<ExperimentVerdict, string> = {
  InsufficientData: '', NoDifference: 'warning', TreatmentBetter: 'success', TreatmentWorse: 'danger',
};

export const VERDICT_LABELS: Record<ExperimentVerdict, { readonly label: string; tone: string; readonly hint: string }> =
  Object.fromEntries((Object.keys(VERDICT_TONES) as ExperimentVerdict[]).map((v) => [v, {
    tone: VERDICT_TONES[v],
    get label() { return t().adminLabels.verdicts[v].label; },
    get hint() { return t().adminLabels.verdicts[v].hint; },
  }])) as Record<ExperimentVerdict, { label: string; tone: string; hint: string }>;

const STATUS_TONES: Record<ExperimentStatus, string> = { Draft: '', Running: 'brand', Stopped: 'warning' };

export const EXPERIMENT_STATUS_LABELS: Record<ExperimentStatus, { readonly label: string; tone: string }> =
  Object.fromEntries((Object.keys(STATUS_TONES) as ExperimentStatus[]).map((s) => [s, {
    tone: STATUS_TONES[s],
    get label() { return t().adminLabels.experimentStatus[s]; },
  }])) as Record<ExperimentStatus, { label: string; tone: string }>;

export const SUSPEND_OPTIONS: { value: number | null; readonly label: string }[] = ([1, 7, 30, null] as const).map((value) => ({
  value,
  get label() { return value === null ? t().adminLabels.suspend.forever : t().adminLabels.suspend[value]; },
}));

/** Fikir bayrakları kod olarak gelir; eski kayıtlardaki Türkçe metin olduğu gibi gösterilir. */
export function ideaFlagLabel(flag: string): string {
  const flags = t().adminLabels.ideaFlags as Record<string, string>;
  return flags[flag] ?? flag;
}
