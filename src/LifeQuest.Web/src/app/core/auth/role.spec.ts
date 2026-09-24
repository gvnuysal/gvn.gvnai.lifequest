import { roleFromToken } from './auth.store';

const token = (payload: object) =>
  ['e30', btoa(unescape(encodeURIComponent(JSON.stringify(payload)))).replace(/=+$/, '').replace(/\+/g, '-').replace(/\//g, '_'), 'sig'].join('.');

describe('roleFromToken', () => {
  it('reads the short and long role claim names', () => {
    expect(roleFromToken(token({ role: 'admin' }))).toBe('admin');
    expect(roleFromToken(token({ 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'user' }))).toBe('user');
  });

  it('handles non-ASCII payloads and invalid tokens safely', () => {
    expect(roleFromToken(token({ role: 'user', name: 'Güven Çağrı' }))).toBe('user');
    expect(roleFromToken('not-a-jwt')).toBeNull();
    expect(roleFromToken(undefined)).toBeNull();
  });
});
