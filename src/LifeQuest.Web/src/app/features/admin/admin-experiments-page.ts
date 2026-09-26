import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AdminApi } from '../../core/api/api-clients';
import { Experiment, ExperimentPreset, WeightField } from '../../core/api/models';
import { firstErrorMessage, parseApiErrors } from '../../core/http/api-error';
import { formatDate } from '../../core/labels/format';
import { ToastService } from '../../core/state/toast.service';
import { Button } from '../../ui/button';
import { Sheet } from '../../ui/sheet';
import { EmptyState, Skeleton } from '../../ui/states';
import { EXPERIMENT_STATUS_LABELS } from './admin-labels';
import { experimentPath } from '../../core/routing/app-paths';

interface OverrideRow {
  field: WeightField;
  value: number;
}

/**
 * A/B deneyleri: Kontrol grubu üretim ağırlıklarıyla, Deneme grubu değiştirilen ağırlıklarla öneri alır.
 * Aynı anda tek deney çalışır; kullanıcılar deterministik olarak gruplara atanır.
 */
@Component({
  selector: 'lq-admin-experiments-page',
  imports: [FormsModule, RouterLink, Button, Sheet, EmptyState, Skeleton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="section">
      <header class="head">
        <div class="stack">
          <h1>Deneyler</h1>
          <p class="muted">Ağırlık değişikliğini herkese açmadan önce kullanıcıların bir kısmında dene; north-star ile karşılaştır.</p>
        </div>
        <button lq-button size="sm" (click)="openCreate()">Yeni deney</button>
      </header>

      @if (presets().length) {
        <section class="stack presets" aria-labelledby="presets-title">
          <h2 id="presets-title" class="presets__title">Önerilen deneyler</h2>
          <p class="muted small">Simülasyon raporunun önerdiği ilk deneyler, öncelik sırasıyla. Birincil metrik north-star, koruma metriği "ilgimi çekmedi" oranı.</p>
          @for (p of presets(); track p.key) {
            <article class="surface item">
              <div class="item__head">
                <strong>{{ p.name }}</strong>
                @if (p.existingStatus; as status) {
                  <span [class]="'pill pill--' + statusLabels[status].tone">{{ statusLabels[status].label }}</span>
                }
              </div>
              <p class="muted small">{{ p.hypothesis }}</p>
              <p class="small">
                @for (o of p.overrides; track o.key) { <span class="change">{{ o.label }}: {{ o.controlValue }} → <strong>{{ o.treatmentValue }}</strong></span> }
              </p>
              @if (p.existingExperimentId; as existingId) {
                <a class="link" [routerLink]="experimentPath(existingId)">Deneye git</a>
              } @else {
                <button lq-button size="sm" variant="secondary" (click)="usePreset(p)">Taslağı hazırla</button>
              }
            </article>
          }
        </section>
      }

      @if (error()) {
        <lq-empty-state icon="info" title="Deneyler yüklenemedi" [message]="error()" />
      } @else if (experiments(); as list) {
        @for (e of list; track e.id) {
          <a class="surface item" [routerLink]="experimentPath(e.id)">
            <div class="item__head">
              <strong>{{ e.name }}</strong>
              <span [class]="'pill pill--' + statusMeta(e).tone">{{ statusMeta(e).label }}</span>
            </div>
            <p class="muted small">{{ e.hypothesis }}</p>
            <p class="small">
              @for (o of e.overrides; track o.key) { <span class="change">{{ o.label }}: {{ o.controlValue }} → <strong>{{ o.treatmentValue }}</strong></span> }
            </p>
            <p class="muted small">
              Deneme payı %{{ share(e) }} ·
              {{ e.startedAt ? date(e.startedAt) + (e.endedAt ? ' – ' + date(e.endedAt) : ' başladı') : 'henüz başlamadı' }}
              @if (e.outcome === 'Adopted') { · <strong>üretime alındı</strong> }
              @if (e.outcome === 'Discarded') { · kapatıldı }
            </p>
          </a>
        } @empty {
          <lq-empty-state icon="target" title="Henüz deney yok"
            message="Yukarıdaki önerilen deneylerden biriyle başlayabilirsin." />
        }
      } @else {
        <lq-skeleton [height]="120" />
      }
    </section>

    <lq-sheet title="Yeni deney" [(open)]="createOpen">
      <form class="stack" (ngSubmit)="create()">
        <div class="field">
          <label for="exp-name">Ad</label>
          <input id="exp-name" class="input" name="name" maxlength="100" [(ngModel)]="name" placeholder="Örn. Sevdiğini tekrarla 0,8" />
        </div>
        <div class="field">
          <label for="exp-hypothesis">Hipotez</label>
          <textarea id="exp-hypothesis" class="input" name="hypothesis" rows="2" maxlength="500" [(ngModel)]="hypothesis"
                    placeholder="Ne olmasını bekliyorsun ve neden?"></textarea>
        </div>
        <div class="field">
          <span class="field__label">Deneme grubunda değişecek ağırlıklar</span>
          @for (row of rows(); track row.field.key) {
            <div class="override">
              <span class="override__label">{{ row.field.label }}</span>
              <span class="muted small">{{ row.field.value }} →</span>
              <input class="input override__value" type="number" [name]="row.field.key" [min]="row.field.min" [max]="row.field.max"
                     [step]="row.field.step" [ngModel]="row.value" (ngModelChange)="setValue(row, $event)" />
              <button type="button" class="link" (click)="removeRow(row)" aria-label="Kaldır">Kaldır</button>
            </div>
          }
          <select class="input" aria-label="Ağırlık ekle" (change)="addRow($event)">
            <option value="">+ Ağırlık ekle</option>
            @for (f of availableFields(); track f.key) { <option [value]="f.key">{{ f.label }} (şu an {{ f.value }})</option> }
          </select>
        </div>
        <div class="field">
          <label for="exp-share">Deneme grubuna düşen kullanıcı payı</label>
          <select id="exp-share" class="input" name="share" [(ngModel)]="shareValue">
            <option [ngValue]="0.2">%20 (temkinli)</option>
            <option [ngValue]="0.5">%50 (en hızlı sonuç)</option>
          </select>
        </div>
        @if (createError()) { <p class="field__error" role="alert">{{ createError() }}</p> }
        <button lq-button type="submit" [block]="true" [loading]="busy()"
                [disabled]="!name.trim() || !hypothesis.trim() || !rows().length">Taslak olarak oluştur</button>
        <p class="muted small">Deney taslak olarak oluşur; detay ekranından başlatırsın.</p>
      </form>
    </lq-sheet>
  `,
  styles: `
    .section { display: flex; flex-direction: column; gap: var(--space-4); }
    .head { display: flex; justify-content: space-between; align-items: flex-start; gap: 12px; }
    .small { font-size: var(--fs-sm); }
    .item { padding: var(--space-4); display: flex; flex-direction: column; gap: 6px; color: inherit; text-decoration: none; }
    .item:hover { border-color: var(--ink-3); }
    .item__head { display: flex; justify-content: space-between; gap: 8px; align-items: flex-start; }
    .presets { gap: var(--space-3); }
    .presets__title { font-size: var(--fs-lg); margin: 0; }
    .item .link { align-self: flex-start; text-decoration: none; }
    .change { display: inline-block; margin-right: 10px; }
    .override { display: grid; grid-template-columns: 1fr auto 6rem auto; align-items: center; gap: 8px; }
    .override__label { font-weight: 700; font-size: var(--fs-sm); }
    .override__value { min-height: 40px; padding: 8px 10px; }
    select.input { appearance: auto; }
    .link { background: none; border: 0; padding: 0; color: var(--primary-text); font-weight: 800; cursor: pointer; font-size: var(--fs-sm); }
  `,
})
export class AdminExperimentsPage {
  protected readonly experimentPath = experimentPath;
  private readonly api = inject(AdminApi);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  protected readonly statusLabels = EXPERIMENT_STATUS_LABELS;
  protected readonly experiments = signal<Experiment[] | null>(null);
  protected readonly presets = signal<ExperimentPreset[]>([]);
  protected readonly error = signal<string | null>(null);
  protected readonly createOpen = signal(false);
  protected readonly createError = signal<string | null>(null);
  protected readonly busy = signal(false);
  protected readonly fields = signal<WeightField[]>([]);
  protected readonly rows = signal<OverrideRow[]>([]);
  protected name = '';
  protected hypothesis = '';
  protected shareValue = 0.5;

  protected readonly availableFields = computed(() => {
    const used = new Set(this.rows().map((r) => r.field.key));
    return this.fields().filter((f) => !used.has(f.key));
  });

  constructor() {
    this.load();
  }

  protected statusMeta(e: Experiment) {
    return EXPERIMENT_STATUS_LABELS[e.status];
  }

  protected share(e: Experiment): number {
    return Math.round(e.treatmentShare * 100);
  }

  protected date(value: string): string {
    return formatDate(value, { day: 'numeric', month: 'short' });
  }

  protected openCreate(preset?: ExperimentPreset): void {
    this.name = preset?.name ?? '';
    this.hypothesis = preset?.hypothesis ?? '';
    this.shareValue = preset?.treatmentShare ?? 0.5;
    this.rows.set([]);
    this.createError.set(null);
    this.createOpen.set(true);

    const fillRows = (fields: WeightField[]) => {
      if (!preset) return;
      this.rows.set(preset.overrides.flatMap((o) => {
        const field = fields.find((f) => f.key === o.key);
        return field ? [{ field, value: o.treatmentValue }] : [];
      }));
    };
    if (this.fields().length) fillRows(this.fields());
    else this.api.weights().subscribe({ next: (w) => { this.fields.set(w.fields); fillRows(w.fields); } });
  }

  /** Hazır deney form olarak açılır; admin gözden geçirip taslağı kendisi oluşturur. */
  protected usePreset(preset: ExperimentPreset): void {
    this.openCreate(preset);
  }

  protected addRow(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const field = this.fields().find((f) => f.key === select.value);
    if (field) this.rows.update((rows) => [...rows, { field, value: field.value }]);
    select.value = '';
  }

  protected removeRow(row: OverrideRow): void {
    this.rows.update((rows) => rows.filter((r) => r !== row));
  }

  protected setValue(row: OverrideRow, value: number | string): void {
    const n = Number(value);
    if (!Number.isNaN(n)) this.rows.update((rows) => rows.map((r) => (r === row ? { ...r, value: n } : r)));
  }

  protected create(): void {
    this.busy.set(true);
    this.createError.set(null);
    this.api.createExperiment({
      name: this.name.trim(),
      hypothesis: this.hypothesis.trim(),
      treatmentOverrides: Object.fromEntries(this.rows().map((r) => [r.field.key, r.value])),
      treatmentShare: this.shareValue,
    }).subscribe({
      next: (experiment) => {
        this.busy.set(false);
        this.createOpen.set(false);
        this.toast.success('Deney taslağı oluşturuldu.');
        void this.router.navigate(experimentPath(experiment.id));
      },
      error: (err: unknown) => {
        this.busy.set(false);
        this.createError.set(parseApiErrors(err).map((e) => e.message).join(' '));
      },
    });
  }

  private load(): void {
    this.api.experimentPresets().subscribe({ next: (list) => this.presets.set(list), error: () => this.presets.set([]) });
    this.api.experiments().subscribe({
      next: (list) => this.experiments.set(list),
      error: (err: unknown) => this.error.set(firstErrorMessage(err)),
    });
  }
}
