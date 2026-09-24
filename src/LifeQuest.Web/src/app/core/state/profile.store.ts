import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ProfileApi } from '../api/api-clients';
import { Profile } from '../api/models';
import { AuthStore } from '../auth/auth.store';

/** Oturumdaki kullanıcının profili; kabuk, guard'lar ve ekranlar arasında paylaşılır. */
@Injectable({ providedIn: 'root' })
export class ProfileStore {
  private readonly api = inject(ProfileApi);
  private readonly auth = inject(AuthStore);
  private loading: Promise<Profile | null> | null = null;

  readonly profile = signal<Profile | null>(null);
  readonly firstName = computed(() => this.profile()?.displayName.split(' ')[0] ?? '');

  constructor() {
    // Farklı kullanıcıya geçişte veya çıkışta önbellek temizlenir.
    effect(() => {
      const userId = this.auth.userId();
      if (this.profile() && this.profile()!.userId !== userId) {
        this.profile.set(null);
        this.loading = null;
      }
    });
  }

  ensureLoaded(): Promise<Profile | null> {
    const current = this.profile();
    if (current && current.userId === this.auth.userId()) return Promise.resolve(current);
    return (this.loading ??= this.reload().finally(() => (this.loading = null)));
  }

  async reload(): Promise<Profile | null> {
    try {
      const profile = await firstValueFrom(this.api.get());
      this.profile.set(profile);
      return profile;
    } catch {
      return null;
    }
  }

  set(profile: Profile): void {
    this.profile.set(profile);
  }
}
