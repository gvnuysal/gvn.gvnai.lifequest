import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ProfileApi } from '../../core/api/api-clients';
import { firstErrorMessage } from '../../core/http/api-error';
import { ProfileStore } from '../../core/state/profile.store';
import { PushAvailability, PushService } from '../../core/state/push.service';
import { ToastService } from '../../core/state/toast.service';
import { Button } from '../../ui/button';

const HOURS = Array.from({ length: 16 }, (_, i) => i + 7);

const UNAVAILABLE_HINTS: Record<Exclude<PushAvailability, 'ready'>, string> = {
  unsupported: 'Bu tarayıcıda anlık bildirim yok. iPhone ve iPad\'de önce LifeQuest\'i Paylaş → Ana Ekrana Ekle ile yükle.',
  denied: 'Bildirim izni kapalı. Tarayıcının site ayarlarından LifeQuest için bildirimlere izin verip tekrar dene.',
  'server-disabled': 'Anlık bildirimler bu sunucuda henüz etkin değil.',
};

/**
 * Günlük hatırlatma: kullanıcı seçtiği saatte, günde bir push alır. Varsayılan kapalıdır; o gün görev tamamlandıysa
 * hatırlatma gitmez. İzin yalnızca kullanıcı "Aç" dediğinde istenir.
 */
@Component({
  selector: 'lq-reminder-card',
  imports: [FormsModule, Button],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="surface card stack" aria-labelledby="reminder-title">
      <h2 id="reminder-title" class="section-title">Günlük hatırlatma</h2>

      @if (activeHour() !== null) {
        <p>Her gün <strong>{{ label(activeHour()!) }}</strong>'de, o gün görev tamamlamadıysan kısa bir hatırlatma gelir.</p>
      } @else {
        <p class="muted">Kapalı. Açarsan seçtiğin saatte, günde en fazla bir kez hatırlatırız. O gün görev tamamladıysan rahatsız etmeyiz.</p>
      }

      <div class="row">
        <label class="visually-hidden" for="reminder-hour">Hatırlatma saati</label>
        <select id="reminder-hour" class="input hour" [ngModel]="hour()" (ngModelChange)="hour.set(+$event)">
          @for (h of hours; track h) { <option [ngValue]="h">{{ label(h) }}</option> }
        </select>
        @if (activeHour() === null) {
          <button lq-button [loading]="busy()" (click)="enable()">Hatırlatmayı aç</button>
        } @else {
          <button lq-button variant="secondary" [disabled]="hour() === activeHour()" [loading]="busy()" (click)="enable()">Saati kaydet</button>
        }
      </div>

      @if (hint()) { <p class="field__hint" role="status">{{ hint() }}</p> }

      @if (activeHour() !== null) {
        <div class="links">
          <button type="button" class="link" [disabled]="busy()" (click)="test()">Deneme bildirimi gönder</button>
          <button type="button" class="link" [disabled]="busy()" (click)="disable()">Hatırlatmayı kapat</button>
        </div>
      }
    </section>
  `,
  styles: `
    .row { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
    .hour { width: auto; min-width: 7rem; appearance: auto; }
    .links { display: flex; gap: 16px; flex-wrap: wrap; }
    .link { background: none; border: 0; padding: 0; color: var(--primary-text); font-weight: 800; cursor: pointer; font-size: var(--fs-sm); }
    .link:disabled { opacity: 0.5; cursor: default; }
  `,
})
export class ReminderCard {
  private readonly profiles = inject(ProfileStore);
  private readonly profileApi = inject(ProfileApi);
  private readonly push = inject(PushService);
  private readonly toast = inject(ToastService);

  protected readonly hours = HOURS;
  protected readonly activeHour = computed(() => this.profiles.profile()?.dailyReminderHour ?? null);
  protected readonly hour = signal(9);
  protected readonly busy = signal(false);
  protected readonly hint = signal<string | null>(null);

  constructor() {
    effect(() => {
      const active = this.activeHour();
      if (active !== null) untracked(() => this.hour.set(active));
    });
  }

  protected label(hour: number): string {
    return `${String(hour).padStart(2, '0')}:00`;
  }

  protected async enable(): Promise<void> {
    this.busy.set(true);
    this.hint.set(null);
    try {
      const availability = await this.push.enable();
      if (availability !== 'ready') {
        this.hint.set(UNAVAILABLE_HINTS[availability]);
        return;
      }
      this.profiles.set(await firstValueFrom(this.profileApi.updatePreferences({ dailyReminderHour: this.hour() })));
      this.toast.success(`Hatırlatma her gün ${this.label(this.hour())}'de.`);
    } catch (err: unknown) {
      this.toast.error(firstErrorMessage(err));
    } finally {
      this.busy.set(false);
    }
  }

  protected async disable(): Promise<void> {
    this.busy.set(true);
    try {
      this.profiles.set(await firstValueFrom(this.profileApi.updatePreferences({ clearDailyReminder: true })));
      this.toast.show('Günlük hatırlatma kapatıldı.');
    } catch (err: unknown) {
      this.toast.error(firstErrorMessage(err));
    } finally {
      this.busy.set(false);
    }
  }

  protected test(): void {
    this.busy.set(true);
    this.push.sendTest().subscribe({
      next: () => {
        this.busy.set(false);
        this.toast.success('Deneme bildirimi gönderildi.');
      },
      error: (err: unknown) => {
        this.busy.set(false);
        this.toast.error(firstErrorMessage(err));
      },
    });
  }
}
