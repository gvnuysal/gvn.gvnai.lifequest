import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

/** Yönetim paneli kabuğu: sekmeler (yatay kaydırılabilir) ve alt sayfa. */
@Component({
  selector: 'lq-admin-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <header class="stack">
        <span class="eyebrow">Yönetim paneli</span>
        <nav class="tabs" aria-label="Yönetim bölümleri">
          @for (tab of tabs; track tab.path) {
            <a [routerLink]="tab.path" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: tab.exact }"
               ariaCurrentWhenActive="page">{{ tab.label }}</a>
          }
        </nav>
      </header>
      <router-outlet />
    </div>
  `,
  styles: `
    .tabs {
      display: flex;
      gap: 6px;
      overflow-x: auto;
      scrollbar-width: none;
      margin: 0 calc(-1 * var(--space-4));
      padding: 2px var(--space-4) 4px;
    }
    .tabs::-webkit-scrollbar { display: none; }
    .tabs a {
      flex-shrink: 0;
      min-height: 40px;
      display: inline-flex;
      align-items: center;
      padding: 0 14px;
      border-radius: var(--radius-pill);
      border: 1.5px solid var(--line);
      background: var(--surface);
      color: var(--ink-2);
      font-weight: 800;
      font-size: var(--fs-sm);
      text-decoration: none;
      white-space: nowrap;
    }
    .tabs a.active { background: var(--primary); border-color: var(--primary); color: var(--primary-ink); }
  `,
})
export class AdminShell {
  protected readonly tabs = [
    { path: '/yonetim', label: 'Metrikler', exact: true },
    { path: '/yonetim/kullanicilar', label: 'Kullanıcılar', exact: false },
    { path: '/yonetim/katalog', label: 'Katalog', exact: false },
    { path: '/yonetim/fikirler', label: 'Fikirler', exact: false },
    { path: '/yonetim/deneyler', label: 'Deneyler', exact: false },
    { path: '/yonetim/oneri-ayarlari', label: 'Öneri ayarları', exact: false },
    { path: '/yonetim/denetim', label: 'Denetim kaydı', exact: false },
  ];
}
