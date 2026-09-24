import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

@Component({
  selector: 'lq-progress-bar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    role: 'progressbar',
    'aria-valuemin': '0',
    'aria-valuemax': '100',
    '[attr.aria-valuenow]': 'percent()',
    '[attr.aria-label]': 'label()',
    '[style.--fill]': 'color()',
    '[style.height.px]': 'height()',
  },
  template: `<span [style.width.%]="percent()"></span>`,
  styles: `
    :host {
      display: block;
      width: 100%;
      border-radius: var(--radius-pill);
      background: color-mix(in srgb, var(--fill) 16%, var(--surface-2));
      overflow: hidden;
    }
    span {
      display: block;
      height: 100%;
      min-width: 6px;
      border-radius: inherit;
      background: var(--fill);
      transition: width 0.8s var(--ease);
    }
  `,
})
export class ProgressBar {
  /** 0-1 arası */
  readonly value = input.required<number>();
  readonly color = input('var(--xp)');
  readonly height = input(10);
  readonly label = input('İlerleme');

  protected readonly percent = computed(() => Math.round(Math.min(1, Math.max(0, this.value())) * 100));
}

@Component({
  selector: 'lq-level-ring',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[style.width.px]': 'size()', '[style.height.px]': 'size()', '[style.--level-size]': 'levelFont()' },
  template: `
    <svg [attr.viewBox]="'0 0 ' + size() + ' ' + size()" aria-hidden="true">
      <circle class="track" [attr.cx]="center()" [attr.cy]="center()" [attr.r]="radius()" [attr.stroke-width]="stroke()" />
      <circle
        class="fill"
        [attr.cx]="center()"
        [attr.cy]="center()"
        [attr.r]="radius()"
        [attr.stroke-width]="stroke()"
        [attr.stroke-dasharray]="circumference()"
        [attr.stroke-dashoffset]="offset()"
        [attr.transform]="'rotate(-90 ' + center() + ' ' + center() + ')'"
      />
    </svg>
    <div class="label">
      <span class="caption">Seviye</span>
      <span class="level">{{ level() }}</span>
    </div>
  `,
  styles: `
    :host { position: relative; display: inline-grid; place-items: center; flex-shrink: 0; }
    svg { position: absolute; inset: 0; }
    circle { fill: none; }
    .track { stroke: var(--xp-soft); }
    .fill { stroke: var(--xp); stroke-linecap: round; transition: stroke-dashoffset 1s var(--ease); }
    .label { display: flex; flex-direction: column; align-items: center; line-height: 1; }
    .caption { font-size: var(--fs-xs); font-weight: 800; color: var(--ink-3); text-transform: uppercase; letter-spacing: 0.08em; }
    .level { font-size: var(--level-size); font-weight: 900; color: var(--ink); }
  `,
})
export class LevelRing {
  readonly level = input.required<number>();
  readonly progress = input.required<number>();
  readonly size = input(120);

  protected readonly stroke = computed(() => Math.max(6, this.size() / 11));
  protected readonly levelFont = computed(() => `${Math.round(this.size() * 0.3)}px`);
  protected readonly center = computed(() => this.size() / 2);
  protected readonly radius = computed(() => (this.size() - this.stroke()) / 2);
  protected readonly circumference = computed(() => 2 * Math.PI * this.radius());
  protected readonly offset = computed(() => this.circumference() * (1 - Math.min(1, Math.max(0, this.progress()))));
}
