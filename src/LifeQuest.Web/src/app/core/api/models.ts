// Backend DTO'larının TS karşılıkları (src/LifeQuest.Application/**).
// Controller'lar IActionResult döndüğü için OpenAPI dokümanında yanıt şeması yok; tipler elle tutulur.

export type LifeCategory = 'Explorer' | 'Culture' | 'Learning' | 'Social' | 'Fitness' | 'Creativity';
export type CostBand = 'Free' | 'Low' | 'Medium' | 'High';
export type DiscoveryRadius = 'Chill' | 'Explore' | 'SurpriseMe';
export type QuestType = 'Daily' | 'Weekly' | 'Adventure' | 'Epic';
export type Difficulty = 'Easy' | 'Medium' | 'Hard' | 'Heroic';
export type QuestStatus = 'Offered' | 'Accepted' | 'Completed' | 'Skipped' | 'Expired';
export type QuestSource = 'Daily' | 'OnDemand' | 'Saved';
export type SkipReason = 'NotInterested' | 'TooExpensive' | 'NoTime' | 'TooFar' | 'NotToday' | 'Other';
export type FeedbackPreference = 'MoreLikeThis' | 'LessLikeThis';
export type InterestSource = 'Explicit' | 'Learned';
export type PhysicalEffort = 'None' | 'Light' | 'Moderate' | 'Vigorous';
export type NotificationPreference = 'Off' | 'WeeklySummary';
export type StarterReactionType = 'Like' | 'Dislike';
export type ErrorType = 'Failure' | 'Validation' | 'NotFound' | 'Conflict' | 'Unauthorized';

export interface ApiError {
  code: string;
  message: string;
  type: ErrorType;
}

// ── Auth ─────────────────────────────────────────────────────────────────────
/** Refresh token HttpOnly çerezdedir; JavaScript'e yalnızca access token ve bitiş zamanları gelir. */
export interface AuthSession {
  userId: string;
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  displayName: string;
  birthYear: number;
  /** Arayüz dili; hesaba yazılır. */
  language?: 'tr' | 'en';
}

export interface LoginRequest {
  email: string;
  password: string;
}

// ── Katalog & profil ─────────────────────────────────────────────────────────
export interface Interest {
  id: string;
  code: string;
  name: string;
  category: LifeCategory;
}

export interface ProfileInterest extends Interest {
  weight: number;
  source: InterestSource;
}

export interface Profile {
  userId: string;
  displayName: string;
  email: string;
  onboardingCompleted: boolean;
  discoveryRadius: DiscoveryRadius;
  budget: CostBand;
  weeklyAvailableMinutes: number;
  goals: LifeCategory[];
  city: string | null;
  timeZoneId: string;
  maxPhysicalEffort: PhysicalEffort;
  notificationPreference: NotificationPreference;
  /** Günlük push hatırlatmasının yerel saati (7–22); null = kapalı. */
  dailyReminderHour: number | null;
  interests: ProfileInterest[];
  /** Hesap dili: arayüz, push ve haftalık özet. */
  language: 'tr' | 'en';
}

export interface InterestSelection {
  code: string;
  weight?: number | null;
}

export interface OnboardingRequest {
  goals: LifeCategory[];
  interests: InterestSelection[];
  weeklyAvailableMinutes: number;
  budget: CostBand;
  discoveryRadius: DiscoveryRadius;
  city: string | null;
  timeZoneId: string | null;
  maxPhysicalEffort: PhysicalEffort;
  starterReactions: StarterReaction[];
}

export interface StarterReaction {
  templateCode: string;
  reaction: StarterReactionType;
}

export interface StarterCard {
  code: string;
  title: string;
  description: string;
  category: LifeCategory;
  cost: CostBand;
  minMinutes: number;
  maxMinutes: number;
}

export interface PreferencesRequest {
  discoveryRadius?: DiscoveryRadius | null;
  budget?: CostBand | null;
  weeklyAvailableMinutes?: number | null;
  goals?: LifeCategory[] | null;
  city?: string | null;
  clearCity?: boolean | null;
  timeZoneId?: string | null;
  maxPhysicalEffort?: PhysicalEffort | null;
  notificationPreference?: NotificationPreference | null;
  dailyReminderHour?: number | null;
  clearDailyReminder?: boolean | null;
  language?: 'tr' | 'en' | null;
}

