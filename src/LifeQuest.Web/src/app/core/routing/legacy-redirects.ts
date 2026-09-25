import { inject } from '@angular/core';
import { Router, Routes } from '@angular/router';
import { APP_PATHS } from './app-paths';

/**
 * Eski Türkçe adresler (ör. /bugun, /yonetim/katalog) yeni İngilizce adreslere yönlenir: yer imleri, kurulu PWA
 * ve daha önce paylaşılan bağlantılar kırılmaz. Kullanım azalınca bu dosya ve app.routes.ts'teki satırı silinebilir.
 * Her yol tam eşleşir (pathMatch: 'full'); aksi halde /yonetim/katalog gibi alt yollar Türkçe kalırdı.
 */
const LEGACY_PATHS: [from: string, to: string][] = [
  ['giris', 'login'],
  ['kayit', 'register'],
  ['bugun', 'today'],
  ['oner', 'suggest'],
  ['aktif', 'quests'],
  ['quest/:id', 'quests/:id'],
  ['ilerleme', 'progress'],
  ['profil', 'profile'],
  ['kaydedilenler', 'saved'],
  ['fikir', 'ideas'],
  ['yonetim', 'admin'],
  ['yonetim/kullanicilar', 'admin/users'],
  ['yonetim/katalog', 'admin/catalog'],
  ['yonetim/katalog/:id', 'admin/catalog/:id'],
  ['yonetim/fikirler', 'admin/ideas'],
  ['yonetim/deneyler', 'admin/experiments'],
  ['yonetim/deneyler/:id', 'admin/experiments/:id'],
  ['yonetim/oneri-ayarlari', 'admin/recommendation-settings'],
  ['yonetim/denetim', 'admin/audit-log'],
];

export const LEGACY_REDIRECTS: Routes = [
  // Fikirden template oluşturma: eski ?fikir= parametresi ?idea= olur. ":id" rotasından önce gelmeli.
  {
    path: 'yonetim/katalog/yeni',
    pathMatch: 'full',
    redirectTo: ({ queryParams }) => {
      const { fikir, ...rest } = queryParams;
      return inject(Router).createUrlTree([APP_PATHS.admin.newTemplate], {
        queryParams: fikir ? { ...rest, idea: fikir } : rest,
      });
    },
  },
  ...LEGACY_PATHS.map(([from, to]) => ({ path: from, pathMatch: 'full' as const, redirectTo: to })),
];
