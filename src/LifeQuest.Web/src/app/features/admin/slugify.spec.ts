import { slugify } from './admin-template-page';

describe('slugify', () => {
  it('turns a Turkish title into a valid template code', () => {
    expect(slugify('Mahalle Kütüphanesi Turu')).toBe('topluluk-mahalle-kutuphanesi-turu');
    expect(slugify('Şehrin İlk Çay Bahçesi!')).toBe('topluluk-sehrin-ilk-cay-bahcesi');
  });

  it('always matches the server-side code rule', () => {
    for (const title of ['A', '???', 'Çok uzun bir başlık '.repeat(10)]) {
      expect(slugify(title)).toMatch(/^[a-z0-9-]{3,64}$/);
    }
  });
});
