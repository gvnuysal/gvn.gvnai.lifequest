import { option, t } from '../../core/i18n/i18n';
import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { AdminApi } from '../../core/api/api-clients';
import { ProductMetrics } from '../../core/api/models';
import { firstErrorMessage } from '../../core/http/api-error';
import { formatDate } from '../../core/labels/format';
import { CATEGORIES, SKIP_REASONS } from '../../core/labels/labels';
import { Segmented, SegmentOption } from '../../ui/segmented';
import { EmptyState, Skeleton } from '../../ui/states';

interface Bar {
  label: string;
  value: number;
  display: string;
  ratio: number;
  color: string;
}

/**
 * Ürün metrikleri (yalnızca admin). North-star: haftalık "anlamlı" gerçek deneyim / aktif kullanıcı.
 * Tek ölçülü, az sayıda kalem olduğu için grafik yerine KPI kartları ve değer etiketli yatay çubuklar.
 */
@Component({
  selector: 'lq-admin-page',
  imports: [Segmented, EmptyState, Skeleton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="section">
      <header class="stack">
        <h1>{{ t().admin.metrics.title }}</h1>
        <p class="muted">{{ t().admin.metrics.lead }}</p>
        <lq-segmented [ariaLabel]="t().admin.metrics.period" [options]="periods" [(value)]="days" />
      </header>

      @if (error()) {
        <lq-empty-state icon="info" [title]="t().admin.metrics.loadFailed" [message]="error()" />
      } @else if (metrics(); as m) {
        <p class="range muted">{{ range() }}</p>

        <section class="hero surface" [attr.aria-label]="t().admin.metrics.northStarAria">
          <span class="eyebrow">North-star</span>
          <p class="hero__value">{{ m.northStar.toFixed(2) }}</p>
          <p class="hero__label">{{ t().admin.metrics.northStarUnit }}</p>
          <p class="muted small">{{ t().admin.metrics.northStarHint }}</p>
        </section>

        <section class="tiles" [attr.aria-label]="t().admin.metrics.summaryAria">
          <div class="tile surface"><span class="tile__value">{{ m.activeUsers }}</span><span class="tile__label">{{ t().admin.metrics.activeUsers }}</span></div>
          <div class="tile surface"><span class="tile__value">{{ m.meaningfulCompletions }}</span><span class="tile__label">{{ t().admin.metrics.meaningful }}</span></div>
          <div class="tile surface"><span class="tile__value">{{ percent(m.newCategoryDiscoveryRate) }}</span><span class="tile__label">{{ t().admin.metrics.newCategory }}</span></div>
          <div class="tile surface"><span class="tile__value">{{ m.averageRating?.toFixed(1) ?? '–' }}</span><span class="tile__label">{{ t().admin.metrics.avgRating }}</span></div>
        </section>

        <section class="surface card" aria-labelledby="funnel-title">
          <h2 id="funnel-title" class="section-title">{{ t().admin.metrics.funnel }}</h2>
          <ul class="bars">
            @for (bar of funnel(); track bar.label) {
              <li [title]="bar.label + ': ' + bar.display">
                <span class="bars__label">{{ bar.label }}</span>
                <span class="bars__track"><span [style.width.%]="bar.ratio * 100" [style.background]="bar.color"></span></span>
                <span class="bars__value">{{ bar.display }}</span>
              </li>
            }
          </ul>
          <p class="muted small">{{ t().admin.metrics.rates(percent(m.funnel.acceptanceRate), percent(m.funnel.completionRate), percent(m.explorationAcceptanceRate)) }}</p>
        </section>

        <section class="surface card" aria-labelledby="skip-title">
          <h2 id="skip-title" class="section-title">{{ t().admin.metrics.skipReasons }}</h2>
          @for (bar of skips(); track bar.label) {
            <ul class="bars"><li [title]="bar.label + ': ' + bar.display">
              <span class="bars__label">{{ bar.label }}</span>
              <span class="bars__track"><span [style.width.%]="bar.ratio * 100" [style.background]="bar.color"></span></span>
              <span class="bars__value">{{ bar.display }}</span>
            </li></ul>
          } @empty {
            <p class="muted small">{{ t().admin.metrics.noSkips }}</p>
          }
        </section>

        <section class="surface card" aria-labelledby="category-title">
          <h2 id="category-title" class="section-title">{{ t().admin.metrics.byCategory }}</h2>
          @for (bar of categories(); track bar.label) {
            <ul class="bars"><li [title]="bar.label + ': ' + bar.display">
              <span class="bars__label">{{ bar.label }}</span>
              <span class="bars__track"><span [style.width.%]="bar.ratio * 100" [style.background]="bar.color"></span></span>
              <span class="bars__value">{{ bar.display }}</span>
            </li></ul>
          } @empty {
            <p class="muted small">{{ t().admin.metrics.noCompletions }}</p>
          }
        </section>
      } @else {
        <lq-skeleton [height]="160" />
        <lq-skeleton [height]="220" />
      }
    </div>
  `,
  styles: `
    .section { display: flex; flex-direction: column; gap: var(--space-5); }
    .small { font-size: var(--fs-sm); }
    .range { font-size: var(--fs-sm); margin-top: -8px; }
    .hero { padding: var(--space-5); display: flex; flex-direction: column; gap: 2px; background: linear-gradient(140deg, var(--primary-soft), var(--surface) 70%); }
    .hero__value { font-size: 3rem; font-weight: 900; line-height: 1.05; font-variant-numeric: tabular-nums; }
    .hero__label { font-weight: 800; color: var(--ink-2); }
    .tiles { display: grid; grid-template-columns: repeat(2, 1fr); gap: 10px; }
    .tile { padding: 14px; display: flex; flex-direction: column; gap: 2px; }
    .tile__value { font-size: var(--fs-2xl); font-weight: 900; font-variant-numeric: tabular-nums; }
    .tile__label { font-size: var(--fs-sm); font-weight: 700; color: var(--ink-3); }
    .card { padding: var(--space-5); display: flex; flex-direction: column; gap: 12px; }
    .bars { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 10px; }
    .bars li { display: grid; grid-template-columns: 8.5rem 1fr 3.5rem; align-items: center; gap: 10px; font-size: var(--fs-sm); }
    .bars__label { color: var(--ink-2); font-weight: 700; }
    .bars__track { height: 12px; border-radius: 99px; background: var(--surface-2); overflow: hidden; }
    .bars__track span { display: block; height: 100%; min-width: 4px; border-radius: 0 4px 4px 0; }
    .bars__value { text-align: right; font-weight: 800; color: var(--ink); font-variant-numeric: tabular-nums; }
  `,
})
export class AdminPage {
  protected readonly t = t;
  private readonly api = inject(AdminApi);

  protected readonly periods: SegmentOption<number>[] = ([7, 30, 90] as const).map((d) => option(d, (x) => x.admin.metrics.periods[d]));
  protected readonly days = signal(7);
  protected readonly metrics = signal<ProductMetrics | null>(null);
  protected readonly error = signal<string | null>(null);

  protected readonly range = computed(() => {
    const m = this.metrics();
    return m ? `${formatDate(m.from, { day: 'numeric', month: 'long' })} – ${formatDate(m.to, { day: 'numeric', month: 'long' })}` : '';
  });

  protected readonly funnel = computed<Bar[]>(() => {
    const f = this.metrics()?.funnel;
    if (!f) return [];
    const max = Math.max(1, f.offered);
    return [
      { label: t().admin.metrics.offered, value: f.offered, display: `${f.offered}`, ratio: f.offered / max, color: 'var(--ink-3)' },
      { label: t().admin.metrics.accepted, value: f.accepted, display: `${f.accepted}`, ratio: f.accepted / max, color: 'var(--brand)' },
      { label: t().admin.metrics.completed, value: f.completed, display: `${f.completed}`, ratio: f.completed / max, color: 'var(--success)' },
    ];
  });

  protected readonly skips = computed<Bar[]>(() =>
    (this.metrics()?.skipReasons ?? []).map((s) => ({
      label: SKIP_REASONS.find((r) => r.value === s.key)?.label ?? s.key,
      value: s.count,
      display: this.percent(s.share),
      ratio: s.share,
      color: 'var(--ink-3)',
    })),
  );

  protected readonly categories = computed<Bar[]>(() =>
    (this.metrics()?.completionsByCategory ?? []).map((c) => ({
      label: CATEGORIES[c.key].label,
      value: c.count,
      display: `${c.count}`,
      ratio: c.share,
      color: `var(--cat-${CATEGORIES[c.key].token})`,
    })),
  );

  constructor() {
    effect(() => this.load(this.days()));
  }

  protected percent(value: number): string {
    return t().format.percent(Math.round(value * 100));
  }

  private load(days: number): void {
    this.error.set(null);
    this.api.metrics(days).subscribe({
      next: (m) => this.metrics.set(m),
      error: (err: unknown) => this.error.set(firstErrorMessage(err)),
    });
  }
}
