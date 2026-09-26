import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { PartiesApi } from '../../core/api/api-clients';
import { Party, Quest } from '../../core/api/models';
import { firstErrorMessage } from '../../core/http/api-error';
import { partyInviteUrl } from '../../core/routing/app-paths';
import { ToastService } from '../../core/state/toast.service';
import { Button } from '../../ui/button';
import { Icon } from '../../ui/icon';

/**
 * Görev detayında Quest Party: kabul edilmiş görev için davet bağlantısı oluşturur ve paylaşır; parti varsa üyeleri
 * ve kimin tamamladığını gösterir. Bağlantıyı alan herkes katılabilir (en fazla 5 kişi); arkadaş listesi yoktur.
 */
@Component({
  selector: 'lq-party-card',
  imports: [Button, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="party surface" aria-labelledby="party-title">
      <h2 id="party-title" class="party__title"><lq-icon name="users" [size]="18" /> Birlikte yap</h2>

      @if (party(); as p) {
        @if (p.status === 'Completed') {
          <p class="done">Parti tamamlandı! Tamamlayan herkes "birlikte" XP'si kazandı.</p>
        }
        <ul class="members">
          @for (m of p.members; track $index) {
            <li [class.dropped]="m.dropped">
              <span>{{ m.displayName }}@if (m.isYou) { <span class="muted"> (sen)</span> }@if (m.isHost) { <span class="muted"> · kurucu</span> }</span>
              <span class="state">
                @if (m.completed) { <lq-icon name="check" [size]="16" /> tamamladı@if (m.bonusXp) { · +{{ m.bonusXp }} XP } }
                @else if (m.dropped) { bıraktı }
                @else { görevde }
              </span>
            </li>
          }
        </ul>
        @if (p.isJoinable) {
          <p class="muted small">Bağlantıyı paylaş: {{ p.members.length }}/{{ p.maxMembers }} kişi. Herkes kendi görevini yapar; partide kalan herkes tamamlayınca bonus gelir.</p>
          <div class="share">
            <input class="input" readonly [value]="link()" aria-label="Davet bağlantısı" (focus)="$any($event.target).select()" />
            <button lq-button size="sm" (click)="share()">{{ canShare ? 'Paylaş' : 'Kopyala' }}</button>
          </div>
        }
      } @else if (quest().status === 'Accepted') {
        <p class="muted">Bir arkadaşını davet et: ikiniz de görevi tamamlayınca ekstra "birlikte" XP'si kazanırsınız.</p>
        <button lq-button variant="secondary" [loading]="busy()" (click)="create()">Davet bağlantısı oluştur</button>
      }
    </section>
  `,
  styles: `
    .party { padding: var(--space-4); display: flex; flex-direction: column; gap: var(--space-3); }
    .party__title { display: flex; align-items: center; gap: 8px; margin: 0; font-size: var(--fs-md, 1rem); }
    .members { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 6px; }
    .members li { display: flex; justify-content: space-between; gap: 8px; font-size: var(--fs-sm); }
    .members li.dropped { opacity: 0.6; }
    .state { display: inline-flex; align-items: center; gap: 4px; color: var(--ink-2); }
    .share { display: flex; gap: 8px; }
    .share .input { flex: 1; min-width: 0; }
    .done { margin: 0; font-weight: 700; }
    .small { font-size: var(--fs-sm); margin: 0; }
  `,
})
export class PartyCard {
  readonly quest = input.required<Quest>();
  readonly party = input<Party | null>(null);
  readonly created = output<Party>();

  private readonly api = inject(PartiesApi);
  private readonly toast = inject(ToastService);

  protected readonly busy = signal(false);
  protected readonly canShare = typeof navigator !== 'undefined' && typeof navigator.share === 'function';
  protected readonly link = computed(() => {
    const p = this.party();
    return p ? partyInviteUrl(p.inviteCode) : '';
  });

  protected create(): void {
    this.busy.set(true);
    this.api.create(this.quest().id).subscribe({
      next: (party) => {
        this.busy.set(false);
        this.created.emit(party);
      },
      error: (err: unknown) => {
        this.busy.set(false);
        this.toast.error(firstErrorMessage(err));
      },
    });
  }

  protected async share(): Promise<void> {
    const p = this.party();
    if (!p) return;
    const text = `"${p.questTitle}" görevini birlikte yapalım mı? LifeQuest'te partime katıl:`;
    try {
      if (this.canShare) {
        await navigator.share({ title: 'Quest Party', text, url: this.link() });
      } else {
        await navigator.clipboard.writeText(this.link());
        this.toast.success('Bağlantı kopyalandı.');
      }
    } catch {
      // Paylaşım iptal edildi.
    }
  }
}
