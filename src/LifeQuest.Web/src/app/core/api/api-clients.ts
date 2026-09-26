import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import {
  Achievement,
  AdminIdea,
  CreateExperimentRequest,
  Experiment,
  ExperimentAction,
  ExperimentDetail,
  ExperimentPreset,
  AdminPlace,
  AdminPlaceRequest,
  PushSettings,
  PushSubscriptionRequest,
  IdeaRequest,
  IdeaStatus,
  MyIdea,
  SavedQuest,
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
  AuthSession,
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

interface RuntimeConfig {
  apiBaseUrl?: string;
}

/**
 * API kök adresi çalışma anında `config.js`'ten okunur: geliştirmede boş (aynı köken, proxy.conf.json),
 * test/üretimde ayrı API alan adı (ör. https://lifequesttestapi.gvnaitech.com). Sondaki "/" atılır.
 */
export function apiBaseUrl(config: RuntimeConfig | undefined = (globalThis as { __LIFEQUEST_CONFIG__?: RuntimeConfig }).__LIFEQUEST_CONFIG__): string {
  return (config?.apiBaseUrl ?? '').trim().replace(/\/+$/, '');
}

export const API = `${apiBaseUrl()}/api/v1`;

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);

  // withCredentials: API ayrı alan adındayken refresh çerezinin yazılıp geri gönderilebilmesi için.
  register(body: RegisterRequest) {
    return this.http.post<AuthSession>(`${API}/auth/register`, body, { withCredentials: true });
  }

  login(body: LoginRequest) {
    return this.http.post<AuthSession>(`${API}/auth/login`, body, { withCredentials: true });
  }

  /** `legacyRefreshToken`: eski sürümün localStorage'da bıraktığı token; bir kez gönderilip çereze taşınır. */
  refresh(legacyRefreshToken?: string) {
    const body = legacyRefreshToken ? { refreshToken: legacyRefreshToken } : {};
    return this.http.post<AuthSession>(`${API}/auth/refresh`, body, { withCredentials: true });
  }

  logout() {
    return this.http.post<void>(`${API}/auth/logout`, {}, { withCredentials: true });
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

  /** KVKK veri taşınabilirliği: tüm veriler JSON dosyası olarak. */
  exportData() {
    return this.http.get(`${API}/profile/export`, { responseType: 'blob', observe: 'response' });
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

  save(id: string) {
    return this.http.post<SavedQuest>(`${API}/quests/${id}/save`, null);
  }

  /** Yerel saat ("2026-10-03T10:00"); null planı kaldırır. */
  plan(id: string, plannedAtLocal: string | null) {
    return this.http.put<Quest>(`${API}/quests/${id}/plan`, { plannedAtLocal });
  }

  calendar(id: string) {
    return this.http.get(`${API}/quests/${id}/calendar.ics`, { responseType: 'blob', observe: 'response' });
  }
}

@Injectable({ providedIn: 'root' })
export class SavedApi {
  private readonly http = inject(HttpClient);

  list() {
    return this.http.get<SavedQuest[]>(`${API}/saved`);
  }

  remove(templateId: string) {
    return this.http.delete<void>(`${API}/saved/${templateId}`);
  }

  start(templateId: string) {
    return this.http.post<Quest>(`${API}/saved/${templateId}/start`, null);
  }
}

@Injectable({ providedIn: 'root' })
export class IdeasApi {
  private readonly http = inject(HttpClient);

  submit(idea: IdeaRequest) {
    return this.http.post<MyIdea>(`${API}/ideas`, idea);
  }

  mine() {
    return this.http.get<MyIdea[]>(`${API}/ideas/mine`);
  }

  withdraw(id: string) {
    return this.http.delete<void>(`${API}/ideas/${id}`);
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
export class PushApi {
  private readonly http = inject(HttpClient);

  settings() {
    return this.http.get<PushSettings>(`${API}/push`);
  }

  subscribe(body: PushSubscriptionRequest) {
    return this.http.put<PushSettings>(`${API}/push/subscription`, body);
  }

  unsubscribe(endpoint: string) {
    return this.http.delete<void>(`${API}/push/subscription`, { body: { endpoint } });
  }

  test() {
    return this.http.post<number>(`${API}/push/test`, null);
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

  createTemplate(template: TemplateInput, sourceIdeaId?: string | null) {
    return this.http.post<AdminTemplate>(`${API}/admin/templates`, template, {
      params: sourceIdeaId ? { sourceIdeaId } : {},
    });
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

  // Mekânlar ve etkinlikler
  places(city?: string) {
    return this.http.get<AdminPlace[]>(`${API}/admin/places`, { params: city ? { city } : {} });
  }

  savePlace(request: AdminPlaceRequest, id?: string) {
    return id
      ? this.http.put<AdminPlace>(`${API}/admin/places/${id}`, request)
      : this.http.post<AdminPlace>(`${API}/admin/places`, request);
  }

  deletePlace(id: string) {
    return this.http.delete<void>(`${API}/admin/places/${id}`);
  }

  // Deneyler
  experiments() {
    return this.http.get<Experiment[]>(`${API}/admin/experiments`);
  }

  experimentPresets() {
    return this.http.get<ExperimentPreset[]>(`${API}/admin/experiments/presets`);
  }

  experiment(id: string) {
    return this.http.get<ExperimentDetail>(`${API}/admin/experiments/${id}`);
  }

  createExperiment(request: CreateExperimentRequest) {
    return this.http.post<Experiment>(`${API}/admin/experiments`, request);
  }

  changeExperiment(id: string, action: ExperimentAction, reason: string | null) {
    return this.http.post<Experiment>(`${API}/admin/experiments/${id}/${action}`, { reason });
  }

  // Topluluk fikirleri
  ideas(status: IdeaStatus | null, pageNumber: number) {
    const params: Record<string, string | number> = { pageNumber, pageSize: 20 };
    if (status) params['status'] = status;
    return this.http.get<PagedResult<AdminIdea>>(`${API}/admin/ideas`, { params });
  }

  idea(id: string) {
    return this.http.get<AdminIdea>(`${API}/admin/ideas/${id}`);
  }

  rejectIdea(id: string, note: string) {
    return this.http.post<AdminIdea>(`${API}/admin/ideas/${id}/reject`, { note });
  }

  // Denetim
  audit(action: AdminAction | null, pageNumber: number) {
    const params: Record<string, string | number> = { pageNumber, pageSize: 30 };
    if (action) params['action'] = action;
    return this.http.get<PagedResult<AuditEntry>>(`${API}/admin/audit`, { params });
  }
}
