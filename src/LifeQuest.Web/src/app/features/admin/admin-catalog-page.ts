import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminApi } from '../../core/api/api-clients';
import { AdminTemplateListItem, CatalogHealth, LifeCategory, PagedResult, SafetyLevel } from '../../core/api/models';
import { firstErrorMessage } from '../../core/http/api-error';
import { CATEGORIES, CATEGORY_ORDER, QUEST_TYPE_LABELS } from '../../core/labels/labels';
import { Button } from '../../ui/button';
import { CategoryIcon } from '../../ui/category-badge';
import { Chip } from '../../ui/chip';
import { EmptyState, Skeleton } from '../../ui/states';
import { SAFETY_LABELS } from './admin-labels';
import { APP_PATHS, templatePath } from '../../core/routing/app-paths';

type StatusFilter = 'all' | SafetyLevel | 'inactive';

/** Katalog yönetimi: editoryal denge, inceleme kuyruğu ve template listesi. */
@Component({
  selector: 'lq-admin-catalog-page',
  imports: [FormsModule, RouterLink, Button, Chip, CategoryIcon, EmptyState, Skeleton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="section">
      <header class="head">
        <div class="stack">
          <h1>Katalog</h1>
          <p class="muted">Kurallara uymayan template kaydedilir ama incelemeden geçene kadar önerilmez.</p>
        </div>
        <a lq-button size="sm" [routerLink]="paths.admin.newTemplate">Yeni template</a>
      </header>

      @if (health(); as h) {
        <section class="surface health" aria-label="Katalog sağlığı">
          <div class="health__stats">
            <div><span class="value">{{ h.offerable }}</span><span class="label">Yayında</span></div>
            <button type="button" class="stat-link" (click)="setStatus('NeedsReview')">
              <span class="value" [class.warn]="h.needsReview > 0">{{ h.needsReview }}</span><span class="label">İnceleme bekliyor</span>
            </button>
            <a class="stat-link" [routerLink]="paths.admin.ideas">
              <span class="value" [class.warn]="h.pendingIdeas > 0">{{ h.pendingIdeas }}</span><span class="label">Bekleyen fikir</span>
            </a>
            <div><span class="value">{{ percent(h.freeShare) }}</span><span class="label">Ücretsiz</span></div>
            <div><span class="value">{{ percent(h.cityIndependentShare) }}</span><span class="label">Şehirden bağımsız</span></div>
          </div>
          <ul class="health__cats">
            @for (c of h.categories; track c.category) {
              <li [title]="catLabel(c.category) + ': ' + c.templates + ' template, ' + c.daily + ' günlük'">
                <lq-category-icon [category]="c.category" [size]="16" />
                <span>{{ catLabel(c.category) }}</span>
                <strong>{{ c.templates }}</strong>
              </li>
            }
          </ul>
          @if (h.warnings.length) {
            <ul class="warnings">
              @for (w of h.warnings; track w) { <li>{{ w }}</li> }
            </ul>
          } @else {
            <p class="ok small">Katalog dengesi editoryal kurallara uygun.</p>
          }
        </section>
      }

      <div class="stack">
        <input class="input" type="search" placeholder="Başlık veya kod ara" aria-label="Template ara"
               [ngModel]="query()" (ngModelChange)="onSearch($event)" />
        <div class="chips" role="group" aria-label="Durum">
          @for (s of statuses; track s.value) {
            <button lq-chip [selected]="status() === s.value" (click)="setStatus(s.value)">{{ s.label }}</button>
          }
        </div>
        <div class="chips" role="group" aria-label="Kategori">
          <button lq-chip [selected]="category() === null" (click)="setCategory(null)">Tüm kategoriler</button>
          @for (c of categoryOrder; track c) {
            <button lq-chip [selected]="category() === c" (click)="setCategory(c)">{{ catLabel(c) }}</button>
          }
        </div>
      </div>

      @if (error()) {
        <lq-empty-state icon="info" title="Katalog yüklenemedi" [message]="error()" />
      } @else if (page(); as p) {
        <p class="muted small">{{ p.totalCount }} template</p>
        <ul class="list">
          @for (t of p.items; track t.id) {
            <li>
              <a class="surface item" [routerLink]="templatePath(t.id)">
                <lq-category-icon [category]="t.category" [size]="20" />
                <div class="item__body">
                  <strong>{{ t.title }}</strong>
                  <span class="muted small">{{ t.code }} · {{ typeLabel(t) }} · v{{ t.version }}{{ t.source === 'Admin' ? ' · admin' : '' }}</span>
                </div>
                <div class="item__status">
                  <span class="pill" [class]="'pill pill--' + safety(t).tone">{{ safety(t).label }}</span>
                  @if (!t.isActive) { <span class="pill">Pasif</span> }
                  @if (t.violationCount) { <span class="muted small">{{ t.violationCount }} kural</span> }
                </div>
              </a>
            </li>
          } @empty {
            <lq-empty-state icon="list" title="Template bulunamadı" message="Aramanı veya filtreleri değiştirmeyi dene." />
          }
        </ul>
        @if (p.totalPages > 1) {
          <nav class="pager" aria-label="Sayfalar">
            <button lq-button variant="soft" size="sm" [disabled]="!p.hasPreviousPage" (click)="pageNumber.set(p.pageNumber - 1)">Önceki</button>
            <span class="muted small">{{ p.pageNumber }} / {{ p.totalPages }}</span>
            <button lq-button variant="soft" size="sm" [disabled]="!p.hasNextPage" (click)="pageNumber.set(p.pageNumber + 1)">Sonraki</button>
          </nav>
        }
      } @else {
        <lq-skeleton [height]="72" />
        <lq-skeleton [height]="72" />
        <lq-skeleton [height]="72" />
      }
    </section>
  `,
  styles: `
    .section { display: flex; flex-direction: column; gap: var(--space-4); }
    .head { display: flex; justify-content: space-between; align-items: flex-start; gap: 12px; }
    .small { font-size: var(--fs-sm); }
    .health { padding: var(--space-4); display: flex; flex-direction: column; gap: 12px; }
    .health__stats { display: grid; grid-template-columns: repeat(2, 1fr); gap: 10px; }
    @media (min-width: 560px) { .health__stats { grid-template-columns: repeat(5, 1fr); } }
    .health__stats > * { display: flex; flex-direction: column; gap: 2px; text-align: left; }
    .stat-link { background: none; border: 0; padding: 0; cursor: pointer; color: inherit; font: inherit; text-decoration: none; }
    .value { font-size: var(--fs-xl); font-weight: 900; font-variant-numeric: tabular-nums; }
    .value.warn { color: var(--xp-ink); }
    .label { font-size: var(--fs-xs); font-weight: 700; color: var(--ink-3); }
    .health__cats { list-style: none; margin: 0; padding: 0; display: grid; grid-template-columns: repeat(2, 1fr); gap: 6px 12px; }
    @media (min-width: 560px) { .health__cats { grid-template-columns: repeat(3, 1fr); } }
    .health__cats li { display: flex; align-items: center; gap: 6px; font-size: var(--fs-sm); color: var(--ink-2); }
    .health__cats strong { margin-left: auto; font-variant-numeric: tabular-nums; color: var(--ink); }
    .warnings { margin: 0; padding: 10px 12px 10px 28px; border-radius: var(--radius-md); background: var(--xp-soft); color: var(--xp-ink); font-size: var(--fs-sm); font-weight: 700; }
    .ok { color: var(--success); font-weight: 700; }
    .chips { display: flex; gap: 6px; overflow-x: auto; scrollbar-width: none; margin: 0 calc(-1 * var(--space-4)); padding: 0 var(--space-4); }
    .chips::-webkit-scrollbar { display: none; }
    .list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 8px; }
    .item { display: flex; align-items: center; gap: 12px; padding: 12px 14px; color: inherit; text-decoration: none; }
    .item:hover { border-color: var(--ink-3); }
    .item__body { display: flex; flex-direction: column; min-width: 0; flex: 1; }
    .item__body strong { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .item__status { display: flex; flex-direction: column; align-items: flex-end; gap: 4px; }
    .pager { display: flex; justify-content: space-between; align-items: center; }
  `,
})
export class AdminCatalogPage {
  protected readonly paths = APP_PATHS;
  protected readonly templatePath = templatePath;
  private readonly api = inject(AdminApi);

  protected readonly categoryOrder = CATEGORY_ORDER;
  protected readonly statuses: { value: StatusFilter; label: string }[] = [
    { value: 'all', label: 'Tümü' },
    { value: 'NeedsReview', label: 'İnceleme bekliyor' },
    { value: 'Safe', label: 'Yayında' },
    { value: 'Blocked', label: 'Engelli' },
    { value: 'inactive', label: 'Pasif' },
  ];

  protected readonly query = signal('');
  private readonly text = signal('');
  protected readonly status = signal<StatusFilter>('all');
  protected readonly category = signal<LifeCategory | null>(null);
  protected readonly pageNumber = signal(1);
  protected readonly page = signal<PagedResult<AdminTemplateListItem> | null>(null);
  protected readonly health = signal<CatalogHealth | null>(null);
  protected readonly error = signal<string | null>(null);
  private searchTimer?: ReturnType<typeof setTimeout>;

  private readonly search = computed(() => {
    const status = this.status();
    return {
      text: this.text() || undefined,
      category: this.category() ?? undefined,
      safety: status === 'all' || status === 'inactive' ? undefined : status,
      isActive: status === 'inactive' ? false : undefined,
      pageNumber: this.pageNumber(),
    };
  });

  constructor() {
    this.api.catalogHealth().subscribe({ next: (h) => this.health.set(h) });
    effect(() => {
      const search = this.search();
      this.error.set(null);
      this.api.templates(search).subscribe({
        next: (page) => this.page.set(page),
        error: (err: unknown) => this.error.set(firstErrorMessage(err)),
      });
    });
  }

  protected onSearch(value: string): void {
    this.query.set(value);
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.pageNumber.set(1);
      this.text.set(value.trim());
    }, 300);
  }

  protected setStatus(status: StatusFilter): void {
    this.status.set(status);
    this.pageNumber.set(1);
  }

  protected setCategory(category: LifeCategory | null): void {
    this.category.set(category);
    this.pageNumber.set(1);
  }

  protected catLabel(category: LifeCategory): string {
    return CATEGORIES[category].label;
  }

  protected typeLabel(t: AdminTemplateListItem): string {
    return QUEST_TYPE_LABELS[t.type];
  }

  protected safety(t: AdminTemplateListItem) {
    return SAFETY_LABELS[t.safety];
  }

  protected percent(value: number): string {
    return `%${Math.round(value * 100)}`;
  }
}
