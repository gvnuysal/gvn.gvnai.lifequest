# Derleme bağlamı: src/LifeQuest.Web → docker build -f src/LifeQuest.Web/Dockerfile src/LifeQuest.Web
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY . .
RUN npm run build

# Root olmayan nginx (8080): statik PWA + /api ve /health için API'ye ters proxy.
FROM nginxinc/nginx-unprivileged:1.27-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY security-headers.conf.inc /etc/nginx/snippets/security-headers.conf.inc
COPY --from=build /app/dist/lifequest-web/browser /usr/share/nginx/html
EXPOSE 8080
