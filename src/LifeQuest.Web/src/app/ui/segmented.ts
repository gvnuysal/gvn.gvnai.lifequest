import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';

export interface SegmentOption<T> {
  value: T;
  label: string;
}

@Component({
  selector: 'lq-segmented',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { role: 'radiogroup', '[attr.aria-label]': 'ariaLabel()' },
  template: `
    @for (option of options(); track option.value) {
      <button
        type="button"
        role="radio"
        [attr.aria-checked]="option.value === value()"
        [class.active]="option.value === value()"
        (click)="value.set(option.value)"
      >
        {{ option.label }}
      </button>
    }
  `,
  styles: `
    :host {
      display: flex;
      padding: 4px;
      gap: 4px;
      border-radius: var(--radius-pill);
      background: var(--surface-2);
      border: 1px solid var(--line);
    }
    button {
      flex: 1;
      min-width: 0;
      min-height: 40px;
      padding: 0 6px;
      border: 0;
      border-radius: var(--radius-pill);
      background: transparent;
      color: var(--ink-2);
      font-weight: 700;
      font-size: var(--fs-sm);
      cursor: pointer;
      transition: background 0.15s, color 0.15s, box-shadow 0.15s;
      white-space: nowrap;
    }
    button.active {
      background: var(--surface);
      color: var(--ink);
      box-shadow: var(--shadow-sm);
    }
  `,
})
export class Segmented<T> {
  readonly options = input.required<SegmentOption<T>[]>();
  readonly value = model.required<T>();
  readonly ariaLabel = input<string>('');
}
