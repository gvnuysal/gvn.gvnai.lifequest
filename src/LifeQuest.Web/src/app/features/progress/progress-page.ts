import { t } from '../../core/i18n/i18n';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { ProgressApi } from '../../core/api/api-clients';
import { Achievement, CategoryProgress, Progress } from '../../core/api/models';
import { firstErrorMessage } from '../../core/http/api-error';
import { formatDate } from '../../core/labels/format';
import { CATEGORIES } from '../../core/labels/labels';
import { CategoryIcon } from '../../ui/category-badge';
import { Icon } from '../../ui/icon';
import { LevelRing, ProgressBar } from '../../ui/progress';
import { EmptyState, Skeleton } from '../../ui/states';

@Component({
  selector: 'lq-progress-page',
  imports: [CategoryIcon, Icon, LevelRing, ProgressBar, EmptyState, Skeleton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <h1>{{ t().progress.title }}</h1>

      @if (error()) {
        <lq-empty-state icon="info" [title]="t().progress.loadFailed" [message]="error()" />
      } @else if (progress(); as p) {
        <section class="hero surface">
          <lq-level-ring [level]="p.lifeLevel" [progress]="p.levelProgress" [size]="132" />
          <div class="hero__text">
            <p class="eyebrow" lang="en">Life XP</p>
            <p class="xp">{{ p.lifeXp }} <span>XP</span></p>
            <p class="muted">{{ t().progress.toNext(p.nextLevelXp - p.lifeXp) }}</p>
            <p class="stat"><lq-icon name="check" [size]="16" /> {{ t().progress.experiences(p.totalCompleted) }}</p>
          </div>
        </section>

        <section class="stack">
          <h2 class="section-title">Life Profile</h2>
          <p class="muted small">{{ t().progress.profileLead }}</p>
          <ul class="categories">
            @for (c of p.categories; track c.category) {
              <li [style.--c]="color(c)">
                <lq-category-icon [category]="c.category" [size]="40" />
                <div class="cat">
                  <div class="cat__head">
                    <strong>{{ label(c) }}</strong>
                    <span>{{ t().progress.levelLine(c.level, c.xp, c.nextLevelXp) }}</span>
                  </div>
                  <lq-progress-bar [value]="c.nextLevelXp ? c.xp / c.nextLevelXp : 0" [color]="color(c)" [height]="8" [label]="t().progress.categoryProgress(label(c))" />
                </div>
              </li>
            }
          </ul>
        </section>

        <section class="stack">
          <h2 class="section-title">{{ t().progress.achievements }} <span class="muted small">{{ unlockedCount() }} / {{ achievements().length }}</span></h2>
          <ul class="achievements">
            @for (a of achievements(); track a.code) {
              <li [class.locked]="!a.unlocked" [attr.aria-label]="a.title + ', ' + (a.unlocked ? t().progress.unlocked : t().progress.locked)">
                <span class="medal"><lq-icon [name]="a.unlocked ? 'trophy' : 'lock'" [size]="22" /></span>
                <strong>{{ a.title }}</strong>
                <span>{{ a.description }}</span>
              </li>
            }
          </ul>
        </section>

        @if (p.recentXp.length) {
          <section class="stack">
            <h2 class="section-title">{{ t().progress.recent }}</h2>
            <ul class="feed">
              @for (entry of p.recentXp; track entry.at) {
                <li>
                  <span class="feed__xp">+{{ entry.lifeXp }}</span>
                  <span class="feed__text">{{ entry.description }}</span>
                  <span class="feed__date">{{ date(entry.at) }}</span>
                </li>
              }
            </ul>
          </section>
        }
      } @else {
        <lq-skeleton [height]="170" />
        <lq-skeleton [height]="320" />
      }
    </div>
  `,
  styles: `
    .small { font-size: var(--fs-sm); }
    .hero { display: flex; align-items: center; gap: var(--space-5); padding: var(--space-5); background: linear-gradient(140deg, var(--xp-soft), var(--surface) 70%); }
    .hero__text { display: flex; flex-direction: column; gap: 2px; }
    .xp { font-size: var(--fs-3xl); font-weight: 900; line-height: 1.1; color: var(--xp-ink); }
    .xp span { font-size: var(--fs-md); }
    .stat { display: inline-flex; align-items: center; gap: 6px; margin-top: 6px; font-weight: 800; color: var(--success); }
    .categories, .achievements, .feed { list-style: none; margin: 0; padding: 0; }
    .categories { display: flex; flex-direction: column; gap: 12px; }
    .categories li { display: flex; align-items: center; gap: 12px; }
    .cat { flex: 1; display: flex; flex-direction: column; gap: 6px; }
    .cat__head { display: flex; justify-content: space-between; gap: 8px; font-size: var(--fs-sm); }
    .cat__head span { color: var(--ink-3); font-weight: 700; }
    .achievements { display: grid; grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); gap: 10px; }
    .achievements li {
      display: flex; flex-direction: column; gap: 4px; padding: 14px; border-radius: var(--radius-lg);
      background: var(--surface); border: 1px solid var(--line); font-size: var(--fs-sm);
    }
    .achievements li span:last-child { color: var(--ink-3); font-size: var(--fs-xs); font-weight: 600; }
    .medal { display: grid; place-items: center; width: 44px; height: 44px; border-radius: 50%; background: var(--xp-soft); color: var(--xp-ink); margin-bottom: 4px; }
    .achievements li.locked { background: var(--surface-2); }
    .achievements li.locked .medal { background: var(--surface-3); color: var(--ink-3); }
    .achievements li.locked strong { color: var(--ink-2); }
    .feed { display: flex; flex-direction: column; gap: 6px; }
    .feed li { display: flex; align-items: center; gap: 10px; padding: 10px 12px; border-radius: var(--radius-md); background: var(--surface); border: 1px solid var(--line); font-size: var(--fs-sm); }
    .feed__xp { font-weight: 900; color: var(--xp-ink); min-width: 3.5ch; }
    .feed__text { flex: 1; font-weight: 700; }
    .feed__date { color: var(--ink-3); font-size: var(--fs-xs); font-weight: 700; }
  `,
})
export class ProgressPage {
  protected readonly t = t;
  protected readonly progress = signal<Progress | null>(null);
  protected readonly achievements = signal<Achievement[]>([]);
  protected readonly error = signal<string | null>(null);
  protected readonly unlockedCount = computed(() => this.achievements().filter((a) => a.unlocked).length);

  constructor() {
    const api = inject(ProgressApi);
    forkJoin({ progress: api.progress(), achievements: api.achievements() }).subscribe({
      next: ({ progress, achievements }) => {
        this.progress.set(progress);
        this.achievements.set(achievements);
      },
      error: (err: unknown) => this.error.set(firstErrorMessage(err)),
    });
  }

  protected label(c: CategoryProgress): string {
    return CATEGORIES[c.category].label;
  }

  protected color(c: CategoryProgress): string {
    return `var(--cat-${CATEGORIES[c.category].token})`;
  }

  protected date(value: string): string {
    return formatDate(value, { day: 'numeric', month: 'short' });
  }
}
