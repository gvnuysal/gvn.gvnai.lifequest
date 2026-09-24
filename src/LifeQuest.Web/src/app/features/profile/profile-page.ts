import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked, WritableSignal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { catchError, Observable, of } from 'rxjs';
import { CatalogApi, ProfileApi } from '../../core/api/api-clients';
import { CostBand, DiscoveryRadius, LifeCategory, NotificationPreference, PhysicalEffort, Profile } from '../../core/api/models';
import { AuthStore } from '../../core/auth/auth.store';
import { firstErrorMessage } from '../../core/http/api-error';
import {
  CATEGORIES,
  CATEGORY_ORDER,
  COST_LABELS,
  COST_ORDER,
  EFFORT_LIMIT_OPTIONS,
  NOTIFICATION_LABELS,
  RADIUS_LABELS,
  WEEKLY_TIME_OPTIONS,
} from '../../core/labels/labels';
import { ProfileStore } from '../../core/state/profile.store';
import { ThemePreference, ThemeService } from '../../core/state/theme.service';
import { ToastService } from '../../core/state/toast.service';
import { Button } from '../../ui/button';
import { CategoryBadge } from '../../ui/category-badge';
import { Chip } from '../../ui/chip';
import { Icon } from '../../ui/icon';
import { InterestPicker, LOVE_WEIGHT } from '../../ui/interest-picker';
import { Segmented, SegmentOption } from '../../ui/segmented';
import { Sheet } from '../../ui/sheet';
import { Skeleton } from '../../ui/states';

