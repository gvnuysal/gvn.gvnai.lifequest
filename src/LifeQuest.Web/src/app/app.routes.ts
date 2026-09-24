import { Routes } from '@angular/router';
import { authGuard, guestGuard, onboardedGuard, onboardingPendingGuard } from './core/auth/guards';

export const routes: Routes = [
  {
    path: 'giris',
    canActivate: [guestGuard],
    title: 'Giriş · LifeQuest',
    loadComponent: () => import('./features/auth/login-page').then((m) => m.LoginPage),
  },
  {
    path: 'kayit',
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
  {
    path: '',
    canActivate: [authGuard, onboardedGuard],
    loadComponent: () => import('./features/shell/shell').then((m) => m.Shell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'bugun' },
      {
        path: 'bugun',
        title: 'Bugün · LifeQuest',
        loadComponent: () => import('./features/today/today-page').then((m) => m.TodayPage),
      },
      {
        path: 'oner',
        title: 'Boş vaktim var · LifeQuest',
        loadComponent: () => import('./features/suggest/suggest-page').then((m) => m.SuggestPage),
      },
      {
        path: 'quest/:id',
        title: 'Quest · LifeQuest',
        loadComponent: () => import('./features/quest/quest-page').then((m) => m.QuestPage),
      },
      {
        path: 'aktif',
        title: 'Görevlerim · LifeQuest',
        loadComponent: () => import('./features/active/active-page').then((m) => m.ActivePage),
      },
      {
        path: 'ilerleme',
        title: 'İlerleme · LifeQuest',
        loadComponent: () => import('./features/progress/progress-page').then((m) => m.ProgressPage),
      },
      {
        path: 'profil',
        title: 'Profil · LifeQuest',
        loadComponent: () => import('./features/profile/profile-page').then((m) => m.ProfilePage),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
