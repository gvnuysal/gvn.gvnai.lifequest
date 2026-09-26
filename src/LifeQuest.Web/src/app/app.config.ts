import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  ApplicationConfig,
  inject,
  isDevMode,
  LOCALE_ID,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { provideServiceWorker } from '@angular/service-worker';
import { registerLocaleData } from '@angular/common';
import localeTr from '@angular/common/locales/tr';
import localeEnGb from '@angular/common/locales/en-GB';
import { TitleStrategy } from '@angular/router';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { AuthStore } from './core/auth/auth.store';
import { networkErrorInterceptor } from './core/http/network-error.interceptor';
import { ThemeService } from './core/state/theme.service';
import { acceptLanguageInterceptor } from './core/i18n/accept-language.interceptor';
import { I18nTitleStrategy } from './core/i18n/title-strategy';
import { currentLang, setLang } from './core/i18n/lang';

registerLocaleData(localeTr);
registerLocaleData(localeEnGb);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    // Angular pipe'ları kullanılmıyor; tarih/sayı biçimi core/labels/format.ts'te o anki dile göre (Intl).
    { provide: LOCALE_ID, useFactory: () => (currentLang() === 'en' ? 'en-GB' : 'tr') },
    { provide: TitleStrategy, useClass: I18nTitleStrategy },
    provideRouter(
      routes,
      withComponentInputBinding(),
      withInMemoryScrolling({ scrollPositionRestoration: 'top' }),
    ),
    provideHttpClient(withInterceptors([acceptLanguageInterceptor, authInterceptor, networkErrorInterceptor])),
    // Açılışta kayıtlı refresh token ile oturumu geri yükle; guard'lar doğru durumu görsün.
    provideAppInitializer(() => {
      setLang(currentLang(), false); // <html lang> ilk değer
      inject(ThemeService);
      return inject(AuthStore).restore();
    }),
    provideServiceWorker('ngsw-worker.js', {
      enabled: !isDevMode(),
      registrationStrategy: 'registerWhenStable:30000',
    }),
  ],
};
