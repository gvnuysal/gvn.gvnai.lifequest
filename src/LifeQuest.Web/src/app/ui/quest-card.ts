import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Quest } from '../core/api/models';
import { formatDate, formatDuration, formatRemaining } from '../core/labels/format';
import { CATEGORIES, COST_LABELS, QUEST_TYPE_LABELS, STATUS_LABELS } from '../core/labels/labels';
import { CategoryIcon } from './category-badge';
import { Icon } from './icon';
import { questPath } from '../core/routing/app-paths';

@Component({
  selector: 'lq-quest-card',
  imports: [RouterLink, Icon, CategoryIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[style.--c]': 'color()', '[style.--c-ink]': 'inkColor()' },
  template: `
    <a class="card" [routerLink]="questPath(quest().id)" [attr.aria-label]="quest().title + ', ' + categoryLabel()">
      <div class="top">
        <lq-category-icon [category]="quest().category" [size]="46" />
        <div class="heading">
          <span class="kicker">
            {{ categoryLabel() }} · {{ typeLabel() }}
            @if (quest().isExploration) {
              <span class="explore"><lq-icon name="sparkles" [size]="12" [strokeWidth]="2.6" /> Keşif önerisi</span>
            }
          </span>
          <h3>{{ quest().title }}</h3>
        </div>
        <span class="xp" [attr.aria-label]="quest().reward.lifeXp + ' XP'">
          <lq-icon name="zap" [size]="14" [strokeWidth]="2.6" />+{{ quest().reward.lifeXp }}
        </span>
      </div>

      @if (showExplanation()) {
        <p class="why"><lq-icon name="sparkles" [size]="14" /> {{ quest().explanation }}</p>
      }

      <div class="meta">
        <span><lq-icon name="clock" [size]="15" /> {{ duration() }}</span>
        <span><lq-icon name="coins" [size]="15" /> {{ cost() }}</span>
        @if (quest().status === 'Accepted' && quest().plannedAt) {
          <span class="remaining"><lq-icon name="flag" [size]="15" /> {{ planned() }}</span>
        } @else if (quest().status === 'Accepted') {
          <span class="remaining"><lq-icon name="hourglass" [size]="15" /> {{ remaining() }}</span>
        } @else if (quest().status !== 'Offered') {
          <span class="status" [attr.data-status]="quest().status">{{ statusLabel() }}</span>
        }
        <lq-icon class="go" name="chevron-right" [size]="18" />
      </div>
    </a>
  `,
  styles: `
    .card {
      position: relative;
      display: flex;
      flex-direction: column;
      gap: 12px;
      padding: 16px;
      padding-left: 20px;
      border-radius: var(--radius-lg);
      background: var(--surface);
      border: 1px solid var(--line);
      box-shadow: var(--shadow-md);
      color: var(--ink);
      font-weight: 400;
      text-decoration: none !important;
      overflow: hidden;
      transition: transform 0.15s var(--ease), box-shadow 0.15s;
    }
    .card::before {
      content: '';
      position: absolute;
      inset: 0 auto 0 0;
      width: 6px;
      background: var(--c);
    }
    .card:hover { transform: translateY(-2px); box-shadow: var(--shadow-lg); }
    .card:active { transform: scale(0.99); }
    .top { display: flex; align-items: flex-start; gap: 12px; }
    .heading { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 2px; }
    .kicker {
      display: flex; flex-wrap: wrap; align-items: center; gap: 6px;
      font-size: var(--fs-xs); font-weight: 800; color: var(--c-ink); text-transform: uppercase; letter-spacing: 0.04em;
    }
    .explore {
      display: inline-flex; align-items: center; gap: 3px; padding: 1px 8px; border-radius: var(--radius-pill);
      background: var(--primary-soft); color: var(--primary-text); text-transform: none; letter-spacing: 0;
    }
    h3 { font-size: var(--fs-md); font-weight: 800; line-height: 1.3; }
    .xp {
      display: inline-flex; align-items: center; gap: 2px; padding: 4px 10px; border-radius: var(--radius-pill);
      background: var(--xp-soft); color: var(--xp-ink); font-weight: 900; font-size: var(--fs-sm); white-space: nowrap;
    }
    .why {
      display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden;
      font-size: var(--fs-sm); color: var(--ink-2); line-height: 1.45;
    }
    .why lq-icon { vertical-align: -2px; color: var(--brand); }
    .meta { display: flex; align-items: center; flex-wrap: wrap; gap: 6px 14px; font-size: var(--fs-sm); color: var(--ink-3); font-weight: 700; }
    .meta span { display: inline-flex; align-items: center; gap: 5px; }
    .remaining { color: var(--primary-text); }
    .status { padding: 2px 10px; border-radius: var(--radius-pill); background: var(--surface-2); }
    .status[data-status='Completed'] { background: var(--success-soft); color: var(--success); }
    .go { margin-left: auto; color: var(--ink-3); }
  `,
})
export class QuestCard {
  protected readonly questPath = questPath;
  protected readonly planned = computed(() => {
    const at = this.quest().plannedAt;
    return at ? formatDate(at, { weekday: 'short', day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' }) : '';
  });

  readonly quest = input.required<Quest>();
  readonly showExplanation = input(true);

  protected readonly categoryLabel = computed(() => CATEGORIES[this.quest().category].label);
  protected readonly typeLabel = computed(() => QUEST_TYPE_LABELS[this.quest().type]);
  protected readonly statusLabel = computed(() => STATUS_LABELS[this.quest().status]);
  protected readonly duration = computed(() => formatDuration(this.quest().minMinutes, this.quest().maxMinutes));
  protected readonly cost = computed(() => COST_LABELS[this.quest().cost].short);
  protected readonly remaining = computed(() => formatRemaining(this.quest().expiresAt));
  protected readonly color = computed(() => `var(--cat-${CATEGORIES[this.quest().category].token})`);
  protected readonly inkColor = computed(() => `var(--cat-${CATEGORIES[this.quest().category].token}-ink)`);
}
