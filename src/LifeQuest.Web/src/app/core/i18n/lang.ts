import { signal } from '@angular/core';

export type Lang = 'tr' | 'en';

export const LANGS: readonly Lang[] = ['tr', 'en'];

const STORAGE_KEY = 'lq.lang';

/** Tarayıcı dili Türkçe ise Türkçe, diğer her durumda İngilizce. */
export function detectLang(
  stored: string | null = readStored(),
  navigatorLanguages: readonly string[] = globalThis.navigator?.languages ?? [globalThis.navigator?.language ?? ''],
): Lang {
  if (stored === 'tr' || stored === 'en') return stored;
  const first = navigatorLanguages.find((l) => !!l) ?? '';
  return first.toLowerCase().startsWith('tr') ? 'tr' : 'en';
}

function readStored(): string | null {
  try {
    return localStorage.getItem(STORAGE_KEY);
  } catch {
    return null;
  }
}

const current = signal<Lang>(detectLang());

/**
 * Uygulamanın o anki dili. Bir signal olduğu için şablonda ya da computed içinde okunan her metin dil değişince
 * kendiliğinden yenilenir (sayfa yenilenmez).
 */
export const currentLang = current.asReadonly();

export function setLang(lang: Lang, remember = true): void {
  current.set(lang);
  if (remember) {
    try {
      localStorage.setItem(STORAGE_KEY, lang);
    } catch {
      // Depolama kapalıysa yalnızca bu oturum için.
    }
  }
  if (typeof document !== 'undefined') document.documentElement.lang = lang;
}

/** Intl biçimlendirme yereli. */
export function locale(lang: Lang = current()): string {
  return lang === 'en' ? 'en-GB' : 'tr-TR';
}
