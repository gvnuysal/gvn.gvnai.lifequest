import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../state/toast.service';
import { firstErrorMessage } from './api-error';

/**
 * Yalnızca genel hatalar (bağlantı yok, 429, 5xx) için ortak bildirim gösterir.
 * İş kuralı hataları (400/404/409) ilgili ekranda, bağlamına uygun şekilde ele alınır.
 */
export const networkErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && (error.status === 0 || error.status === 429 || error.status >= 500)) {
        toast.error(firstErrorMessage(error));
      }
      return throwError(() => error);
    }),
  );
};
