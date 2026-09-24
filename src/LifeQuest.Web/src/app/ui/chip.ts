import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Seçilebilir çip. <button lq-chip [selected]="..."> */
@Component({
  selector: 'button[lq-chip]',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    type: 'button',
    '[attr.aria-pressed]': 'selected()',
    '[class.selected]': 'selected()',
    '[class.strong]': 'strong()',
  },
  template: `<ng-content />`,
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      min-height: 40px;
      padding: 0 14px;
      border-radius: var(--radius-pill);
      border: 1.5px solid var(--line);
      background: var(--surface);
      color: var(--ink-2);
      font-weight: 700;
      font-size: var(--fs-sm);
      white-space: nowrap;
      flex-shrink: 0;
      cursor: pointer;
      transition: all 0.15s var(--ease);
      -webkit-tap-highlight-color: transparent;
    }
    :host(:hover) { border-color: var(--ink-3); }
    :host(.selected) {
      border-color: var(--brand);
      background: var(--primary-soft);
      color: var(--primary-text);
    }
    :host(.selected.strong) {
      background: var(--primary);
      border-color: var(--primary);
      color: var(--primary-ink);
    }
  `,
})
export class Chip {
  readonly selected = input(false);
  /** İkinci seviye seçim (ör. "çok seviyorum"). */
  readonly strong = input(false);
}
