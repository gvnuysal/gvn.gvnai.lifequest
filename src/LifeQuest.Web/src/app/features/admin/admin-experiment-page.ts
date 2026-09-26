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
      <a class="back" [routerLink]="paths.admin.experiments"><lq-icon name="arrow-left" [size]="18" /> Deneyler</a>

      @if (error()) {
        <lq-empty-state icon="info" title="Deney yüklenemedi" [message]="error()" />
      } @else if (detail(); as d) {
        <header class="stack">
          <h1>{{ d.experiment.name }}</h1>
          <div class="row wrap">
            <span [class]="'pill pill--' + statusMeta().tone">{{ statusMeta().label }}</span>
            <span class="pill">Deneme payı %{{ share() }}</span>
            @if (d.experiment.outcome === 'Adopted') { <span class="pill pill--success">Üretime alındı</span> }
            @if (d.experiment.outcome === 'Discarded') { <span class="pill">Kapatıldı</span> }
          </div>
          <p class="muted">{{ d.experiment.hypothesis }}</p>
        </header>

        <section class="surface card">
          <h2 class="section-title">Değişen ağırlıklar</h2>
          <table>
            <thead><tr><th>Ağırlık</th><th>Kontrol</th><th>Deneme</th></tr></thead>
            <tbody>
              @for (o of d.experiment.overrides; track o.key) {
                <tr><td>{{ o.label }}</td><td>{{ o.controlValue }}</td><td><strong>{{ o.treatmentValue }}</strong></td></tr>
              }
            </tbody>
          </table>
          <p class="muted small">Kontrol değeri şu anki üretim ağırlığıdır.</p>
        </section>

        @if (d.results; as r) {
          <section class="surface card verdict" [attr.data-tone]="verdict()!.tone">
            <span class="eyebrow">Sonuç · {{ r.weeks }} hafta</span>
            <p class="verdict__label">{{ verdict()!.label }}</p>
            <p class="small">{{ verdictHint() }}</p>
            @if (r.guardrailBreached) {
              <p class="guardrail small" role="alert">
                <strong>Koruma metriği aşıldı:</strong> Deneme grubunda "ilgimi çekmedi" oranı
                {{ pct(r.control.notInterestedRate) }} → {{ pct(r.treatment.notInterestedRate) }}
                (izin verilen artış en fazla {{ pct(r.guardrailMaxIncrease) }} puan). North-star artsa da üretime almadan önce incele.
              </p>
            }
            <div class="ci" [attr.aria-label]="'North-star farkı ' + r.northStar.difference + ', güven aralığı ' + r.northStar.ciLow + ' ile ' + r.northStar.ciHigh">
              <span class="ci__zero" [style.left.%]="ciPosition(0)"></span>
              <span class="ci__range" [style.left.%]="ciPosition(r.northStar.ciLow)"
                    [style.width.%]="ciPosition(r.northStar.ciHigh) - ciPosition(r.northStar.ciLow)"></span>
              <span class="ci__point" [style.left.%]="ciPosition(r.northStar.difference)"></span>
            </div>
            <p class="muted small">
              North-star farkı {{ signed(r.northStar.difference) }} (%95 güven aralığı {{ signed(r.northStar.ciLow) }} … {{ signed(r.northStar.ciHigh) }})
              @if (r.northStar.relativeLift !== null) { · göreli {{ signed(r.northStar.relativeLift * 100, 1) }}% }
            </p>
          </section>

          <section class="surface card">
            <h2 class="section-title">Gruplar</h2>
            <table>
              <thead><tr><th>Metrik</th><th>Kontrol</th><th>Deneme</th></tr></thead>
              <tbody>
                @for (m of metrics(); track m.label) {
                  <tr [title]="m.hint ?? ''"><td>{{ m.label }}</td><td>{{ m.control }}</td><td>{{ m.treatment }}</td></tr>
                }
              </tbody>
            </table>
            <p class="muted small">Tamamlamalar, görevin önerildiği gruba sayılır. Koruma metriği: "ilgimi çekmedi" oranı.</p>
          </section>
        } @else {
          <p class="muted">Deney başlatıldığında sonuçlar burada görünür.</p>
        }

        <div class="actions">
          @switch (d.experiment.status) {
            @case ('Draft') { <button lq-button (click)="open('start')">Deneyi başlat</button> }
            @case ('Running') { <button lq-button variant="soft" (click)="open('stop')">Deneyi durdur</button> }
            @case ('Stopped') {
              @if (d.experiment.outcome === 'None') {
                <button lq-button (click)="open('adopt')">Deneme ayarını üretime al</button>
                <button lq-button variant="soft" (click)="open('discard')">Kapat (üretimi değiştirme)</button>
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
            <label for="exp-reason">Not (denetim kaydına yazılır)</label>
            <textarea id="exp-reason" class="input" rows="2" maxlength="500" #reason></textarea>
          </div>
          <button lq-button [block]="true" [loading]="busy()" (click)="run(action, reason.value)">Onayla</button>
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
  protected readonly paths = APP_PATHS;
  readonly id = input.required<string>();

  private readonly api = inject(AdminApi);
  private readonly toast = inject(ToastService);

  protected readonly detail = signal<ExperimentDetail | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly pending = signal<ExperimentAction | null>(null);
  protected readonly busy = signal(false);

  protected readonly pct = (v: number) => `%${Math.round(v * 100)}`;

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
    return [
      row('Kullanıcı', (v) => `${v.users}`),
      row('North-star / hafta', (v) => `${v.northStar.toFixed(2)} ±${v.northStarStandardError.toFixed(2)}`, 'Kullanıcı başına haftalık anlamlı deneyim ± standart hata'),
      row('Öneri', (v) => `${v.offered}`),
      row('Kabul oranı', (v) => pct(v.acceptanceRate)),
      row('Tamamlama oranı', (v) => pct(v.completionRate)),
      row('Keşif kabulü', (v) => pct(v.explorationAcceptanceRate)),
      row('"İlgimi çekmedi"', (v) => pct(v.notInterestedRate), 'Koruma metriği: düşük olması iyi'),
      row('Ortalama puan', (v) => v.averageRating?.toFixed(1) ?? '–'),
    ];
  });

  protected readonly actionTitle = computed(() => ({
    start: 'Deneyi başlat', stop: 'Deneyi durdur', adopt: 'Deneme ayarını üretime al', discard: 'Deneyi kapat',
  })[this.pending() ?? 'start']);

  protected readonly actionHint = computed(() => ({
    start: `Kullanıcıların %${this.share()} kadarı hemen deneme ağırlıklarıyla öneri almaya başlar. Aynı anda yalnızca bir deney çalışabilir.`,
    stop: 'Tüm kullanıcılar üretim ağırlıklarına döner. Sonuçlar korunur; sonra kazananı uygulayabilir veya kapatabilirsin.',
    adopt: 'Deneme ağırlıkları "Öneri ayarları"na yazılır ve herkes için geçerli olur. İşlem denetim kaydına düşer.',
    discard: 'Üretim ağırlıkları değişmez; deney kapatılır.',
  })[this.pending() ?? 'start']);

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
        this.toast.success('Kaydedildi.');
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
