import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthSession } from '../api/models';
import { authInterceptor } from './auth.interceptor';
import { AuthStore } from './auth.store';

const tokens = (n: number): AuthSession => ({
  userId: 'user-1',
  accessToken: `access-${n}`,
  accessTokenExpiresAt: '2026-01-01T00:15:00Z',
  refreshTokenExpiresAt: '2026-02-01T00:00:00Z',
});

/** Node 25'in deneysel global localStorage'ı jsdom'unkini gölgeliyor; testte bellek içi depolama kullanılır. */
function memoryStorage(): Storage {
  const data = new Map<string, string>();
  return {
    get length() {
      return data.size;
    },
    clear: () => data.clear(),
    getItem: (key) => data.get(key) ?? null,
    key: (index) => [...data.keys()][index] ?? null,
    removeItem: (key) => void data.delete(key),
    setItem: (key, value) => void data.set(key, String(value)),
  };
}

describe('authInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let auth: AuthStore;

  beforeEach(() => {
    vi.stubGlobal('localStorage', memoryStorage());
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthStore);
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
  });

  afterEach(() => {
    backend.verify();
    vi.unstubAllGlobals();
  });

  async function signIn(): Promise<void> {
    const login = firstValueFrom(auth.login({ email: 'a@b.com', password: 'x' }));
    const request = backend.expectOne('/api/v1/auth/login');
    expect(request.request.withCredentials).toBe(true);
    request.flush(tokens(1));
    await login;
  }

  it('adds the bearer token to API requests only', async () => {
    await signIn();

    http.get('/api/v1/progress').subscribe();
    http.get('/assets/x.json').subscribe();

    expect(backend.expectOne('/api/v1/progress').request.headers.get('Authorization')).toBe('Bearer access-1');
    expect(backend.expectOne('/assets/x.json').request.headers.has('Authorization')).toBe(false);
  });

  it('never sends the bearer token to anonymous auth endpoints', async () => {
    await signIn();
    auth.refresh().subscribe();

    const refresh = backend.expectOne('/api/v1/auth/refresh');
    expect(refresh.request.headers.has('Authorization')).toBe(false);
    // Refresh token çerezde: gövde boş, çerez withCredentials ile gider.
    expect(refresh.request.body).toEqual({});
    expect(refresh.request.withCredentials).toBe(true);
    refresh.flush(tokens(2));
  });

  it('refreshes once on 401 and retries the original request with the new token', async () => {
    await signIn();
    const result = firstValueFrom(http.get<{ ok: boolean }>('/api/v1/progress'));

    backend.expectOne('/api/v1/progress').flush(null, { status: 401, statusText: 'Unauthorized' });
    backend.expectOne('/api/v1/auth/refresh').flush(tokens(2));

    const retry = backend.expectOne('/api/v1/progress');
    expect(retry.request.headers.get('Authorization')).toBe('Bearer access-2');
    retry.flush({ ok: true });

    expect(await result).toEqual({ ok: true });
    expect(localStorage.getItem('lq.session')).toBe('1');
  });

  it('shares a single refresh between concurrent 401 responses', async () => {
    await signIn();
    const a = firstValueFrom(http.get('/api/v1/progress'));
    const b = firstValueFrom(http.get('/api/v1/achievements'));

    backend.expectOne('/api/v1/progress').flush(null, { status: 401, statusText: 'Unauthorized' });
    backend.expectOne('/api/v1/achievements').flush(null, { status: 401, statusText: 'Unauthorized' });

    backend.expectOne('/api/v1/auth/refresh').flush(tokens(2));

    backend.expectOne('/api/v1/progress').flush({});
    backend.expectOne('/api/v1/achievements').flush([]);
    await Promise.all([a, b]);
  });

  it('ends the session when the refresh token is rejected', async () => {
    await signIn();
    const result = firstValueFrom(http.get('/api/v1/progress'));

    backend.expectOne('/api/v1/progress').flush(null, { status: 401, statusText: 'Unauthorized' });
    backend.expectOne('/api/v1/auth/refresh').flush([{ code: 'INVALID_REFRESH_TOKEN' }], { status: 401, statusText: 'Unauthorized' });

    await expect(result).rejects.toBeTruthy();
    expect(auth.isAuthenticated()).toBe(false);
    expect(localStorage.getItem('lq.session')).toBeNull();
  });

  it('signs out without refreshing when the account is suspended', async () => {
    await signIn();
    const result = firstValueFrom(http.get('/api/v1/progress')).catch((e: unknown) => e);

    backend.expectOne('/api/v1/progress').flush(
      [{ code: 'ACCOUNT_SUSPENDED', message: 'Hesabın askıya alındı.', type: 'Unauthorized' }],
      { status: 401, statusText: 'Unauthorized' },
    );

    await result;
    backend.expectNone('/api/v1/auth/refresh');
    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.hasSession()).toBe(false);
  });

  it('migrates a refresh token left in localStorage by the old version into the cookie', async () => {
    localStorage.setItem('lq.refresh', 'legacy-token');
    const restore = auth.restore();

    const refresh = backend.expectOne('/api/v1/auth/refresh');
    expect(refresh.request.body).toEqual({ refreshToken: 'legacy-token' });
    refresh.flush(tokens(2));
    await restore;

    expect(auth.isAuthenticated()).toBe(true);
    expect(localStorage.getItem('lq.refresh')).toBeNull();
    expect(localStorage.getItem('lq.session')).toBe('1');
  });

  it('skips the refresh call on start-up when this browser never signed in', async () => {
    await auth.restore();
    backend.expectNone('/api/v1/auth/refresh');
    expect(auth.isAuthenticated()).toBe(false);
  });

  it('refreshes a stale token after a role change so the new role takes effect', async () => {
    await signIn();
    const result = firstValueFrom(http.get<{ ok: boolean }>('/api/v1/admin/metrics'));

    backend.expectOne('/api/v1/admin/metrics').flush(
      [{ code: 'TOKEN_STALE', message: 'Yetkiler değişti.', type: 'Unauthorized' }],
      { status: 401, statusText: 'Unauthorized' },
    );
    backend.expectOne('/api/v1/auth/refresh').flush(tokens(2));
    const retry = backend.expectOne('/api/v1/admin/metrics');
    expect(retry.request.headers.get('Authorization')).toBe('Bearer access-2');
    retry.flush({ ok: true });

    expect(await result).toEqual({ ok: true });
  });
});
