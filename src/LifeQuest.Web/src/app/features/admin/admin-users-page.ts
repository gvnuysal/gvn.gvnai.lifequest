import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { AdminApi } from '../../core/api/api-clients';
import { AdminUser, AdminUserFilter, PagedResult } from '../../core/api/models';
import { firstErrorMessage } from '../../core/http/api-error';
import { formatDate } from '../../core/labels/format';
import { ToastService } from '../../core/state/toast.service';
import { Button } from '../../ui/button';
import { Chip } from '../../ui/chip';
import { Segmented, SegmentOption } from '../../ui/segmented';
import { Sheet } from '../../ui/sheet';
import { EmptyState, Skeleton } from '../../ui/states';
import { SUSPEND_OPTIONS } from './admin-labels';

type UserAction = 'suspend' | 'unsuspend' | 'role' | 'delete';

/**
 * Kullanıcı yönetimi. Yalnızca hesap bilgisi ve toplam sayılar görünür; görev içerikleri, puanlar ve ilgiler
 * bilinçli olarak gösterilmez. Her işlem denetim kaydına yazılır.
 */
@Component({
  selector: 'lq-admin-users-page',
  imports: [FormsModule, Button, Chip, Segmented, Sheet, EmptyState, Skeleton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="section">
      <header class="stack">
        <h1>Kullanıcılar</h1>
        <p class="muted">Hesap bilgisi ve toplam sayılar. Görev içerikleri, puanlar ve ilgiler burada görünmez.</p>
        <input class="input" type="search" placeholder="E-posta veya ad ara" aria-label="Kullanıcı ara"
               [ngModel]="query()" (ngModelChange)="onSearch($event)" />
        <lq-segmented ariaLabel="Filtre" [options]="filters" [value]="filter()" (valueChange)="setFilter($event)" />
      </header>

      @if (error()) {
        <lq-empty-state icon="info" title="Kullanıcılar yüklenemedi" [message]="error()" />
      } @else if (page(); as p) {
        <p class="muted small">{{ p.totalCount }} hesap</p>
        <ul class="list">
          @for (user of p.items; track user.id) {
            <li class="surface user">
              <div class="user__head">
                <div class="user__name">
                  <strong>{{ user.displayName }}</strong>
                  <span class="muted small">{{ user.email }}</span>
                </div>
                <div class="row wrap">
                  @if (user.role === 'admin') { <span class="pill pill--brand">Admin</span> }
                  @if (user.isSuspended) { <span class="pill pill--danger">Askıda</span> }
                  @if (user.isSelf) { <span class="pill">Sen</span> }
                </div>
              </div>
              <dl class="facts">
                <div><dt>Kayıt</dt><dd>{{ date(user.createdAt) }}</dd></div>
                <div><dt>Son giriş</dt><dd>{{ user.lastLoginAt ? date(user.lastLoginAt) : '–' }}</dd></div>
                <div><dt>Tamamlanan</dt><dd>{{ user.completedQuests }}</dd></div>
                <div><dt>Life XP</dt><dd>{{ user.lifeXp }}</dd></div>
              </dl>
              @if (user.isSuspended) {
                <p class="notice small">
                  {{ user.suspendedUntil ? date(user.suspendedUntil) + ' tarihine kadar' : 'Süresiz' }} askıda · {{ user.suspensionReason }}
                </p>
              }
              @if (user.isSelf) {
                <p class="muted small">Kendi hesabında yönetim işlemi yapamazsın.</p>
              } @else {
                <div class="actions">
                  @if (user.isSuspended) {
                    <button lq-button variant="soft" size="sm" (click)="openAction(user, 'unsuspend')">Askıyı kaldır</button>
                  } @else if (user.role !== 'admin') {
                    <button lq-button variant="soft" size="sm" (click)="openAction(user, 'suspend')">Askıya al</button>
                  }
                  <button lq-button variant="soft" size="sm" [disabled]="user.isBootstrapAdmin" (click)="openAction(user, 'role')">
                    {{ user.role === 'admin' ? 'Admin rolünü al' : 'Admin yap' }}
                  </button>
                  @if (user.role !== 'admin') {
                    <button lq-button variant="danger" size="sm" (click)="openAction(user, 'delete')">Sil</button>
                  }
                </div>
                @if (user.isBootstrapAdmin) {
                  <p class="muted small">Bu hesap Admin:BootstrapEmails ayarından admin; rolü panelden alınamaz.</p>
                }
              }
            </li>
          } @empty {
            <lq-empty-state icon="users" title="Kullanıcı bulunamadı" message="Aramanı veya filtreni değiştirmeyi dene." />
          }
        </ul>
        @if (p.totalPages > 1) {
          <nav class="pager" aria-label="Sayfalar">
            <button lq-button variant="soft" size="sm" [disabled]="!p.hasPreviousPage" (click)="go(p.pageNumber - 1)">Önceki</button>
            <span class="muted small">{{ p.pageNumber }} / {{ p.totalPages }}</span>
            <button lq-button variant="soft" size="sm" [disabled]="!p.hasNextPage" (click)="go(p.pageNumber + 1)">Sonraki</button>
          </nav>
        }
      } @else {
        <lq-skeleton [height]="140" />
        <lq-skeleton [height]="140" />
      }
    </section>

    <lq-sheet [title]="sheetTitle()" [open]="!!selected()" (openChange)="$event || close()">
      @if (selected(); as user) {
        <form class="stack" (ngSubmit)="confirm()">
          <p><strong>{{ user.displayName }}</strong> · <span class="muted">{{ user.email }}</span></p>

          @switch (action()) {
            @case ('suspend') {
              <div class="field">
                <span class="field__label">Süre</span>
                <div class="row wrap">
                  @for (option of suspendOptions; track option.label) {
                    <button type="button" lq-chip [selected]="days() === option.value" (click)="days.set(option.value)">{{ option.label }}</button>
                  }
                </div>
              </div>
              <p class="muted small">Açık oturumlar hemen kapanır. Kullanıcı süre bitene veya askı kaldırılana kadar giriş yapamaz.</p>
            }
            @case ('unsuspend') {
              <p class="muted">Kullanıcı yeniden giriş yapabilecek.</p>
            }
            @case ('role') {
              <p class="muted">
                {{ user.role === 'admin'
                  ? 'Kullanıcı yönetim paneline erişimini kaybedecek.'
                  : 'Kullanıcı kullanıcıları, kataloğu ve öneri ayarlarını yönetebilecek.' }}
                Değişiklik açık oturumlarda da hemen geçerli olur.
              </p>
            }
            @case ('delete') {
              <p class="notice">Hesap ve tüm verisi (profil, görevler, XP, özetler) kalıcı olarak silinir. Bu işlem geri alınamaz.</p>
              <div class="field">
                <label for="confirm-email">Onay için e-posta adresini yaz</label>
                <input id="confirm-email" class="input" name="confirmEmail" autocomplete="off" [(ngModel)]="confirmEmail" />
              </div>
            }
          }

          @if (action() === 'suspend' || action() === 'delete') {
            <div class="field">
              <label for="reason">Gerekçe</label>
              <textarea id="reason" class="input" name="reason" rows="3" maxlength="500" [(ngModel)]="reason"
                        placeholder="Denetim kaydına yazılır"></textarea>
            </div>
          }

          @if (actionError()) {
            <p class="field__error" role="alert">{{ actionError() }}</p>
          }
          <button lq-button type="submit" [variant]="action() === 'delete' ? 'danger' : 'primary'" [block]="true" [loading]="busy()"
                  [disabled]="!canConfirm()">{{ confirmLabel() }}</button>
        </form>
      }
    </lq-sheet>
  `,
  styles: `
    .section { display: flex; flex-direction: column; gap: var(--space-4); }
    .small { font-size: var(--fs-sm); }
    .wrap { flex-wrap: wrap; }
    .list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 12px; }
    .user { padding: var(--space-4); display: flex; flex-direction: column; gap: 12px; }
    .user__head { display: flex; justify-content: space-between; align-items: flex-start; gap: 8px; flex-wrap: wrap; }
    .user__name { display: flex; flex-direction: column; min-width: 0; }
    .user__name span { overflow-wrap: anywhere; }
    .facts { display: grid; grid-template-columns: repeat(2, 1fr); gap: 8px 12px; margin: 0; }
    .facts div { display: flex; flex-direction: column; }
    .facts dt { font-size: var(--fs-xs); font-weight: 700; color: var(--ink-3); }
    .facts dd { margin: 0; font-weight: 800; font-variant-numeric: tabular-nums; }
    @media (min-width: 560px) { .facts { grid-template-columns: repeat(4, 1fr); } }
    .actions { display: flex; flex-wrap: wrap; gap: 8px; }
    .notice { padding: 10px 12px; border-radius: var(--radius-md); background: var(--danger-soft); color: var(--danger); font-weight: 700; }
    .pager { display: flex; justify-content: space-between; align-items: center; }
  `,
})
export class AdminUsersPage {
  private readonly api = inject(AdminApi);
  private readonly toast = inject(ToastService);

  protected readonly filters: SegmentOption<AdminUserFilter>[] = [
    { value: 'All', label: 'Tümü' },
    { value: 'Admins', label: 'Admin' },
    { value: 'Suspended', label: 'Askıda' },
  ];
  protected readonly suspendOptions = SUSPEND_OPTIONS;

  protected readonly query = signal('');
  private readonly search = signal('');
  protected readonly filter = signal<AdminUserFilter>('All');
  private readonly pageNumber = signal(1);
  protected readonly page = signal<PagedResult<AdminUser> | null>(null);
  protected readonly error = signal<string | null>(null);

  protected readonly selected = signal<AdminUser | null>(null);
  protected readonly action = signal<UserAction | null>(null);
  protected readonly days = signal<number | null>(7);
  protected reason = '';
  protected confirmEmail = '';
  protected readonly busy = signal(false);
  protected readonly actionError = signal<string | null>(null);
  private searchTimer?: ReturnType<typeof setTimeout>;

  protected readonly sheetTitle = computed(() => {
    const user = this.selected();
    switch (this.action()) {
      case 'suspend': return 'Hesabı askıya al';
      case 'unsuspend': return 'Askıyı kaldır';
      case 'role': return user?.role === 'admin' ? 'Admin rolünü al' : 'Admin yap';
      case 'delete': return 'Hesabı kalıcı olarak sil';
      default: return '';
    }
  });

  protected readonly confirmLabel = computed(() => (this.action() === 'delete' ? 'Kalıcı olarak sil' : 'Onayla'));

  constructor() {
    effect(() => this.load(this.search(), this.filter(), this.pageNumber()));
  }

  protected onSearch(value: string): void {
    this.query.set(value);
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.pageNumber.set(1);
      this.search.set(value.trim());
    }, 300);
  }

  protected setFilter(filter: AdminUserFilter): void {
    this.filter.set(filter);
    this.pageNumber.set(1);
  }

  protected go(page: number): void {
    this.pageNumber.set(page);
  }

  protected date(value: string): string {
    return formatDate(value, { day: 'numeric', month: 'short', year: 'numeric' });
  }

  protected openAction(user: AdminUser, action: UserAction): void {
    this.reason = '';
    this.confirmEmail = '';
    this.days.set(7);
    this.actionError.set(null);
    this.action.set(action);
    this.selected.set(user);
  }

  protected close(): void {
    this.selected.set(null);
    this.action.set(null);
  }

  protected canConfirm(): boolean {
    const action = this.action();
    if (action === 'suspend') return this.reason.trim().length > 0;
    if (action === 'delete')
      return this.reason.trim().length > 0 && this.confirmEmail.trim().toLowerCase() === this.selected()?.email;
    return true;
  }

  protected confirm(): void {
    const user = this.selected();
    const action = this.action();
    if (!user || !action || !this.canConfirm()) return;

    this.busy.set(true);
    this.actionError.set(null);
    const request: Observable<unknown> =
      action === 'suspend' ? this.api.suspend(user.id, this.days(), this.reason.trim())
      : action === 'unsuspend' ? this.api.unsuspend(user.id)
      : action === 'role' ? this.api.setRole(user.id, user.role === 'admin' ? 'user' : 'admin')
      : this.api.deleteUser(user.id, this.reason.trim(), this.confirmEmail.trim());

    request.subscribe({
      next: () => {
        this.busy.set(false);
        this.close();
        this.toast.success(action === 'delete' ? 'Hesap silindi.' : 'Değişiklik kaydedildi.');
        this.load(this.search(), this.filter(), this.pageNumber());
      },
      error: (err: unknown) => {
        this.busy.set(false);
        this.actionError.set(firstErrorMessage(err));
      },
    });
  }

  private load(search: string, filter: AdminUserFilter, pageNumber: number): void {
    this.error.set(null);
    this.api.users(search, filter, pageNumber).subscribe({
      next: (page) => this.page.set(page),
      error: (err: unknown) => this.error.set(firstErrorMessage(err)),
    });
  }
}
