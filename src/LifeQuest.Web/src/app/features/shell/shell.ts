import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Icon, IconName } from '../../ui/icon';
import { APP_PATHS } from '../../core/routing/app-paths';
import { t } from '../../core/i18n/i18n';

interface NavItem {
  path: string;
  key: 'today' | 'quests' | 'progress' | 'profile';
  icon: IconName;
}

@Component({
  selector: 'lq-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main id="content">
      <router-outlet />
    </main>

    <nav [attr.aria-label]="t().nav.mainMenu">
      @for (item of items; track item.path) {
        <a [routerLink]="item.path" routerLinkActive="active" ariaCurrentWhenActive="page">
          <span class="pill"><lq-icon [name]="item.icon" [size]="22" /></span>
          <span class="label">{{ t().nav[item.key] }}</span>
        </a>
      }
    </nav>
  `,
  styles: `
    nav {
      position: fixed;
      left: 50%;
      bottom: 0;
      transform: translateX(-50%);
      width: min(100%, var(--content-width));
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      padding: 6px 8px calc(6px + env(safe-area-inset-bottom));
      background: color-mix(in srgb, var(--surface) 88%, transparent);
      backdrop-filter: blur(14px);
      -webkit-backdrop-filter: blur(14px);
      border-top: 1px solid var(--line);
      z-index: 20;
    }
    @media (min-width: 600px) {
      nav { bottom: 12px; border: 1px solid var(--line); border-radius: var(--radius-xl); box-shadow: var(--shadow-lg); width: min(calc(100% - 24px), 480px); }
    }
    a {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 2px;
      min-height: 52px;
      justify-content: center;
      color: var(--ink-3);
      font-size: var(--fs-xs);
      font-weight: 800;
      text-decoration: none !important;
      -webkit-tap-highlight-color: transparent;
    }
    .pill { display: grid; place-items: center; width: 56px; height: 30px; border-radius: var(--radius-pill); transition: background 0.2s var(--ease); }
    a.active { color: var(--primary-text); }
    a.active .pill { background: var(--primary-soft); }
  `,
})
export class Shell {
  protected readonly t = t;
  protected readonly items: NavItem[] = [
    { path: APP_PATHS.today, key: 'today', icon: 'sun' },
    { path: APP_PATHS.quests, key: 'quests', icon: 'list' },
    { path: APP_PATHS.progress, key: 'progress', icon: 'trophy' },
    { path: APP_PATHS.profile, key: 'profile', icon: 'user' },
  ];
}
