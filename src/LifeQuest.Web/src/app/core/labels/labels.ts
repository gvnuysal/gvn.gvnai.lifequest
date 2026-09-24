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

export interface CategoryMeta {
  label: string;
  icon: IconName;
  /** CSS değişken adı öneki: var(--cat-explorer), var(--cat-explorer-ink) */
  token: string;
}

export const CATEGORIES: Record<LifeCategory, CategoryMeta> = {
  Explorer: { label: 'Keşif', icon: 'compass', token: 'explorer' },
  Culture: { label: 'Kültür', icon: 'landmark', token: 'culture' },
  Learning: { label: 'Öğrenme', icon: 'book', token: 'learning' },
  Social: { label: 'Sosyal', icon: 'users', token: 'social' },
  Fitness: { label: 'Hareket', icon: 'activity', token: 'fitness' },
  Creativity: { label: 'Yaratıcılık', icon: 'palette', token: 'creativity' },
};

export const CATEGORY_ORDER: LifeCategory[] = ['Explorer', 'Culture', 'Learning', 'Social', 'Fitness', 'Creativity'];

export const CATEGORY_DESCRIPTIONS: Record<LifeCategory, string> = {
  Explorer: 'Yeni mekânlar, semtler, lezzetler',
  Culture: 'Sanat, sinema, müze, müzik',
  Learning: 'Okuma, dil, bilim, yeni beceriler',
  Social: 'Arkadaşlar, topluluklar, gönüllülük',
  Fitness: 'Yürüyüş, koşu, yoga, hareket',
  Creativity: 'Fotoğraf, çizim, yazı, el işi',
};

export const COST_LABELS: Record<CostBand, { short: string; label: string; hint: string }> = {
  Free: { short: 'Ücretsiz', label: 'Ücretsiz', hint: 'Para harcamadan' },
  Low: { short: '₺', label: 'Düşük', hint: 'Bir kahve parası' },
  Medium: { short: '₺₺', label: 'Orta', hint: 'Bilet, ders ücreti' },
  High: { short: '₺₺₺', label: 'Yüksek', hint: 'Özel deneyimler' },
};

export const COST_ORDER: CostBand[] = ['Free', 'Low', 'Medium', 'High'];

export const RADIUS_LABELS: Record<DiscoveryRadius, { label: string; description: string; icon: IconName }> = {
  Chill: { label: 'Sakin', description: 'Çoğunlukla sevdiğin, tanıdık şeyler', icon: 'leaf' },
  Explore: { label: 'Dengeli', description: 'Tanıdık ile yeniyi dengeler', icon: 'compass' },
  SurpriseMe: { label: 'Şaşırt Beni', description: 'Daha fazla yenilik ve sürpriz', icon: 'sparkles' },
};

export const QUEST_TYPE_LABELS: Record<QuestType, string> = {
  Daily: 'Günlük',
  Weekly: 'Haftalık',
  Adventure: 'Macera',
  Epic: 'Destansı',
};

export const DIFFICULTY_LABELS: Record<Difficulty, string> = {
  Easy: 'Kolay',
  Medium: 'Orta',
  Hard: 'Zor',
  Heroic: 'Kahramanca',
};

export const STATUS_LABELS: Record<QuestStatus, string> = {
  Offered: 'Önerildi',
  Accepted: 'Devam ediyor',
  Completed: 'Tamamlandı',
  Skipped: 'Geçildi',
  Expired: 'Süresi doldu',
};

export const SKIP_REASONS: { value: SkipReason; label: string; hint: string }[] = [
  { value: 'NotInterested', label: 'İlgimi çekmedi', hint: 'Benzerlerini daha az göreceksin' },
  { value: 'TooExpensive', label: 'Pahalı', hint: 'Bütçene daha uygun öneriler gelir' },
  { value: 'NoTime', label: 'Zamanım yok', hint: 'Daha kısa görevler öne çıkar' },
  { value: 'TooFar', label: 'Uzak', hint: '' },
  { value: 'NotToday', label: 'Bugün uygun değil', hint: 'İlgi alanlarını etkilemez' },
  { value: 'Other', label: 'Başka bir sebep', hint: '' },
];

export interface ScoreComponentMeta {
  key: keyof Omit<ScoreBreakdown, 'total'>;
  label: string;
  penalty: boolean;
}

export const SCORE_COMPONENTS: ScoreComponentMeta[] = [
  { key: 'interest', label: 'İlgi uyumu', penalty: false },
  { key: 'novelty', label: 'Yenilik', penalty: false },
  { key: 'context', label: 'Bağlam (saat, süre)', penalty: false },
  { key: 'goalFit', label: 'Hedeflerine uyum', penalty: false },
  { key: 'diversity', label: 'Çeşitlilik', penalty: false },
  { key: 'feedbackFit', label: 'Geri bildirimlerin', penalty: false },
  { key: 'repetition', label: 'Tekrar', penalty: true },
  { key: 'friction', label: 'Engel (bütçe, süre)', penalty: true },
  { key: 'risk', label: 'Risk', penalty: true },
];

export const WEEKLY_TIME_OPTIONS = [
  { minutes: 120, label: '1–2 saat', short: '1–2 sa', hint: 'Küçük molalar' },
  { minutes: 300, label: '3–5 saat', short: '3–5 sa', hint: 'Birkaç akşam' },
  { minutes: 600, label: '6–10 saat', short: '6–10 sa', hint: 'Hafta sonları dahil' },
  { minutes: 900, label: '10+ saat', short: '10+ sa', hint: 'Bol vakit' },
];

export const EFFORT_LABELS: Record<PhysicalEffort, { label: string; hint: string }> = {
  None: { label: 'Yok', hint: 'Oturarak yapılabilir' },
  Light: { label: 'Hafif', hint: 'Kısa yürüyüş, hafif hareket' },
  Moderate: { label: 'Orta', hint: 'Uzun yürüyüş, ayakta etkinlik' },
  Vigorous: { label: 'Yoğun', hint: 'Koşu, bisiklet, dans' },
};

/** Profilde seçilebilen efor üst sınırı (erişilebilirlik / hareket kısıtı). */
export const EFFORT_LIMIT_OPTIONS: { value: PhysicalEffort; label: string; hint: string }[] = [
  { value: 'Light', label: 'Hafif', hint: 'Yalnızca hafif hareket içeren öneriler' },
  { value: 'Moderate', label: 'Orta', hint: 'Yoğun spor içermeyen öneriler' },
  { value: 'Vigorous', label: 'Fark etmez', hint: 'Her türlü hareket olabilir' },
];

export const NOTIFICATION_LABELS: Record<NotificationPreference, string> = {
  Off: 'Kapalı',
  WeeklySummary: 'Haftalık özet',
};
