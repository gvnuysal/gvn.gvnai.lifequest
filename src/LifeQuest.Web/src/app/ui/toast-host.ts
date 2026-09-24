import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ToastService } from '../core/state/toast.service';
import { Icon } from './icon';

@Component({
  selector: 'lq-toast-host',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="stack" role="status" aria-live="polite">
      @for (toast of toasts.toasts(); track toast.id) {
        <div class="toast" [class]="'toast toast--' + toast.kind">
          <lq-icon [name]="toast.kind === 'success' ? 'check' : toast.kind === 'error' ? 'info' : 'sparkles'" />
          <span>{{ toast.message }}</span>
          <button type="button" (click)="toasts.dismiss(toast.id)" aria-label="Bildirimi kapat">
            <lq-icon name="x" [size]="16" />
          </button>
        </div>
      }
    </div>
  `,
  styles: `
    .stack {
      position: fixed;
      left: 50%;
      top: calc(env(safe-area-inset-top) + 12px);
      transform: translateX(-50%);
      width: min(calc(100% - 24px), var(--content-width));
      display: flex;
      flex-direction: column;
      gap: 8px;
      z-index: 100;
      pointer-events: none;
    }
    .toast {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 12px 12px 12px 16px;
      border-radius: var(--radius-md);
      background: var(--ink);
      color: var(--bg);
      font-weight: 700;
      font-size: var(--fs-sm);
      box-shadow: var(--shadow-lg);
      pointer-events: auto;
      animation: drop 0.25s var(--ease);
    }
    .toast span { flex: 1; }
    .toast--success lq-icon:first-child { color: var(--success-soft); }
    .toast--error { background: var(--danger); color: #fff; }
    button { display: grid; place-items: center; width: 28px; height: 28px; border: 0; border-radius: 50%; background: transparent; color: inherit; cursor: pointer; opacity: 0.8; }
    @keyframes drop { from { transform: translateY(-12px); opacity: 0; } }
  `,
})
export class ToastHost {
  protected readonly toasts = inject(ToastService);
}
