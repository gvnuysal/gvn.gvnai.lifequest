import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  effect,
  input,
  model,
  viewChild,
} from '@angular/core';
import { Icon } from './icon';
import { t } from '../core/i18n/i18n';

/**
 * Alttan açılan panel (mobil) / ortalanmış diyalog (geniş ekran). Yerel <dialog> kullanır:
 * odak yakalama, Esc ile kapanma ve arka plan erişilemezliği tarayıcıdan gelir.
 */
@Component({
  selector: 'lq-sheet',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <dialog #dialog (close)="open.set(false)" (click)="onBackdrop($event)" [attr.aria-label]="title()">
      <div class="panel">
        <header>
          <h2>{{ title() }}</h2>
          <button type="button" class="close" (click)="open.set(false)" [attr.aria-label]="t().common.close">
            <lq-icon name="x" />
          </button>
        </header>
        <div class="body"><ng-content /></div>
      </div>
    </dialog>
  `,
  styles: `
    dialog {
      width: 100%;
      max-width: var(--content-width);
      max-height: 90dvh;
      margin: auto auto 0;
      padding: 0;
      border: 0;
      border-radius: var(--radius-xl) var(--radius-xl) 0 0;
      background: var(--surface);
      color: var(--ink);
      box-shadow: var(--shadow-lg);
    }
    dialog[open] { animation: rise 0.28s var(--ease); }
    dialog::backdrop { background: var(--overlay); }
    @media (min-width: 640px) {
      dialog { margin: auto; border-radius: var(--radius-xl); }
    }
    .panel { padding: var(--space-5) var(--space-5) calc(var(--space-6) + env(safe-area-inset-bottom)); }
    header { display: flex; align-items: center; justify-content: space-between; gap: 12px; margin-bottom: var(--space-4); }
    h2 { font-size: var(--fs-lg); }
    .close {
      display: grid;
      place-items: center;
      width: 40px;
      height: 40px;
      border: 0;
      border-radius: 50%;
      background: var(--surface-2);
      color: var(--ink-2);
      cursor: pointer;
    }
    @keyframes rise { from { transform: translateY(24px); opacity: 0; } }
  `,
})
export class Sheet {
  readonly open = model(false);
  protected readonly t = t;
  readonly title = input.required<string>();

  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  constructor() {
    effect(() => {
      const dialog = this.dialog().nativeElement;
      if (this.open() && !dialog.open) dialog.showModal();
      else if (!this.open() && dialog.open) dialog.close();
    });
  }

  protected onBackdrop(event: MouseEvent): void {
    if (event.target === this.dialog().nativeElement) this.open.set(false);
  }
}
