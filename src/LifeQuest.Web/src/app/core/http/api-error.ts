import { HttpErrorResponse } from '@angular/common/http';
import { ApiError } from '../api/models';

/**
 * Backend üç farklı hata gövdesi döndürebilir:
 *  1. Framework Result hataları: `ApiError[]` ({ code, message, type })
 *  2. Framework exception middleware'i: problem+json ({ title, detail, status })
 *  3. ASP.NET model binding: ValidationProblemDetails ({ errors: { alan: [mesaj] } })
 * Hepsi tek bir `ApiError[]` biçimine indirgenir.
 */
export function parseApiErrors(error: unknown): ApiError[] {
  if (!(error instanceof HttpErrorResponse)) {
    return [{ code: 'UNKNOWN', message: 'Beklenmeyen bir hata oluştu.', type: 'Failure' }];
  }

  if (error.status === 0) {
    return [{ code: 'NETWORK', message: 'Sunucuya ulaşılamıyor. Bağlantını kontrol et.', type: 'Failure' }];
  }

  if (error.status === 429) {
    return [{ code: 'RATE_LIMITED', message: 'Çok fazla istek gönderildi. Biraz sonra tekrar dene.', type: 'Failure' }];
  }

  const body = error.error;

  if (Array.isArray(body) && body.every(isApiError)) {
    return body;
  }

  if (body && typeof body === 'object') {
    const errors = (body as { errors?: Record<string, string[]> }).errors;
    if (errors && typeof errors === 'object') {
      return Object.entries(errors).flatMap(([field, messages]) =>
        messages.map((message) => ({ code: field, message, type: 'Validation' as const })),
      );
    }

    const detail = (body as { detail?: string }).detail;
    if (typeof detail === 'string' && error.status < 500) {
      return [{ code: `HTTP_${error.status}`, message: detail, type: typeForStatus(error.status) }];
    }
  }

  if (error.status >= 500) {
    return [{ code: 'SERVER', message: 'Sunucuda bir sorun oluştu. Lütfen tekrar dene.', type: 'Failure' }];
  }

  return [{ code: `HTTP_${error.status}`, message: 'İstek tamamlanamadı.', type: typeForStatus(error.status) }];
}

/** Kullanıcıya gösterilecek tek satırlık mesaj. */
export function firstErrorMessage(error: unknown): string {
  return parseApiErrors(error)[0].message;
}

export function hasErrorCode(error: unknown, code: string): boolean {
  return parseApiErrors(error).some((e) => e.code === code);
}

/**
 * Doğrulama hatalarını form kontrol adlarına göre gruplar. Backend alan adları PascalCase
 * (ör. "DisplayName"), form kontrolleri camelCase'dir.
 */
export function fieldErrors(error: unknown): Record<string, string> {
  const result: Record<string, string> = {};
  for (const e of parseApiErrors(error)) {
    if (e.type !== 'Validation' || !/^[A-Za-z][A-Za-z0-9.]*$/.test(e.code) || e.code.toUpperCase() === e.code) continue;
    const key = e.code.charAt(0).toLowerCase() + e.code.slice(1);
    result[key] ??= e.message;
  }
  return result;
}

function isApiError(value: unknown): value is ApiError {
  return !!value && typeof value === 'object' && 'code' in value && 'message' in value;
}

function typeForStatus(status: number): ApiError['type'] {
  switch (status) {
    case 400:
      return 'Validation';
    case 401:
      return 'Unauthorized';
    case 404:
      return 'NotFound';
    case 409:
      return 'Conflict';
    default:
      return 'Failure';
  }
}
