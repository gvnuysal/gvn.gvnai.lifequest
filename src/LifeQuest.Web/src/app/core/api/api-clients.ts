import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import {
  Achievement,
  AdminAction,
  AdminTemplate,
  AdminTemplateListItem,
  AdminUser,
  AdminUserFilter,
  AuditEntry,
  CatalogHealth,
  RecommendationWeights,
  SafetyLevel,
  TemplateInput,
  TemplateSearch,
  TemplateValidation,
  UserRole,
  AuthTokens,
  Interest,
  InterestSelection,
  LoginRequest,
  OnboardingRequest,
  PagedResult,
  PreferencesRequest,
  Profile,
  ProductMetrics,
  Progress,
  StarterCard,
  WeeklySummary,
  Quest,
  QuestCompletion,
  QuestDetail,
  QuestFeedbackResult,
  QuestList,
  QuestStatus,
  FeedbackPreference,
  RegisterRequest,
  SkipReason,
  SuggestRequest,
} from './models';

/** Göreli taban adres: geliştirmede proxy.conf.json, üretimde aynı origin. */
export const API = '/api/v1';

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);

  register(body: RegisterRequest) {
    return this.http.post<AuthTokens>(`${API}/auth/register`, body);
  }

  login(body: LoginRequest) {
    return this.http.post<AuthTokens>(`${API}/auth/login`, body);
  }

  refresh(refreshToken: string) {
    return this.http.post<AuthTokens>(`${API}/auth/refresh`, { refreshToken });
  }

  logout(refreshToken: string) {
    return this.http.post<void>(`${API}/auth/logout`, { refreshToken });
  }
}

@Injectable({ providedIn: 'root' })
export class CatalogApi {
  private readonly http = inject(HttpClient);

  interests() {
    return this.http.get<Interest[]>(`${API}/catalog/interests`);
  }
}

@Injectable({ providedIn: 'root' })
export class ProfileApi {
  private readonly http = inject(HttpClient);

  get() {
    return this.http.get<Profile>(`${API}/profile`);
  }

  completeOnboarding(body: OnboardingRequest) {
    return this.http.put<Profile>(`${API}/profile/onboarding`, body);
  }

  updatePreferences(body: PreferencesRequest) {
    return this.http.patch<Profile>(`${API}/profile/preferences`, body);
  }

  setInterests(interests: InterestSelection[]) {
    return this.http.put<Profile>(`${API}/profile/interests`, { interests });
  }

  deleteAccount(password: string) {
    return this.http.delete<void>(`${API}/profile`, { body: { password } });
  }
}

@Injectable({ providedIn: 'root' })
export class QuestsApi {
  private readonly http = inject(HttpClient);

  today() {
    return this.http.get<QuestList>(`${API}/quests/today`);
  }

  suggest(body: SuggestRequest) {
    return this.http.post<QuestList>(`${API}/quests/suggestions`, body);
  }

  active() {
    return this.http.get<Quest[]>(`${API}/quests/active`);
  }

  history(pageNumber: number, pageSize: number, status: QuestStatus | null) {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    if (status) params = params.set('status', status);
    return this.http.get<PagedResult<Quest>>(`${API}/quests/history`, { params });
  }

  get(id: string) {
    return this.http.get<QuestDetail>(`${API}/quests/${id}`);
  }

  accept(id: string) {
    return this.http.post<Quest>(`${API}/quests/${id}/accept`, null);
  }

  complete(id: string) {
    return this.http.post<QuestCompletion>(`${API}/quests/${id}/complete`, null);
  }

  skip(id: string, reason: SkipReason) {
    return this.http.post<Quest>(`${API}/quests/${id}/skip`, { reason });
  }

  feedback(id: string, rating: number | null, preference: FeedbackPreference | null) {
    return this.http.post<QuestFeedbackResult>(`${API}/quests/${id}/feedback`, { rating, preference });
  }
}

@Injectable({ providedIn: 'root' })
export class ProgressApi {
  private readonly http = inject(HttpClient);

  progress() {
    return this.http.get<Progress>(`${API}/progress`);
  }

  achievements() {
    return this.http.get<Achievement[]>(`${API}/achievements`);
  }
}

