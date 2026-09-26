import { effect, inject, Injectable } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';
import { Dictionary, t } from './i18n';

export type TitleKey = keyof Dictionary['titles'];

/**
 * Rota başlıkları sözlükten: rotada <code>title: 'today'</code> gibi bir anahtar durur. Yönetim sayfaları
 * "… · Yönetim/Admin", diğerleri "… · LifeQuest" soneki alır. Dil değişince başlık da güncellenir.
 */
@Injectable({ providedIn: 'root' })
export class I18nTitleStrategy extends TitleStrategy {
  private readonly title = inject(Title);
  private key: TitleKey | null = null;

  constructor() {
    super();
    effect(() => this.apply(t()));
  }

  override updateTitle(snapshot: RouterStateSnapshot): void {
    this.key = (this.buildTitle(snapshot) as TitleKey | undefined) ?? null;
    this.apply(t());
  }

  private apply(d: Dictionary): void {
    const name = this.key ? d.titles[this.key] : undefined;
    if (!name) {
      this.title.setTitle(d.common.appName);
      return;
    }
    const suffix = this.key!.startsWith('admin') && this.key !== 'adminMetrics' ? d.titles.adminSuffix : d.common.appName;
    this.title.setTitle(`${name} · ${suffix}`);
  }
}
