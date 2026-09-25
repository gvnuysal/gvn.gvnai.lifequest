import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { API } from '../api/api-clients';
import { hasErrorCode } from '../http/api-error';
import { AuthStore } from './auth.store';

const ANONYMOUS_AUTH_ENDPOINTS = /\/auth\/(login|register|refresh)$/;

/**
 * API isteklerine Bearer token ekler. 401 alınırsa refresh token ile bir kez yenileyip isteği tekrarlar;
 * yenileme de başarısızsa oturumu kapatır. Rol değişikliğinde sunucu 401 TOKEN_STALE döner; aynı yenileme
 * akışı güncel rolle yeni token alır.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith(API)) return next(req);

  const auth = inject(AuthStore);
  const isAnonymousAuthCall = ANONYMOUS_AUTH_ENDPOINTS.test(req.url);

  const withToken = (request: HttpRequest<unknown>) => {
    const token = auth.accessToken();
    return token && !isAnonymousAuthCall
      ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : request;
  };

  return next(withToken(req)).pipe(
    catchError((error: unknown) => {
      const canRefresh =
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        !isAnonymousAuthCall &&
        auth.hasRefreshToken();

      if (!canRefresh) return throwError(() => error);

      // Askıya alınan hesabın oturumu yenilenemez; kullanıcı nedenini görerek çıkış yapar.
      if (hasErrorCode(error, 'ACCOUNT_SUSPENDED')) {
        auth.sessionExpired('suspended');
        return throwError(() => error);
      }

      return auth.refresh().pipe(
        catchError((refreshError: unknown) => {
          auth.sessionExpired();
          return throwError(() => refreshError);
        }),
        switchMap(() => next(withToken(req))),
      );
    }),
  );
};
