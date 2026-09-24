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
}

export interface PreferencesRequest {
  discoveryRadius?: DiscoveryRadius | null;
  budget?: CostBand | null;
  weeklyAvailableMinutes?: number | null;
  goals?: LifeCategory[] | null;
  city?: string | null;
  clearCity?: boolean | null;
  timeZoneId?: string | null;
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
