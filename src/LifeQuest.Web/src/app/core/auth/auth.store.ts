import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, finalize, firstValueFrom, map, shareReplay, tap } from 'rxjs';
import { AuthApi } from '../api/api-clients';
import { AuthTokens, LoginRequest, RegisterRequest } from '../api/models';
import { ToastService } from '../state/toast.service';

const REFRESH_KEY = 'lq.refresh';

/**
 * Oturum durumu. Access token yalnızca bellekte tutulur; refresh token sayfa yenilemelerinde oturumu
 * sürdürebilmek için localStorage'dadır (ödünleşim: ileride httpOnly çereze taşınmalı).
 * Backend refresh token'ı her kullanımda döndürür (rotation); eşzamanlı yenilemeler tek istekte birleştirilir.
 */
@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly api = inject(AuthApi);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  private readonly tokens = signal<AuthTokens | null>(null);
  private refreshInFlight: Observable<AuthTokens> | null = null;

  readonly accessToken = computed(() => this.tokens()?.accessToken ?? null);
  readonly isAuthenticated = computed(() => this.tokens() !== null);
  readonly userId = computed(() => this.tokens()?.userId ?? null);
  readonly role = computed(() => roleFromToken(this.tokens()?.accessToken));
  readonly isAdmin = computed(() => this.role() === 'admin');

  /** Uygulama açılışında: kayıtlı refresh token varsa oturumu sessizce yeniler. */
  async restore(): Promise<void> {
    if (!this.storedRefreshToken()) return;
    try {
      await firstValueFrom(this.refresh());
    } catch {
      this.clear();
    }
  }

  login(request: LoginRequest): Observable<void> {
    return this.api.login(request).pipe(
      tap((tokens) => this.set(tokens)),
      map(() => undefined),
    );
  }

  register(request: RegisterRequest): Observable<void> {
    return this.api.register(request).pipe(
      tap((tokens) => this.set(tokens)),
      map(() => undefined),
    );
  }

  hasRefreshToken(): boolean {
    return this.storedRefreshToken() !== null;
  }

  /** Tek uçuşlu yenileme: aynı anda gelen 401'ler aynı isteği bekler. */
  refresh(): Observable<AuthTokens> {
    const refreshToken = this.storedRefreshToken();
    if (!refreshToken) throw new Error('Refresh token yok.');

    this.refreshInFlight ??= this.api.refresh(refreshToken).pipe(
      tap((tokens) => this.set(tokens)),
      finalize(() => (this.refreshInFlight = null)),
      shareReplay({ bufferSize: 1, refCount: false }),
    );

    return this.refreshInFlight;
  }

  async logout(): Promise<void> {
    const refreshToken = this.storedRefreshToken();
    if (refreshToken && this.isAuthenticated()) {
      try {
        await firstValueFrom(this.api.logout(refreshToken));
      } catch {
        // Sunucuya ulaşılamasa da yerel oturum kapatılır.
      }
    }
    this.clear();
    await this.router.navigate(['/giris']);
  }

  /** Yenileme başarısız olduğunda interceptor tarafından çağrılır. */
  sessionExpired(): void {
    if (!this.isAuthenticated() && !this.hasRefreshToken()) return;
    this.clear();
    this.toast.show('Oturumun sona erdi. Lütfen tekrar giriş yap.');
    void this.router.navigate(['/giris'], { queryParams: { returnUrl: this.router.url } });
  }

  clear(): void {
    this.tokens.set(null);
    try {
      localStorage.removeItem(REFRESH_KEY);
    } catch {
      // yok say
    }
  }

  private set(tokens: AuthTokens): void {
    this.tokens.set(tokens);
    try {
      localStorage.setItem(REFRESH_KEY, tokens.refreshToken);
    } catch {
      // Depolama kapalıysa oturum yalnızca bu sekme için sürer.
    }
  }

  private storedRefreshToken(): string | null {
    try {
      return localStorage.getItem(REFRESH_KEY);
    } catch {
      return null;
    }
  }
}

const ROLE_CLAIMS = ['role', 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

/** Yalnızca arayüz kararları içindir (menü gösterimi); yetki kontrolü her zaman API'dedir. */
export function roleFromToken(token: string | undefined): string | null {
  if (!token) return null;
  try {
    const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
    const bytes = Uint8Array.from(atob(padded), (c) => c.charCodeAt(0));
    const json = JSON.parse(new TextDecoder().decode(bytes)) as Record<string, unknown>;
    const claim = ROLE_CLAIMS.map((key) => json[key]).find((value) => value !== undefined);
    return Array.isArray(claim) ? String(claim[0]) : claim ? String(claim) : null;
  } catch {
    return null;
  }
}
