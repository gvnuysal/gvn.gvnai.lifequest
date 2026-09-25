import { HttpResponse } from '@angular/common/http';

/** Content-Disposition'daki dosya adını okur (RFC 5987 filename* dahil). */
export function fileNameFrom(disposition: string | null, fallback: string): string {
  if (!disposition) return fallback;
  const encoded = /filename\*=UTF-8''([^;]+)/i.exec(disposition);
  if (encoded) return decodeURIComponent(encoded[1]);
  const plain = /filename="?([^";]+)"?/i.exec(disposition);
  return plain ? plain[1] : fallback;
}

/** Yetkili istekle alınmış dosyayı tarayıcıda indirir (bağlantı token'sız açılamayacağı için blob ile). */
export function saveResponse(response: HttpResponse<Blob>, fallbackName: string): void {
  if (!response.body) return;
  const url = URL.createObjectURL(response.body);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileNameFrom(response.headers.get('Content-Disposition'), fallbackName);
  document.body.appendChild(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}
