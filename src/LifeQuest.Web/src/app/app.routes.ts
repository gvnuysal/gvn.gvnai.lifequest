import { Routes } from '@angular/router';
import { adminGuard, authGuard, guestGuard, onboardedGuard, onboardingPendingGuard } from './core/auth/guards';
import { LEGACY_REDIRECTS } from './core/routing/legacy-redirects';

/** Adresler İngilizce, başlıklar Türkçe. Bağlantılar core/routing/app-paths.ts sabitlerini kullanır. */

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    title: 'Giriş · LifeQuest',
    loadComponent: () => import('./features/auth/login-page').then((m) => m.LoginPage),
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    title: 'Kayıt ol · LifeQuest',
    loadComponent: () => import('./features/auth/register-page').then((m) => m.RegisterPage),
  },
  {
    path: 'onboarding',
    canActivate: [authGuard, onboardingPendingGuard],
    title: 'Hoş geldin · LifeQuest',
    loadComponent: () => import('./features/onboarding/onboarding-page').then((m) => m.OnboardingPage),
  },
  // Eski Türkçe adresler (/bugun, /yonetim/...) yeni adreslere yönlenir; kabuğun guard'larından önce eşleşmeli.
  ...LEGACY_REDIRECTS,
  {
    path: '',
    canActivate: [authGuard, onboardedGuard],
    loadComponent: () => import('./features/shell/shell').then((m) => m.Shell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'today' },
      {
        path: 'today',
        title: 'Bugün · LifeQuest',
        loadComponent: () => import('./features/today/today-page').then((m) => m.TodayPage),
      },
      {
        path: 'saved',
        title: 'Sonra yaparım · LifeQuest',
        loadComponent: () => import('./features/saved/saved-page').then((m) => m.SavedPage),
      },
      {
        path: 'ideas',
        title: 'Bir deneyim öner · LifeQuest',
        loadComponent: () => import('./features/ideas/ideas-page').then((m) => m.IdeasPage),
      },
      {
        path: 'suggest',
        title: 'Boş vaktim var · LifeQuest',
        loadComponent: () => import('./features/suggest/suggest-page').then((m) => m.SuggestPage),
      },
      {
        path: 'quests/:id',
        title: 'Quest · LifeQuest',
        loadComponent: () => import('./features/quest/quest-page').then((m) => m.QuestPage),
      },
      {
        path: 'quests',
        title: 'Görevlerim · LifeQuest',
        loadComponent: () => import('./features/active/active-page').then((m) => m.ActivePage),
      },
      {
        path: 'progress',
        title: 'İlerleme · LifeQuest',
        loadComponent: () => import('./features/progress/progress-page').then((m) => m.ProgressPage),
      },
      {
        path: 'admin',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/admin/admin-shell').then((m) => m.AdminShell),
        children: [
          {
            path: '',
            title: 'Ürün metrikleri · LifeQuest',
            loadComponent: () => import('./features/admin/admin-page').then((m) => m.AdminPage),
          },
          {
            path: 'users',
            title: 'Kullanıcılar · Yönetim',
            loadComponent: () => import('./features/admin/admin-users-page').then((m) => m.AdminUsersPage),
          },
          {
            path: 'catalog',
            title: 'Katalog · Yönetim',
            loadComponent: () => import('./features/admin/admin-catalog-page').then((m) => m.AdminCatalogPage),
          },
          {
            path: 'catalog/new',
            title: 'Yeni template · Yönetim',
            loadComponent: () => import('./features/admin/admin-template-page').then((m) => m.AdminTemplatePage),
          },
          {
            path: 'catalog/:id',
            title: 'Template · Yönetim',
            loadComponent: () => import('./features/admin/admin-template-page').then((m) => m.AdminTemplatePage),
          },
          {
            path: 'ideas',
            title: 'Topluluk fikirleri · Yönetim',
            loadComponent: () => import('./features/admin/admin-ideas-page').then((m) => m.AdminIdeasPage),
          },
          {
            path: 'experiments',
            title: 'Deneyler · Yönetim',
            loadComponent: () => import('./features/admin/admin-experiments-page').then((m) => m.AdminExperimentsPage),
          },
          {
            path: 'experiments/:id',
            title: 'Deney · Yönetim',
            loadComponent: () => import('./features/admin/admin-experiment-page').then((m) => m.AdminExperimentPage),
          },
          {
            path: 'places',
            title: 'Mekânlar ve etkinlikler · Yönetim',
            loadComponent: () => import('./features/admin/admin-places-page').then((m) => m.AdminPlacesPage),
          },
          {
            path: 'recommendation-settings',
            title: 'Öneri ayarları · Yönetim',
            loadComponent: () => import('./features/admin/admin-weights-page').then((m) => m.AdminWeightsPage),
          },
          {
            path: 'audit-log',
            title: 'Denetim kaydı · Yönetim',
            loadComponent: () => import('./features/admin/admin-audit-page').then((m) => m.AdminAuditPage),
          },
        ],
      },
      {
        path: 'profile',
        title: 'Profil · LifeQuest',
        loadComponent: () => import('./features/profile/profile-page').then((m) => m.ProfilePage),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
