import { Injectable, inject, signal } from '@angular/core';
import { SwPush } from '@angular/service-worker';
import { firstValueFrom, take } from 'rxjs';
import { PushApi } from '../api/api-clients';

export type PushAvailability = 'ready' | 'unsupported' | 'denied' | 'server-disabled';

/**
 * Web Push aboneliği. Bildirimleri Angular service worker gösterir ve tıklanınca ilgili sayfayı açar. Service worker
 * yalnızca üretim derlemesinde çalıştığından geliştirme sunucusunda push "desteklenmiyor" görünür.
 */
@Injectable({ providedIn: 'root' })
export class PushService {
  private readonly swPush = inject(SwPush);
  private readonly api = inject(PushApi);

  /** Bu tarayıcı bu oturumda abone mi (sunucudaki cihaz kaydından bağımsız). */
  readonly subscribed = signal(false);

  constructor() {
    if (this.swPush.isEnabled) this.swPush.subscription.subscribe((s) => this.subscribed.set(s !== null));
  }

  availability(): PushAvailability {
    if (!this.swPush.isEnabled || typeof Notification === 'undefined') return 'unsupported';
    return Notification.permission === 'denied' ? 'denied' : 'ready';
  }

  /** İzin ister (kullanıcı hareketi içinde çağrılmalı), aboneliği sunucuya kaydeder. */
  async enable(): Promise<PushAvailability> {
    const availability = this.availability();
    if (availability !== 'ready') return availability;

    const settings = await firstValueFrom(this.api.settings());
    if (!settings.enabled || !settings.publicKey) return 'server-disabled';

    try {
      const subscription = await this.swPush.requestSubscription({ serverPublicKey: settings.publicKey });
      const json = subscription.toJSON();
      await firstValueFrom(this.api.subscribe({
        endpoint: subscription.endpoint,
        keys: { p256dh: json.keys?.['p256dh'] ?? '', auth: json.keys?.['auth'] ?? '' },
      }));
      return 'ready';
    } catch (error) {
      if (typeof Notification !== 'undefined' && Notification.permission === 'denied') return 'denied';
      throw error;
    }
  }

  /** Bu cihazın aboneliğini hem tarayıcıda hem sunucuda kaldırır. */
  async disable(): Promise<void> {
    if (!this.swPush.isEnabled) return;
    const current = await firstValueFrom(this.swPush.subscription.pipe(take(1)));
    if (!current) return;
    try {
      await firstValueFrom(this.api.unsubscribe(current.endpoint));
    } finally {
      await this.swPush.unsubscribe();
    }
  }

  sendTest() {
    return this.api.test();
  }
}
