/**
 * Sözlük tipi Türkçe kaynaktan türetilir; İngilizce sözlük bu tipi karşılamak zorundadır. Eksik ya da fazla anahtar,
 * yanlış parametreli fonksiyon derleme hatasıdır.
 */
export type Widen<T> = T extends string
  ? string
  : T extends (...args: infer A) => string
    ? (...args: A) => string
    : { [K in keyof T]: Widen<T[K]> };

/** Bir sözlük bölümü: Türkçe kaynak ve onun tipini karşılayan İngilizcesi yan yana. */
export function section<T>(tr: T, en: Widen<T>): { tr: T; en: Widen<T> } {
  return { tr, en };
}
