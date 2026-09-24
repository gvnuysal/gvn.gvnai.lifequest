const turkish = 'tr-TR';

/** 45–90 dk, 1–2 sa, 5–15 sa gibi okunabilir süre aralığı. */
export function formatDuration(minMinutes: number, maxMinutes: number): string {
  if (maxMinutes < 60) return minMinutes === maxMinutes ? `${maxMinutes} dk` : `${minMinutes}–${maxMinutes} dk`;

  const hours = (m: number) => {
    const h = m / 60;
    return Number.isInteger(h) ? `${h}` : h.toLocaleString(turkish, { maximumFractionDigits: 1 });
  };

  if (minMinutes < 60) return `${minMinutes} dk – ${hours(maxMinutes)} sa`;
  return minMinutes === maxMinutes ? `${hours(maxMinutes)} sa` : `${hours(minMinutes)}–${hours(maxMinutes)} sa`;
}

/** "3 gün kaldı", "5 saat kaldı", "süresi doldu" */
export function formatRemaining(expiresAt: string, now: Date = new Date()): string {
  const diffMs = new Date(expiresAt).getTime() - now.getTime();
  if (diffMs <= 0) return 'Süresi doldu';

  const hours = diffMs / 3_600_000;
  if (hours < 1) return `${Math.max(1, Math.round(diffMs / 60_000))} dakika kaldı`;
  if (hours < 24) return `${Math.round(hours)} saat kaldı`;
  return `${Math.round(hours / 24)} gün kaldı`;
}

export function formatDate(value: string | Date, options: Intl.DateTimeFormatOptions = { day: 'numeric', month: 'long' }): string {
  const date = typeof value === 'string' ? new Date(value.length === 10 ? `${value}T00:00:00` : value) : value;
  return new Intl.DateTimeFormat(turkish, options).format(date);
}

export function greeting(now: Date = new Date()): string {
  const hour = now.getHours();
  if (hour < 6) return 'İyi geceler';
  if (hour < 12) return 'Günaydın';
  if (hour < 18) return 'İyi günler';
  return 'İyi akşamlar';
}

export function browserTimeZone(): string | null {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone ?? null;
  } catch {
    return null;
  }
}
