// Çalışma anı yapılandırması. Geliştirmede API aynı kökendedir (proxy.conf.json).
// Docker imajında bu dosya nginx tarafından API_BASE_URL ortam değişkeninden üretilir;
// aynı imaj test ve üretimde yalnızca ortam değişkeni değiştirilerek kullanılır.
window.__LIFEQUEST_CONFIG__ = { apiBaseUrl: '' };
