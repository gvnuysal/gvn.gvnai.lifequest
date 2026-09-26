/**
 * Giriş/kayıt/onboarding sonrası gidilecek uygulama içi adres (ör. Quest Party davet bağlantısı). Yalnızca bu
 * sekmede ve yalnızca site içi yollar için tutulur; dış adrese yönlendirme yapılamaz.
 */
const KEY = 'lq.next';

export function isSafeInternalUrl(url: string | null | undefined): url is string {
  return !!url && url.startsWith('/') && !url.startsWith('//') && !url.startsWith('/\\');
}

export function rememberDestination(url: string | null | undefined): void {
  if (!isSafeInternalUrl(url)) return;
  try {
    sessionStorage.setItem(KEY, url);
  } catch {
    // yok say
  }
}

export function consumeDestination(): string | null {
  try {
    const url = sessionStorage.getItem(KEY);
    sessionStorage.removeItem(KEY);
    return isSafeInternalUrl(url) ? url : null;
  } catch {
    return null;
  }
}
