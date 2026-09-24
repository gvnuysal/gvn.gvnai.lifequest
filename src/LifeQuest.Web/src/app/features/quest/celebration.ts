import { ChangeDetectionStrategy, Component, OnInit, input, output, signal } from '@angular/core';
import { Achievement, FeedbackPreference, QuestCompletion } from '../../core/api/models';
import { Button } from '../../ui/button';
import { Icon } from '../../ui/icon';
import { Rating } from '../../ui/rating';

export interface FeedbackSubmission {
  rating: number | null;
  preference: FeedbackPreference | null;
}

/**
 * Tamamlama kutlaması + deneyim değerlendirmesi. Ödül dili olumludur; kayıp korkusu veya streak yoktur.
 */
@Component({
  selector: 'lq-celebration',
  imports: [Button, Icon, Rating],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { role: 'dialog', 'aria-modal': 'true', 'aria-labelledby': 'celebration-title' },
  template: `
    <div class="panel">
      <div class="burst" aria-hidden="true">
        @for (i of confetti; track i) {
          <span [style.--i]="i"></span>
        }
      </div>
      <div class="medal"><lq-icon name="trophy" [size]="40" /></div>
      <h2 id="celebration-title">{{ completion().alreadyCompleted ? 'Bu quest zaten tamamlanmış' : 'Harika, tamamladın!' }}</h2>
      @if (!completion().alreadyCompleted) {
        <p class="xp">+{{ shownXp() }} <span>XP</span></p>
        @if (completion().leveledUp) {
          <p class="level-up"><lq-icon name="star" [size]="18" [strokeWidth]="2.6" /> Seviye {{ completion().lifeLevel }}'e ulaştın!</p>
        }
        @for (achievement of achievements(); track achievement.code) {
          <p class="achievement"><lq-icon name="trophy" [size]="16" /> Yeni başarım: <strong>{{ achievement.title }}</strong></p>
        }
      }

      <div class="feedback">
        <p class="question">Deneyim nasıldı?</p>
        <lq-rating [(value)]="rating" />
        <div class="prefs" role="group" aria-label="Benzer öneriler">
          <button type="button" [class.on]="preference() === 'MoreLikeThis'" [attr.aria-pressed]="preference() === 'MoreLikeThis'" (click)="toggle('MoreLikeThis')">
            <lq-icon name="thumbs-up" [size]="18" /> Daha fazla göster
          </button>
          <button type="button" [class.on]="preference() === 'LessLikeThis'" [attr.aria-pressed]="preference() === 'LessLikeThis'" (click)="toggle('LessLikeThis')">
            <lq-icon name="thumbs-down" [size]="18" /> Daha az göster
          </button>
        </div>
      </div>

      <button lq-button [block]="true" [loading]="busy()" [disabled]="busy()" (click)="submit()">
        {{ rating() || preference() ? 'Kaydet ve devam et' : 'Devam et' }}
      </button>
    </div>
  `,
  styles: `
    :host {
      position: fixed; inset: 0; z-index: 50; display: grid; place-items: end center;
      background: var(--overlay); animation: fade 0.2s;
    }
    @media (min-width: 640px) { :host { place-items: center; } }
    .panel {
      position: relative; width: min(100%, var(--content-width)); padding: var(--space-8) var(--space-5) calc(var(--space-6) + env(safe-area-inset-bottom));
      border-radius: var(--radius-xl) var(--radius-xl) 0 0; background: var(--surface); text-align: center;
      display: flex; flex-direction: column; align-items: center; gap: 10px; overflow: hidden; animation: rise 0.35s var(--ease);
    }
    @media (min-width: 640px) { .panel { border-radius: var(--radius-xl); } }
    .medal { display: grid; place-items: center; width: 84px; height: 84px; border-radius: 50%; background: var(--xp-soft); color: var(--xp-ink); animation: pop 0.5s var(--ease); }
    .xp { font-size: var(--fs-3xl); font-weight: 900; color: var(--xp-ink); line-height: 1; }
    .xp span { font-size: var(--fs-lg); }
    .level-up, .achievement { display: inline-flex; align-items: center; gap: 6px; padding: 6px 12px; border-radius: var(--radius-pill); background: var(--primary-soft); color: var(--primary-text); font-weight: 800; }
    .achievement { background: var(--success-soft); color: var(--success); }
    .feedback { display: flex; flex-direction: column; align-items: center; gap: 6px; width: 100%; margin: var(--space-3) 0; padding-top: var(--space-4); border-top: 1px solid var(--line); }
    .question { font-weight: 800; }
    .prefs { display: flex; gap: 8px; flex-wrap: wrap; justify-content: center; }
    .prefs button {
      display: inline-flex; align-items: center; gap: 6px; min-height: 40px; padding: 0 14px; border-radius: var(--radius-pill);
      border: 1.5px solid var(--line); background: var(--surface); color: var(--ink-2); font-weight: 700; font-size: var(--fs-sm); cursor: pointer;
    }
    .prefs button.on { border-color: var(--brand); background: var(--primary-soft); color: var(--primary-text); }
    .burst { position: absolute; inset: 0; pointer-events: none; }
    .burst span {
      position: absolute; top: 90px; left: 50%; width: 8px; height: 14px; border-radius: 2px;
      background: hsl(calc(var(--i) * 37deg) 85% 62%);
      animation: confetti 1.1s var(--ease) forwards; animation-delay: calc(var(--i) * 12ms); opacity: 0;
      --dx: calc((var(--i) - 7) * 22px);
    }
    @keyframes confetti {
      0% { opacity: 1; transform: translate(0, 0) rotate(0); }
      100% { opacity: 0; transform: translate(var(--dx), calc(-60px + var(--i) * 9px)) rotate(calc(var(--i) * 40deg)); }
    }
    @keyframes pop { 0% { transform: scale(0.4); } 70% { transform: scale(1.12); } }
    @keyframes rise { from { transform: translateY(40px); opacity: 0; } }
    @keyframes fade { from { opacity: 0; } }
  `,
})
export class Celebration implements OnInit {
  readonly completion = input.required<QuestCompletion>();
  readonly achievements = input<Achievement[]>([]);
  readonly busy = input(false);
  readonly done = output<FeedbackSubmission>();

  protected readonly confetti = Array.from({ length: 15 }, (_, i) => i);
  protected readonly rating = signal<number | null>(null);
  protected readonly preference = signal<FeedbackPreference | null>(null);
  protected readonly shownXp = signal(0);

  ngOnInit(): void {
    const target = this.completion().quest.reward.lifeXp;
    const reduceMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
    if (reduceMotion) {
      this.shownXp.set(target);
      return;
    }
    const start = performance.now();
    const tick = (now: number) => {
      const t = Math.min(1, (now - start) / 900);
      this.shownXp.set(Math.round(target * (1 - Math.pow(1 - t, 3))));
      if (t < 1) requestAnimationFrame(tick);
    };
    requestAnimationFrame(tick);
  }

  protected toggle(preference: FeedbackPreference): void {
    this.preference.update((current) => (current === preference ? null : preference));
  }

  protected submit(): void {
    this.done.emit({ rating: this.rating(), preference: this.preference() });
  }
}
