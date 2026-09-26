import { SAFETY_LABELS } from './admin-labels';
import { SafetyLevel } from '../../core/api/models';
import { option, t } from '../../core/i18n/i18n';
import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { AdminApi } from '../../core/api/api-clients';
import { AdminAction, AuditEntry, PagedResult } from '../../core/api/models';
import { firstErrorMessage } from '../../core/http/api-error';
import { formatDate } from '../../core/labels/format';
import { Button } from '../../ui/button';
import { Chip } from '../../ui/chip';
import { EmptyState, Skeleton } from '../../ui/states';
import { AUDIT_ACTION_LABELS } from './admin-labels';

interface DetailLine {
  label: string;
  value: string;
}

/** Admin işlemlerinin değiştirilemez kaydı: kim, ne zaman, neyi, neden değiştirdi. */
@Component({
  selector: 'lq-admin-audit-page',
  imports: [Button, Chip, EmptyState, Skeleton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="section">
      <header class="stack">
        <h1>{{ t().adminAudit.title }}</h1>
        <p class="muted">{{ t().adminAudit.lead }}</p>
      </header>

      <div class="chips" role="group" [attr.aria-label]="t().adminAudit.actionAria">
        <button lq-chip [selected]="action() === null" (click)="setAction(null)">{{ t().adminAudit.all }}</button>
        @for (a of actions; track a) {
          <button lq-chip [selected]="action() === a" (click)="setAction(a)">{{ labels[a] }}</button>
        }
      </div>

      @if (error()) {
        <lq-empty-state icon="info" [title]="t().adminAudit.loadFailed" [message]="error()" />
      } @else if (page(); as p) {
        <ol class="timeline">
          @for (e of p.items; track e.id) {
            <li class="surface entry">
              <div class="entry__head">
                <strong>{{ labels[e.action] }}</strong>
                <time class="muted small" [attr.datetime]="e.createdAt">{{ date(e.createdAt) }}</time>
              </div>
              <p class="small"><span class="target">{{ e.targetLabel }}</span> · <span class="muted">{{ e.actorEmail }}</span></p>
              @if (e.reason) { <p class="reason small">“{{ e.reason }}”</p> }
              @if (details(e); as lines) {
                @if (lines.length) {
                  <dl class="details">
                    @for (l of lines; track $index) { <div><dt>{{ l.label }}</dt><dd>{{ l.value }}</dd></div> }
                  </dl>
                }
              }
            </li>
          } @empty {
            <lq-empty-state icon="list" [title]="t().adminAudit.noneTitle" [message]="t().adminAudit.noneHint" />
          }
        </ol>
        @if (p.totalPages > 1) {
          <nav class="pager" [attr.aria-label]="t().adminUsers.pages">
            <button lq-button variant="soft" size="sm" [disabled]="!p.hasPreviousPage" (click)="pageNumber.set(p.pageNumber - 1)">{{ t().adminAudit.newer }}</button>
            <span class="muted small">{{ p.pageNumber }} / {{ p.totalPages }}</span>
            <button lq-button variant="soft" size="sm" [disabled]="!p.hasNextPage" (click)="pageNumber.set(p.pageNumber + 1)">{{ t().adminAudit.older }}</button>
          </nav>
        }
      } @else {
        <lq-skeleton [height]="96" />
        <lq-skeleton [height]="96" />
      }
    </section>
  `,
  styles: `
    .section { display: flex; flex-direction: column; gap: var(--space-4); }
    .small { font-size: var(--fs-sm); }
    .chips { display: flex; gap: 6px; overflow-x: auto; scrollbar-width: none; margin: 0 calc(-1 * var(--space-4)); padding: 0 var(--space-4); }
    .chips::-webkit-scrollbar { display: none; }
    .timeline { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 10px; }
    .entry { padding: 12px 14px; display: flex; flex-direction: column; gap: 6px; }
    .entry__head { display: flex; justify-content: space-between; gap: 8px; flex-wrap: wrap; }
    .target { font-weight: 800; overflow-wrap: anywhere; }
    .reason { color: var(--ink-2); font-style: italic; }
    .details { margin: 0; display: flex; flex-direction: column; gap: 4px; font-size: var(--fs-xs); }
    .details div { display: flex; gap: 8px; }
    .details dt { color: var(--ink-3); font-weight: 700; min-width: 8rem; }
    .details dd { margin: 0; font-variant-numeric: tabular-nums; overflow-wrap: anywhere; }
    .pager { display: flex; justify-content: space-between; align-items: center; }
  `,
})
export class AdminAuditPage {
  protected readonly t = t;
  private readonly api = inject(AdminApi);

  protected readonly labels = AUDIT_ACTION_LABELS;
  protected readonly actions = Object.keys(AUDIT_ACTION_LABELS) as AdminAction[];
  protected readonly action = signal<AdminAction | null>(null);
  protected readonly pageNumber = signal(1);
  protected readonly page = signal<PagedResult<AuditEntry> | null>(null);
  protected readonly error = signal<string | null>(null);

  constructor() {
    effect(() => {
      const action = this.action();
      const pageNumber = this.pageNumber();
      this.error.set(null);
      this.api.audit(action, pageNumber).subscribe({
        next: (page) => this.page.set(page),
        error: (err: unknown) => this.error.set(firstErrorMessage(err)),
      });
    });
  }

  protected setAction(action: AdminAction | null): void {
    this.action.set(action);
    this.pageNumber.set(1);
  }

  protected date(value: string): string {
    return formatDate(value, { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' });
  }

  /** Ağırlık farkları "önce → sonra", diğer ayrıntılar okunabilir satırlar olarak gösterilir. */
  protected details(entry: AuditEntry): DetailLine[] {
    if (!entry.details) return [];
    try {
      const data: unknown = JSON.parse(entry.details);
      if (Array.isArray(data)) {
        return data
          .filter((c): c is { key: string; before: number; after: number } => typeof c === 'object' && c !== null && 'key' in c)
          .map((c) => ({ label: c.key, value: `${c.before} → ${c.after}` }));
      }
      if (data && typeof data === 'object') {
        const d = data as Record<string, unknown>;
        const lines: DetailLine[] = [];
        if ('before' in d && 'after' in d && typeof d['before'] !== 'object') lines.push({ label: t().adminAudit.change, value: `${d['before']} → ${d['after']}` });
        if (typeof d['until'] === 'string') lines.push({ label: t().adminAudit.until, value: this.date(d['until']) });
        if ('until' in d && d['until'] === null) lines.push({ label: t().adminAudit.duration, value: t().adminAudit.forever });
        if (typeof d['safety'] === 'string') lines.push({ label: t().adminAudit.status, value: SAFETY_LABELS[d['safety'] as SafetyLevel]?.label ?? d['safety'] });
        if (Array.isArray(d['violations']) && d['violations'].length) lines.push({ label: t().adminAudit.violations, value: d['violations'].join(' · ') });
        return lines;
      }
    } catch {
      /* Beklenmeyen ayrıntı biçimi: gösterilmez. */
    }
    return [];
  }
}
