import { t } from '../../core/i18n/i18n';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { APP_PATHS } from '../../core/routing/app-paths';

/** Yönetim paneli kabuğu: sekmeler (yatay kaydırılabilir) ve alt sayfa. */
@Component({
  selector: 'lq-admin-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <header class="stack">
        <span class="eyebrow">{{ t().admin.panel }}</span>
        <nav class="tabs" [attr.aria-label]="t().admin.sectionsAria">
          @for (tab of tabs; track tab.path) {
            <a [routerLink]="tab.path" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: tab.exact }"
               ariaCurrentWhenActive="page">{{ t().admin.tabs[tab.key] }}</a>
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
  protected readonly t = t;
  protected readonly tabs = [
    { path: APP_PATHS.admin.root, key: 'metrics' as const, exact: true },
    { path: APP_PATHS.admin.users, key: 'users' as const, exact: false },
    { path: APP_PATHS.admin.catalog, key: 'catalog' as const, exact: false },
    { path: APP_PATHS.admin.ideas, key: 'ideas' as const, exact: false },
    { path: APP_PATHS.admin.experiments, key: 'experiments' as const, exact: false },
    { path: APP_PATHS.admin.places, key: 'places' as const, exact: false },
    { path: APP_PATHS.admin.recommendationSettings, key: 'weights' as const, exact: false },
    { path: APP_PATHS.admin.auditLog, key: 'audit' as const, exact: false },
  ];
}
