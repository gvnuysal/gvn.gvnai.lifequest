import { option, t } from '../../core/i18n/i18n';
import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AdminApi } from '../../core/api/api-clients';
import { ExperimentAction, ExperimentDetail, VariantResult } from '../../core/api/models';
import { firstErrorMessage } from '../../core/http/api-error';
import { formatDate } from '../../core/labels/format';
import { ToastService } from '../../core/state/toast.service';
import { Button } from '../../ui/button';
import { Icon } from '../../ui/icon';
import { Sheet } from '../../ui/sheet';
import { EmptyState, Skeleton } from '../../ui/states';
import { EXPERIMENT_STATUS_LABELS, VERDICT_LABELS } from './admin-labels';
import { APP_PATHS } from '../../core/routing/app-paths';

interface MetricRow {
  label: string;
  control: string;
  treatment: string;
  hint?: string;
}

/**
 * Deney sonucu. Birincil metrik north-star (kullanıcı başına haftalık anlamlı deneyim); fark için %95 güven
 * aralığı gösterilir. "İlgimi çekmedi" oranı koruma metriğidir: artıyorsa deneme ayarı kullanıcıyı yoruyor olabilir.
 */
@Component({
  selector: 'lq-admin-experiment-page',
  imports: [RouterLink, Button, Icon, Sheet, EmptyState, Skeleton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="section">
      <a class="back" [routerLink]="paths.admin.experiments"><lq-icon name="arrow-left" [size]="18" /> {{ t().adminExperiment.back }}</a>

      @if (error()) {
        <lq-empty-state icon="info" [title]="t().adminExperiment.loadFailed" [message]="error()" />
      } @else if (detail(); as d) {
        <header class="stack">
          <h1>{{ d.experiment.name }}</h1>
          <div class="row wrap">
            <span [class]="'pill pill--' + statusMeta().tone">{{ statusMeta().label }}</span>
            <span class="pill">{{ t().adminExperiment.share(t().format.percent(share())) }}</span>
            @if (d.experiment.outcome === 'Adopted') { <span class="pill pill--success">{{ t().adminExperiment.adopted }}</span> }
            @if (d.experiment.outcome === 'Discarded') { <span class="pill">{{ t().adminExperiment.discarded }}</span> }
          </div>
          <p class="muted">{{ d.experiment.hypothesis }}</p>
        </header>

        <section class="surface card">
          <h2 class="section-title">{{ t().adminExperiment.changedWeights }}</h2>
          <table>
            <thead><tr><th>{{ t().adminExperiment.weight }}</th><th>{{ t().adminExperiment.control }}</th><th>{{ t().adminExperiment.treatment }}</th></tr></thead>
            <tbody>
              @for (o of d.experiment.overrides; track o.key) {
                <tr><td>{{ o.label }}</td><td>{{ o.controlValue }}</td><td><strong>{{ o.treatmentValue }}</strong></td></tr>
              }
            </tbody>
          </table>
          <p class="muted small">{{ t().adminExperiment.controlHint }}</p>
        </section>

        @if (d.results; as r) {
          <section class="surface card verdict" [attr.data-tone]="verdict()!.tone">
            <span class="eyebrow">{{ t().adminExperiment.result(r.weeks) }}</span>
            <p class="verdict__label">{{ verdict()!.label }}</p>
            <p class="small">{{ verdictHint() }}</p>
            @if (r.guardrailBreached) {
              <p class="guardrail small" role="alert">
                <strong>{{ t().adminExperiment.guardrailTitle }}</strong>
                {{ t().adminExperiment.guardrail(pct(r.control.notInterestedRate), pct(r.treatment.notInterestedRate), pct(r.guardrailMaxIncrease)) }}
              </p>
            }
            <div class="ci" [attr.aria-label]="t().adminExperiment.ciAria(r.northStar.difference, r.northStar.ciLow, r.northStar.ciHigh)">
              <span class="ci__zero" [style.left.%]="ciPosition(0)"></span>
              <span class="ci__range" [style.left.%]="ciPosition(r.northStar.ciLow)"
                    [style.width.%]="ciPosition(r.northStar.ciHigh) - ciPosition(r.northStar.ciLow)"></span>
              <span class="ci__point" [style.left.%]="ciPosition(r.northStar.difference)"></span>
            </div>
            <p class="muted small">
              {{ t().adminExperiment.ciText(signed(r.northStar.difference), signed(r.northStar.ciLow), signed(r.northStar.ciHigh)) }}
              @if (r.northStar.relativeLift !== null) { {{ t().adminExperiment.relative(signed(r.northStar.relativeLift * 100, 1)) }} }
            </p>
          </section>

          <section class="surface card">
            <h2 class="section-title">{{ t().adminExperiment.groups }}</h2>
            <table>
              <thead><tr><th>{{ t().adminExperiment.metric }}</th><th>{{ t().adminExperiment.control }}</th><th>{{ t().adminExperiment.treatment }}</th></tr></thead>
              <tbody>
                @for (m of metrics(); track m.label) {
                  <tr [title]="m.hint ?? ''"><td>{{ m.label }}</td><td>{{ m.control }}</td><td>{{ m.treatment }}</td></tr>
                }
              </tbody>
            </table>
            <p class="muted small">{{ t().adminExperiment.groupsHint }}</p>
          </section>
        } @else {
          <p class="muted">{{ t().adminExperiment.notStarted }}</p>
        }

        <div class="actions">
          @switch (d.experiment.status) {
            @case ('Draft') { <button lq-button (click)="open('start')">{{ t().adminExperiment.actions.start }}</button> }
            @case ('Running') { <button lq-button variant="soft" (click)="open('stop')">{{ t().adminExperiment.actions.stop }}</button> }
            @case ('Stopped') {
              @if (d.experiment.outcome === 'None') {
                <button lq-button (click)="open('adopt')">{{ t().adminExperiment.actions.adopt }}</button>
                <button lq-button variant="soft" (click)="open('discard')">{{ t().adminExperiment.discardButton }}</button>
              }
            }
          }
        </div>
      } @else {
        <lq-skeleton [height]="140" />
        <lq-skeleton [height]="220" />
      }
    </section>

    <lq-sheet [title]="actionTitle()" [open]="!!pending()" (openChange)="$event || pending.set(null)">
      @if (pending(); as action) {
        <div class="stack">
          <p class="muted">{{ actionHint() }}</p>
          <div class="field">
            <label for="exp-reason">{{ t().adminExperiment.note }}</label>
            <textarea id="exp-reason" class="input" rows="2" maxlength="500" #reason></textarea>
          </div>
          <button lq-button [block]="true" [loading]="busy()" (click)="run(action, reason.value)">{{ t().adminExperiment.confirm }}</button>
        </div>
      }
    </lq-sheet>
  `,
  styles: `
    .section { display: flex; flex-direction: column; gap: var(--space-4); }
    .back { display: inline-flex; align-items: center; gap: 6px; font-weight: 800; color: var(--ink-2); text-decoration: none; width: fit-content; }
    .wrap { flex-wrap: wrap; }
    .small { font-size: var(--fs-sm); }
    .card { padding: var(--space-4); display: flex; flex-direction: column; gap: 10px; overflow-x: auto; }
    table { width: 100%; border-collapse: collapse; font-size: var(--fs-sm); font-variant-numeric: tabular-nums; }
    th { text-align: left; color: var(--ink-3); font-size: var(--fs-xs); font-weight: 800; padding: 6px 4px; border-bottom: 1px solid var(--line); }
    td { padding: 8px 4px; border-bottom: 1px solid var(--line); }
    th:not(:first-child), td:not(:first-child) { text-align: right; }
    .guardrail { padding: 10px 12px; border-radius: var(--radius-md); border: 1px solid var(--danger); color: var(--ink-1); }
    .verdict__label { font-size: var(--fs-xl); font-weight: 900; }
    .verdict[data-tone='success'] .verdict__label { color: var(--success); }
    .verdict[data-tone='danger'] .verdict__label { color: var(--danger); }
    .verdict[data-tone='warning'] .verdict__label { color: var(--xp-ink); }
    .ci { position: relative; height: 28px; margin: 6px 0; border-radius: 99px; background: var(--surface-2); }
    .ci__zero { position: absolute; top: 2px; bottom: 2px; width: 2px; background: var(--ink-3); }
    .ci__range { position: absolute; top: 9px; height: 10px; min-width: 4px; border-radius: 99px; background: color-mix(in srgb, var(--primary) 35%, transparent); }
    .ci__point { position: absolute; top: 6px; width: 16px; height: 16px; margin-left: -8px; border-radius: 50%; background: var(--primary); border: 2px solid var(--surface); }
    .actions { display: flex; flex-wrap: wrap; gap: 8px; }
  `,
})
export class AdminExperimentPage {
  protected readonly t = t;
  protected readonly paths = APP_PATHS;
  readonly id = input.required<string>();

  private readonly api = inject(AdminApi);
  private readonly toast = inject(ToastService);

  protected readonly detail = signal<ExperimentDetail | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly pending = signal<ExperimentAction | null>(null);
  protected readonly busy = signal(false);

  protected readonly pct = (v: number) => t().format.percent(Math.round(v * 100));

  protected readonly statusMeta = computed(() => EXPERIMENT_STATUS_LABELS[this.detail()?.experiment.status ?? 'Draft']);
  protected readonly share = computed(() => Math.round((this.detail()?.experiment.treatmentShare ?? 0) * 100));
  protected readonly verdict = computed(() => {
    const r = this.detail()?.results;
    return r ? VERDICT_LABELS[r.verdict] : null;
  });
  protected readonly verdictHint = computed(() =>
    (this.verdict()?.hint ?? '').replace('{min}', String(this.detail()?.results?.minUsersPerVariant ?? 30)),
  );

  /** Güven aralığı çubuğu: sıfır ve aralık aynı ölçekte, simetrik eksende. */
  private readonly ciScale = computed(() => {
    const ns = this.detail()?.results?.northStar;
    return ns ? Math.max(0.5, Math.abs(ns.ciLow), Math.abs(ns.ciHigh)) * 1.15 : 1;
  });

  protected readonly metrics = computed<MetricRow[]>(() => {
    const r = this.detail()?.results;
    if (!r) return [];
    const row = (label: string, pick: (v: VariantResult) => string, hint?: string): MetricRow =>
      ({ label, control: pick(r.control), treatment: pick(r.treatment), hint });
    const pct = this.pct;
    const L = t().adminExperiment.rows;
    return [
      row(L.users, (v) => `${v.users}`),
      row(L.northStar, (v) => `${v.northStar.toFixed(2)} ±${v.northStarStandardError.toFixed(2)}`, L.northStarHint),
      row(L.offered, (v) => `${v.offered}`),
      row(L.acceptance, (v) => pct(v.acceptanceRate)),
      row(L.completion, (v) => pct(v.completionRate)),
      row(L.exploration, (v) => pct(v.explorationAcceptanceRate)),
      row(L.notInterested, (v) => pct(v.notInterestedRate), L.notInterestedHint),
      row(L.rating, (v) => v.averageRating?.toFixed(1) ?? '–'),
    ];
  });

  protected readonly actionTitle = computed(() => t().adminExperiment.actions[this.pending() ?? 'start']);

  protected readonly actionHint = computed(() => {
    const hints = t().adminExperiment.hints;
    const action = this.pending() ?? 'start';
    return action === 'start' ? hints.start(t().format.percent(this.share())) : hints[action];
  });

  constructor() {
    effect(() => this.load(this.id()));
  }

  protected ciPosition(value: number): number {
    return Math.min(100, Math.max(0, 50 + (value / this.ciScale()) * 50));
  }

  protected signed(value: number, digits = 2): string {
    return `${value > 0 ? '+' : ''}${value.toFixed(digits)}`;
  }

  protected date(value: string): string {
    return formatDate(value, { day: 'numeric', month: 'short' });
  }

  protected open(action: ExperimentAction): void {
    this.pending.set(action);
  }

  protected run(action: ExperimentAction, reason: string): void {
    this.busy.set(true);
    this.api.changeExperiment(this.id(), action, reason.trim() || null).subscribe({
      next: () => {
        this.busy.set(false);
        this.pending.set(null);
        this.toast.success(t().adminExperiment.saved);
        this.load(this.id());
      },
      error: (err: unknown) => {
        this.busy.set(false);
        this.toast.error(firstErrorMessage(err));
      },
    });
  }

  private load(id: string): void {
    this.api.experiment(id).subscribe({
      next: (d) => this.detail.set(d),
      error: (err: unknown) => this.error.set(firstErrorMessage(err)),
    });
  }
}
