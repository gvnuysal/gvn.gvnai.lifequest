import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { LanguageSwitch } from '../../ui/language-switch';

@Component({
  selector: 'lq-auth-layout',
  imports: [LanguageSwitch],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page page--bare">
      <div class="lang"><lq-language-switch /></div>
      <header class="brand">
        <img src="icons/icon.svg" alt="" width="64" height="64" />
        <p class="eyebrow" lang="en">LifeQuest</p>
        <h1>{{ heading() }}</h1>
        <p class="lead">{{ lead() }}</p>
      </header>
      <section class="surface card">
        <ng-content />
      </section>
      <ng-content select="[footer]" />
    </div>
  `,
  styles: `
    .lang { display: flex; justify-content: flex-end; }
    .page { min-height: 100dvh; justify-content: center; }
    .brand { display: flex; flex-direction: column; align-items: center; text-align: center; gap: 8px; }
    .brand img { border-radius: 18px; box-shadow: var(--shadow-md); margin-bottom: 6px; }
    .lead { color: var(--ink-2); max-width: 34ch; }
    .card { padding: var(--space-6) var(--space-5); }
  `,
})
export class AuthLayout {
  readonly heading = input.required<string>();
  readonly lead = input.required<string>();
}
