/**
 * Uygulama içi adreslerin tek kaynağı. Adresler İngilizce, sayfa içerikleri Türkçedir.
 * Rota tanımları app.routes.ts'te; bağlantılar ve yönlendirmeler her zaman bu sabitleri kullanır.
 */
export const APP_PATHS = {
  login: '/login',
  register: '/register',
  onboarding: '/onboarding',
  today: '/today',
  suggest: '/suggest',
  quests: '/quests',
  progress: '/progress',
  profile: '/profile',
  saved: '/saved',
  ideas: '/ideas',
  admin: {
    root: '/admin',
    users: '/admin/users',
    catalog: '/admin/catalog',
    newTemplate: '/admin/catalog/new',
    ideas: '/admin/ideas',
    experiments: '/admin/experiments',
    places: '/admin/places',
    recommendationSettings: '/admin/recommendation-settings',
    auditLog: '/admin/audit-log',
  },
} as const;

/** Giriş sonrası ve yetkisiz erişimde varsayılan hedef. */
export const HOME_PATH = APP_PATHS.today;

export const questPath = (id: string) => [APP_PATHS.quests, id];
export const templatePath = (id: string) => [APP_PATHS.admin.catalog, id];
export const experimentPath = (id: string) => [APP_PATHS.admin.experiments, id];
/** Quest Party davetleri yalnızca kodla açılır (/party/KOD); kodsuz sayfa yoktur. */
const PARTY_BASE = '/party';
export const partyPath = (code: string) => [PARTY_BASE, code];

/** Paylaşılan davet bağlantısı (tam adres). */
export const partyInviteUrl = (code: string, origin: string = globalThis.location?.origin ?? '') => `${origin}${PARTY_BASE}/${code}`;
