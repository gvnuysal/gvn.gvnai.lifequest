import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Route, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../../app.routes';
import { APP_PATHS } from './app-paths';
import { LEGACY_REDIRECTS } from './legacy-redirects';

@Component({ template: '' })
class Blank {}

/** Rota ağacını "/today", "/admin/catalog/:id" gibi tam yollara açar (yönlendirme ve joker hariç). */
function flatten(list: Route[], prefix = ''): string[] {
  return list.flatMap((route) => {
    if (route.redirectTo !== undefined || route.path === '**') return [];
    const full = [prefix, route.path].filter(Boolean).join('/');
    return [`/${full}`, ...flatten(route.children ?? [], full)];
  });
}

function values(obj: object): string[] {
  return Object.values(obj).flatMap((v) => (typeof v === 'string' ? [v] : values(v)));
}

describe('app routing', () => {
  it('every APP_PATHS entry points to a real route', () => {
    const known = new Set(flatten(routes));
    for (const path of values(APP_PATHS)) expect(known, path).toContain(path);
  });

  it('route paths are English (no Turkish segments left)', () => {
    const turkish = new Set(['bugun', 'giris', 'kayit', 'oner', 'aktif', 'ilerleme', 'profil', 'yonetim', 'katalog',
      'yeni', 'fikir', 'fikirler', 'deneyler', 'denetim', 'kaydedilenler', 'kullanicilar', 'oneri-ayarlari']);
    for (const path of flatten(routes)) {
      for (const segment of path.split('/')) expect(turkish.has(segment), path).toBe(false);
    }
  });

  describe('legacy Turkish URLs', () => {
    let harness: RouterTestingHarness;

    beforeEach(async () => {
      // Guard'sız, yalnızca yönlendirme davranışını sınayan yapı: gerçek yönlendirmeler + her hedef için boş sayfa.
      TestBed.configureTestingModule({
        providers: [
          provideRouter([
            ...LEGACY_REDIRECTS,
            ...flatten(routes).map((path) => ({ path: path.slice(1), component: Blank })),
          ]),
        ],
      });
      harness = await RouterTestingHarness.create();
    });

    const cases: [string, string][] = [
      ['/bugun', '/today'],
      ['/giris?returnUrl=%2Fbugun', '/login?returnUrl=%2Fbugun'],
      ['/quest/42', '/quests/42'],
      ['/aktif', '/quests'],
      ['/kaydedilenler', '/saved'],
      ['/yonetim', '/admin'],
      ['/yonetim/katalog', '/admin/catalog'],
      ['/yonetim/katalog/abc', '/admin/catalog/abc'],
      ['/yonetim/deneyler/abc', '/admin/experiments/abc'],
      ['/yonetim/oneri-ayarlari', '/admin/recommendation-settings'],
      ['/yonetim/denetim', '/admin/audit-log'],
      ['/yonetim/katalog/yeni?fikir=xyz', '/admin/catalog/new?idea=xyz'],
      ['/yonetim/katalog/yeni', '/admin/catalog/new'],
    ];

    for (const [from, to] of cases) {
      it(`${from} → ${to}`, async () => {
        await harness.navigateByUrl(from);
        expect(TestBed.inject(Router).url).toBe(to);
      });
    }
  });
});
