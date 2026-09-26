import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, finalize, firstValueFrom, map, shareReplay, tap } from 'rxjs';
import { AuthApi } from '../api/api-clients';
import { AuthSession, LoginRequest, RegisterRequest } from '../api/models';
import { ToastService } from '../state/toast.service';
import { APP_PATHS } from '../routing/app-paths';
import { t } from '../i18n/i18n';

/** Eski sürümün refresh token'ı sakladığı anahtar; açılışta bir kez çereze taşınıp silinir. */
const LEGACY_REFRESH_KEY = 'lq.refresh';
/** Gizli olmayan ipucu: bu tarayıcıda açık oturum (çerez) var mı. Anonim ziyarette boşuna /refresh çağrılmaz. */
const SESSION_HINT_KEY = 'lq.session';

/**
 * Oturum durumu. Access token yalnızca bellekte tutulur; refresh token HttpOnly çerezdedir ve JavaScript'ten
 * okunamaz. Sayfa yenilendiğinde oturum çerezle sessizce sürdürülür. Backend her yenilemede yeni çerez yazar
 * (rotation); eşzamanlı yenilemeler tek istekte birleştirilir.
 */
@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly api = inject(AuthApi);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  private readonly session = signal<AuthSession | null>(null);
  private refreshInFlight: Observable<AuthSession> | null = null;

  readonly accessToken = computed(() => this.session()?.accessToken ?? null);
  readonly isAuthenticated = computed(() => this.session() !== null);
  readonly userId = computed(() => this.session()?.userId ?? null);
  readonly role = computed(() => roleFromToken(this.session()?.accessToken));
  readonly isAdmin = computed(() => this.role() === 'admin');

  /** Uygulama açılışında: bu tarayıcıda oturum açılmışsa çerezle sessizce yeniler. */
  async restore(): Promise<void> {
    if (!this.hasSession()) return;
    try {
      await firstValueFrom(this.refresh());
    } catch {
      this.clear();
    }
  }

  login(request: LoginRequest): Observable<void> {
    return this.api.login(request).pipe(
      tap((session) => this.set(session)),
      map(() => undefined),
    );
  }

  register(request: RegisterRequest): Observable<void> {
    return this.api.register(request).pipe(
      tap((session) => this.set(session)),
      map(() => undefined),
    );
  }

  /** Yenilemeye değer bir oturum var mı (çerezin kendisi JavaScript'ten görünmez). */
  hasSession(): boolean {
    return read(SESSION_HINT_KEY) !== null || read(LEGACY_REFRESH_KEY) !== null;
  }

  /** Tek uçuşlu yenileme: aynı anda gelen 401'ler aynı isteği bekler. */
  refresh(): Observable<AuthSession> {
    this.refreshInFlight ??= this.api.refresh(read(LEGACY_REFRESH_KEY) ?? undefined).pipe(
      tap((session) => this.set(session)),
      finalize(() => (this.refreshInFlight = null)),
      shareReplay({ bufferSize: 1, refCount: false }),
    );

    return this.refreshInFlight;
  }

  async logout(): Promise<void> {
    if (this.isAuthenticated()) {
      try {
        await firstValueFrom(this.api.logout());
      } catch {
        // Sunucuya ulaşılamasa da yerel oturum kapatılır.
      }
    }
    this.clear();
    await this.router.navigate([APP_PATHS.login]);
  }

  /** Yenileme başarısız olduğunda veya hesap askıya alındığında interceptor tarafından çağrılır. */
  sessionExpired(reason: 'expired' | 'suspended' = 'expired'): void {
    if (!this.isAuthenticated() && !this.hasSession()) return;
    this.clear();
    if (reason === 'suspended') this.toast.error(t().errors.suspended);
    else this.toast.show(t().errors.sessionExpired);
    void this.router.navigate([APP_PATHS.login], { queryParams: { returnUrl: this.router.url } });
  }

  clear(): void {
    this.session.set(null);
    remove(SESSION_HINT_KEY);
    remove(LEGACY_REFRESH_KEY);
  }

  private set(session: AuthSession): void {
    this.session.set(session);
    write(SESSION_HINT_KEY, '1');
    // Token artık çerezde; eski sürümden kalan kopya silinir.
    remove(LEGACY_REFRESH_KEY);
  }
}

function read(key: string): string | null {
  try {
    return localStorage.getItem(key);
  } catch {
    return null;
  }
}

function write(key: string, value: string): void {
  try {
    localStorage.setItem(key, value);
  } catch {
    // Depolama kapalıysa oturum yalnızca bu sekme için sürer.
  }
}

function remove(key: string): void {
  try {
    localStorage.removeItem(key);
  } catch {
    // yok say
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
