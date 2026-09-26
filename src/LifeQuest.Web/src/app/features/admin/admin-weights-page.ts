import { option, t } from '../../core/i18n/i18n';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminApi } from '../../core/api/api-clients';
import { RecommendationWeights, WeightField } from '../../core/api/models';
import { firstErrorMessage, parseApiErrors } from '../../core/http/api-error';
import { formatDate } from '../../core/labels/format';
import { ToastService } from '../../core/state/toast.service';
import { Button } from '../../ui/button';
import { EmptyState, Skeleton } from '../../ui/states';
import { WEIGHT_GROUPS } from './admin-labels';

/**
 * Öneri motoru ağırlıkları. Değerler appsettings varsayılanlarının üzerine yazılır ve kaydedildiği an yeni
 * önerilerde kullanılır. Her değişiklik gerekçesiyle denetim kaydına düşer.
 */
@Component({
  selector: 'lq-admin-weights-page',
  imports: [FormsModule, Button, EmptyState, Skeleton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="section">
      <header class="stack">
        <h1>{{ t().adminWeights.title }}</h1>
        <p class="muted">{{ t().adminWeights.lead }}</p>
        <p class="tip small">
          {{ t().adminWeights.tip }}
          <code>dotnet run --project tools/LifeQuest.Simulation</code>
        </p>
        @if (weights(); as w) {
          @if (w.updatedAt) {
            <p class="muted small">{{ t().adminWeights.lastChange(date(w.updatedAt), w.updatedBy ?? '') }}</p>
          }
        }
      </header>

      @if (error()) {
        <lq-empty-state icon="info" [title]="t().adminWeights.loadFailed" [message]="error()" />
      } @else if (weights()) {
        @for (group of groups(); track group.value) {
          <section class="surface card" [attr.aria-labelledby]="'g-' + group.value">
            <div>
              <h2 class="section-title" [id]="'g-' + group.value">{{ group.label }}</h2>
              <p class="muted small">{{ group.hint }}</p>
            </div>
            @for (field of group.fields; track field.key) {
              <div class="weight" [class.weight--changed]="isChanged(field)">
                <div class="weight__head">
                  <label [for]="field.key">{{ field.label }}</label>
                  <div class="row">
                    @if (field.isOverridden && !isChanged(field)) { <span class="pill pill--brand">{{ t().adminWeights.custom }}</span> }
                    @if (isChanged(field)) { <span class="pill pill--warning">{{ t().adminWeights.unsaved }}</span> }
                  </div>
                </div>
                <div class="weight__controls">
                  <input type="range" [attr.aria-label]="field.label" [min]="field.min" [max]="field.max" [step]="field.step"
                         [ngModel]="valueOf(field)" (ngModelChange)="set(field, $event)" />
                  <input class="input weight__number" type="number" [id]="field.key" [min]="field.min" [max]="field.max" [step]="field.step"
                         [ngModel]="valueOf(field)" (ngModelChange)="set(field, $event)" />
                </div>
                <p class="muted small">
                  {{ field.description }} · {{ t().adminWeights.defaultValue(format(field, field.defaultValue)) }} · {{ field.min }}–{{ field.max }}
                  @if (valueOf(field) !== field.defaultValue) {
                    · <button type="button" class="link" (click)="set(field, field.defaultValue)">{{ t().adminWeights.toDefault }}</button>
                  }
                </p>
                @if (fieldErrors()[field.key]; as e) { <span class="field__error">{{ e }}</span> }
              </div>
            }
          </section>
        }

        <section class="surface card save" [attr.aria-label]="t().adminWeights.saveAria">
          <div class="field">
            <label for="reason">{{ t().adminWeights.reason }}</label>
            <textarea id="reason" class="input" rows="2" maxlength="500" [placeholder]="t().adminWeights.reasonPlaceholder"
                      [(ngModel)]="reason"></textarea>
          </div>
          @if (saveError()) { <p class="field__error" role="alert">{{ saveError() }}</p> }
          <div class="actions">
            <button lq-button [loading]="busy()" [disabled]="!changedCount() || !reason.trim()" (click)="save()">
              {{ changedCount() ? t().adminWeights.saveChanges(changedCount()) : t().adminWeights.noChanges }}
            </button>
            @if (changedCount()) {
              <button lq-button variant="soft" (click)="discard()">{{ t().adminWeights.discard }}</button>
            }
            @if (overriddenCount() && !changedCount()) {
              <button lq-button variant="soft" [loading]="busy()" (click)="resetAll()">{{ t().adminWeights.resetAll }}</button>
            }
          </div>
        </section>
      } @else {
        <lq-skeleton [height]="200" />
        <lq-skeleton [height]="200" />
      }
    </section>
  `,
  styles: `
    .section { display: flex; flex-direction: column; gap: var(--space-4); }
    .small { font-size: var(--fs-sm); }
    .tip { padding: 10px 12px; border-radius: var(--radius-md); background: var(--info-soft); color: var(--ink-2); }
    .tip code { font-size: var(--fs-xs); overflow-wrap: anywhere; }
    .card { padding: var(--space-4); display: flex; flex-direction: column; gap: 14px; }
    .weight { display: flex; flex-direction: column; gap: 6px; padding: 10px 12px; margin: 0 -12px; border-radius: var(--radius-md); }
    .weight--changed { background: var(--xp-soft); }
    .weight__head { display: flex; justify-content: space-between; align-items: center; gap: 8px; }
    .weight__head label { font-weight: 800; }
    .weight__controls { display: flex; align-items: center; gap: 12px; }
    .weight__controls input[type='range'] { flex: 1; accent-color: var(--primary); min-height: 32px; }
    .weight__number { width: 6.5rem; min-height: 40px; padding: 8px 10px; text-align: right; font-variant-numeric: tabular-nums; }
    .link { background: none; border: 0; padding: 0; color: var(--primary-text); font-weight: 800; cursor: pointer; font-size: inherit; }
    .save { position: sticky; bottom: calc(var(--nav-height) + var(--space-3) + env(safe-area-inset-bottom)); box-shadow: var(--shadow-lg); }
    .actions { display: flex; flex-wrap: wrap; gap: 8px; }
  `,
})
export class AdminWeightsPage {
  protected readonly t = t;
  private readonly api = inject(AdminApi);
  private readonly toast = inject(ToastService);

  protected readonly weights = signal<RecommendationWeights | null>(null);
  protected readonly draft = signal<Record<string, number>>({});
  protected readonly error = signal<string | null>(null);
  protected readonly saveError = signal<string | null>(null);
  protected readonly fieldErrors = signal<Record<string, string>>({});
  protected readonly busy = signal(false);
  protected reason = '';

  protected readonly groups = computed(() => {
    const fields = this.weights()?.fields ?? [];
    return WEIGHT_GROUPS.map((g) => ({ ...g, fields: fields.filter((f) => f.group === g.value) })).filter((g) => g.fields.length);
  });

  protected readonly changedCount = computed(() => Object.keys(this.changes()).length);
  protected readonly overriddenCount = computed(() => (this.weights()?.fields ?? []).filter((f) => f.isOverridden).length);

  private readonly changes = computed(() => {
    const byKey = new Map((this.weights()?.fields ?? []).map((f) => [f.key, f]));
    return Object.fromEntries(Object.entries(this.draft()).filter(([key, value]) => byKey.get(key)?.value !== value));
  });

  constructor() {
    this.api.weights().subscribe({
      next: (w) => this.weights.set(w),
      error: (err: unknown) => this.error.set(firstErrorMessage(err)),
    });
  }

  protected valueOf(field: WeightField): number {
    return this.draft()[field.key] ?? field.value;
  }

  protected isChanged(field: WeightField): boolean {
    return field.key in this.changes();
  }

  protected set(field: WeightField, raw: number | string): void {
    const value = Number(raw);
    if (Number.isNaN(value)) return;
    const clamped = Math.min(field.max, Math.max(field.min, field.isInteger ? Math.round(value) : Math.round(value * 1000) / 1000));
    this.draft.update((d) => ({ ...d, [field.key]: clamped }));
  }

  protected format(field: WeightField, value: number): string {
    return field.isInteger ? t().adminWeights.days(value) : value.toFixed(2);
  }

  protected date(value: string): string {
    return formatDate(value, { day: 'numeric', month: 'long', hour: '2-digit', minute: '2-digit' });
  }

  protected discard(): void {
    this.draft.set({});
    this.fieldErrors.set({});
    this.saveError.set(null);
  }

  protected save(): void {
    const w = this.weights();
    if (!w || !this.changedCount()) return;
    this.run(this.api.updateWeights(w.revision, this.changes(), this.reason.trim()), t().adminWeights.saved);
  }

  protected resetAll(): void {
    const w = this.weights();
    if (!w) return;
    this.run(this.api.resetWeights(w.revision, null, this.reason.trim() || null), t().adminWeights.reset);
  }

  private run(request: ReturnType<AdminApi['weights']>, message: string): void {
    this.busy.set(true);
    this.saveError.set(null);
    this.fieldErrors.set({});
    request.subscribe({
      next: (w) => {
        this.busy.set(false);
        this.weights.set(w);
        this.draft.set({});
        this.reason = '';
        this.toast.success(message);
      },
      error: (err: unknown) => {
        this.busy.set(false);
        const errors = parseApiErrors(err);
        this.fieldErrors.set(Object.fromEntries(
          errors.filter((e) => e.code.startsWith('Values.')).map((e) => [e.code.slice('Values.'.length), e.message])));
        this.saveError.set(errors.find((e) => !e.code.startsWith('Values.'))?.message ?? (errors.length ? t().adminWeights.outOfRange : null));
      },
    });
  }
}
