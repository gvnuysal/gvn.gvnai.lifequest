import { ChangeDetectionStrategy, Component, computed, inject, input, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { AdminApi, CatalogApi } from '../../core/api/api-clients';
import {
  AdminTemplate,
  CostBand,
  DayPart,
  Difficulty,
  Interest,
  LifeCategory,
  PhysicalEffort,
  QuestType,
  SafetyLevel,
  TemplateInput,
} from '../../core/api/models';
import { firstErrorMessage, parseApiErrors } from '../../core/http/api-error';
import {
  CATEGORIES,
  CATEGORY_ORDER,
  COST_LABELS,
  COST_ORDER,
  DIFFICULTY_LABELS,
  EFFORT_LABELS,
  QUEST_TYPE_LABELS,
} from '../../core/labels/labels';
import { ToastService } from '../../core/state/toast.service';
import { Button } from '../../ui/button';
import { Chip } from '../../ui/chip';
import { Icon } from '../../ui/icon';
import { Sheet } from '../../ui/sheet';
import { EmptyState, Skeleton } from '../../ui/states';
import { DAY_PART_OPTIONS, SAFETY_LABELS } from './admin-labels';

type SafetyAction = { safety: SafetyLevel; title: string; needsNote: boolean };

/**
 * Template oluşturma ve düzenleme. Editoryal kurallar kaydı engellemez: ihlal varsa template "inceleme
 * bekliyor" olarak kaydedilir ve admin onaylayana kadar önerilmez. Kod sonradan değiştirilemez.
 */
@Component({
  selector: 'lq-admin-template-page',
  imports: [ReactiveFormsModule, RouterLink, Button, Chip, Icon, Sheet, EmptyState, Skeleton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="section">
      <a class="back" routerLink="/yonetim/katalog"><lq-icon name="arrow-left" [size]="18" /> Katalog</a>

      @if (loadError()) {
        <lq-empty-state icon="info" title="Template yüklenemedi" [message]="loadError()" />
      } @else if (ready()) {
        <header class="stack">
          <h1>{{ template() ? template()!.title : 'Yeni template' }}</h1>
          @if (template(); as t) {
            <div class="row wrap">
              <span [class]="'pill pill--' + safetyMeta(t.safety).tone">{{ safetyMeta(t.safety).label }}</span>
              @if (!t.isActive) { <span class="pill">Pasif</span> }
              <span class="pill">v{{ t.version }}</span>
              <span class="pill">{{ t.source === 'Admin' ? 'Admin tarafından yönetiliyor' : 'Seed verisiyle senkron' }}</span>
            </div>
          }
        </header>

        @if (template(); as t) {
          <section class="surface card" aria-labelledby="decision-title">
            <h2 id="decision-title" class="section-title">Yayın durumu</h2>
            @if (t.violations.length) {
              <ul class="violations">
                @for (v of t.violations; track v) { <li>{{ v }}</li> }
              </ul>
            } @else {
              <p class="ok small">Editoryal güvenlik kurallarının hepsine uyuyor.</p>
            }
            <div class="actions">
              @if (t.safety !== 'Safe') {
                <button lq-button size="sm" (click)="openSafety('Safe')">Onayla ve yayınla</button>
              }
              @if (t.safety === 'Safe') {
                <button lq-button variant="soft" size="sm" (click)="openSafety('NeedsReview')">İncelemeye al</button>
              }
              @if (t.safety !== 'Blocked') {
                <button lq-button variant="danger" size="sm" (click)="openSafety('Blocked')">Engelle</button>
              }
              <button lq-button variant="soft" size="sm" [loading]="busy()" (click)="toggleActive(t)">
                {{ t.isActive ? 'Pasifleştir' : 'Etkinleştir' }}
              </button>
            </div>
            @if (t.source === 'Seed') {
              <p class="muted small">Düzenlediğinde veya karar verdiğinde bu template seed verisiyle senkronlanmayı bırakır.</p>
            }
          </section>
        }

        <form class="stack form" [formGroup]="form" (ngSubmit)="save()">
          <div class="field">
            <label for="code">Kod</label>
            <input id="code" class="input" formControlName="code" autocomplete="off" placeholder="ornek-kod"
                   [attr.aria-invalid]="fieldError('code') ? true : null" />
            <span class="field__hint">Küçük harf, rakam ve tire. Kaydettikten sonra değiştirilemez.</span>
            @if (fieldError('code'); as e) { <span class="field__error">{{ e }}</span> }
          </div>
          <div class="field">
            <label for="title">Başlık</label>
            <input id="title" class="input" formControlName="title" maxlength="150" />
            <span class="field__hint">{{ form.controls.title.value.length }} / 80 önerilen</span>
            @if (fieldError('title'); as e) { <span class="field__error">{{ e }}</span> }
          </div>
          <div class="field">
            <label for="description">Açıklama</label>
            <textarea id="description" class="input" rows="3" formControlName="description" maxlength="1000"></textarea>
            <span class="field__hint">{{ form.controls.description.value.length }} karakter · 30–300 önerilen</span>
            @if (fieldError('description'); as e) { <span class="field__error">{{ e }}</span> }
          </div>

          <div class="grid">
            <div class="field">
              <label for="category">Kategori</label>
              <select id="category" class="input" formControlName="category">
                @for (c of categories; track c) { <option [value]="c">{{ catLabel(c) }}</option> }
              </select>
            </div>
            <div class="field">
              <label for="secondary">İkincil kategori</label>
              <select id="secondary" class="input" formControlName="secondaryCategory">
                <option [ngValue]="null">Yok</option>
                @for (c of categories; track c) { <option [ngValue]="c">{{ catLabel(c) }}</option> }
              </select>
              @if (fieldError('secondaryCategory'); as e) { <span class="field__error">{{ e }}</span> }
            </div>
            <div class="field">
              <label for="type">Tür</label>
              <select id="type" class="input" formControlName="type">
                @for (t of types; track t) { <option [value]="t">{{ typeLabels[t] }}</option> }
              </select>
            </div>
            <div class="field">
              <label for="difficulty">Zorluk</label>
              <select id="difficulty" class="input" formControlName="difficulty">
                @for (d of difficulties; track d) { <option [value]="d">{{ difficultyLabels[d] }}</option> }
              </select>
            </div>
            <div class="field">
              <label for="min">En kısa (dk)</label>
              <input id="min" class="input" type="number" min="1" formControlName="minMinutes" />
            </div>
            <div class="field">
              <label for="max">En uzun (dk)</label>
              <input id="max" class="input" type="number" min="1" formControlName="maxMinutes" />
              @if (fieldError('maxMinutes'); as e) { <span class="field__error">{{ e }}</span> }
            </div>
            <div class="field">
              <label for="cost">Maliyet</label>
              <select id="cost" class="input" formControlName="cost">
                @for (c of costs; track c) { <option [value]="c">{{ costLabels[c].label }}</option> }
              </select>
            </div>
            <div class="field">
              <label for="effort">Fiziksel efor</label>
              <select id="effort" class="input" formControlName="effort">
                @for (e of efforts; track e) { <option [value]="e">{{ effortLabels[e].label }}</option> }
              </select>
            </div>
            <div class="field">
              <label for="cooldown">Tekrar aralığı (gün)</label>
              <input id="cooldown" class="input" type="number" min="0" max="365" formControlName="cooldownDays" />
            </div>
            <div class="field">
              <label for="risk">Risk skoru (0–1)</label>
              <input id="risk" class="input" type="number" min="0" max="1" step="0.05" formControlName="riskScore" />
            </div>
          </div>

          <div class="field">
            <span class="field__label">Gün dilimleri</span>
            <div class="row wrap">
              @for (p of dayParts; track p.value) {
                <button lq-chip [selected]="hasDayPart(p.value)" (click)="toggleDayPart(p.value)">{{ p.label }}</button>
              }
            </div>
            @if (fieldError('dayParts'); as e) { <span class="field__error">{{ e }}</span> }
          </div>

          <div class="field">
            <span class="field__label">İlgi alanları</span>
            <div class="row wrap">
              @for (i of selectedInterests(); track i.id) {
                <button lq-chip [selected]="true" (click)="toggleInterest(i.id)">{{ i.name }} <lq-icon name="x" [size]="14" /></button>
              } @empty {
                <span class="muted small">En az bir ilgi alanı seç.</span>
              }
            </div>
            <select class="input" aria-label="İlgi alanı ekle" (change)="addInterest($event)">
              <option value="">+ İlgi alanı ekle</option>
              @for (i of availableInterests(); track i.id) { <option [value]="i.id">{{ i.name }} · {{ catLabel(i.category) }}</option> }
            </select>
            @if (fieldError('interestIds'); as e) { <span class="field__error">{{ e }}</span> }
          </div>

          <div class="toggles">
            <label class="toggle"><input type="checkbox" formControlName="isOutdoor" /> Açık havada yapılır</label>
            <label class="toggle"><input type="checkbox" formControlName="requiresCity" /> Şehir bilgisi gerektirir</label>
            <label class="toggle"><input type="checkbox" formControlName="isStarter" /> Onboarding başlangıç kartı</label>
          </div>

          @if (check(); as c) {
            <section class="check" [class.check--ok]="!c.violations.length" aria-live="polite">
              @if (c.violations.length) {
                <strong>Kaydedilirse inceleme bekleyecek:</strong>
                <ul>@for (v of c.violations; track v) { <li>{{ v }}</li> }</ul>
              } @else {
                <strong>Kurallara uygun; kaydedilince hemen yayına girer.</strong>
              }
            </section>
          }

          @if (saveError()) { <p class="field__error" role="alert">{{ saveError() }}</p> }

          <div class="actions">
            <button lq-button type="button" variant="soft" [loading]="checking()" (click)="runCheck()">Kontrol et</button>
            <button lq-button type="submit" [loading]="busy()">{{ template() ? 'Değişiklikleri kaydet' : 'Oluştur' }}</button>
          </div>
        </form>
      } @else {
        <lq-skeleton [height]="120" />
        <lq-skeleton [height]="320" />
      }
    </section>

    <lq-sheet [title]="safetyAction()?.title ?? ''" [open]="!!safetyAction()" (openChange)="$event || safetyAction.set(null)">
      @if (safetyAction(); as a) {
        <div class="stack">
          @if (a.safety === 'Safe' && template()?.violations?.length) {
            <p class="muted">Bu template kural ihlali içeriyor. Onaylarsan kullanıcılara önerilmeye başlar.</p>
          } @else if (a.safety === 'Blocked') {
            <p class="muted">Engellenen template seed verisiyle de yeniden açılmaz.</p>
          } @else if (a.safety === 'NeedsReview') {
            <p class="muted">Template yeniden onaylanana kadar önerilmez.</p>
          } @else {
            <p class="muted">Template kullanıcılara önerilmeye başlar.</p>
          }
          <div class="field">
            <label for="note">Gerekçe{{ a.needsNote ? '' : ' (isteğe bağlı)' }}</label>
            <textarea id="note" class="input" rows="3" maxlength="500" #note></textarea>
          </div>
          @if (safetyError()) { <p class="field__error" role="alert">{{ safetyError() }}</p> }
          <button lq-button [block]="true" [variant]="a.safety === 'Blocked' ? 'danger' : 'primary'" [loading]="busy()"
                  (click)="decide(a, note.value)">Onayla</button>
        </div>
      }
    </lq-sheet>
  `,
  styles: `
    .section { display: flex; flex-direction: column; gap: var(--space-4); }
    .back { display: inline-flex; align-items: center; gap: 6px; font-weight: 800; color: var(--ink-2); text-decoration: none; width: fit-content; }
    .wrap { flex-wrap: wrap; }
    .small { font-size: var(--fs-sm); }
    .card { padding: var(--space-4); display: flex; flex-direction: column; gap: 12px; }
    .violations, .check ul { margin: 0; padding-left: 20px; }
    .violations { color: var(--xp-ink); font-weight: 700; font-size: var(--fs-sm); }
    .ok { color: var(--success); font-weight: 700; }
    .actions { display: flex; flex-wrap: wrap; gap: 8px; }
    .form { gap: var(--space-4); }
    .grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: var(--space-3); }
    select.input { appearance: auto; }
    .toggles { display: flex; flex-direction: column; gap: 10px; }
    .toggle { display: flex; align-items: center; gap: 10px; font-weight: 700; color: var(--ink-2); min-height: 32px; }
    .toggle input { width: 20px; height: 20px; accent-color: var(--primary); }
    .check { padding: 12px 14px; border-radius: var(--radius-md); background: var(--xp-soft); color: var(--xp-ink); font-size: var(--fs-sm); display: flex; flex-direction: column; gap: 6px; }
    .check--ok { background: var(--success-soft); color: var(--success); }
  `,
})
export class AdminTemplatePage implements OnInit {
  /** Rota parametresi; "yeni" rotasında tanımsızdır. */
  readonly id = input<string>();

  private readonly api = inject(AdminApi);
  private readonly catalogApi = inject(CatalogApi);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder).nonNullable;

  protected readonly categories = CATEGORY_ORDER;
  protected readonly types: QuestType[] = ['Daily', 'Weekly', 'Adventure', 'Epic'];
  protected readonly difficulties: Difficulty[] = ['Easy', 'Medium', 'Hard', 'Heroic'];
  protected readonly costs = COST_ORDER;
  protected readonly efforts: PhysicalEffort[] = ['None', 'Light', 'Moderate', 'Vigorous'];
  protected readonly dayParts = DAY_PART_OPTIONS;
  protected readonly typeLabels = QUEST_TYPE_LABELS;
  protected readonly difficultyLabels = DIFFICULTY_LABELS;
  protected readonly costLabels = COST_LABELS;
  protected readonly effortLabels = EFFORT_LABELS;

  protected readonly form = this.fb.group({
    code: ['', [Validators.required, Validators.pattern(/^[a-z0-9-]{3,64}$/)]],
    title: ['', Validators.required],
    description: ['', Validators.required],
    category: 'Learning' as LifeCategory,
    secondaryCategory: this.fb.control<LifeCategory | null>(null),
    type: 'Weekly' as QuestType,
    difficulty: 'Easy' as Difficulty,
    minMinutes: 30,
    maxMinutes: 60,
    cost: 'Free' as CostBand,
    dayParts: this.fb.control<DayPart[]>(['Morning', 'Afternoon', 'Evening']),
    requiresCity: false,
    isOutdoor: false,
    cooldownDays: 14,
    riskScore: 0,
    interestIds: this.fb.control<string[]>([]),
    effort: 'Light' as PhysicalEffort,
    isStarter: false,
  });

  protected readonly template = signal<AdminTemplate | null>(null);
  protected readonly interests = signal<Interest[]>([]);
  protected readonly ready = signal(false);
  protected readonly loadError = signal<string | null>(null);
  protected readonly busy = signal(false);
  protected readonly checking = signal(false);
  protected readonly check = signal<{ violations: string[] } | null>(null);
  protected readonly saveError = signal<string | null>(null);
  protected readonly fieldErrors = signal<Record<string, string>>({});
  protected readonly safetyAction = signal<SafetyAction | null>(null);
  protected readonly safetyError = signal<string | null>(null);
  private readonly selectedIds = signal<string[]>([]);

  protected readonly selectedInterests = computed(() => {
    const byId = new Map(this.interests().map((i) => [i.id, i]));
    return this.selectedIds().map((id) => byId.get(id)).filter((i): i is Interest => !!i);
  });

  protected readonly availableInterests = computed(() => {
    const selected = new Set(this.selectedIds());
    return this.interests().filter((i) => !selected.has(i.id));
  });

  ngOnInit(): void {
    const id = this.id();
    forkJoin({ interests: this.catalogApi.interests(), template: id ? this.api.template(id) : of(null) }).subscribe({
      next: ({ interests, template }) => {
        this.interests.set(interests);
        if (template) this.fill(template);
        this.ready.set(true);
      },
      error: (err: unknown) => this.loadError.set(firstErrorMessage(err)),
    });
  }

  protected catLabel(category: LifeCategory): string {
    return CATEGORIES[category].label;
  }

  protected safetyMeta(safety: SafetyLevel) {
    return SAFETY_LABELS[safety];
  }

  protected fieldError(name: string): string | null {
    const server = this.fieldErrors()[name];
    if (server) return server;
    const control = this.form.get(name);
    if (!control || !control.invalid || !control.touched) return null;
    if (name === 'code') return 'Kod 3-64 karakter; küçük harf, rakam ve tire içermeli.';
    return 'Bu alan gerekli.';
  }

  protected hasDayPart(part: DayPart): boolean {
    return this.form.controls.dayParts.value.includes(part);
  }

  protected toggleDayPart(part: DayPart): void {
    const parts = this.form.controls.dayParts.value;
    this.form.controls.dayParts.setValue(parts.includes(part) ? parts.filter((p) => p !== part) : [...parts, part]);
    this.check.set(null);
  }

  protected toggleInterest(id: string): void {
    this.setInterests(this.selectedIds().filter((i) => i !== id));
  }

  protected addInterest(event: Event): void {
    const select = event.target as HTMLSelectElement;
    if (select.value) this.setInterests([...this.selectedIds(), select.value]);
    select.value = '';
  }

  protected runCheck(): void {
    this.checking.set(true);
    this.api.validateTemplate(this.input()).subscribe({
      next: (result) => {
        this.checking.set(false);
        this.fieldErrors.set({});
        this.check.set(result);
      },
      error: (err: unknown) => {
        this.checking.set(false);
        this.showErrors(err);
      },
    });
  }

  protected save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    const current = this.template();
    this.busy.set(true);
    this.saveError.set(null);
    const request = current
      ? this.api.updateTemplate(current.id, current.version, this.input())
      : this.api.createTemplate(this.input());

    request.subscribe({
      next: (saved) => {
        this.busy.set(false);
        this.toast.success(saved.safety === 'Safe' ? 'Template kaydedildi ve yayında.' : 'Template kaydedildi; inceleme bekliyor.');
        if (current) this.fill(saved);
        else void this.router.navigate(['/yonetim/katalog', saved.id]);
      },
      error: (err: unknown) => {
        this.busy.set(false);
        this.showErrors(err);
      },
    });
  }

  protected openSafety(safety: SafetyLevel): void {
    const violations = this.template()?.violations.length ?? 0;
    const titles: Record<SafetyLevel, string> = { Safe: 'Onayla ve yayınla', NeedsReview: 'İncelemeye al', Blocked: 'Template\'i engelle' };
    this.safetyError.set(null);
    this.safetyAction.set({ safety, title: titles[safety], needsNote: safety === 'Blocked' || (safety === 'Safe' && violations > 0) });
  }

  protected decide(action: SafetyAction, note: string): void {
    const t = this.template();
    if (!t) return;
    if (action.needsNote && !note.trim()) {
      this.safetyError.set('Bu karar için gerekçe yazmalısın.');
      return;
    }

    this.busy.set(true);
    this.api.setTemplateSafety(t.id, action.safety, note.trim() || null).subscribe({
      next: (saved) => {
        this.busy.set(false);
        this.safetyAction.set(null);
        this.fill(saved);
        this.toast.success('Karar kaydedildi.');
      },
      error: (err: unknown) => {
        this.busy.set(false);
        this.safetyError.set(firstErrorMessage(err));
      },
    });
  }

  protected toggleActive(t: AdminTemplate): void {
    this.busy.set(true);
    this.api.setTemplateActive(t.id, !t.isActive).subscribe({
      next: (saved) => {
        this.busy.set(false);
        this.fill(saved);
        this.toast.success(saved.isActive ? 'Template etkinleştirildi.' : 'Template pasifleştirildi.');
      },
      error: (err: unknown) => {
        this.busy.set(false);
        this.toast.error(firstErrorMessage(err));
      },
    });
  }

  private fill(t: AdminTemplate): void {
    this.template.set(t);
    this.form.reset({
      code: t.code,
      title: t.title,
      description: t.description,
      category: t.category,
      secondaryCategory: t.secondaryCategory,
      type: t.type,
      difficulty: t.difficulty,
      minMinutes: t.minMinutes,
      maxMinutes: t.maxMinutes,
      cost: t.cost,
      dayParts: t.dayParts,
      requiresCity: t.requiresCity,
      isOutdoor: t.isOutdoor,
      cooldownDays: t.cooldownDays,
      riskScore: t.riskScore,
      interestIds: t.interestIds,
      effort: t.effort,
      isStarter: t.isStarter,
    });
    this.form.controls.code.disable();
    this.selectedIds.set(t.interestIds);
    this.check.set(null);
    this.fieldErrors.set({});
  }

  private setInterests(ids: string[]): void {
    this.selectedIds.set(ids);
    this.form.controls.interestIds.setValue(ids);
    this.check.set(null);
  }

  private input(): TemplateInput {
    const v = this.form.getRawValue();
    return { ...v, minMinutes: Number(v.minMinutes), maxMinutes: Number(v.maxMinutes), cooldownDays: Number(v.cooldownDays), riskScore: Number(v.riskScore) };
  }

  /** Doğrulama hataları ilgili alanın altında, diğerleri formun sonunda gösterilir. */
  private showErrors(err: unknown): void {
    const errors = parseApiErrors(err);
    const fields: Record<string, string> = {};
    const general: string[] = [];
    for (const e of errors) {
      const field = e.code.replace(/^Template\./, '');
      const name = field.charAt(0).toLowerCase() + field.slice(1);
      if (e.type === 'Validation' && this.form.get(name)) fields[name] = e.message;
      else general.push(e.message);
    }
    this.fieldErrors.set(fields);
    this.saveError.set(general[0] ?? (Object.keys(fields).length ? 'Formdaki hataları düzelt.' : null));
  }
}
