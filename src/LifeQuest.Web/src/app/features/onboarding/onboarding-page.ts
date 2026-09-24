import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { catchError, of } from 'rxjs';
import { CatalogApi, ProfileApi } from '../../core/api/api-clients';
import { CostBand, DiscoveryRadius, LifeCategory } from '../../core/api/models';
import { firstErrorMessage } from '../../core/http/api-error';
import { browserTimeZone } from '../../core/labels/format';
import {
  CATEGORIES,
  CATEGORY_DESCRIPTIONS,
  CATEGORY_ORDER,
  COST_LABELS,
  COST_ORDER,
  RADIUS_LABELS,
  WEEKLY_TIME_OPTIONS,
} from '../../core/labels/labels';
import { ProfileStore } from '../../core/state/profile.store';
import { ToastService } from '../../core/state/toast.service';
import { Button } from '../../ui/button';
import { InterestPicker } from '../../ui/interest-picker';
import { OptionCard } from '../../ui/option-card';
import { Skeleton } from '../../ui/states';

const STEPS = ['Hedefler', 'İlgi alanları', 'Zaman ve bütçe', 'Keşif modu'] as const;

@Component({
  selector: 'lq-onboarding-page',
  imports: [FormsModule, Button, InterestPicker, OptionCard, Skeleton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page page--bare">
      <header class="head">
        <div class="dots" role="progressbar" [attr.aria-valuenow]="step() + 1" aria-valuemin="1" [attr.aria-valuemax]="steps.length" [attr.aria-label]="'Adım ' + (step() + 1) + ' / ' + steps.length">
          @for (s of steps; track s; let i = $index) {
            <span [class.on]="i <= step()"></span>
          }
        </div>
        <p class="eyebrow">Adım {{ step() + 1 }} / {{ steps.length }} · {{ steps[step()] }}</p>
      </header>

      @switch (step()) {
        @case (0) {
          <section class="stack">
            <h1>Merhaba{{ name() ? ', ' + name() : '' }}! Hangi alanlarda ilerlemek istersin?</h1>
            <p class="muted">Life Profile'ın bir kişilik testi değil; yalnızca yaşadığın deneyimleri yansıtır. İstediğin kadar seç, sonra değiştirebilirsin.</p>
            <div class="stack">
              @for (category of categories; track category) {
                <button lq-option
                  [heading]="categoryMeta[category].label"
                  [description]="categoryDescriptions[category]"
                  [icon]="categoryMeta[category].icon"
                  [selected]="goals().includes(category)"
                  (click)="toggleGoal(category)"></button>
              }
            </div>
          </section>
        }
        @case (1) {
          <section class="stack">
            <h1>Neler ilgini çeker?</h1>
            <p class="muted">Öneriler buradan başlar ve geri bildirimlerinle zamanla gelişir. En az bir alan seç.</p>
            @if (interests(); as list) {
              <lq-interest-picker [interests]="list" [(selection)]="interestSelection" />
            } @else {
              <lq-skeleton [height]="320" />
            }
          </section>
        }
        @case (2) {
          <section class="stack">
            <h1>Haftada ne kadar vaktin var?</h1>
            <div class="grid">
              @for (option of timeOptions; track option.minutes) {
                <button lq-option [heading]="option.label" [description]="option.hint"
                  [selected]="weeklyMinutes() === option.minutes" (click)="weeklyMinutes.set(option.minutes)"></button>
              }
            </div>
            <h2 class="sub">Bütçe yaklaşımın?</h2>
            <div class="grid">
              @for (cost of costs; track cost) {
                <button lq-option [heading]="costLabels[cost].label" [description]="costLabels[cost].hint"
                  [selected]="budget() === cost" (click)="budget.set(cost)"></button>
              }
            </div>
          </section>
        }
        @case (3) {
          <section class="stack">
            <h1>Ne kadar keşif istersin?</h1>
            <p class="muted">Yeniliğin dozunu sen belirlersin; istediğin zaman değiştirebilirsin.</p>
            @for (radius of radii; track radius) {
              <button lq-option [heading]="radiusLabels[radius].label" [description]="radiusLabels[radius].description"
                [icon]="radiusLabels[radius].icon" [selected]="discoveryRadius() === radius" (click)="discoveryRadius.set(radius)"></button>
            }
            <div class="field">
              <label for="city">Şehir <span class="muted">(isteğe bağlı)</span></label>
              <input id="city" class="input" [ngModel]="city()" (ngModelChange)="city.set($event)" placeholder="Ör. İstanbul" autocomplete="address-level2" maxlength="80" />
              <span class="field__hint">Yalnızca şehir gerektiren quest'ler için kullanılır. Konumunu asla takip etmeyiz.</span>
            </div>
          </section>
        }
      }

      <footer class="actions">
        @if (step() > 0) {
          <button lq-button variant="ghost" type="button" (click)="back()">Geri</button>
        }
        <button lq-button type="button" [block]="true" [disabled]="!canContinue() || busy()" [loading]="busy()" (click)="next()">
          {{ step() === steps.length - 1 ? 'Maceraya başla' : 'Devam' }}
        </button>
      </footer>
    </div>
  `,
  styles: `
    .head { display: flex; flex-direction: column; gap: 10px; }
    .dots { display: flex; gap: 6px; }
    .dots span { flex: 1; height: 6px; border-radius: 99px; background: var(--line); transition: background 0.3s; }
    .dots span.on { background: var(--brand); }
    .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; }
    .sub { font-size: var(--fs-lg); margin-top: var(--space-3); }
    .actions {
      position: sticky;
      bottom: 0;
      display: flex;
      gap: 10px;
      padding: var(--space-4) 0 calc(var(--space-4) + env(safe-area-inset-bottom));
      background: linear-gradient(to top, var(--bg) 70%, transparent);
    }
  `,
})
export class OnboardingPage {
  private readonly profileApi = inject(ProfileApi);
  private readonly profiles = inject(ProfileStore);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  protected readonly steps = STEPS;
  protected readonly categories = CATEGORY_ORDER;
  protected readonly categoryMeta = CATEGORIES;
  protected readonly categoryDescriptions = CATEGORY_DESCRIPTIONS;
  protected readonly timeOptions = WEEKLY_TIME_OPTIONS;
  protected readonly costs = COST_ORDER;
  protected readonly costLabels = COST_LABELS;
  protected readonly radii: DiscoveryRadius[] = ['Chill', 'Explore', 'SurpriseMe'];
  protected readonly radiusLabels = RADIUS_LABELS;

  protected readonly interests = toSignal(inject(CatalogApi).interests().pipe(catchError(() => of([]))));
  protected readonly name = this.profiles.firstName;

  protected readonly step = signal(0);
  protected readonly busy = signal(false);
  protected readonly goals = signal<LifeCategory[]>([]);
  protected readonly interestSelection = signal<Record<string, number>>({});
  protected readonly weeklyMinutes = signal(300);
  protected readonly budget = signal<CostBand>('Low');
  protected readonly discoveryRadius = signal<DiscoveryRadius>('Explore');
  protected readonly city = signal('');

  protected readonly canContinue = computed(() => {
    switch (this.step()) {
      case 0:
        return this.goals().length > 0;
      case 1:
        return Object.keys(this.interestSelection()).length > 0;
      default:
        return true;
    }
  });

  constructor() {
    void this.profiles.ensureLoaded();
  }

  protected toggleGoal(category: LifeCategory): void {
    this.goals.update((goals) => (goals.includes(category) ? goals.filter((g) => g !== category) : [...goals, category]));
  }

  protected back(): void {
    this.step.update((s) => Math.max(0, s - 1));
  }

  protected next(): void {
    if (this.step() < STEPS.length - 1) {
      this.step.update((s) => s + 1);
      window.scrollTo({ top: 0 });
      return;
    }
    this.finish();
  }

  private finish(): void {
    this.busy.set(true);
    const city = this.city().trim();

    this.profileApi
      .completeOnboarding({
        goals: this.goals(),
        interests: Object.entries(this.interestSelection()).map(([code, weight]) => ({ code, weight })),
        weeklyAvailableMinutes: this.weeklyMinutes(),
        budget: this.budget(),
        discoveryRadius: this.discoveryRadius(),
        city: city.length > 0 ? city : null,
        timeZoneId: browserTimeZone(),
      })
      .subscribe({
        next: (profile) => {
          this.profiles.set(profile);
          void this.router.navigate(['/bugun']);
        },
        error: (err: unknown) => {
          this.toast.error(firstErrorMessage(err));
          this.busy.set(false);
        },
      });
  }
}
