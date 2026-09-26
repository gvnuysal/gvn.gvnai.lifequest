import { ChangeDetectionStrategy, Component, output } from '@angular/core';
import { t } from '../core/i18n/i18n';
import { currentLang, Lang, LANGS, setLang } from '../core/i18n/lang';

/**
 * TR / EN seçici. Varsayılan davranış dili hemen değiştirir ve tarayıcıda hatırlar; oturum açıkken profil sayfası
 * <code>changed</code> olayıyla dili hesaba da yazar.
 */
@Component({
  selector: 'lq-language-switch',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="switch" role="radiogroup" [attr.aria-label]="t().common.language">
      @for (lang of langs; track lang) {
        <button type="button" role="radio" [attr.aria-checked]="current() === lang" [class.active]="current() === lang"
                [attr.lang]="lang" (click)="select(lang)">
          {{ t().common.languages[lang] }}
        </button>
      }
    </div>
  `,
  styles: `
    .switch { display: inline-flex; gap: 2px; padding: 3px; border-radius: var(--radius-pill); background: var(--surface-2); }
    button { border: 0; background: none; padding: 6px 12px; border-radius: var(--radius-pill); font: inherit; font-size: var(--fs-sm);
             font-weight: 800; color: var(--ink-2); cursor: pointer; min-height: 32px; }
    button.active { background: var(--surface); color: var(--ink-1); box-shadow: var(--shadow-sm, 0 1px 2px rgb(0 0 0 / 0.08)); }
  `,
})
export class LanguageSwitch {
  protected readonly t = t;
  protected readonly langs = LANGS;
  protected readonly current = currentLang;
  readonly changed = output<Lang>();

  protected select(lang: Lang): void {
    if (lang === currentLang()) return;
    setLang(lang);
    this.changed.emit(lang);
  }
}
