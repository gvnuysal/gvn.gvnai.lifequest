import { formatDuration, formatRemaining, greeting } from './format';

describe('formatDuration', () => {
  it.each([
    [10, 20, '10–20 dk'],
    [45, 45, '45 dk'],
    [45, 90, '45 dk – 1,5 sa'],
    [60, 120, '1–2 sa'],
    [90, 150, '1,5–2,5 sa'],
  ])('%i–%i → %s', (min, max, expected) => {
    expect(formatDuration(min, max)).toBe(expected);
  });
});

describe('formatRemaining', () => {
  const now = new Date('2026-09-24T12:00:00Z');

  it('describes remaining time without pressure words', () => {
    expect(formatRemaining('2026-09-24T12:30:00Z', now)).toBe('30 dakika kaldı');
    expect(formatRemaining('2026-09-24T17:00:00Z', now)).toBe('5 saat kaldı');
    expect(formatRemaining('2026-09-27T12:00:00Z', now)).toBe('3 gün kaldı');
    expect(formatRemaining('2026-09-24T11:00:00Z', now)).toBe('Süresi doldu');
  });
});

describe('greeting', () => {
  it('greets by local time of day', () => {
    expect(greeting(new Date(2026, 8, 24, 8))).toBe('Günaydın');
    expect(greeting(new Date(2026, 8, 24, 14))).toBe('İyi günler');
    expect(greeting(new Date(2026, 8, 24, 20))).toBe('İyi akşamlar');
  });
});
