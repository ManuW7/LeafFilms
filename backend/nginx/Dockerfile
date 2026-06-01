FROM node:24-alpine AS frontend-build

WORKDIR /app

COPY frontend/leaf-films/package*.json ./
RUN npm ci

COPY frontend/leaf-films/ ./
RUN npm run build

FROM nginx:1.25-alpine

COPY backend/nginx/nginx.conf /etc/nginx/nginx.conf
COPY --from=frontend-build /app/dist /usr/share/nginx/html
