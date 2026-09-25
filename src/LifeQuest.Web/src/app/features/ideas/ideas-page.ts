import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { IdeasApi } from '../../core/api/api-clients';
import { CostBand, IdeaStatus, LifeCategory, MyIdea } from '../../core/api/models';
import { firstErrorMessage, parseApiErrors } from '../../core/http/api-error';
import { formatDate } from '../../core/labels/format';
import { CATEGORIES, CATEGORY_ORDER, COST_LABELS, COST_ORDER } from '../../core/labels/labels';
import { ToastService } from '../../core/state/toast.service';
import { Button } from '../../ui/button';
import { Chip } from '../../ui/chip';
import { EmptyState, Skeleton } from '../../ui/states';

const STATUS: Record<IdeaStatus, { label: string; tone: string }> = {
  Pending: { label: 'İnceleniyor', tone: 'warning' },
  Accepted: { label: 'Kataloğa eklendi', tone: 'success' },
  Rejected: { label: 'Eklenmedi', tone: '' },
};

/**
 * Topluluk fikirleri: kullanıcı yaşamak istediği bir deneyimi önerir, ekip inceler. Bağlantı ve iletişim bilgisi
 * kabul edilmez; kabul edilen fikir kimlik bilgisi taşımadan kataloğa girer. XP ödülü yoktur.
 */