@Injectable({ providedIn: 'root' })
export class OnboardingApi {
  private readonly http = inject(HttpClient);

  starterCards() {
    return this.http.get<StarterCard[]>(`${API}/onboarding/starter-cards`);
  }
}

@Injectable({ providedIn: 'root' })
export class SummariesApi {
  private readonly http = inject(HttpClient);

  /** Okunmamış özet yoksa API 204 döner → null. */
  latest() {
    return this.http.get<WeeklySummary | null>(`${API}/summaries/latest`);
  }

  markRead(id: string) {
    return this.http.post<void>(`${API}/summaries/${id}/read`, null);
  }
}

@Injectable({ providedIn: 'root' })
export class AdminApi {
  private readonly http = inject(HttpClient);

  metrics(days: number) {
    return this.http.get<ProductMetrics>(`${API}/admin/metrics`, { params: { days } });
  }

  // Kullanıcılar
  users(search: string, filter: AdminUserFilter, pageNumber: number) {
    return this.http.get<PagedResult<AdminUser>>(`${API}/admin/users`, {
      params: { search, filter, pageNumber, pageSize: 20 },
    });
  }

  suspend(id: string, days: number | null, reason: string) {
    return this.http.post<AdminUser>(`${API}/admin/users/${id}/suspend`, { days, reason });
  }

  unsuspend(id: string) {
    return this.http.post<AdminUser>(`${API}/admin/users/${id}/unsuspend`, null);
  }

  setRole(id: string, role: UserRole) {
    return this.http.put<AdminUser>(`${API}/admin/users/${id}/role`, { role });
  }

  deleteUser(id: string, reason: string, confirmEmail: string) {
    return this.http.delete<void>(`${API}/admin/users/${id}`, { body: { reason, confirmEmail } });
  }

  // Katalog
  templates(search: TemplateSearch) {
    const params: Record<string, string | number | boolean> = { pageNumber: search.pageNumber, pageSize: 20 };
    if (search.text) params['text'] = search.text;
    if (search.category) params['category'] = search.category;
    if (search.safety) params['safety'] = search.safety;
    if (search.isActive !== undefined) params['isActive'] = search.isActive;
    return this.http.get<PagedResult<AdminTemplateListItem>>(`${API}/admin/templates`, { params });
  }

  template(id: string) {
    return this.http.get<AdminTemplate>(`${API}/admin/templates/${id}`);
  }

  createTemplate(template: TemplateInput) {
    return this.http.post<AdminTemplate>(`${API}/admin/templates`, template);
  }

  updateTemplate(id: string, version: number, template: TemplateInput) {
    return this.http.put<AdminTemplate>(`${API}/admin/templates/${id}`, { version, template });
  }

  validateTemplate(template: TemplateInput) {
    return this.http.post<TemplateValidation>(`${API}/admin/templates/validate`, template);
  }

  setTemplateSafety(id: string, safety: SafetyLevel, note: string | null) {
    return this.http.post<AdminTemplate>(`${API}/admin/templates/${id}/safety`, { safety, note });
  }

  setTemplateActive(id: string, active: boolean) {
    return this.http.post<AdminTemplate>(`${API}/admin/templates/${id}/${active ? 'activate' : 'deactivate'}`, null);
  }

  catalogHealth() {
    return this.http.get<CatalogHealth>(`${API}/admin/catalog/health`);
  }

  // Öneri ağırlıkları
  weights() {
    return this.http.get<RecommendationWeights>(`${API}/admin/recommendation-weights`);
  }

  updateWeights(revision: number, values: Record<string, number>, reason: string) {
    return this.http.put<RecommendationWeights>(`${API}/admin/recommendation-weights`, { revision, values, reason });
  }

  resetWeights(revision: number, keys: string[] | null, reason: string | null) {
    return this.http.post<RecommendationWeights>(`${API}/admin/recommendation-weights/reset`, { revision, keys, reason });
  }

  // Denetim
  audit(action: AdminAction | null, pageNumber: number) {
    const params: Record<string, string | number> = { pageNumber, pageSize: 30 };
    if (action) params['action'] = action;
    return this.http.get<PagedResult<AuditEntry>>(`${API}/admin/audit`, { params });
  }
}
