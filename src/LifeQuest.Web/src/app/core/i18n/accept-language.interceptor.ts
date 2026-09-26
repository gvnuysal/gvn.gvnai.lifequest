import { HttpInterceptorFn } from '@angular/common/http';
import { API } from '../api/api-clients';
import { currentLang } from './lang';

/** API hata ve içerik metinlerini arayüz dilinde istemek için her API isteğine Accept-Language ekler. */
export const acceptLanguageInterceptor: HttpInterceptorFn = (req, next) =>
  req.url.startsWith(API) ? next(req.clone({ setHeaders: { 'Accept-Language': currentLang() } })) : next(req);
