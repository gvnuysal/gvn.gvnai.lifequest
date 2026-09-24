import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger' | 'soft';

/** Yerel <button>/<a> üzerine uygulanır; erişilebilirlik tarayıcı öğesinden gelir. */
@Component({
  selector: 'button[lq-button], a[lq-button]',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'lq-button',
    '[class.lq-button--primary]': "variant() === 'primary'",
    '[class.lq-button--secondary]': "variant() === 'secondary'",
    '[class.lq-button--ghost]': "variant() === 'ghost'",
    '[class.lq-button--danger]': "variant() === 'danger'",
    '[class.lq-button--soft]': "variant() === 'soft'",
    '[class.lq-button--sm]': "size() === 'sm'",
    '[class.lq-button--block]': 'block()',
    '[class.lq-button--loading]': 'loading()',
    '[attr.aria-busy]': 'loading() || null',
  },
  template: `
    @if (loading()) {
      <span class="spinner" aria-hidden="true"></span>
    }
    <span class="content"><ng-content /></span>
  `,
  styles: `
    :host {
      position: relative;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
      min-height: 48px;
      padding: 0 20px;
      border: 1.5px solid transparent;
      border-radius: var(--radius-pill);
      font-weight: 800;
      font-size: var(--fs-md);
      cursor: pointer;
      text-decoration: none !important;
      transition: transform 0.12s var(--ease), background 0.15s, box-shadow 0.15s, border-color 0.15s;
      user-select: none;
      -webkit-tap-highlight-color: transparent;
    }
    :host(:active:not([disabled])) { transform: scale(0.97); }
    :host([disabled]) { opacity: 0.55; cursor: not-allowed; }
    .content { display: inline-flex; align-items: center; gap: 8px; }
    :host(.lq-button--loading) .content { visibility: hidden; }
    :host(.lq-button--primary) {
      background: var(--primary);
      color: var(--primary-ink);
      box-shadow: 0 6px 16px -6px color-mix(in srgb, var(--primary) 70%, transparent);
    }
    :host(.lq-button--primary:hover:not([disabled])) { background: var(--primary-hover); }
    :host(.lq-button--secondary) { background: var(--surface); color: var(--ink); border-color: var(--line); }
    :host(.lq-button--secondary:hover:not([disabled])) { border-color: var(--ink-3); }
    :host(.lq-button--ghost) { background: transparent; color: var(--ink-2); }
    :host(.lq-button--ghost:hover:not([disabled])) { background: var(--surface-2); }
    :host(.lq-button--soft) { background: var(--primary-soft); color: var(--primary-text); }
    :host(.lq-button--danger) { background: var(--danger-soft); color: var(--danger); }
    :host(.lq-button--sm) { min-height: 38px; padding: 0 14px; font-size: var(--fs-sm); }
    :host(.lq-button--block) { display: flex; width: 100%; }
    .spinner {
      position: absolute;
      width: 20px;
      height: 20px;
      border-radius: 50%;
      border: 2.5px solid currentColor;
      border-right-color: transparent;
      animation: spin 0.7s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }
  `,
})
export class Button {
  readonly variant = input<ButtonVariant>('primary');
  readonly size = input<'md' | 'sm'>('md');
  readonly block = input(false);
  readonly loading = input(false);
}
