import { Location } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { QuestsApi } from '../../core/api/api-clients';
import { saveResponse } from '../../core/http/download';
import { NearbyPlace, Party, QuestCompletion, QuestDetail, SkipReason } from '../../core/api/models';
import { firstErrorMessage } from '../../core/http/api-error';
import { formatDate, formatDuration, formatRemaining } from '../../core/labels/format';
import {
  CATEGORIES,
  COST_LABELS,
  DIFFICULTY_LABELS,
  EFFORT_LABELS,
  QUEST_TYPE_LABELS,
  SCORE_COMPONENTS,
  SKIP_REASONS,
  STATUS_LABELS,
} from '../../core/labels/labels';
import { ToastService } from '../../core/state/toast.service';
import { Button } from '../../ui/button';
import { CategoryBadge, CategoryIcon } from '../../ui/category-badge';
import { Icon } from '../../ui/icon';
import { OptionCard } from '../../ui/option-card';
import { Rating } from '../../ui/rating';
import { Sheet } from '../../ui/sheet';
import { EmptyState, Skeleton } from '../../ui/states';
import { Celebration, FeedbackSubmission } from './celebration';
import { PartyCard } from './party-card';
import { APP_PATHS } from '../../core/routing/app-paths';

@Component({
  selector: 'lq-quest-page',
  imports: [RouterLink, Button, CategoryBadge, CategoryIcon, Icon, OptionCard, Rating, Sheet, EmptyState, Skeleton, Celebration, PartyCard],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './quest-page.html',
  styleUrl: './quest-page.scss',
})
export class QuestPage {
  protected readonly paths = APP_PATHS;
  private readonly api = inject(QuestsApi);
  private readonly toast = inject(ToastService);
  private readonly location = inject(Location);

  /** Route parametresi (withComponentInputBinding). */
  readonly id = input.required<string>();

