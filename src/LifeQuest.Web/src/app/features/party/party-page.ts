import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { PartiesApi } from '../../core/api/api-clients';
import { PartyInvite } from '../../core/api/models';
import { firstErrorMessage } from '../../core/http/api-error';
import { formatDate } from '../../core/labels/format';
import { CATEGORIES } from '../../core/labels/labels';
import { APP_PATHS, questPath } from '../../core/routing/app-paths';
import { ToastService } from '../../core/state/toast.service';
import { Button } from '../../ui/button';
import { Icon } from '../../ui/icon';
import { EmptyState, Skeleton } from '../../ui/states';

/** Davet bağlantısı: görevi ve kurucuyu gösterir; katılınca görev kullanıcının listesine eklenir. */
@Component({
  selector: 'lq-party-page',
  imports: [RouterLink, Button, Icon, EmptyState, Skeleton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      @if (error()) {
        <lq-empty-state icon="users" title="Davet açılamadı" [message]="error()">
          <a lq-button variant="secondary" [routerLink]="paths.today">Bugüne dön</a>
        </lq-empty-state>
      } @else if (invite(); as i) {
        <section class="surface card stack invite">
          <span class="eyebrow"><lq-icon name="users" [size]="16" /> Quest Party</span>
          <h1>{{ i.hostName }} seni birlikte yapmaya çağırıyor</h1>
          <p class="quest"><strong>{{ i.questTitle }}</strong> · {{ category() }}</p>
          <p class="muted">
            {{ i.memberCount }}/{{ i.maxMembers }} kişi · {{ expires() }} tarihine kadar.
            Herkes kendi görevini kendi yapar; partide kalan herkes tamamlayınca tamamlayanlar ekstra "birlikte" XP'si kazanır.
          </p>

          @if (i.isMember && i.myQuestId) {
            <a lq-button [block]="true" [routerLink]="questPath(i.myQuestId)">Görevine git</a>
          } @else if (i.isJoinable) {
            <button lq-button [block]="true" [loading]="busy()" (click)="join(i.inviteCode)">Katıl</button>
            <p class="muted small">Katılınca görev aktif görevlerine eklenir. Diğer üyeler yalnızca görünen adını ve görevi tamamlayıp tamamlamadığını görür.</p>
          } @else {
            <p class="closed">Bu parti artık katılıma açık değil.</p>
          }
        </section>
      } @else {
        <lq-skeleton [height]="220" />
      }
    </div>
  `,
  styles: `
    .invite { padding: var(--space-5); gap: var(--space-3); }
    .eyebrow { display: inline-flex; align-items: center; gap: 6px; }
    h1 { margin: 0; font-size: var(--fs-xl); }
    .quest { margin: 0; }
    .small { font-size: var(--fs-sm); margin: 0; }
    .closed { font-weight: 700; margin: 0; }
  `,
})
export class PartyPage {
  readonly code = input.required<string>();

  protected readonly paths = APP_PATHS;
  protected readonly questPath = questPath;
  private readonly api = inject(PartiesApi);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  protected readonly invite = signal<PartyInvite | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly busy = signal(false);
  protected readonly category = computed(() => {
    const i = this.invite();
    return i ? CATEGORIES[i.category].label : '';
  });
  protected readonly expires = computed(() => {
    const i = this.invite();
    return i ? formatDate(i.expiresAt, { day: 'numeric', month: 'long', hour: '2-digit', minute: '2-digit' }) : '';
  });

  constructor() {
    effect(() => {
      const code = this.code();
      this.api.invite(code).subscribe({
        next: (invite) => this.invite.set(invite),
        error: (err: unknown) => this.error.set(firstErrorMessage(err)),
      });
    });
  }

  protected join(code: string): void {
    this.busy.set(true);
    this.api.join(code).subscribe({
      next: (invite) => {
        this.toast.success('Partiye katıldın. Görev aktif görevlerine eklendi.');
        void this.router.navigate(questPath(invite.myQuestId!));
      },
      error: (err: unknown) => {
        this.busy.set(false);
        this.toast.error(firstErrorMessage(err));
      },
    });
  }
}
