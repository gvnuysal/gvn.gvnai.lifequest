import { ChangeDetectionStrategy, Component, computed, input, model } from '@angular/core';
import { Interest, LifeCategory } from '../core/api/models';
import { CATEGORIES, CATEGORY_ORDER } from '../core/labels/labels';
import { CategoryBadge } from './category-badge';
import { Chip } from './chip';
import { Icon } from './icon';

export const LIKE_WEIGHT = 0.6;
export const LOVE_WEIGHT = 0.9;

/**
 * İlgi seçimi: dokunuşla "ilgimi çekiyor" (0.6) → "çok seviyorum" (0.9) → seçili değil.
 * Seçim, ilgi kodu → ağırlık sözlüğüdür.
 */
@Component({
  selector: 'lq-interest-picker',
  imports: [Chip, Icon, CategoryBadge],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p class="legend">
      <span><span class="dot"></span> Bir kez dokun: ilgimi çekiyor</span>
      <span><span class="dot dot--strong"></span> İki kez: çok seviyorum</span>
    </p>
    @for (group of groups(); track group.category) {
      <section>
        <lq-category-badge [category]="group.category" />
        <div class="chips">
          @for (interest of group.interests; track interest.code) {
            <button
              lq-chip
              [selected]="weight(interest.code) > 0"
              [strong]="weight(interest.code) >= loveWeight"
              (click)="cycle(interest.code)"
              [attr.aria-label]="interest.name + ': ' + stateLabel(interest.code)"
            >
              @if (weight(interest.code) >= loveWeight) {
                <lq-icon name="heart" [size]="14" [strokeWidth]="2.6" />
              } @else if (weight(interest.code) > 0) {
                <lq-icon name="check" [size]="14" [strokeWidth]="2.8" />
              }
              {{ interest.name }}
            </button>
          }
        </div>
      </section>
    }
  `,
  styles: `
    :host { display: flex; flex-direction: column; gap: var(--space-5); }
    .legend { display: flex; flex-wrap: wrap; gap: 8px 16px; font-size: var(--fs-xs); font-weight: 700; color: var(--ink-3); }
    .legend > span { display: inline-flex; align-items: center; gap: 6px; }
    .dot { width: 10px; height: 10px; border-radius: 50%; background: var(--primary-soft); border: 1.5px solid var(--brand); }
    .dot--strong { background: var(--primary); border-color: var(--primary); }
    section { display: flex; flex-direction: column; align-items: flex-start; gap: 10px; }
    .chips { display: flex; flex-wrap: wrap; gap: 8px; }
  `,
})
export class InterestPicker {
  readonly interests = input.required<Interest[]>();
  readonly selection = model<Record<string, number>>({});

  protected readonly loveWeight = LOVE_WEIGHT;

  protected readonly groups = computed(() =>
    CATEGORY_ORDER.map((category: LifeCategory) => ({
      category,
      label: CATEGORIES[category].label,
      interests: this.interests().filter((i) => i.category === category),
    })).filter((g) => g.interests.length > 0),
  );

  protected weight(code: string): number {
    return this.selection()[code] ?? 0;
  }

  protected stateLabel(code: string): string {
    const weight = this.weight(code);
    return weight >= LOVE_WEIGHT ? 'çok seviyorum' : weight > 0 ? 'ilgimi çekiyor' : 'seçili değil';
  }

  protected cycle(code: string): void {
    const current = this.weight(code);
    const next = { ...this.selection() };
    if (current === 0) next[code] = LIKE_WEIGHT;
    else if (current < LOVE_WEIGHT) next[code] = LOVE_WEIGHT;
    else delete next[code];
    this.selection.set(next);
  }
}
