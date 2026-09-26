import { computed } from '@angular/core';
import { common, errors, format, nav, titles } from './sections/common';
import { labels } from './sections/labels';
import { ui } from './sections/ui';
import { auth } from './sections/auth';
import { onboarding } from './sections/onboarding';
import { today } from './sections/today';
import { quests, suggest } from './sections/quests';
import { quest } from './sections/quest';
import { celebration, party } from './sections/party';
import { ideas, progress, saved } from './sections/pages';
import { profile, reminder } from './sections/profile';
import { currentLang } from './lang';

/** Türkçe sözlük: tüm bölümler. Tipi İngilizce sözlüğün sözleşmesidir. */
export const TR = {
  common: common.tr,
  nav: nav.tr,
  titles: titles.tr,
  errors: errors.tr,
  format: format.tr,
  labels: labels.tr,
  ui: ui.tr,
  auth: auth.tr,
  onboarding: onboarding.tr,
  today: today.tr,
  suggest: suggest.tr,
  quests: quests.tr,
  quest: quest.tr,
  celebration: celebration.tr,
  party: party.tr,
  saved: saved.tr,
  progress: progress.tr,
  ideas: ideas.tr,
  profile: profile.tr,
  reminder: reminder.tr,
};

export type Dictionary = typeof TR;

export const EN: Dictionary = {
  common: common.en,
  nav: nav.en,
  titles: titles.en,
  errors: errors.en,
  format: format.en,
  labels: labels.en,
  ui: ui.en,
  auth: auth.en,
  onboarding: onboarding.en,
  today: today.en,
  suggest: suggest.en,
  quests: quests.en,
  quest: quest.en,
  celebration: celebration.en,
  party: party.en,
  saved: saved.en,
  progress: progress.en,
  ideas: ideas.en,
  profile: profile.en,
  reminder: reminder.en,
};

/**
 * O anki dilin sözlüğü. Şablonda <code>{{ t().today.title }}</code>: dil değişince okunan her metin yenilenir.
 * Bileşenlerde <code>protected readonly t = t;</code> olarak kullanılır.
 */
export const t = computed<Dictionary>(() => (currentLang() === 'en' ? EN : TR));

/**
 * Etiketi her okumada sözlükten gelen seçenek (segmented, chip listeleri): sabit dizilerde dil değişince etiket de
 * değişir. <code>option('active', (d) => d.quests.tabActive)</code>
 */
export function option<T>(value: T, read: (d: Dictionary) => string): { value: T; readonly label: string } {
  return { value, get label() { return read(t()); } };
}
