import { Routes } from '@angular/router';
import { adminGuard, authGuard, guestGuard, onboardedGuard, onboardingPendingGuard } from './core/auth/guards';
import { LEGACY_REDIRECTS } from './core/routing/legacy-redirects';

/** Adresler İngilizce, başlıklar Türkçe. Bağlantılar core/routing/app-paths.ts sabitlerini kullanır. */

// Rota başlıkları sözlük anahtarıdır (core/i18n/sections/common.ts → titles); I18nTitleStrategy çevirir.
export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    title: 'login',
    loadComponent: () => import('./features/auth/login-page').then((m) => m.LoginPage),
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    title: 'register',
    loadComponent: () => import('./features/auth/register-page').then((m) => m.RegisterPage),
  },
  {
    path: 'onboarding',
    canActivate: [authGuard, onboardingPendingGuard],
    title: 'onboarding',
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
        title: 'today',
        loadComponent: () => import('./features/today/today-page').then((m) => m.TodayPage),
      },
      {
        path: 'party/:code',
        title: 'party',
        loadComponent: () => import('./features/party/party-page').then((m) => m.PartyPage),
      },
      {
        path: 'saved',
        title: 'saved',
        loadComponent: () => import('./features/saved/saved-page').then((m) => m.SavedPage),
      },
      {
        path: 'ideas',
        title: 'ideas',
        loadComponent: () => import('./features/ideas/ideas-page').then((m) => m.IdeasPage),
      },
      {
        path: 'suggest',
        title: 'suggest',
        loadComponent: () => import('./features/suggest/suggest-page').then((m) => m.SuggestPage),
      },
      {
        path: 'quests/:id',
        title: 'quest',
        loadComponent: () => import('./features/quest/quest-page').then((m) => m.QuestPage),
      },
      {
        path: 'quests',
        title: 'quests',
        loadComponent: () => import('./features/active/active-page').then((m) => m.ActivePage),
      },
      {
        path: 'progress',
        title: 'progress',
        loadComponent: () => import('./features/progress/progress-page').then((m) => m.ProgressPage),
      },
      {
        path: 'admin',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/admin/admin-shell').then((m) => m.AdminShell),
        children: [
          {
            path: '',
            title: 'adminMetrics',
            loadComponent: () => import('./features/admin/admin-page').then((m) => m.AdminPage),
          },
          {
            path: 'users',
            title: 'adminUsers',
            loadComponent: () => import('./features/admin/admin-users-page').then((m) => m.AdminUsersPage),
          },
          {
            path: 'catalog',
            title: 'adminCatalog',
            loadComponent: () => import('./features/admin/admin-catalog-page').then((m) => m.AdminCatalogPage),
          },
          {
            path: 'catalog/new',
            title: 'adminNewTemplate',
            loadComponent: () => import('./features/admin/admin-template-page').then((m) => m.AdminTemplatePage),
          },
          {
            path: 'catalog/:id',
            title: 'adminTemplate',
            loadComponent: () => import('./features/admin/admin-template-page').then((m) => m.AdminTemplatePage),
          },
          {
            path: 'ideas',
            title: 'adminIdeas',
            loadComponent: () => import('./features/admin/admin-ideas-page').then((m) => m.AdminIdeasPage),
          },
          {
            path: 'experiments',
            title: 'adminExperiments',
            loadComponent: () => import('./features/admin/admin-experiments-page').then((m) => m.AdminExperimentsPage),
          },
          {
            path: 'experiments/:id',
            title: 'adminExperiment',
            loadComponent: () => import('./features/admin/admin-experiment-page').then((m) => m.AdminExperimentPage),
          },
          {
            path: 'places',
            title: 'adminPlaces',
            loadComponent: () => import('./features/admin/admin-places-page').then((m) => m.AdminPlacesPage),
          },
          {
            path: 'recommendation-settings',
            title: 'adminWeights',
            loadComponent: () => import('./features/admin/admin-weights-page').then((m) => m.AdminWeightsPage),
          },
          {
            path: 'audit-log',
            title: 'adminAudit',
            loadComponent: () => import('./features/admin/admin-audit-page').then((m) => m.AdminAuditPage),
          },
        ],
      },
      {
        path: 'profile',
        title: 'profile',
        loadComponent: () => import('./features/profile/profile-page').then((m) => m.ProfilePage),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
