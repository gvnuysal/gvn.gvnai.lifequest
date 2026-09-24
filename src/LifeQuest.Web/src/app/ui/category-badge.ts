import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { LifeCategory } from '../core/api/models';
import { CATEGORIES } from '../core/labels/labels';
import { Icon } from './icon';

@Component({
  selector: 'lq-category-badge',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[style.--c]': 'color()', '[style.--c-ink]': 'inkColor()' },
  template: `
    <lq-icon [name]="meta().icon" [size]="compact() ? 14 : 16" [strokeWidth]="2.4" />
    @if (!iconOnly()) {
      <span>{{ meta().label }}</span>
    }
  `,
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 4px 10px 4px 8px;
      border-radius: var(--radius-pill);
      background: color-mix(in srgb, var(--c) 14%, transparent);
      color: var(--c-ink);
      font-size: var(--fs-xs);
      font-weight: 800;
      white-space: nowrap;
    }
  `,
})
export class CategoryBadge {
  readonly category = input.required<LifeCategory>();
  readonly iconOnly = input(false);
  readonly compact = input(false);

  protected readonly meta = computed(() => CATEGORIES[this.category()]);
  protected readonly color = computed(() => `var(--cat-${this.meta().token})`);
  protected readonly inkColor = computed(() => `var(--cat-${this.meta().token}-ink)`);
}

/** Kategori ikonunu renkli yuvarlak içinde gösterir. */
@Component({
  selector: 'lq-category-icon',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[style.--c]': 'color()',
    '[style.--c-ink]': 'inkColor()',
    '[style.width.px]': 'size()',
    '[style.height.px]': 'size()',
  },
  template: `<lq-icon [name]="meta().icon" [size]="size() * 0.5" [strokeWidth]="2.2" />`,
  styles: `
    :host {
      display: inline-grid;
      place-items: center;
      flex-shrink: 0;
      border-radius: 32%;
      background: color-mix(in srgb, var(--c) 16%, var(--surface));
      color: var(--c-ink);
    }
  `,
})
export class CategoryIcon {
  readonly category = input.required<LifeCategory>();
  readonly size = input(44);

  protected readonly meta = computed(() => CATEGORIES[this.category()]);
  protected readonly color = computed(() => `var(--cat-${this.meta().token})`);
  protected readonly inkColor = computed(() => `var(--cat-${this.meta().token}-ink)`);
}
