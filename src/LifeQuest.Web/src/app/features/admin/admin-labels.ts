import { AdminAction, DayPart, SafetyLevel, WeightGroup } from '../../core/api/models';

export const SAFETY_LABELS: Record<SafetyLevel, { label: string; tone: 'success' | 'warning' | 'danger' }> = {
  Safe: { label: 'Yayında', tone: 'success' },
  NeedsReview: { label: 'İnceleme bekliyor', tone: 'warning' },
  Blocked: { label: 'Engelli', tone: 'danger' },
};

export const DAY_PART_OPTIONS: { value: DayPart; label: string }[] = [
  { value: 'Morning', label: 'Sabah' },
  { value: 'Afternoon', label: 'Öğleden sonra' },
  { value: 'Evening', label: 'Akşam' },
  { value: 'Night', label: 'Gece' },
];

export const WEIGHT_GROUPS: { value: WeightGroup; label: string; hint: string }[] = [
  { value: 'Interest', label: 'İlgi uyumu', hint: 'Keşif moduna göre ilgi alanlarına verilen önem.' },
  { value: 'Novelty', label: 'Yenilik', hint: 'Daha önce denenmemiş alanlara verilen önem.' },
  { value: 'Score', label: 'Diğer skor bileşenleri', hint: 'Bağlam, hedef, çeşitlilik ve geri bildirim.' },
  { value: 'Penalty', label: 'Cezalar', hint: 'Tekrar, sürtünme, risk ve görmezden gelinen öneriler.' },
  { value: 'TasteGraph', label: 'Taste Graph', hint: "Komşu ilgilerin ve eşleşmeyen quest'lerin skoru." },
  { value: 'Exploration', label: 'Keşif slotu', hint: 'Günlük listedeki kontrollü keşif önerisi.' },
  { value: 'Windows', label: 'Pencereler (gün)', hint: 'Geçmişin ne kadar geriye bakılarak değerlendirildiği.' },
];

export const AUDIT_ACTION_LABELS: Record<AdminAction, string> = {
  UserSuspended: 'Hesap askıya alındı',
  UserUnsuspended: 'Askı kaldırıldı',
  UserDeleted: 'Hesap silindi',
  UserRoleChanged: 'Rol değişti',
  TemplateCreated: 'Template oluşturuldu',
  TemplateUpdated: 'Template düzenlendi',
  TemplateSafetyChanged: 'Güvenlik kararı',
  TemplateActivated: 'Template etkinleştirildi',
  TemplateDeactivated: 'Template pasifleştirildi',
  WeightsUpdated: 'Ağırlıklar güncellendi',
  WeightsReset: 'Ağırlıklar varsayılana döndü',
};

export const SUSPEND_OPTIONS: { value: number | null; label: string }[] = [
  { value: 1, label: '1 gün' },
  { value: 7, label: '7 gün' },
  { value: 30, label: '30 gün' },
  { value: null, label: 'Süresiz' },
];