@Component({
  selector: 'lq-profile-page',
  imports: [FormsModule, RouterLink, Button, CategoryBadge, Chip, Icon, InterestPicker, Segmented, Sheet, Skeleton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './profile-page.html',
  styleUrl: './profile-page.scss',
})
export class ProfilePage {
  private readonly profileApi = inject(ProfileApi);
  private readonly profiles = inject(ProfileStore);
  private readonly auth = inject(AuthStore);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  protected readonly theme = inject(ThemeService);

  protected readonly profile = this.profiles.profile;
  protected readonly catalog = toSignal(inject(CatalogApi).interests().pipe(catchError(() => of([]))), { initialValue: [] });

  protected readonly categories = CATEGORY_ORDER;
  protected readonly categoryMeta = CATEGORIES;
  protected readonly loveWeight = LOVE_WEIGHT;
  protected readonly radiusOptions: SegmentOption<DiscoveryRadius>[] = (['Chill', 'Explore', 'SurpriseMe'] as const).map((r) => ({
    value: r,
    label: RADIUS_LABELS[r].label,
  }));
  protected readonly costOptions: SegmentOption<CostBand>[] = COST_ORDER.map((c) => ({ value: c, label: COST_LABELS[c].label }));
  protected readonly timeOptions: SegmentOption<number>[] = WEEKLY_TIME_OPTIONS.map((o) => ({ value: o.minutes, label: o.short }));
  protected readonly effortOptions: SegmentOption<PhysicalEffort>[] = EFFORT_LIMIT_OPTIONS.map((o) => ({ value: o.value, label: o.label }));
  protected readonly notificationOptions: SegmentOption<NotificationPreference>[] = (['WeeklySummary', 'Off'] as const).map((v) => ({
    value: v,
    label: NOTIFICATION_LABELS[v],
  }));
  protected readonly isAdmin = this.auth.isAdmin;
  protected readonly themeOptions: SegmentOption<ThemePreference>[] = [
    { value: 'system', label: 'Sistem' },
    { value: 'light', label: 'Açık' },
    { value: 'dark', label: 'Koyu' },
  ];

  // Düzenlenebilir tercih kopyası
  protected readonly radius = signal<DiscoveryRadius>('Explore');
  protected readonly budget = signal<CostBand>('Low');
  protected readonly weeklyMinutes = signal(300);
  protected readonly goals = signal<LifeCategory[]>([]);
  protected readonly city = signal('');
  protected readonly maxEffort = signal<PhysicalEffort>('Vigorous');
  protected readonly notifications = signal<NotificationPreference>('WeeklySummary');
  protected readonly savingPreferences = signal(false);

  protected readonly interestsOpen = signal(false);
  protected readonly interestDraft = signal<Record<string, number>>({});
  protected readonly savingInterests = signal(false);

  protected readonly deleteOpen = signal(false);
  protected readonly deletePassword = signal('');
  protected readonly deleting = signal(false);

  protected readonly radiusDescription = computed(() => RADIUS_LABELS[this.radius()].description);
  protected readonly initial = computed(() => this.profile()?.displayName.charAt(0).toLocaleUpperCase('tr-TR') ?? '?');
  protected readonly dirty = computed(() => {
    const p = this.profile();
    if (!p) return false;
    return (
      p.discoveryRadius !== this.radius() ||
      p.budget !== this.budget() ||
      p.weeklyAvailableMinutes !== this.weeklyMinutes() ||
      (p.city ?? '') !== this.city().trim() ||
      p.maxPhysicalEffort !== this.maxEffort() ||
      p.notificationPreference !== this.notifications() ||
      [...p.goals].sort().join() !== [...this.goals()].sort().join()
    );
  });

  constructor() {
    void this.profiles.reload();
    effect(() => {
      const profile = this.profile();
      if (profile) untracked(() => this.resetForm(profile));
    });
  }

  protected toggleGoal(category: LifeCategory): void {
    this.goals.update((goals) => (goals.includes(category) ? goals.filter((g) => g !== category) : [...goals, category]));
  }

  protected snapWeeklyMinutes(minutes: number): number {
    return WEEKLY_TIME_OPTIONS.reduce((best, o) => (Math.abs(o.minutes - minutes) < Math.abs(best - minutes) ? o.minutes : best), 300);
  }

  protected savePreferences(): void {
    const city = this.city().trim();
    this.save(
      this.profileApi.updatePreferences({
        discoveryRadius: this.radius(),
        budget: this.budget(),
        weeklyAvailableMinutes: this.weeklyMinutes(),
        goals: this.goals(),
        city: city || null,
        clearCity: !city,
        maxPhysicalEffort: this.maxEffort(),
        notificationPreference: this.notifications(),
      }),
      this.savingPreferences,
      'Tercihlerin kaydedildi. Yarınki öneriler buna göre şekillenecek.',
    );
  }

  protected openInterests(): void {
    const draft: Record<string, number> = {};
    for (const interest of this.profile()?.interests ?? []) {
      if (interest.source === 'Explicit') draft[interest.code] = interest.weight;
    }
    this.interestDraft.set(draft);
    this.interestsOpen.set(true);
  }

  protected saveInterests(): void {
    const interests = Object.entries(this.interestDraft()).map(([code, weight]) => ({ code, weight }));
    if (interests.length === 0) {
      this.toast.error('En az bir ilgi alanı seçmelisin.');
      return;
    }
    this.save(this.profileApi.setInterests(interests), this.savingInterests, 'İlgi alanların güncellendi.', () =>
      this.interestsOpen.set(false),
    );
  }

  protected logout(): void {
    void this.auth.logout();
  }

  protected deleteAccount(): void {
    this.deleting.set(true);
    this.profileApi.deleteAccount(this.deletePassword()).subscribe({
      next: () => {
        this.deleteOpen.set(false);
        this.auth.clear();
        this.toast.show('Hesabın ve tüm verilerin silindi.');
        void this.router.navigate(['/kayit']);
      },
      error: (err: unknown) => {
        this.toast.error(firstErrorMessage(err));
        this.deleting.set(false);
      },
    });
  }

  private save(request: Observable<Profile>, busy: WritableSignal<boolean>, message: string, after?: () => void): void {
    busy.set(true);
    request.subscribe({
      next: (profile) => {
        this.profiles.set(profile);
        busy.set(false);
        after?.();
        this.toast.success(message);
      },
      error: (err: unknown) => {
        busy.set(false);
        this.toast.error(firstErrorMessage(err));
      },
    });
  }

  private resetForm(profile: Profile): void {
    this.radius.set(profile.discoveryRadius);
    this.budget.set(profile.budget);
    this.weeklyMinutes.set(this.snapWeeklyMinutes(profile.weeklyAvailableMinutes));
    this.goals.set([...profile.goals]);
    this.city.set(profile.city ?? '');
    this.maxEffort.set(profile.maxPhysicalEffort === 'None' ? 'Light' : profile.maxPhysicalEffort);
    this.notifications.set(profile.notificationPreference);
  }
}
