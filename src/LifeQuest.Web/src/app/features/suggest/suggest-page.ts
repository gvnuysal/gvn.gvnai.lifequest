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

@Component({
  selector: 'lq-suggest-page',
  imports: [RouterLink, Button, Icon, QuestCard, Segmented, EmptyState, Skeleton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <a class="back" routerLink="/bugun"><lq-icon name="arrow-left" [size]="18" /> Bugün</a>
      <header class="stack">
        <h1>Ne kadar vaktin var?</h1>
        <p class="muted">Bu ana uygun, gerçek hayatta yapabileceğin öneriler getirelim.</p>
      </header>

      <section class="surface form">
        <div class="field">
          <span class="field__label" id="time-label">Süre</span>
          <lq-segmented ariaLabel="Süre" [options]="timeOptions" [(value)]="minutes" />
        </div>
        <div class="field">
          <span class="field__label">Bütçe</span>
          <lq-segmented ariaLabel="Bütçe" [options]="costOptions" [(value)]="cost" />
        </div>
        <button lq-button [block]="true" [loading]="busy()" [disabled]="busy()" (click)="suggest()">
          <lq-icon name="sparkles" [size]="18" /> Öner
        </button>
      </section>

      @if (busy()) {
        <lq-skeleton [height]="150" />
        <lq-skeleton [height]="150" />
      } @else if (limitReached()) {
        <lq-empty-state icon="hourglass" title="Bugünlük bu kadar" message="Bugünkü bağlamsal öneri hakkını kullandın. Günün önerileri seni bekliyor.">
          <a lq-button variant="secondary" size="sm" routerLink="/bugun">Bugünün önerileri</a>
        </lq-empty-state>
      } @else if (result(); as list) {
        <section class="stack" aria-live="polite">
          <h2 class="section-title">Sana özel öneriler</h2>
          @for (quest of list.quests; track quest.id) {
            <lq-quest-card [quest]="quest" />
          } @empty {
            <lq-empty-state icon="compass" title="Uygun öneri bulamadık" [message]="list.message" />
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
  private readonly quests = inject(QuestsApi);
  private readonly toast = inject(ToastService);

  protected readonly timeOptions: SegmentOption<number>[] = [
    { value: 30, label: '30 dk' },
    { value: 60, label: '1 sa' },
    { value: 120, label: '2 sa' },
    { value: 180, label: '3 sa+' },
  ];
  protected readonly costOptions: SegmentOption<CostBand>[] = COST_ORDER.map((c) => ({ value: c, label: COST_LABELS[c].label }));

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
