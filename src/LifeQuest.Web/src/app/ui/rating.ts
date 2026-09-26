import { ChangeDetectionStrategy, Component, model, signal } from '@angular/core';
import { Icon } from './icon';
import { t } from '../core/i18n/i18n';

@Component({
  selector: 'lq-rating',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { role: 'radiogroup', '[attr.aria-label]': 't().ui.rating' },
  template: `
    @for (star of stars; track star) {
      <button
        type="button"
        role="radio"
        [attr.aria-checked]="value() === star"
        [attr.aria-label]="t().ui.stars(star)"
        [class.on]="star <= (hover() ?? value() ?? 0)"
        (click)="value.set(star)"
        (mouseenter)="hover.set(star)"
        (mouseleave)="hover.set(null)"
      >
        <lq-icon name="star" [size]="34" [strokeWidth]="1.8" />
      </button>
    }
  `,
  styles: `
    :host { display: inline-flex; gap: 4px; }
    button {
      display: grid;
      place-items: center;
      width: 48px;
      height: 48px;
      border: 0;
      background: transparent;
      color: var(--line);
      cursor: pointer;
      transition: transform 0.15s var(--ease), color 0.15s;
    }
    button.on { color: var(--xp); }
    button.on ::ng-deep path { fill: currentColor; }
    button:active { transform: scale(0.9); }
  `,
})
export class Rating {
  protected readonly t = t;
  readonly value = model<number | null>(null);
  protected readonly hover = signal<number | null>(null);
  protected readonly stars = [1, 2, 3, 4, 5];
}