export interface PushSettings {
  /** Sunucuda VAPID anahtarları tanımlı mı. */
  enabled: boolean;
  publicKey: string | null;
  devices: number;
}

export interface PushSubscriptionRequest {
  endpoint: string;
  keys: { p256dh: string; auth: string };
}

// ── Quest ────────────────────────────────────────────────────────────────────
export interface QuestReward {
  lifeXp: number;
  primaryCategoryXp: number;
  secondaryCategoryXp: number;
}

export interface Quest {
  id: string;
  title: string;
  description: string;
  type: QuestType;
  difficulty: Difficulty;
  category: LifeCategory;
  secondaryCategory: LifeCategory | null;
  minMinutes: number;
  maxMinutes: number;
  cost: CostBand;
  effort: PhysicalEffort;
  reward: QuestReward;
  status: QuestStatus;
  source: QuestSource;
  offeredAt: string;
  expiresAt: string;
  acceptedAt: string | null;
  completedAt: string | null;
  skipReason: SkipReason | null;
  rating: number | null;
  preference: FeedbackPreference | null;
  isExploration: boolean;
  explanation: string;
  plannedAt: string | null;
}

export interface ScoreBreakdown {
  interest: number;
  novelty: number;
  context: number;
  goalFit: number;
  diversity: number;
  feedbackFit: number;
  repetition: number;
  friction: number;
  risk: number;
  total: number;
}

export interface QuestDetail {
  quest: Quest;
  score: ScoreBreakdown;
  reasonCodes: string[];
  /** Kullanıcının şehrinde bu göreve bağlı mekân ve yaklaşan etkinlikler (yalnızca açık görevlerde). */
  nearbyPlaces: NearbyPlace[];
  party: Party | null;
}

export type PartyStatus = 'Open' | 'Completed';

export interface PartyMember {
  displayName: string;
  isHost: boolean;
  isYou: boolean;
  completed: boolean;
  dropped: boolean;
  bonusXp: number;
}

export interface Party {
  inviteCode: string;
  questTitle: string;
  category: LifeCategory;
  status: PartyStatus;
  expiresAt: string;
  maxMembers: number;
  isJoinable: boolean;
  members: PartyMember[];
}

export interface PartyInvite {
  inviteCode: string;
  questTitle: string;
  category: LifeCategory;
  hostName: string;
  memberCount: number;
  maxMembers: number;
  expiresAt: string;
  isMember: boolean;
  isJoinable: boolean;
  myQuestId: string | null;
}

export type OutdoorWeather = 'Unknown' | 'Good' | 'Poor';

export interface WeatherInfo {
  city: string;
  temperatureC: number;
  summary: string;
  outdoor: OutdoorWeather;
  /** Hava açık hava için uygun değilse kısa açıklama. */
  advice: string | null;
  /** WMO hava kodu. */
  code: number;
}

export type LocalPlaceKind = 'Venue' | 'Event';

export interface NearbyPlace {
  id: string;
  kind: LocalPlaceKind;
  name: string;
  address: string | null;
  url: string | null;
  note: string | null;
  startsAt: string | null;
  endsAt: string | null;
}

export interface QuestList {
  date: string;
  quests: Quest[];
  message: string | null;
  weather: WeatherInfo | null;
}

export interface Achievement {
  code: string;
  title: string;
  description: string;
  unlocked: boolean;
  unlockedAt: string | null;
}

export interface QuestCompletion {
  quest: Quest;
  alreadyCompleted: boolean;
  lifeXp: number;
  lifeLevel: number;
  leveledUp: boolean;
  newAchievements: Achievement[];
  /** Quest Party bu tamamlamayla bittiyse kazanılan "birlikte" XP'si. */
  partyBonusXp: number;
}

export interface QuestFeedbackResult {
  quest: Quest;
  newAchievements: Achievement[];
}

