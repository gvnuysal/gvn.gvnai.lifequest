import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { WeatherInfo } from '../core/api/models';
import { Icon } from './icon';

/** Kullanıcının şehrinde hava ve açık hava görevleri için kısa not. Şehir yoksa hiç gösterilmez. */
@Component({
  selector: 'lq-weather-chip',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p class="weather" [attr.data-outdoor]="weather().outdoor">
      <lq-icon [name]="icon()" [size]="18" />
      <span><strong>{{ weather().city }}</strong> · {{ weather().temperatureC }}° · {{ weather().summary }}</span>
    </p>
    @if (weather().advice; as advice) { <p class="advice">{{ advice }}</p> }
  `,
  styles: `
    :host { display: flex; flex-direction: column; gap: 4px; }
    .weather { display: inline-flex; align-items: center; gap: 8px; margin: 0; font-size: var(--fs-sm); color: var(--ink-2); }
    .advice { margin: 0; font-size: var(--fs-sm); color: var(--ink-2); }
  `,
})
export class WeatherChip {
  readonly weather = input.required<WeatherInfo>();

  protected readonly icon = computed(() => {
    const w = this.weather();
    if (w.outdoor === 'Poor' && /yağ|sağanak|çisel|fırtına|kar/i.test(w.summary)) return 'cloud-rain';
    return /açık/i.test(w.summary) ? 'sun' : 'cloud';
  });
}
