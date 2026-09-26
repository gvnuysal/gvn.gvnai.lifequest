import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CATEGORIES, SKIP_REASONS } from '../labels/labels';
import { acceptLanguageInterceptor } from './accept-language.interceptor';
import { EN, option, t, TR } from './i18n';
import { currentLang, detectLang, setLang } from './lang';

describe('i18n', () => {
  afterEach(() => setLang('tr', false));

  it('detects Turkish browsers and falls back to English otherwise; a stored choice wins', () => {
    expect(detectLang(null, ['tr-TR', 'en'])).toBe('tr');
    expect(detectLang(null, ['de-DE'])).toBe('en');
    expect(detectLang(null, [])).toBe('en');
    expect(detectLang('tr', ['en-US'])).toBe('tr');
  });

  it('switches every label at once without rebuilding objects', () => {
    const reason = SKIP_REASONS[0];
    const tab = option('active', (d) => d.quests.tabActive);

    setLang('tr', false);
    expect(CATEGORIES.Fitness.label).toBe('Hareket');
    expect(reason.label).toBe('İlgimi çekmedi');
    expect(tab.label).toBe('Devam eden');

    setLang('en', false);
    expect(currentLang()).toBe('en');
    expect(t()).toBe(EN);
    expect(CATEGORIES.Fitness.label).toBe('Movement');
    expect(reason.label).toBe('Not for me');
    expect(tab.label).toBe('In progress');
    expect(document.documentElement.lang).toBe('en');
  });

  it('has an English entry for every Turkish key', () => {
    const keys = (o: object, prefix = ''): string[] =>
      Object.entries(o).flatMap(([k, v]) => (v && typeof v === 'object' && !Array.isArray(v) ? keys(v, `${prefix}${k}.`) : [`${prefix}${k}`]));
    expect(keys(EN)).toEqual(keys(TR));
  });

  it('sends the interface language to the API', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([acceptLanguageInterceptor])), provideHttpClientTesting()],
    });
    const http = TestBed.inject(HttpClient);
    const backend = TestBed.inject(HttpTestingController);

    setLang('en', false);
    http.get('/api/v1/profile').subscribe();
    http.get('/assets/x.json').subscribe();

    expect(backend.expectOne('/api/v1/profile').request.headers.get('Accept-Language')).toBe('en');
    expect(backend.expectOne('/assets/x.json').request.headers.has('Accept-Language')).toBe(false);
    backend.verify();
  });
});