  protected readonly detail = signal<QuestDetail | null>(null);
  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);
  protected readonly busy = signal(false);
  protected readonly whyOpen = signal(false);
  protected readonly skipOpen = signal(false);
  protected readonly skipReason = signal<SkipReason | null>(null);
  protected readonly completion = signal<QuestCompletion | null>(null);
  protected readonly rating = signal<number | null>(null);
  protected readonly planOpen = signal(false);
  protected readonly planValue = signal('');

  protected readonly skipReasons = SKIP_REASONS;
  protected readonly scoreComponents = SCORE_COMPONENTS;

  protected readonly quest = computed(() => this.detail()?.quest ?? null);
  protected readonly category = computed(() => (this.quest() ? CATEGORIES[this.quest()!.category] : null));
  protected readonly secondaryLabel = computed(() =>
    this.quest()?.secondaryCategory ? CATEGORIES[this.quest()!.secondaryCategory!].label : '',
  );
  protected readonly accent = computed(() => (this.category() ? `var(--cat-${this.category()!.token})` : 'var(--brand)'));
  protected readonly facts = computed(() => {
    const q = this.quest();
    if (!q) return [];
    return [
      { icon: 'clock' as const, label: 'Süre', value: formatDuration(q.minMinutes, q.maxMinutes) },
      { icon: 'coins' as const, label: 'Maliyet', value: COST_LABELS[q.cost].label },
      { icon: 'target' as const, label: 'Zorluk', value: DIFFICULTY_LABELS[q.difficulty] },
      { icon: 'flag' as const, label: 'Tür', value: QUEST_TYPE_LABELS[q.type] },
      { icon: 'activity' as const, label: 'Efor', value: EFFORT_LABELS[q.effort].label },
    ];
  });
  protected readonly statusLabel = computed(() => (this.quest() ? STATUS_LABELS[this.quest()!.status] : ''));
  protected readonly remaining = computed(() => (this.quest() ? formatRemaining(this.quest()!.expiresAt) : ''));
  protected readonly completedOn = computed(() =>
    this.quest()?.completedAt ? formatDate(this.quest()!.completedAt!, { day: 'numeric', month: 'long' }) : '',
  );

  constructor() {
    effect(() => this.load(this.id()));
  }

  protected readonly plannedLabel = computed(() => {
    const at = this.quest()?.plannedAt;
    return at ? formatDate(at, { weekday: 'long', day: 'numeric', month: 'long', hour: '2-digit', minute: '2-digit' }) : '';
  });
  protected readonly planMin = computed(() => toLocalInput(new Date()));
  protected readonly planMax = computed(() => {
    const expires = this.quest()?.expiresAt;
    return expires ? toLocalInput(new Date(new Date(expires).getTime() - 60_000)) : '';
  });

  protected back(): void {
    this.location.back();
  }

  protected accept(): void {
    this.run(this.api.accept(this.quest()!.id), (quest) => {
      this.patchQuest(quest);
      this.toast.success('Quest kabul edildi. Keyfini çıkar!');
    });
  }

  protected complete(): void {
    this.run(this.api.complete(this.quest()!.id), (result) => {
      this.patchQuest(result.quest);
      this.completion.set(result);
    });
  }

  protected confirmSkip(): void {
    const reason = this.skipReason();
    if (!reason) return;
    this.run(this.api.skip(this.quest()!.id, reason), (quest) => {
      this.patchQuest(quest);
      this.skipOpen.set(false);
      this.toast.show('Geçildi. Geri bildirimin sonraki önerileri şekillendirecek.');
    });
  }

  protected saveForLater(): void {
    this.run(this.api.save(this.quest()!.id), () =>
      this.toast.success('"Sonra yaparım" listene eklendi. Hazır olduğunda oradan başlatabilirsin.'));
  }

  protected openPlan(): void {
    const current = this.quest()?.plannedAt;
    const suggestion = new Date(Date.now() + 24 * 3600_000);
    suggestion.setHours(10, 0, 0, 0);
    this.planValue.set(toLocalInput(current ? new Date(current) : suggestion));
    this.planOpen.set(true);
  }

  protected savePlan(): void {
    this.run(this.api.plan(this.quest()!.id, this.planValue()), (quest) => {
      this.patchQuest(quest);
      this.planOpen.set(false);
      this.toast.success('Planlandı. "Takvime ekle" ile kendi takvimine koyabilirsin.');
    });
  }

  protected clearPlan(): void {
    this.run(this.api.plan(this.quest()!.id, null), (quest) => {
      this.patchQuest(quest);
      this.planOpen.set(false);
    });
  }

  protected downloadCalendar(): void {
    this.run(this.api.calendar(this.quest()!.id), (response) => saveResponse(response, 'lifequest.ics'));
  }

  /** Kutlama ekranından gelen değerlendirme. */
  protected onCelebrationDone(feedback: FeedbackSubmission): void {
    if (!feedback.rating && !feedback.preference) {
      this.completion.set(null);
      return;
    }
    this.sendFeedback(feedback, () => this.completion.set(null));
  }

  protected rateLater(): void {
    const rating = this.rating();
    if (!rating) return;
    this.sendFeedback({ rating, preference: null });
  }

  private sendFeedback(feedback: FeedbackSubmission, after?: () => void): void {
    this.run(this.api.feedback(this.quest()!.id, feedback.rating, feedback.preference), (result) => {
      this.patchQuest(result.quest);
      after?.();
      const achievement = result.newAchievements[0];
      this.toast.success(achievement ? `Yeni başarım: ${achievement.title}` : 'Teşekkürler! Önerilerin buna göre gelişecek.');
    });
  }

  private load(id: string): void {
    this.loading.set(true);
    this.notFound.set(false);
    this.api.get(id).subscribe({
      next: (detail) => {
        this.detail.set(detail);
        this.loading.set(false);
      },
      error: () => {
        this.notFound.set(true);
        this.loading.set(false);
      },
    });
  }

  protected partyCreated(party: Party): void {
    const current = this.detail();
    if (current) this.detail.set({ ...current, party });
  }

  /** Bağlam skoru gerçek dünya payıyla 1'i aşabilir; çubuk tam genişlikte durur. */
  protected barWidth(value: number): number {
    return Math.min(Math.max(value, 0), 1) * 100;
  }

  protected eventWhen(place: NearbyPlace): string {
    return formatDate(place.startsAt!, { weekday: 'long', day: 'numeric', month: 'long', hour: '2-digit', minute: '2-digit' });
  }

  private patchQuest(quest: QuestDetail['quest']): void {
    const current = this.detail();
    if (current) this.detail.set({ ...current, quest });
  }

  /** Aksiyon çalıştırır; 409 gibi iş kuralı hatalarında mesajı gösterip güncel durumu yeniden yükler. */
  private run<T>(request: Observable<T>, onSuccess: (value: T) => void): void {
    this.busy.set(true);
    request.subscribe({
      next: (value) => {
        onSuccess(value);
        this.busy.set(false);
      },
      error: (err: unknown) => {
        this.toast.error(firstErrorMessage(err));
        this.busy.set(false);
        this.load(this.id());
      },
    });
  }
}

/** datetime-local girdisi için yerel saat biçimi: 2026-10-03T10:00 */
function toLocalInput(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}
