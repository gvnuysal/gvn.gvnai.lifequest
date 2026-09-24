import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { QuestsApi } from '../../core/api/api-clients';
import { Quest, QuestStatus } from '../../core/api/models';
import { firstErrorMessage } from '../../core/http/api-error';
import { formatDate } from '../../core/labels/format';
import { CATEGORIES, STATUS_LABELS } from '../../core/labels/labels';
import { Button } from '../../ui/button';
import { CategoryIcon } from '../../ui/category-badge';
import { Chip } from '../../ui/chip';
import { QuestCard } from '../../ui/quest-card';
import { Segmented, SegmentOption } from '../../ui/segmented';
import { EmptyState, Skeleton } from '../../ui/states';

type Tab = 'active' | 'history';

@Component({
  selector: 'lq-active-page',
  imports: [RouterLink, Button, CategoryIcon, Chip, QuestCard, Segmented, EmptyState, Skeleton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <h1>Görevlerim</h1>
      <lq-segmented ariaLabel="Görev listesi" [options]="tabs" [value]="tab()" (valueChange)="switchTab($event)" />

      @if (tab() === 'active') {
        @if (loadingActive()) {
          <lq-skeleton [height]="140" />
          <lq-skeleton [height]="140" />
        } @else if (error()) {
          <lq-empty-state icon="info" title="Görevler yüklenemedi" [message]="error()" />
        } @else {
          @for (quest of active(); track quest.id) {
            <lq-quest-card [quest]="quest" [showExplanation]="false" />
          } @empty {
            <lq-empty-state icon="flag" title="Devam eden görevin yok" message="Bugünün önerilerinden birini kabul ederek başlayabilirsin.">
              <a lq-button variant="secondary" size="sm" routerLink="/bugun">Önerilere göz at</a>
            </lq-empty-state>
          }
        }
      } @else {
        <div class="filters" role="group" aria-label="Duruma göre filtrele">
          @for (filter of filters; track filter.label) {
            <button lq-chip [selected]="status() === filter.value" (click)="setStatus(filter.value)">{{ filter.label }}</button>
          }
        </div>

        <ul class="history">
          @for (quest of history(); track quest.id) {
            <li>
              <a [routerLink]="['/quest', quest.id]">
                <lq-category-icon [category]="quest.category" [size]="40" />
                <span class="history__text">
                  <strong>{{ quest.title }}</strong>
                  <span>{{ categoryLabel(quest) }} · {{ date(quest) }}</span>
                </span>
                <span class="pill" [attr.data-status]="quest.status">{{ statusLabel(quest.status) }}</span>
              </a>
            </li>
          }
        </ul>

        @if (loadingHistory()) {
          <lq-skeleton [height]="64" />
        } @else if (!history().length) {
          <lq-empty-state icon="list" title="Henüz kayıt yok" />
        } @else if (hasMore()) {
          <button lq-button variant="secondary" [block]="true" (click)="loadHistory(page() + 1)">Daha fazla yükle</button>
        }
      }
    </div>
  `,
  styles: `
    .filters { display: flex; gap: 8px; overflow-x: auto; scrollbar-width: none; margin: 0 calc(-1 * var(--space-4)); padding: 2px var(--space-4); }
    .history { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 8px; }
    .history a {
      display: flex; align-items: center; gap: 12px; padding: 10px 12px; border-radius: var(--radius-md);
      background: var(--surface); border: 1px solid var(--line); color: var(--ink); font-weight: 400; text-decoration: none !important;
    }
    .history__text { flex: 1; min-width: 0; display: flex; flex-direction: column; }
    .history__text strong { font-weight: 800; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .history__text span { font-size: var(--fs-xs); color: var(--ink-3); font-weight: 700; }
    .pill { padding: 3px 10px; border-radius: var(--radius-pill); font-size: var(--fs-xs); font-weight: 800; background: var(--surface-2); color: var(--ink-2); white-space: nowrap; }
    .pill[data-status='Completed'] { background: var(--success-soft); color: var(--success); }
    .pill[data-status='Accepted'] { background: var(--primary-soft); color: var(--primary-text); }
  `,
})
export class ActivePage {
  private readonly api = inject(QuestsApi);

  protected readonly tabs: SegmentOption<Tab>[] = [
    { value: 'active', label: 'Devam eden' },
    { value: 'history', label: 'Geçmiş' },
  ];
  protected readonly filters: { value: QuestStatus | null; label: string }[] = [
    { value: null, label: 'Tümü' },
    { value: 'Completed', label: 'Tamamlanan' },
    { value: 'Skipped', label: 'Geçilen' },
    { value: 'Expired', label: 'Süresi dolan' },
  ];

  protected readonly tab = signal<Tab>('active');
  protected readonly active = signal<Quest[]>([]);
  protected readonly loadingActive = signal(true);
  protected readonly history = signal<Quest[]>([]);
  protected readonly loadingHistory = signal(false);
  protected readonly status = signal<QuestStatus | null>(null);
  protected readonly page = signal(1);
  protected readonly hasMore = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.api.active().subscribe({
      next: (quests) => {
        this.active.set(quests);
        this.loadingActive.set(false);
      },
      error: (err: unknown) => {
        this.error.set(firstErrorMessage(err));
        this.loadingActive.set(false);
      },
    });
  }

  protected switchTab(tab: Tab): void {
    this.tab.set(tab);
    if (tab === 'history' && this.history().length === 0) this.loadHistory(1);
  }

  protected setStatus(status: QuestStatus | null): void {
    this.status.set(status);
    this.history.set([]);
    this.loadHistory(1);
  }

  protected loadHistory(page: number): void {
    this.loadingHistory.set(true);
    this.api.history(page, 20, this.status()).subscribe({
      next: (result) => {
        this.history.update((list) => (page === 1 ? result.items : [...list, ...result.items]));
        this.page.set(result.pageNumber);
        this.hasMore.set(result.hasNextPage);
        this.loadingHistory.set(false);
      },
      error: () => this.loadingHistory.set(false),
    });
  }

  protected categoryLabel(quest: Quest): string {
    return CATEGORIES[quest.category].label;
  }

  protected statusLabel(status: QuestStatus): string {
    return STATUS_LABELS[status];
  }

  protected date(quest: Quest): string {
    return formatDate(quest.completedAt ?? quest.offeredAt, { day: 'numeric', month: 'short' });
  }
}
