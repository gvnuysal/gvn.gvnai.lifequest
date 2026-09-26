import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ProfileApi } from '../../core/api/api-clients';
import { firstErrorMessage } from '../../core/http/api-error';
import { ProfileStore } from '../../core/state/profile.store';
import { PushAvailability, PushService } from '../../core/state/push.service';
import { ToastService } from '../../core/state/toast.service';
import { Button } from '../../ui/button';
import { Dictionary, t } from '../../core/i18n/i18n';

const HOURS = Array.from({ length: 16 }, (_, i) => i + 7);

const UNAVAILABLE_HINTS: Record<Exclude<PushAvailability, 'ready'>, (d: Dictionary) => string> = {
  unsupported: (d) => d.reminder.unsupported,
  denied: (d) => d.reminder.denied,
  'server-disabled': (d) => d.reminder.serverDisabled,
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
      <h2 id="reminder-title" class="section-title">{{ t().reminder.title }}</h2>

      @if (activeHour() !== null) {
        <p>{{ t().reminder.active(label(activeHour()!)) }}</p>
      } @else {
        <p class="muted">{{ t().reminder.off }}</p>
      }

      <div class="row">
        <label class="visually-hidden" for="reminder-hour">{{ t().reminder.hour }}</label>
        <select id="reminder-hour" class="input hour" [ngModel]="hour()" (ngModelChange)="hour.set(+$event)">
          @for (h of hours; track h) { <option [ngValue]="h">{{ label(h) }}</option> }
        </select>
        @if (activeHour() === null) {
          <button lq-button [loading]="busy()" (click)="enable()">{{ t().reminder.enable }}</button>
        } @else {
          <button lq-button variant="secondary" [disabled]="hour() === activeHour()" [loading]="busy()" (click)="enable()">{{ t().reminder.saveTime }}</button>
        }
      </div>

      @if (hint()) { <p class="field__hint" role="status">{{ hint() }}</p> }

      @if (activeHour() !== null) {
        <div class="links">
          <button type="button" class="link" [disabled]="busy()" (click)="test()">{{ t().reminder.test }}</button>
          <button type="button" class="link" [disabled]="busy()" (click)="disable()">{{ t().reminder.disable }}</button>
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
  protected readonly t = t;
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
        this.hint.set(UNAVAILABLE_HINTS[availability](t()));
        return;
      }
      this.profiles.set(await firstValueFrom(this.profileApi.updatePreferences({ dailyReminderHour: this.hour() })));
      this.toast.success(t().reminder.enabled(this.label(this.hour())));
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
      this.toast.show(t().reminder.disabled);
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
        this.toast.success(t().reminder.testSent);
      },
      error: (err: unknown) => {
        this.busy.set(false);
        this.toast.error(firstErrorMessage(err));
      },
    });
  }
}
