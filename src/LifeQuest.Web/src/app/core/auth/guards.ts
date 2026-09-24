import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { ProfileStore } from '../state/profile.store';
import { AuthStore } from './auth.store';

export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthStore);
  return auth.isAuthenticated()
    ? true
    : inject(Router).createUrlTree(['/giris'], { queryParams: { returnUrl: state.url } });
};

export const guestGuard: CanActivateFn = () =>
  inject(AuthStore).isAuthenticated() ? inject(Router).createUrlTree(['/bugun']) : true;

/** Uygulama kabuğu: onboarding tamamlanmadan öneri alınamaz. */
export const onboardedGuard: CanActivateFn = async () => {
  const router = inject(Router);
  const profile = await inject(ProfileStore).ensureLoaded();
  return profile?.onboardingCompleted ? true : router.createUrlTree(['/onboarding']);
};

/** Onboarding ekranı: zaten tamamlandıysa ana ekrana. */
export const onboardingPendingGuard: CanActivateFn = async () => {
  const router = inject(Router);
  const profile = await inject(ProfileStore).ensureLoaded();
  return profile?.onboardingCompleted ? router.createUrlTree(['/bugun']) : true;
};

/** Yönetim ekranı. Asıl yetki kontrolü API'de ([Authorize(Roles = "admin")]). */
export const adminGuard: CanActivateFn = () =>
  inject(AuthStore).isAdmin() ? true : inject(Router).createUrlTree(['/bugun']);
