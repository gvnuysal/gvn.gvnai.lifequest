import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import {
  Achievement,
  AuthTokens,
  Interest,
  InterestSelection,
  LoginRequest,
  OnboardingRequest,
  PagedResult,
  PreferencesRequest,
  Profile,
  Progress,
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
