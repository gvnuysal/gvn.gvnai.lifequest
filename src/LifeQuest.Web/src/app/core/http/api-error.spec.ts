import { HttpErrorResponse } from '@angular/common/http';
import { fieldErrors, firstErrorMessage, hasErrorCode, parseApiErrors } from './api-error';

const response = (status: number, error: unknown) => new HttpErrorResponse({ status, error });

describe('parseApiErrors', () => {
  it('passes framework Result errors through', () => {
    const errors = [{ code: 'QUEST_EXPIRED', message: 'Süresi doldu.', type: 'Conflict' }];
    expect(parseApiErrors(response(409, errors))).toEqual(errors);
    expect(hasErrorCode(response(409, errors), 'QUEST_EXPIRED')).toBe(true);
  });

  it('maps problem+json from the exception middleware', () => {
    const errors = parseApiErrors(response(409, { title: 'Conflict', detail: 'Kayıt eşzamanlı değişti.', status: 409 }));
    expect(errors).toEqual([{ code: 'HTTP_409', message: 'Kayıt eşzamanlı değişti.', type: 'Conflict' }]);
  });

  it('maps ASP.NET model binding validation problems', () => {
    const errors = parseApiErrors(response(400, { errors: { Reason: ['Geçersiz değer.'] } }));
    expect(errors).toEqual([{ code: 'Reason', message: 'Geçersiz değer.', type: 'Validation' }]);
  });

  it('uses friendly messages for network, rate limit and server errors', () => {
    expect(firstErrorMessage(response(0, null))).toContain('Sunucuya ulaşılamıyor');
    expect(firstErrorMessage(response(429, null))).toContain('Çok fazla istek');
    expect(firstErrorMessage(response(500, { detail: 'stack trace' }))).not.toContain('stack trace');
  });
});

describe('fieldErrors', () => {
  it('maps PascalCase validation codes to camelCase form controls and skips business codes', () => {
    const result = fieldErrors(
      response(400, [
        { code: 'DisplayName', message: 'Çok kısa.', type: 'Validation' },
        { code: 'UNDERAGE', message: '18+', type: 'Validation' },
        { code: 'EMAIL_TAKEN', message: 'Kayıtlı.', type: 'Conflict' },
      ]),
    );
    expect(result).toEqual({ displayName: 'Çok kısa.' });
  });
});
