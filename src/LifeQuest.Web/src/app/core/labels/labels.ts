import {
  CostBand,
  Difficulty,
  DiscoveryRadius,
  NotificationPreference,
  PhysicalEffort,
  LifeCategory,
  QuestStatus,
  QuestType,
  ScoreBreakdown,
  SkipReason,
} from '../api/models';
import { IconName } from '../../ui/icon';
import { t } from '../i18n/i18n';

/*
 * Etiketler o anki dilin sözlüğünden getter'larla okunur: şablonda ya da computed içinde okunduklarında dil
 * değişince kendiliğinden yenilenirler. Yapı (ikon, sıra, renk) burada, metin core/i18n/sections/labels.ts'te.
 */

export interface CategoryMeta {
  readonly label: string;
  icon: IconName;
  /** CSS değişken adı öneki: var(--cat-explorer), var(--cat-explorer-ink) */
  token: string;
}

const CATEGORY_ICONS: Record<LifeCategory, { icon: IconName; token: string }> = {
  Explorer: { icon: 'compass', token: 'explorer' },
  Culture: { icon: 'landmark', token: 'culture' },
  Learning: { icon: 'book', token: 'learning' },
  Social: { icon: 'users', token: 'social' },
  Fitness: { icon: 'activity', token: 'fitness' },
  Creativity: { icon: 'palette', token: 'creativity' },
};

export const CATEGORY_ORDER: LifeCategory[] = ['Explorer', 'Culture', 'Learning', 'Social', 'Fitness', 'Creativity'];

export const CATEGORIES: Record<LifeCategory, CategoryMeta> = Object.fromEntries(
  CATEGORY_ORDER.map((c) => [c, { ...CATEGORY_ICONS[c], get label() { return t().labels.categories[c]; } }]),
) as Record<LifeCategory, CategoryMeta>;

export const CATEGORY_DESCRIPTIONS: Record<LifeCategory, string> = lookup(CATEGORY_ORDER, (c) => t().labels.categoryDescriptions[c]);

export const COST_ORDER: CostBand[] = ['Free', 'Low', 'Medium', 'High'];

export const COST_LABELS: Record<CostBand, { readonly short: string; readonly label: string; readonly hint: string }> =
  Object.fromEntries(COST_ORDER.map((c) => [c, {
    get short() { return t().labels.cost[c].short; },
    get label() { return t().labels.cost[c].label; },
    get hint() { return t().labels.cost[c].hint; },
  }])) as Record<CostBand, { short: string; label: string; hint: string }>;

const RADIUS_ICONS: Record<DiscoveryRadius, IconName> = { Chill: 'leaf', Explore: 'compass', SurpriseMe: 'sparkles' };

export const RADIUS_LABELS: Record<DiscoveryRadius, { readonly label: string; readonly description: string; icon: IconName }> =
  Object.fromEntries((['Chill', 'Explore', 'SurpriseMe'] as const).map((r) => [r, {
    icon: RADIUS_ICONS[r],
    get label() { return t().labels.radius[r].label; },
    get description() { return t().labels.radius[r].description; },
  }])) as Record<DiscoveryRadius, { label: string; description: string; icon: IconName }>;

export const QUEST_TYPE_LABELS: Record<QuestType, string> =
  lookup<QuestType>(['Daily', 'Weekly', 'Adventure', 'Epic'], (q) => t().labels.questTypes[q]);

export const DIFFICULTY_LABELS: Record<Difficulty, string> =
  lookup<Difficulty>(['Easy', 'Medium', 'Hard', 'Heroic'], (d) => t().labels.difficulties[d]);

export const STATUS_LABELS: Record<QuestStatus, string> =
  lookup<QuestStatus>(['Offered', 'Accepted', 'Completed', 'Skipped', 'Expired'], (s) => t().labels.statuses[s]);

export const SKIP_REASONS: { readonly value: SkipReason; readonly label: string; readonly hint: string }[] =
  (['NotInterested', 'TooExpensive', 'NoTime', 'TooFar', 'NotToday', 'Other'] as const).map((value) => ({
    value,
    get label() { return t().labels.skipReasons[value].label; },
    get hint() { return t().labels.skipReasons[value].hint; },
  }));

export interface ScoreComponentMeta {
  key: keyof Omit<ScoreBreakdown, 'total'>;
  readonly label: string;
  penalty: boolean;
}

export const SCORE_COMPONENTS: ScoreComponentMeta[] = (
  [
    ['interest', false], ['novelty', false], ['context', false], ['goalFit', false], ['diversity', false],
    ['feedbackFit', false], ['repetition', true], ['friction', true], ['risk', true],
  ] as const
).map(([key, penalty]) => ({ key, penalty, get label() { return t().labels.scoreComponents[key]; } }));

export const WEEKLY_TIME_OPTIONS: { minutes: number; readonly label: string; readonly short: string; readonly hint: string }[] =
  ([120, 300, 600, 900] as const).map((minutes) => ({
    minutes,
    get label() { return t().labels.weeklyTime[minutes].label; },
    get short() { return t().labels.weeklyTime[minutes].short; },
    get hint() { return t().labels.weeklyTime[minutes].hint; },
  }));

export const EFFORT_LABELS: Record<PhysicalEffort, { readonly label: string; readonly hint: string }> =
  Object.fromEntries((['None', 'Light', 'Moderate', 'Vigorous'] as const).map((e) => [e, {
    get label() { return t().labels.effort[e].label; },
    get hint() { return t().labels.effort[e].hint; },
  }])) as Record<PhysicalEffort, { label: string; hint: string }>;

/** Profilde seçilebilen efor üst sınırı (erişilebilirlik / hareket kısıtı). */
export const EFFORT_LIMIT_OPTIONS: { value: PhysicalEffort; readonly label: string; readonly hint: string }[] =
  (['Light', 'Moderate', 'Vigorous'] as const).map((value) => ({
    value,
    get label() { return t().labels.effortLimit[value].label; },
    get hint() { return t().labels.effortLimit[value].hint; },
  }));

export const NOTIFICATION_LABELS: Record<NotificationPreference, string> =
  lookup<NotificationPreference>(['Off', 'WeeklySummary'], (n) => t().labels.notifications[n]);

/** Anahtarları sabit, değerleri her okumada sözlükten gelen salt okunur harita. */
function lookup<K extends string>(keys: readonly K[], read: (key: K) => string): Record<K, string> {
  const target = {} as Record<K, string>;
  for (const key of keys) Object.defineProperty(target, key, { get: () => read(key), enumerable: true });
  return target;
}
