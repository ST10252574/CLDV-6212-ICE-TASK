# ---------- build stage ----------
FROM node:20-alpine AS build
WORKDIR /app

COPY package.json package-lock.json* ./
# `npm ci` gives reproducible installs when package-lock.json is committed (it should be).
RUN if [ -f package-lock.json ]; then npm ci; else npm install; fi

COPY . .
RUN npm run build

# ---------- runtime stage ----------
# The "unprivileged" nginx image runs as a non-root user (so it must listen on a port above 1024).
FROM nginxinc/nginx-unprivileged:1.27-alpine AS final

# nginx renders every file in /etc/nginx/templates/*.template with the container's environment
# variables at start-up, so the same image works locally and on Render.
#   PORT        port nginx listens on (Render injects its own; 8080 locally)
#   BACKEND_URL where /api/* is proxied to (http://api:8080 in compose, the API's public URL on Render)
ENV PORT=8080
ENV BACKEND_URL=http://api:8080

COPY nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html

USER nginx
EXPOSE 8080
