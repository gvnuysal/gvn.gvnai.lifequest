import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { SavedApi } from '../../core/api/api-clients';
import { SavedQuest } from '../../core/api/models';
import { firstErrorMessage } from '../../core/http/api-error';
import { formatDuration } from '../../core/labels/format';
import { CATEGORIES, COST_LABELS, QUEST_TYPE_LABELS } from '../../core/labels/labels';
import { ToastService } from '../../core/state/toast.service';
import { Button } from '../../ui/button';
import { CategoryIcon } from '../../ui/category-badge';
import { Icon } from '../../ui/icon';
import { EmptyState, Skeleton } from '../../ui/states';
import { APP_PATHS, questPath } from '../../core/routing/app-paths';

/** "Sonra yaparım": kabul edilmeyen öneriler kaybolmasın; hazır olunca buradan başlatılır. */
@Component({
  selector: 'lq-saved-page',
  imports: [RouterLink, Button, CategoryIcon, Icon, EmptyState, Skeleton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <header class="stack">
        <h1>Sonra yaparım</h1>
        <p class="muted">Şimdi olmasa da kaçırmak istemediğin deneyimler. Başlattığında aktif görevlerine eklenir.</p>
      </header>

      @if (error()) {
        <lq-empty-state icon="info" title="Liste yüklenemedi" [message]="error()" />
      } @else if (items(); as list) {
        <ul class="list">
          @for (item of list; track item.templateId) {
            <li class="surface item" [class.item--off]="!item.isAvailable">
              <div class="item__head">
                <lq-category-icon [category]="item.category" [size]="42" />
                <div class="item__text">
                  <span class="kicker">{{ category(item) }} · {{ type(item) }}</span>
                  <strong>{{ item.title }}</strong>
                </div>
              </div>
              <p class="muted small">{{ item.description }}</p>
              @if (item.isAvailable) {
                <div class="meta small">
                  <span><lq-icon name="clock" [size]="15" /> {{ duration(item) }}</span>
                  <span><lq-icon name="coins" [size]="15" /> {{ cost(item) }}</span>
                </div>
              } @else {
                <p class="small off">Bu deneyim şu an katalogda sunulmuyor.</p>
              }
              <div class="actions">
                @if (item.isAvailable) {
                  <button lq-button size="sm" [loading]="busy() === item.templateId" [disabled]="!!busy()" (click)="start(item)">
                    <lq-icon name="check" [size]="16" /> Şimdi başla
                  </button>
                }
                <button lq-button variant="ghost" size="sm" [disabled]="!!busy()" (click)="remove(item)">Kaldır</button>
              </div>
            </li>
          } @empty {
            <lq-empty-state icon="heart" title="Listen boş" message="Bir önerinin detayında &quot;Sonra yaparım&quot;a dokunarak buraya ekleyebilirsin.">
              <a lq-button variant="secondary" size="sm" [routerLink]="paths.today">Önerilere göz at</a>
            </lq-empty-state>
          }
        </ul>
      } @else {
        <lq-skeleton [height]="140" />
        <lq-skeleton [height]="140" />
      }
    </div>
  `,
  styles: `
    .small { font-size: var(--fs-sm); }
    .list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 12px; }
    .item { padding: var(--space-4); display: flex; flex-direction: column; gap: 10px; }
    .item--off { opacity: 0.75; }
    .item__head { display: flex; align-items: center; gap: 12px; }
    .item__text { display: flex; flex-direction: column; min-width: 0; }
    .kicker { font-size: var(--fs-xs); font-weight: 800; color: var(--ink-3); text-transform: uppercase; letter-spacing: 0.04em; }
    .meta { display: flex; gap: 14px; color: var(--ink-2); font-weight: 700; }
    .meta span { display: inline-flex; align-items: center; gap: 4px; }
    .off { color: var(--ink-3); font-weight: 700; }
    .actions { display: flex; gap: 8px; flex-wrap: wrap; }
  `,
})
export class SavedPage {
  protected readonly paths = APP_PATHS;
  private readonly api = inject(SavedApi);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  protected readonly items = signal<SavedQuest[] | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly busy = signal<string | null>(null);

  constructor() {
    this.api.list().subscribe({
      next: (list) => this.items.set(list),
      error: (err: unknown) => this.error.set(firstErrorMessage(err)),
    });
  }

  protected category(item: SavedQuest): string {
    return CATEGORIES[item.category].label;
  }

  protected type(item: SavedQuest): string {
    return QUEST_TYPE_LABELS[item.type];
  }

  protected duration(item: SavedQuest): string {
    return formatDuration(item.minMinutes, item.maxMinutes);
  }

  protected cost(item: SavedQuest): string {
    return COST_LABELS[item.cost].label;
  }

  protected start(item: SavedQuest): void {
    this.busy.set(item.templateId);
    this.api.start(item.templateId).subscribe({
      next: (quest) => {
        this.busy.set(null);
        this.toast.success('Başladı! Aktif görevlerine eklendi.');
        void this.router.navigate(questPath(quest.id));
      },
      error: (err: unknown) => {
        this.busy.set(null);
        this.toast.error(firstErrorMessage(err));
      },
    });
  }

  protected remove(item: SavedQuest): void {
    this.busy.set(item.templateId);
    this.api.remove(item.templateId).subscribe({
      next: () => {
        this.busy.set(null);
        this.items.update((list) => list?.filter((i) => i.templateId !== item.templateId) ?? null);
      },
      error: (err: unknown) => {
        this.busy.set(null);
        this.toast.error(firstErrorMessage(err));
      },
    });
  }
}
