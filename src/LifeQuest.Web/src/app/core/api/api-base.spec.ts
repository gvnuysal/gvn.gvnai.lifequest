import { API, apiBaseUrl } from './api-clients';

describe('apiBaseUrl', () => {
  it('is same-origin when no runtime config is present', () => {
    expect(apiBaseUrl(undefined)).toBe('');
    expect(API).toBe('/api/v1');
  });

  it('uses the configured API origin without a trailing slash', () => {
    expect(apiBaseUrl({ apiBaseUrl: 'https://lifequesttestapi.gvnaitech.com/' })).toBe('https://lifequesttestapi.gvnaitech.com');
    expect(apiBaseUrl({ apiBaseUrl: '  ' })).toBe('');
  });
});
