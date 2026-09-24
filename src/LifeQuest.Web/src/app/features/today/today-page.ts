import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ProgressApi, QuestsApi } from '../../core/api/api-clients';
import { Progress, Quest, QuestList } from '../../core/api/models';
import { firstErrorMessage } from '../../core/http/api-error';
import { formatDate, greeting } from '../../core/labels/format';
import { ProfileStore } from '../../core/state/profile.store';
import { Button } from '../../ui/button';
import { Icon } from '../../ui/icon';
import { ProgressBar } from '../../ui/progress';
import { QuestCard } from '../../ui/quest-card';
import { EmptyState, Skeleton } from '../../ui/states';

@Component({
  selector: 'lq-today-page',
  imports: [RouterLink, Button, Icon, ProgressBar, QuestCard, EmptyState, Skeleton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <header class="hello">
        <div>
          <p class="eyebrow">{{ dateLabel() }}</p>
          <h1>{{ greeting }}{{ name() ? ', ' + name() : '' }}</h1>
        </div>
        @if (progress(); as p) {
          <a class="level" routerLink="/ilerleme" [attr.aria-label]="'Seviye ' + p.lifeLevel + ', ilerlemeyi gör'">
            <span class="level__badge"><lq-icon name="star" [size]="14" [strokeWidth]="2.6" /> Sv. {{ p.lifeLevel }}</span>
            <lq-progress-bar [value]="p.levelProgress" [height]="6" label="Sonraki seviyeye ilerleme" />
            <span class="level__xp">{{ p.lifeXp }} / {{ p.nextLevelXp }} XP</span>
          </a>
        }
      </header>

      @if (activeCount() > 0) {
        <a class="active-strip" routerLink="/aktif">
          <lq-icon name="flag" [size]="18" />
          <span><strong>{{ activeCount() }}</strong> görevin devam ediyor</span>
          <lq-icon name="chevron-right" [size]="18" />
        </a>
      }

      <section class="stack">
        <h2 class="section-title">Bugünün önerileri</h2>
        @if (loading()) {
          <lq-skeleton [height]="150" />
          <lq-skeleton [height]="150" />
          <lq-skeleton [height]="150" />
        } @else if (error()) {
          <lq-empty-state icon="info" title="Öneriler yüklenemedi" [message]="error()">
            <button lq-button variant="secondary" size="sm" (click)="load()"><lq-icon name="refresh" [size]="16" /> Tekrar dene</button>
          </lq-empty-state>
        } @else if (today()?.quests?.length) {
          @for (quest of today()!.quests; track quest.id) {
            <lq-quest-card [quest]="quest" />
          }
        } @else {
          <lq-empty-state icon="compass" title="Bugün için öneri yok" [message]="today()?.message ?? null">
            <a lq-button variant="secondary" size="sm" routerLink="/profil">Tercihlerimi düzenle</a>
          </lq-empty-state>
        }
      </section>

      <a class="cta" routerLink="/oner">
        <span class="cta__icon"><lq-icon name="clock" [size]="24" /></span>
        <span class="cta__text">
          <strong>Boş vaktin mi var?</strong>
          <span>Ne kadar zamanın olduğunu söyle, ona göre önerelim.</span>
        </span>
        <lq-icon name="chevron-right" />
      </a>
    </div>
  `,
  styles: `
    .hello { display: flex; align-items: flex-end; justify-content: space-between; gap: 16px; }
    .level {
      display: flex; flex-direction: column; gap: 4px; width: 118px; padding: 8px 10px;
      border-radius: var(--radius-md); background: var(--surface); border: 1px solid var(--line);
      color: var(--ink); text-decoration: none !important; box-shadow: var(--shadow-sm);
    }
    .level__badge { display: inline-flex; align-items: center; gap: 4px; font-weight: 900; font-size: var(--fs-sm); color: var(--xp-ink); }
    .level__xp { font-size: 11px; font-weight: 700; color: var(--ink-3); }
    .active-strip {
      display: flex; align-items: center; gap: 10px; padding: 12px 14px; border-radius: var(--radius-md);
      background: var(--primary-soft); color: var(--primary-text); font-weight: 700; text-decoration: none !important;
    }
    .active-strip span { flex: 1; }
    .cta {
      display: flex; align-items: center; gap: 14px; padding: 16px; border-radius: var(--radius-lg);
      background: linear-gradient(135deg, var(--ink) 0%, color-mix(in srgb, var(--ink) 80%, var(--brand)) 100%);
      color: var(--bg); text-decoration: none !important; box-shadow: var(--shadow-lg);
    }
    .cta__icon { display: grid; place-items: center; width: 48px; height: 48px; border-radius: 14px; background: color-mix(in srgb, var(--bg) 16%, transparent); color: var(--xp); }
    .cta__text { flex: 1; display: flex; flex-direction: column; gap: 2px; font-weight: 600; }
    .cta__text span { font-size: var(--fs-sm); opacity: 0.85; }
  `,
})
export class TodayPage {
  private readonly quests = inject(QuestsApi);
  private readonly progressApi = inject(ProgressApi);

  protected readonly greeting = greeting();
  protected readonly name = inject(ProfileStore).firstName;

  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly today = signal<QuestList | null>(null);
  protected readonly active = signal<Quest[]>([]);
  protected readonly progress = signal<Progress | null>(null);

  protected readonly activeCount = computed(() => this.active().length);
  protected readonly dateLabel = computed(() =>
    formatDate(this.today()?.date ?? new Date(), { weekday: 'long', day: 'numeric', month: 'long' }),
  );

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);

    forkJoin({ today: this.quests.today(), active: this.quests.active(), progress: this.progressApi.progress() }).subscribe({
      next: ({ today, active, progress }) => {
        this.today.set(today);
        this.active.set(active);
        this.progress.set(progress);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.error.set(firstErrorMessage(err));
        this.loading.set(false);
      },
    });
  }
}