export interface SuggestRequest {
  availableMinutes: number | null;
  maxCost: CostBand | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

// ── İlerleme ─────────────────────────────────────────────────────────────────
export interface CategoryProgress {
  category: LifeCategory;
  displayName: string;
  xp: number;
  level: number;
  nextLevelXp: number;
  completedCount: number;
}

export interface XpEntry {
  at: string;
  description: string;
  lifeXp: number;
  category: LifeCategory;
  categoryXp: number;
}

export interface Progress {
  lifeXp: number;
  lifeLevel: number;
  currentLevelXp: number;
  nextLevelXp: number;
  levelProgress: number;
  totalCompleted: number;
  categories: CategoryProgress[];
  recentXp: XpEntry[];
}

// ── Bildirim & yönetim ──────────────────────────────────────────────────────
export interface WeeklySummary {
  id: string;
  weekStart: string;
  title: string;
  message: string;
  completedCount: number;
  xpEarned: number;
  newCategories: LifeCategory[];
  topCategory: LifeCategory | null;
  createdAt: string;
}

export interface Share<T> {
  key: T;
  count: number;
  share: number;
}

export interface ProductMetrics {
  from: string;
  to: string;
  activeUsers: number;
  meaningfulCompletions: number;
  northStar: number;
  funnel: { offered: number; accepted: number; completed: number; acceptanceRate: number; completionRate: number };
  newCategoryDiscoveryRate: number;
  explorationAcceptanceRate: number;
  averageRating: number | null;
  skipReasons: Share<SkipReason>[];
  completionsByCategory: Share<LifeCategory>[];
}

// ── Yönetim (yalnızca admin) ───────────────────────────────────────────────

export type SafetyLevel = 'Safe' | 'NeedsReview' | 'Blocked';
export type EditorialSource = 'Seed' | 'Admin';
export type DayPart = 'Morning' | 'Afternoon' | 'Evening' | 'Night';
export type AdminUserFilter = 'All' | 'Admins' | 'Suspended';
export type UserRole = 'user' | 'admin';
export type AdminAction =
  | 'UserSuspended'
  | 'UserUnsuspended'
  | 'UserDeleted'
  | 'UserRoleChanged'
  | 'TemplateCreated'
  | 'TemplateUpdated'
  | 'TemplateSafetyChanged'
  | 'TemplateActivated'
  | 'TemplateDeactivated'
  | 'WeightsUpdated'
  | 'WeightsReset'
  | 'ExperimentCreated'
  | 'ExperimentStarted'
  | 'ExperimentStopped'
  | 'ExperimentAdopted'
  | 'ExperimentDiscarded'
  | 'IdeaRejected'
  | 'IdeaAccepted'
  | 'PlaceCreated'
  | 'PlaceUpdated'
  | 'PlaceDeleted';
export type AdminTargetType = 'User' | 'QuestTemplate' | 'RecommendationSettings' | 'Experiment' | 'QuestIdea' | 'LocalPlace';

export interface AdminPlace extends NearbyPlace {
  city: string;
  isActive: boolean;
  isPast: boolean;
  templates: { id: string; code: string; title: string }[];
  createdBy: string;
  createdAt: string;
}

export interface AdminPlaceRequest {
  kind: LocalPlaceKind;
  city: string;
  name: string;
  address: string | null;
  url: string | null;
  note: string | null;
  startsAt: string | null;
  endsAt: string | null;
  templateIds: string[];
  isActive: boolean;
}
export type WeightGroup = 'Interest' | 'Novelty' | 'Score' | 'Penalty' | 'TasteGraph' | 'Exploration' | 'Windows';

export interface AdminUser {
  id: string;
  email: string;
  displayName: string;
  role: UserRole;
  createdAt: string;
  lastLoginAt: string | null;
  isSuspended: boolean;
  suspendedUntil: string | null;
  suspensionReason: string | null;
  completedQuests: number;
  lifeXp: number;
  isBootstrapAdmin: boolean;
  isSelf: boolean;
}

export interface TemplateInput {
  code: string;
  title: string;
  description: string;
  type: QuestType;
  difficulty: Difficulty;
  category: LifeCategory;
  secondaryCategory: LifeCategory | null;
  minMinutes: number;
  maxMinutes: number;
  cost: CostBand;
  dayParts: DayPart[];
  requiresCity: boolean;
  isOutdoor: boolean;
  cooldownDays: number;
  riskScore: number;
  interestIds: string[];
  effort: PhysicalEffort;
  isStarter: boolean;
  /** İngilizce başlık/açıklama; boşsa İngilizce kullanıcı Türkçeyi görür. */
  titleEn?: string | null;
  descriptionEn?: string | null;
}

export interface AdminTemplate extends TemplateInput {
  id: string;
  safety: SafetyLevel;
  isActive: boolean;
  source: EditorialSource;
  version: number;
  violations: string[];
}

export interface AdminTemplateListItem {
  id: string;
  code: string;
  title: string;
  category: LifeCategory;
  type: QuestType;
  cost: CostBand;
  safety: SafetyLevel;
  isActive: boolean;
  source: EditorialSource;
  version: number;
  violationCount: number;
}

export interface TemplateSearch {
  text?: string;
  category?: LifeCategory;
  safety?: SafetyLevel;
  isActive?: boolean;
  pageNumber: number;
}

export interface TemplateValidation {
  violations: string[];
  resultingSafety: SafetyLevel;
}

export interface CatalogHealth {
  offerable: number;
  needsReview: number;
  blocked: number;
  pendingIdeas: number;
  freeShare: number;
  cityIndependentShare: number;
  categories: { category: LifeCategory; templates: number; daily: number }[];
  warnings: string[];
}

export interface WeightField {
  key: string;
  label: string;
  group: WeightGroup;
  description: string;
  min: number;
  max: number;
  step: number;
  isInteger: boolean;
  value: number;
  defaultValue: number;
  isOverridden: boolean;
}

export interface RecommendationWeights {
  revision: number;
  updatedAt: string | null;
  updatedBy: string | null;
  fields: WeightField[];
}

export interface AuditEntry {
  id: string;
  createdAt: string;
  actorEmail: string;
  action: AdminAction;
  targetType: AdminTargetType;
  targetId: string | null;
  targetLabel: string;
  reason: string | null;
  details: string | null;
}

// ── Sonra yaparım, fikirler, deneyler ───────────────────────────────────────

export interface SavedQuest {
  templateId: string;
  title: string;
  description: string;
  category: LifeCategory;
  type: QuestType;
  minMinutes: number;
  maxMinutes: number;
  cost: CostBand;
  effort: PhysicalEffort;
  savedAt: string;
  isAvailable: boolean;
}

export type IdeaStatus = 'Pending' | 'Accepted' | 'Rejected';

export interface IdeaRequest {
  title: string;
  description: string;
  category: LifeCategory;
  minutes: number;
  cost: CostBand;
  isOutdoor: boolean;
}

export interface MyIdea extends IdeaRequest {
  id: string;
  status: IdeaStatus;
  reviewNote: string | null;
  submittedAt: string;
  reviewedAt: string | null;
}

export interface AdminIdea extends IdeaRequest {
  id: string;
  status: IdeaStatus;
  flags: string[];
  reviewNote: string | null;
  reviewedBy: string | null;
  templateId: string | null;
  submittedAt: string;
  reviewedAt: string | null;
}

export type ExperimentStatus = 'Draft' | 'Running' | 'Stopped';
export type ExperimentOutcome = 'None' | 'Adopted' | 'Discarded';
export type ExperimentVerdict = 'InsufficientData' | 'NoDifference' | 'TreatmentBetter' | 'TreatmentWorse';
export type ExperimentAction = 'start' | 'stop' | 'adopt' | 'discard';

export interface Experiment {
  id: string;
  name: string;
  hypothesis: string;
  status: ExperimentStatus;
  outcome: ExperimentOutcome;
  treatmentShare: number;
  overrides: { key: string; label: string; controlValue: number; treatmentValue: number }[];
  createdAt: string;
  createdBy: string;
  startedAt: string | null;
  endedAt: string | null;
}

export interface VariantResult {
  users: number;
  offered: number;
  accepted: number;
  completed: number;
  meaningful: number;
  northStar: number;
  northStarStandardError: number;
  acceptanceRate: number;
  completionRate: number;
  explorationAcceptanceRate: number;
  notInterestedRate: number;
  averageRating: number | null;
}

export interface ExperimentDetail {
  experiment: Experiment;
  results: {
    weeks: number;
    control: VariantResult;
    treatment: VariantResult;
    northStar: { difference: number; ciLow: number; ciHigh: number; relativeLift: number | null };
    verdict: ExperimentVerdict;
    minUsersPerVariant: number;
    /** Deneme grubunda "ilgimi çekmedi" oranı izin verilenden fazla arttı. */
    guardrailBreached: boolean;
    guardrailMaxIncrease: number;
  } | null;
}

export interface ExperimentPreset {
  key: string;
  name: string;
  hypothesis: string;
  treatmentShare: number;
  source: string;
  overrides: Experiment['overrides'];
  existingExperimentId: string | null;
  existingStatus: ExperimentStatus | null;
}

export interface CreateExperimentRequest {
  name: string;
  hypothesis: string;
  treatmentOverrides: Record<string, number>;
  treatmentShare: number;
}
