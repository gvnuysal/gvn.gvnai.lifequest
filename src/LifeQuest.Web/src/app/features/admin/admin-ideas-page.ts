import { option, t } from '../../core/i18n/i18n';
import { ideaFlagLabel } from './admin-labels';
import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminApi } from '../../core/api/api-clients';
import { AdminIdea, IdeaStatus, PagedResult } from '../../core/api/models';
import { firstErrorMessage } from '../../core/http/api-error';
import { formatDate } from '../../core/labels/format';
import { CATEGORIES, COST_LABELS } from '../../core/labels/labels';
import { ToastService } from '../../core/state/toast.service';
import { Button } from '../../ui/button';
import { Segmented, SegmentOption } from '../../ui/segmented';
import { Sheet } from '../../ui/sheet';
import { EmptyState, Skeleton } from '../../ui/states';
import { APP_PATHS, templatePath } from '../../core/routing/app-paths';

/**
 * Topluluk fikirleri kuyruğu. Gönderenin kimliği gösterilmez: karar içeriğe göre verilir. Kabul, fikri düzenlenebilir
 * bir template taslağına dönüştürerek yapılır; reddetme notu kullanıcıya gösterilir.
 */
@Component({
  selector: 'lq-admin-ideas-page',
  imports: [FormsModule, RouterLink, Button, Segmented, Sheet, EmptyState, Skeleton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="section">
      <header class="stack">
        <h1>{{ t().adminIdeas.title }}</h1>
        <p class="muted">{{ t().adminIdeas.lead }}</p>
        <lq-segmented [ariaLabel]="t().adminIdeas.status" [options]="statuses" [value]="status()" (valueChange)="setStatus($event)" />
      </header>

      @if (error()) {
        <lq-empty-state icon="info" [title]="t().adminIdeas.loadFailed" [message]="error()" />
      } @else if (page(); as p) {
        <p class="muted small">{{ t().adminIdeas.count(p.totalCount) }}</p>
        @for (idea of p.items; track idea.id) {
          <article class="surface idea">
            <div class="idea__head">
              <strong>{{ idea.title }}</strong>
              <span class="muted small">{{ date(idea.submittedAt) }}</span>
            </div>
            <p>{{ idea.description }}</p>
            <p class="muted small">{{ catLabel(idea) }} · {{ t().adminIdeas.minutes(idea.minutes) }} · {{ costLabel(idea) }}{{ idea.isOutdoor ? ' · ' + t().adminIdeas.outdoor : '' }}</p>
            @if (idea.flags.length) {
              <div class="row wrap">
                @for (f of idea.flags; track f) { <span class="pill pill--warning">{{ flagLabel(f) }}</span> }
              </div>
            }
            @if (idea.status === 'Pending') {
              <div class="actions">
                <a lq-button size="sm" [routerLink]="paths.admin.newTemplate" [queryParams]="{ idea: idea.id }">{{ t().adminIdeas.toTemplate }}</a>
                <button lq-button variant="soft" size="sm" (click)="openReject(idea)">{{ t().adminIdeas.reject }}</button>
              </div>
            } @else {
              <p class="small review">
                {{ idea.status === 'Accepted' ? t().adminIdeas.accepted : t().adminIdeas.rejected }} · {{ idea.reviewedBy }}
                @if (idea.reviewNote) { · “{{ idea.reviewNote }}” }
                @if (idea.templateId) { · <a [routerLink]="templatePath(idea.templateId)">{{ t().adminIdeas.openTemplate }}</a> }
              </p>
            }
          </article>
        } @empty {
          <lq-empty-state icon="sparkles" [title]="t().adminIdeas.noneTitle" [message]="t().adminIdeas.noneHint" />
        }
        @if (p.totalPages > 1) {
          <nav class="pager" [attr.aria-label]="t().adminUsers.pages">
            <button lq-button variant="soft" size="sm" [disabled]="!p.hasPreviousPage" (click)="pageNumber.set(p.pageNumber - 1)">{{ t().adminUsers.previous }}</button>
            <span class="muted small">{{ p.pageNumber }} / {{ p.totalPages }}</span>
            <button lq-button variant="soft" size="sm" [disabled]="!p.hasNextPage" (click)="pageNumber.set(p.pageNumber + 1)">{{ t().adminUsers.next }}</button>
          </nav>
        }
      } @else {
        <lq-skeleton [height]="140" />
      }
    </section>

    <lq-sheet [title]="t().adminIdeas.rejectTitle" [open]="!!rejecting()" (openChange)="$event || rejecting.set(null)">
      @if (rejecting(); as idea) {
        <div class="stack">
          <p><strong>{{ idea.title }}</strong></p>
          <div class="field">
            <label for="reject-note">{{ t().adminIdeas.note }}</label>
            <textarea id="reject-note" class="input" rows="3" maxlength="300" [(ngModel)]="note"
                      [placeholder]="t().adminIdeas.notePlaceholder"></textarea>
          </div>
          @if (rejectError()) { <p class="field__error" role="alert">{{ rejectError() }}</p> }
          <button lq-button [block]="true" [loading]="busy()" [disabled]="!note.trim()" (click)="reject(idea)">{{ t().adminIdeas.rejectSend }}</button>
        </div>
      }
    </lq-sheet>
  `,
  styles: `
    .section { display: flex; flex-direction: column; gap: var(--space-4); }
    .small { font-size: var(--fs-sm); }
    .wrap { flex-wrap: wrap; }
    .idea { padding: var(--space-4); display: flex; flex-direction: column; gap: 8px; }
    .idea__head { display: flex; justify-content: space-between; gap: 8px; }
    .actions { display: flex; gap: 8px; flex-wrap: wrap; }
    .review { color: var(--ink-2); }
    .pager { display: flex; justify-content: space-between; align-items: center; }
  `,
})
export class AdminIdeasPage {
  protected readonly t = t;
  protected readonly flagLabel = ideaFlagLabel;
  protected readonly paths = APP_PATHS;
  protected readonly templatePath = templatePath;
  private readonly api = inject(AdminApi);
  private readonly toast = inject(ToastService);

  protected readonly statuses: SegmentOption<IdeaStatus>[] = (['Pending', 'Accepted', 'Rejected'] as const).map((s) =>
    option<IdeaStatus>(s, (d) => d.adminIdeas.statuses[s]));
  protected readonly status = signal<IdeaStatus>('Pending');
  protected readonly pageNumber = signal(1);
  protected readonly page = signal<PagedResult<AdminIdea> | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly rejecting = signal<AdminIdea | null>(null);
  protected readonly rejectError = signal<string | null>(null);
  protected readonly busy = signal(false);
  protected note = '';

  constructor() {
    effect(() => this.load(this.status(), this.pageNumber()));
  }

  protected setStatus(status: IdeaStatus): void {
    this.status.set(status);
    this.pageNumber.set(1);
  }

  protected catLabel(idea: AdminIdea): string {
    return CATEGORIES[idea.category].label;
  }

  protected costLabel(idea: AdminIdea): string {
    return COST_LABELS[idea.cost].label;
  }

  protected date(value: string): string {
    return formatDate(value, { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' });
  }

  protected openReject(idea: AdminIdea): void {
    this.note = '';
    this.rejectError.set(null);
    this.rejecting.set(idea);
  }

  protected reject(idea: AdminIdea): void {
    this.busy.set(true);
    this.api.rejectIdea(idea.id, this.note.trim()).subscribe({
      next: () => {
        this.busy.set(false);
        this.rejecting.set(null);
        this.toast.success(t().adminIdeas.rejectedToast);
        this.load(this.status(), this.pageNumber());
      },
      error: (err: unknown) => {
        this.busy.set(false);
        this.rejectError.set(firstErrorMessage(err));
      },
    });
  }

  private load(status: IdeaStatus, pageNumber: number): void {
    this.error.set(null);
    this.api.ideas(status, pageNumber).subscribe({
      next: (page) => this.page.set(page),
      error: (err: unknown) => this.error.set(firstErrorMessage(err)),
    });
  }
}
