import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Icon, IconName } from './icon';

@Component({
  selector: 'lq-empty-state',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="art"><lq-icon [name]="icon()" [size]="30" /></div>
    <h3>{{ title() }}</h3>
    @if (message()) {
      <p>{{ message() }}</p>
    }
    <ng-content />
  `,
  styles: `
    :host {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 10px;
      padding: var(--space-8) var(--space-5);
      text-align: center;
      border: 1.5px dashed var(--line);
      border-radius: var(--radius-lg);
      background: color-mix(in srgb, var(--surface) 60%, transparent);
    }
    .art { display: grid; place-items: center; width: 64px; height: 64px; border-radius: 50%; background: var(--primary-soft); color: var(--primary-text); }
    p { color: var(--ink-2); max-width: 36ch; }
  `,
})
export class EmptyState {
  readonly icon = input<IconName>('sparkles');
  readonly title = input.required<string>();
  readonly message = input<string | null>(null);
}

@Component({
  selector: 'lq-skeleton',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { 'aria-hidden': 'true', '[style.height.px]': 'height()' },
  template: ``,
  styles: `
    :host {
      display: block;
      border-radius: var(--radius-lg);
      background: linear-gradient(90deg, var(--surface-2) 25%, var(--surface-3) 50%, var(--surface-2) 75%);
      background-size: 200% 100%;
      animation: shimmer 1.3s linear infinite;
    }
    @keyframes shimmer { to { background-position: -200% 0; } }
  `,
})
export class Skeleton {
  readonly height = input(120);
}
