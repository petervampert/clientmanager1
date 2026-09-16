# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM node:24-alpine AS build
WORKDIR /app

# Install dependencies (layer cache friendly)
COPY package.json package-lock.json ./
RUN npm ci --ignore-scripts

# Copy source and build for production
COPY . .
RUN npx ng build --configuration=production

# ── Stage 2: Serve with nginx ─────────────────────────────────────────────────
FROM nginx:alpine AS runtime

# Remove default nginx config
RUN rm /etc/nginx/conf.d/default.conf

# Copy custom nginx config
COPY nginx.conf /etc/nginx/conf.d/app.conf

# Copy built Angular app from stage 1
COPY --from=build /app/dist/client-manager/browser /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
