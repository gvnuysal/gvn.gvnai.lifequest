import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { ProfileStore } from '../state/profile.store';
import { AuthStore } from './auth.store';
import { APP_PATHS, HOME_PATH } from '../routing/app-paths';

export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthStore);
  return auth.isAuthenticated()
    ? true
    : inject(Router).createUrlTree([APP_PATHS.login], { queryParams: { returnUrl: state.url } });
};

export const guestGuard: CanActivateFn = () =>
  inject(AuthStore).isAuthenticated() ? inject(Router).createUrlTree([HOME_PATH]) : true;

/** Uygulama kabuğu: onboarding tamamlanmadan öneri alınamaz. */
export const onboardedGuard: CanActivateFn = async () => {
  const router = inject(Router);
  const profile = await inject(ProfileStore).ensureLoaded();
  return profile?.onboardingCompleted ? true : router.createUrlTree([APP_PATHS.onboarding]);
};

/** Onboarding ekranı: zaten tamamlandıysa ana ekrana. */
export const onboardingPendingGuard: CanActivateFn = async () => {
  const router = inject(Router);
  const profile = await inject(ProfileStore).ensureLoaded();
  return profile?.onboardingCompleted ? router.createUrlTree([HOME_PATH]) : true;
};

/** Yönetim ekranı. Asıl yetki kontrolü API'de ([Authorize(Roles = "admin")]). */
export const adminGuard: CanActivateFn = () =>
  inject(AuthStore).isAdmin() ? true : inject(Router).createUrlTree([HOME_PATH]);
