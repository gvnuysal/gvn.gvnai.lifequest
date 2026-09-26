import { option, t } from '../../core/i18n/i18n';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { QuestsApi } from '../../core/api/api-clients';
import { CostBand, QuestList } from '../../core/api/models';
import { firstErrorMessage, hasErrorCode } from '../../core/http/api-error';
import { COST_LABELS, COST_ORDER } from '../../core/labels/labels';
import { ProfileStore } from '../../core/state/profile.store';
import { ToastService } from '../../core/state/toast.service';
import { Button } from '../../ui/button';
import { Icon } from '../../ui/icon';
import { QuestCard } from '../../ui/quest-card';
import { Segmented, SegmentOption } from '../../ui/segmented';
import { EmptyState, Skeleton } from '../../ui/states';
import { APP_PATHS } from '../../core/routing/app-paths';

@Component({
  selector: 'lq-suggest-page',
  imports: [RouterLink, Button, Icon, QuestCard, Segmented, EmptyState, Skeleton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <a class="back" [routerLink]="paths.today"><lq-icon name="arrow-left" [size]="18" /> {{ t().suggest.back }}</a>
      <header class="stack">
        <h1>{{ t().suggest.title }}</h1>
        <p class="muted">{{ t().suggest.lead }}</p>
      </header>

      <section class="surface form">
        <div class="field">
          <span class="field__label" id="time-label">{{ t().suggest.duration }}</span>
          <lq-segmented [ariaLabel]="t().suggest.duration" [options]="timeOptions" [(value)]="minutes" />
        </div>
        <div class="field">
          <span class="field__label">{{ t().suggest.budget }}</span>
          <lq-segmented [ariaLabel]="t().suggest.budget" [options]="costOptions" [(value)]="cost" />
        </div>
        <button lq-button [block]="true" [loading]="busy()" [disabled]="busy()" (click)="suggest()">
          <lq-icon name="sparkles" [size]="18" /> {{ t().suggest.submit }}
        </button>
      </section>

      @if (busy()) {
        <lq-skeleton [height]="150" />
        <lq-skeleton [height]="150" />
      } @else if (limitReached()) {
        <lq-empty-state icon="hourglass" [title]="t().suggest.limitTitle" [message]="t().suggest.limitMessage">
          <a lq-button variant="secondary" size="sm" [routerLink]="paths.today">{{ t().suggest.todays }}</a>
        </lq-empty-state>
      } @else if (result(); as list) {
        <section class="stack" aria-live="polite">
          <h2 class="section-title">{{ t().suggest.forYou }}</h2>
          @for (quest of list.quests; track quest.id) {
            <lq-quest-card [quest]="quest" />
          } @empty {
            <lq-empty-state icon="compass" [title]="t().suggest.noneTitle" [message]="list.message" />
          }
        </section>
      }
    </div>
  `,
  styles: `
    .back { display: inline-flex; align-items: center; gap: 6px; color: var(--ink-2); align-self: flex-start; }
    .form { display: flex; flex-direction: column; gap: var(--space-4); padding: var(--space-5); }
  `,
})
export class SuggestPage {
  protected readonly paths = APP_PATHS;
  private readonly quests = inject(QuestsApi);
  private readonly toast = inject(ToastService);

  protected readonly t = t;
  protected readonly timeOptions: SegmentOption<number>[] = ([30, 60, 120, 180] as const).map((m) => option(m, (d) => d.suggest.options[m]));
  protected readonly costOptions: SegmentOption<CostBand>[] = COST_ORDER.map((c) => option(c, () => COST_LABELS[c].label));

  protected readonly minutes = signal(120);
  protected readonly cost = signal<CostBand>(inject(ProfileStore).profile()?.budget ?? 'Low');
  protected readonly busy = signal(false);
  protected readonly limitReached = signal(false);
  protected readonly result = signal<QuestList | null>(null);

  protected suggest(): void {
    this.busy.set(true);
    this.limitReached.set(false);

    this.quests.suggest({ availableMinutes: this.minutes(), maxCost: this.cost() }).subscribe({
      next: (list) => {
        this.result.set(list);
        this.busy.set(false);
      },
      error: (err: unknown) => {
        const limit = hasErrorCode(err, 'SUGGESTION_LIMIT_REACHED');
        this.limitReached.set(limit);
        if (!limit) this.toast.error(firstErrorMessage(err));
        this.busy.set(false);
      },
    });
  }
}
