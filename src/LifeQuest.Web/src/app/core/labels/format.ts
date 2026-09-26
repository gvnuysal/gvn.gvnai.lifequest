import { t } from '../i18n/i18n';
import { currentLang, locale } from '../i18n/lang';

/** 45–90 dk / 45–90 min, 1–2 sa / 1–2 h gibi okunabilir süre aralığı (o anki dilde). */
export function formatDuration(minMinutes: number, maxMinutes: number): string {
  const { min, hour } = t().format;
  if (maxMinutes < 60) return minMinutes === maxMinutes ? `${maxMinutes} ${min}` : `${minMinutes}–${maxMinutes} ${min}`;

  const hours = (m: number) => {
    const h = m / 60;
    return Number.isInteger(h) ? `${h}` : h.toLocaleString(locale(), { maximumFractionDigits: 1 });
  };

  if (minMinutes < 60) return `${minMinutes} ${min} – ${hours(maxMinutes)} ${hour}`;
  return minMinutes === maxMinutes ? `${hours(maxMinutes)} ${hour}` : `${hours(minMinutes)}–${hours(maxMinutes)} ${hour}`;
}

/** "3 gün kaldı" / "3 days left", "süresi doldu" / "expired" */
export function formatRemaining(expiresAt: string, now: Date = new Date()): string {
  const f = t().format;
  const diffMs = new Date(expiresAt).getTime() - now.getTime();
  if (diffMs <= 0) return f.expired;

  const hours = diffMs / 3_600_000;
  if (hours < 1) return f.minutesLeft(Math.max(1, Math.round(diffMs / 60_000)));
  if (hours < 24) return f.hoursLeft(Math.round(hours));
  return f.daysLeft(Math.round(hours / 24));
}

export function formatDate(value: string | Date, options: Intl.DateTimeFormatOptions = { day: 'numeric', month: 'long' }): string {
  const date = typeof value === 'string' ? new Date(value.length === 10 ? `${value}T00:00:00` : value) : value;
  return new Intl.DateTimeFormat(locale(currentLang()), options).format(date);
}

export function greeting(now: Date = new Date()): string {
  const g = t().format.greetings;
  const hour = now.getHours();
  if (hour < 6) return g.night;
  if (hour < 12) return g.morning;
  if (hour < 18) return g.day;
  return g.evening;
}

export function browserTimeZone(): string | null {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone ?? null;
  } catch {
    return null;
  }
}
