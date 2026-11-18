# Optional storefront image only; compose.yaml runs infrastructure.
FROM node:22.20.0-bookworm-slim AS dependencies
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund

FROM dependencies AS build
ENV NEXT_TELEMETRY_DISABLED=1
COPY next.config.ts tsconfig.json next-env.d.ts postcss.config.mjs ./
COPY src/app ./src/app
COPY src/components ./src/components
COPY src/lib ./src/lib
COPY src/styles ./src/styles
COPY public ./public
RUN npm run build

FROM node:22.20.0-bookworm-slim AS storefront
WORKDIR /app
ENV NODE_ENV=production NEXT_TELEMETRY_DISABLED=1 HOSTNAME=0.0.0.0 PORT=3000
ENV BOTZONE_API_URL=http://host.docker.internal:5080
COPY --from=build --chown=node:node /app/.next/standalone ./
USER node
EXPOSE 3000
CMD ["node", "server.js"]