@Component({
  selector: 'lq-ideas-page',
  imports: [ReactiveFormsModule, Button, Chip, EmptyState, Skeleton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <header class="stack">
        <h1>Bir deneyim öner</h1>
        <p class="muted">
          Başkalarının da sevebileceği küçük bir gerçek hayat deneyimi mi var? Paylaş; ekibimiz inceleyip güvenlik
          kurallarına uygunsa kataloğa ekler. Kimliğin deneyimle birlikte paylaşılmaz.
        </p>
      </header>

      <form class="surface card stack" [formGroup]="form" (ngSubmit)="submit()">
        <div class="field">
          <label for="idea-title">Başlık</label>
          <input id="idea-title" class="input" formControlName="title" maxlength="80" placeholder="Örn. Mahalle kütüphanesi turu" />
          <span class="field__hint">{{ form.controls.title.value.length }} / 80</span>
        </div>
        <div class="field">
          <label for="idea-description">Ne yapılıyor?</label>
          <textarea id="idea-description" class="input" rows="3" formControlName="description" maxlength="300"
                    placeholder="Deneyimi kendi cümlelerinle anlat. Bağlantı, e-posta veya telefon ekleme."></textarea>
          <span class="field__hint">{{ form.controls.description.value.length }} / 300 · en az 30 karakter</span>
        </div>
        <div class="field">
          <span class="field__label">Alan</span>
          <div class="chips">
            @for (c of categories; track c) {
              <button lq-chip [selected]="form.controls.category.value === c" (click)="form.controls.category.setValue(c)">{{ catLabel(c) }}</button>
            }
          </div>
        </div>
        <div class="grid">
          <div class="field">
            <label for="idea-minutes">Yaklaşık süre (dk)</label>
            <input id="idea-minutes" class="input" type="number" min="5" max="600" formControlName="minutes" />
          </div>
          <div class="field">
            <label for="idea-cost">Maliyet</label>
            <select id="idea-cost" class="input" formControlName="cost">
              @for (c of costs; track c) { <option [value]="c">{{ costLabel(c) }}</option> }
            </select>
          </div>
        </div>
        <label class="toggle"><input type="checkbox" formControlName="isOutdoor" /> Açık havada yapılır</label>
        @if (error()) { <p class="field__error" role="alert">{{ error() }}</p> }
        <button lq-button type="submit" [block]="true" [loading]="busy()" [disabled]="form.invalid">Fikri gönder</button>
      </form>

      <section class="stack">
        <h2 class="section-title">Fikirlerim</h2>
        @if (ideas(); as list) {
          @for (idea of list; track idea.id) {
            <article class="surface idea">
              <div class="idea__head">
                <strong>{{ idea.title }}</strong>
                <span [class]="'pill pill--' + status(idea).tone">{{ status(idea).label }}</span>
              </div>
              <p class="muted small">{{ catLabel(idea.category) }} · {{ date(idea.submittedAt) }}</p>
              @if (idea.reviewNote) { <p class="note small">{{ idea.reviewNote }}</p> }
              @if (idea.status === 'Pending') {
                <button lq-button variant="ghost" size="sm" (click)="withdraw(idea)">Geri çek</button>
              }
            </article>
          } @empty {
            <lq-empty-state icon="sparkles" title="Henüz fikir göndermedin" message="İlk fikrin kataloğa eklenen ilk topluluk deneyimi olabilir." />
          }
        } @else {
          <lq-skeleton [height]="80" />
        }
      </section>
    </div>
  `,
  styles: `
    .small { font-size: var(--fs-sm); }
    .card { padding: var(--space-4); }
    .chips { display: flex; flex-wrap: wrap; gap: 6px; }
    .grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: var(--space-3); }
    select.input { appearance: auto; }
    .toggle { display: flex; align-items: center; gap: 10px; font-weight: 700; color: var(--ink-2); }
    .toggle input { width: 20px; height: 20px; accent-color: var(--primary); }
    .idea { padding: 12px 14px; display: flex; flex-direction: column; gap: 6px; align-items: flex-start; }
    .idea__head { display: flex; justify-content: space-between; gap: 8px; width: 100%; align-items: flex-start; }
    .note { padding: 8px 10px; border-radius: var(--radius-md); background: var(--surface-2); color: var(--ink-2); width: 100%; }
  `,
})
export class IdeasPage {
  private readonly api = inject(IdeasApi);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder).nonNullable;

  protected readonly categories = CATEGORY_ORDER;
  protected readonly costs = COST_ORDER;
  protected readonly form = this.fb.group({
    title: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(80)]],
    description: ['', [Validators.required, Validators.minLength(30), Validators.maxLength(300)]],
    category: 'Explorer' as LifeCategory,
    minutes: [45, [Validators.required, Validators.min(5), Validators.max(600)]],
    cost: 'Free' as CostBand,
    isOutdoor: false,
  });

  protected readonly ideas = signal<MyIdea[] | null>(null);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.load();
  }

  protected catLabel(category: LifeCategory): string {
    return CATEGORIES[category].label;
  }

  protected costLabel(cost: CostBand): string {
    return COST_LABELS[cost].label;
  }

  protected status(idea: MyIdea) {
    return STATUS[idea.status];
  }

  protected date(value: string): string {
    return formatDate(value, { day: 'numeric', month: 'short' });
  }

  protected submit(): void {
    if (this.form.invalid) return;
    this.busy.set(true);
    this.error.set(null);
    const v = this.form.getRawValue();
    this.api.submit({ ...v, minutes: Number(v.minutes) }).subscribe({
      next: (idea) => {
        this.busy.set(false);
        this.ideas.update((list) => [idea, ...(list ?? [])]);
        this.form.reset();
        this.toast.success('Teşekkürler! Fikrin incelemeye alındı.');
      },
      error: (err: unknown) => {
        this.busy.set(false);
        this.error.set(parseApiErrors(err)[0]?.message ?? null);
      },
    });
  }

  protected withdraw(idea: MyIdea): void {
    this.api.withdraw(idea.id).subscribe({
      next: () => this.ideas.update((list) => list?.filter((i) => i.id !== idea.id) ?? null),
      error: (err: unknown) => this.toast.error(firstErrorMessage(err)),
    });
  }

  private load(): void {
    this.api.mine().subscribe({ next: (list) => this.ideas.set(list), error: () => this.ideas.set([]) });
  }
}
