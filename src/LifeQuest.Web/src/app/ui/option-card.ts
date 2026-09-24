import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Icon, IconName } from './icon';

/** Büyük, açıklamalı seçim kartı (radio/checkbox davranışı dışarıdan verilir). */
@Component({
  selector: 'button[lq-option]',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { type: 'button', '[class.selected]': 'selected()', '[attr.aria-pressed]': 'selected()' },
  template: `
    @if (icon()) {
      <span class="icon"><lq-icon [name]="icon()!" [size]="22" /></span>
    }
    <span class="text">
      <span class="title">{{ heading() }}</span>
      @if (description()) {
        <span class="desc">{{ description() }}</span>
      }
    </span>
    <span class="check" aria-hidden="true"><lq-icon name="check" [size]="16" [strokeWidth]="3" /></span>
  `,
  styles: `
    :host {
      display: flex;
      align-items: center;
      gap: 12px;
      width: 100%;
      min-height: 64px;
      padding: 12px 14px;
      border-radius: var(--radius-lg);
      border: 1.5px solid var(--line);
      background: var(--surface);
      text-align: left;
      cursor: pointer;
      transition: all 0.15s var(--ease);
      -webkit-tap-highlight-color: transparent;
    }
    :host(:hover) { border-color: var(--ink-3); }
    :host(.selected) { border-color: var(--brand); background: var(--primary-soft); box-shadow: 0 0 0 3px color-mix(in srgb, var(--brand) 15%, transparent); }
    .icon { display: grid; place-items: center; width: 40px; height: 40px; border-radius: 12px; background: var(--surface-2); color: var(--ink-2); flex-shrink: 0; }
    :host(.selected) .icon { background: var(--surface); color: var(--primary-text); }
    .text { flex: 1; display: flex; flex-direction: column; gap: 2px; }
    .title { font-weight: 800; }
    .desc { font-size: var(--fs-sm); color: var(--ink-2); }
    .check { display: grid; place-items: center; width: 24px; height: 24px; border-radius: 50%; border: 1.5px solid var(--line); color: transparent; flex-shrink: 0; }
    :host(.selected) .check { background: var(--primary); border-color: var(--primary); color: var(--primary-ink); }
  `,
})
export class OptionCard {
  readonly heading = input.required<string>();
  readonly description = input<string | null>(null);
  readonly icon = input<IconName | null>(null);
  readonly selected = input(false);
}
