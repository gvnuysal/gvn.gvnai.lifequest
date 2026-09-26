import { option, t } from '../../core/i18n/i18n';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, Subject, switchMap } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AdminApi } from '../../core/api/api-clients';
import { AdminPlace, AdminTemplateListItem, LocalPlaceKind } from '../../core/api/models';
import { firstErrorMessage, parseApiErrors } from '../../core/http/api-error';
import { formatDate } from '../../core/labels/format';
import { ToastService } from '../../core/state/toast.service';
import { Button } from '../../ui/button';
import { Sheet } from '../../ui/sheet';
import { EmptyState, Skeleton } from '../../ui/states';

interface PickedTemplate {
  id: string;
  code: string;
  title: string;
}

/** datetime-local değeri (yerel saat) ↔ ISO (UTC). */
function toLocalInput(iso: string | null): string {
  if (!iso) return '';
  const d = new Date(iso);
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

/**
 * Şehir bazında mekân ve etkinlikler. Görevlere bağlanır: o şehirdeki kullanıcı görev detayında görür, yaklaşan
 * etkinliği olan görev önerilerde öne çıkar. Kullanıcıdan konum alınmaz; eşleşme profildeki şehir adıyla yapılır.
 */
@Component({
  selector: 'lq-admin-places-page',
  imports: [FormsModule, Button, Sheet, EmptyState, Skeleton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="section">
      <header class="head">
        <div class="stack">
          <h1>{{ t().adminPlaces.title }}</h1>
          <p class="muted">{{ t().adminPlaces.lead }}</p>
        </div>
        <button lq-button size="sm" (click)="openEditor()">{{ t().adminPlaces.add }}</button>
      </header>

      <input class="input" type="search" [placeholder]="t().adminPlaces.filter" [attr.aria-label]="t().adminPlaces.filterAria"
             [ngModel]="cityFilter()" (ngModelChange)="filter($event)" />

      @if (error()) {
        <lq-empty-state icon="info" [title]="t().adminPlaces.loadFailed" [message]="error()" />
      } @else if (places(); as list) {
        @for (p of list; track p.id) {
          <article class="surface item" [class.item--muted]="!p.isActive || p.isPast">
            <div class="item__head">
              <strong>{{ p.name }}</strong>
              <span class="pill">{{ p.kind === 'Event' ? t().adminPlaces.event : t().adminPlaces.venue }}</span>
            </div>
            <p class="small">
              {{ p.city }}
              @if (p.startsAt) { · {{ when(p.startsAt) }} – {{ when(p.endsAt!) }} }
              @if (p.isPast) { · <strong>{{ t().adminPlaces.past }}</strong> }
              @if (!p.isActive) { · <strong>{{ t().adminPlaces.inactive }}</strong> }
            </p>
            <p class="muted small">{{ t().adminPlaces.quests(titles(p)) }}</p>
            <div class="actions">
              <button type="button" class="link" (click)="openEditor(p)">{{ t().adminPlaces.edit }}</button>
              <button type="button" class="link link--danger" (click)="remove(p)">{{ t().adminPlaces.remove }}</button>
            </div>
          </article>
        } @empty {
          <lq-empty-state icon="map-pin" [title]="t().adminPlaces.noneTitle"
            [message]="t().adminPlaces.noneHint" />
        }
      } @else {
        <lq-skeleton [height]="120" />
      }
    </section>

    <lq-sheet [title]="editingId() ? t().adminPlaces.edit : t().adminPlaces.newTitle" [(open)]="editorOpen">
      <form class="stack" (ngSubmit)="save()">
        <div class="field">
          <span class="field__label">{{ t().adminPlaces.kind }}</span>
          <div class="kinds" role="radiogroup" [attr.aria-label]="t().adminPlaces.kind">
            <label><input type="radio" name="kind" value="Venue" [(ngModel)]="kind" /> {{ t().adminPlaces.venue }}</label>
            <label><input type="radio" name="kind" value="Event" [(ngModel)]="kind" /> {{ t().adminPlaces.event }}</label>
          </div>
        </div>
        <div class="field">
          <label for="place-city">{{ t().adminPlaces.city }}</label>
          <input id="place-city" class="input" name="city" maxlength="80" [(ngModel)]="city" [placeholder]="t().adminPlaces.cityPlaceholder" />
        </div>
        <div class="field">
          <label for="place-name">{{ t().adminPlaces.name }}</label>
          <input id="place-name" class="input" name="name" maxlength="120" [(ngModel)]="name" />
        </div>
        @if (kind === 'Event') {
          <div class="row">
            <div class="field">
              <label for="place-start">{{ t().adminPlaces.start }}</label>
              <input id="place-start" class="input" type="datetime-local" name="startsAt" [(ngModel)]="startsAt" />
            </div>
            <div class="field">
              <label for="place-end">{{ t().adminPlaces.end }}</label>
              <input id="place-end" class="input" type="datetime-local" name="endsAt" [(ngModel)]="endsAt" />
            </div>
          </div>
        }
        <div class="field">
          <label for="place-address">{{ t().adminPlaces.address }} <span class="muted">{{ t().adminPlaces.optional }}</span></label>
          <input id="place-address" class="input" name="address" maxlength="200" [(ngModel)]="address" />
        </div>
        <div class="field">
          <label for="place-url">{{ t().adminPlaces.url }} <span class="muted">(https)</span></label>
          <input id="place-url" class="input" type="url" name="url" maxlength="500" [(ngModel)]="url" />
        </div>
        <div class="field">
          <label for="place-note">{{ t().adminPlaces.note }} <span class="muted">{{ t().adminPlaces.optional }}</span></label>
          <textarea id="place-note" class="input" rows="2" name="note" maxlength="300" [(ngModel)]="note"></textarea>
        </div>
        <div class="field">
          <span class="field__label">{{ t().adminPlaces.linked(picked().length) }}</span>
          @for (tpl of picked(); track tpl.id) {
            <div class="picked"><span>{{ tpl.title }} <span class="muted small">{{ tpl.code }}</span></span>
              <button type="button" class="link" (click)="unpick(tpl)" [attr.aria-label]="t().adminPlaces.unlinkAria(tpl.title)">{{ t().adminPlaces.unlink }}</button></div>
          }
          <input class="input" type="search" [placeholder]="t().adminPlaces.search" [attr.aria-label]="t().adminPlaces.searchAria"
                 [ngModel]="templateQuery()" (ngModelChange)="searchTemplates($event)" name="templateQuery" />
          @for (tpl of templateResults(); track tpl.id) {
            <button type="button" class="result" (click)="pick(tpl)">{{ tpl.title }} <span class="muted small">{{ tpl.code }}</span></button>
          }
        </div>
        <label class="check"><input type="checkbox" name="isActive" [(ngModel)]="isActive" /> {{ t().adminPlaces.visible }}</label>
        @if (saveError()) { <p class="field__error" role="alert">{{ saveError() }}</p> }
        <button lq-button type="submit" [block]="true" [loading]="busy()"
                [disabled]="!city.trim() || !name.trim() || !picked().length">{{ t().adminPlaces.save }}</button>
      </form>
    </lq-sheet>
  `,
  styles: `
    .section { display: flex; flex-direction: column; gap: var(--space-4); }
    .head { display: flex; justify-content: space-between; align-items: flex-start; gap: 12px; }
    .small { font-size: var(--fs-sm); margin: 0; }
    .item { padding: var(--space-4); display: flex; flex-direction: column; gap: 6px; }
    .item--muted { opacity: 0.65; }
    .item__head { display: flex; justify-content: space-between; gap: 8px; }
    .actions { display: flex; gap: 16px; }
    .kinds { display: flex; gap: 16px; }
    .row { display: grid; grid-template-columns: 1fr 1fr; gap: 8px; }
    .picked { display: flex; justify-content: space-between; gap: 8px; align-items: center; font-size: var(--fs-sm); }
    .result { text-align: left; background: none; border: 1px solid var(--line, var(--ink-4)); border-radius: var(--radius-md); padding: 8px 10px; cursor: pointer; color: inherit; font: inherit; }
    .check { display: flex; gap: 8px; align-items: center; }
    .link { background: none; border: 0; padding: 0; color: var(--primary-text); font-weight: 800; cursor: pointer; font-size: var(--fs-sm); }
    .link--danger { color: var(--danger); }
    @media (max-width: 480px) { .row { grid-template-columns: 1fr; } }
  `,
})
export class AdminPlacesPage {
  protected readonly t = t;
  private readonly api = inject(AdminApi);
  private readonly toast = inject(ToastService);

  protected readonly places = signal<AdminPlace[] | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly cityFilter = signal('');
  protected readonly editorOpen = signal(false);
  protected readonly editingId = signal<string | null>(null);
  protected readonly busy = signal(false);
  protected readonly saveError = signal<string | null>(null);
  protected readonly picked = signal<PickedTemplate[]>([]);
  protected readonly templateQuery = signal('');
  private readonly templateMatches = signal<AdminTemplateListItem[]>([]);
  protected readonly templateResults = computed(() => {
    const chosen = new Set(this.picked().map((t) => t.id));
    return this.templateMatches().filter((t) => !chosen.has(t.id)).slice(0, 6);
  });

  protected kind: LocalPlaceKind = 'Venue';
  protected city = '';
  protected name = '';
  protected address = '';
  protected url = '';
  protected note = '';
  protected startsAt = '';
  protected endsAt = '';
  protected isActive = true;

  private readonly filters = new Subject<string>();
  private readonly searches = new Subject<string>();

  constructor() {
    this.filters.pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed()).subscribe(() => this.load());
    this.searches
      .pipe(
        debounceTime(250),
        distinctUntilChanged(),
        switchMap((text) => this.api.templates({ text, isActive: true, pageNumber: 1 })),
        takeUntilDestroyed(),
      )
      .subscribe({ next: (page) => this.templateMatches.set(page.items) });
    this.load();
  }

  protected when(value: string): string {
    return formatDate(value, { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' });
  }

  protected titles(place: AdminPlace): string {
    return place.templates.map((t) => t.title).join(', ') || '–';
  }

  protected filter(value: string): void {
    this.cityFilter.set(value);
    this.filters.next(value.trim());
  }

  protected searchTemplates(value: string): void {
    this.templateQuery.set(value);
    if (value.trim().length >= 2) this.searches.next(value.trim());
    else this.templateMatches.set([]);
  }

  protected pick(template: AdminTemplateListItem): void {
    this.picked.update((list) => [...list, { id: template.id, code: template.code, title: template.title }]);
  }

  protected unpick(template: PickedTemplate): void {
    this.picked.update((list) => list.filter((t) => t.id !== template.id));
  }

  protected openEditor(place?: AdminPlace): void {
    this.editingId.set(place?.id ?? null);
    this.kind = place?.kind ?? 'Venue';
    this.city = place?.city ?? this.cityFilter().trim();
    this.name = place?.name ?? '';
    this.address = place?.address ?? '';
    this.url = place?.url ?? '';
    this.note = place?.note ?? '';
    this.startsAt = toLocalInput(place?.startsAt ?? null);
    this.endsAt = toLocalInput(place?.endsAt ?? null);
    this.isActive = place?.isActive ?? true;
    this.picked.set(place?.templates.map((t) => ({ ...t })) ?? []);
    this.templateQuery.set('');
    this.templateMatches.set([]);
    this.saveError.set(null);
    this.editorOpen.set(true);
  }

  protected save(): void {
    const isEvent = this.kind === 'Event';
    this.busy.set(true);
    this.saveError.set(null);
    this.api
      .savePlace(
        {
          kind: this.kind,
          city: this.city.trim(),
          name: this.name.trim(),
          address: this.address.trim() || null,
          url: this.url.trim() || null,
          note: this.note.trim() || null,
          startsAt: isEvent && this.startsAt ? new Date(this.startsAt).toISOString() : null,
          endsAt: isEvent && this.endsAt ? new Date(this.endsAt).toISOString() : null,
          templateIds: this.picked().map((t) => t.id),
          isActive: this.isActive,
        },
        this.editingId() ?? undefined,
      )
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.editorOpen.set(false);
          this.toast.success(t().adminPlaces.saved);
          this.load();
        },
        error: (err: unknown) => {
          this.busy.set(false);
          this.saveError.set(parseApiErrors(err).map((e) => e.message).join(' '));
        },
      });
  }

  protected remove(place: AdminPlace): void {
    if (!confirm(t().adminPlaces.confirmDelete(place.name))) return;
    this.api.deletePlace(place.id).subscribe({
      next: () => {
        this.toast.show(t().adminPlaces.deleted);
        this.load();
      },
      error: (err: unknown) => this.toast.error(firstErrorMessage(err)),
    });
  }

  private load(): void {
    this.api.places(this.cityFilter().trim() || undefined).subscribe({
      next: (list) => {
        this.error.set(null);
        this.places.set(list);
      },
      error: (err: unknown) => this.error.set(firstErrorMessage(err)),
    });
  }
}
