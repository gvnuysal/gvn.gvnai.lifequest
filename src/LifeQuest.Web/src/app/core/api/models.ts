// Backend DTO'larının TS karşılıkları (src/LifeQuest.Application/**).
// Controller'lar IActionResult döndüğü için OpenAPI dokümanında yanıt şeması yok; tipler elle tutulur.

export type LifeCategory = 'Explorer' | 'Culture' | 'Learning' | 'Social' | 'Fitness' | 'Creativity';
export type CostBand = 'Free' | 'Low' | 'Medium' | 'High';
export type DiscoveryRadius = 'Chill' | 'Explore' | 'SurpriseMe';
export type QuestType = 'Daily' | 'Weekly' | 'Adventure' | 'Epic';
export type Difficulty = 'Easy' | 'Medium' | 'Hard' | 'Heroic';
export type QuestStatus = 'Offered' | 'Accepted' | 'Completed' | 'Skipped' | 'Expired';
export type QuestSource = 'Daily' | 'OnDemand';
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
export interface AuthTokens {
  userId: string;
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  displayName: string;
  birthYear: number;
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
  interests: ProfileInterest[];
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
}

export interface QuestList {
  date: string;
  quests: Quest[];
  message: string | null;
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
  | 'WeightsReset';
export type AdminTargetType = 'User' | 'QuestTemplate' | 'RecommendationSettings';
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
