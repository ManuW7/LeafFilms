# LeafFilms Architecture and Requirements

## Overview

LeafFilms is built as a local Docker Compose microservice system. The frontend is a React single-page application served by Nginx. Backend functionality is split between independent .NET services. Each stateful service owns its own PostgreSQL database. Redis is used for cached/read-optimized data, and RabbitMQ is used for asynchronous publish/subscribe flows.

The system is intended to run on one machine for development and demonstration:

```bash
cp .env.example .env
docker compose up --build
```

## Technology Stack

- Frontend: React, TypeScript, Vite, Zustand, React Router.
- Backend: ASP.NET Core Web API, Entity Framework Core, Npgsql.
- API Gateway: Nginx.
- Databases: PostgreSQL, one database per service.
- Cache/read model storage: Redis.
- Message broker: RabbitMQ.
- Auth: JWT bearer tokens with role-based authorization.
- Observability: Serilog JSON logs to stdout, correlation ID middleware, health checks.
- Resilience: Polly retry policies and circuit breaker for synchronous service calls.
- API documentation: Swagger/OpenAPI per service.
- CI: GitHub Actions workflow for backend build, frontend lint/build, and compose validation.

## Services

| Service | Responsibility | Storage |
| --- | --- | --- |
| UserService | Registration, login, JWT creation, profile lookup, admin promotion | `user-db` PostgreSQL |
| CatalogueService | Movies, movie search, admin movie CRUD, cached movie reads, movie rating read data | `catalogue-db` PostgreSQL, Redis |
| ReviewService | Reviews, review validation, synchronous movie existence check, review events | `review-db` PostgreSQL |
| SocialService | Follow/unfollow graph and followers/following queries | `social-db` PostgreSQL |
| ActivityService | Watch history, watchlist, playlists, movie watched events | `activity-db` PostgreSQL |
| FeedService | User feed read model, WebSocket notifications, event consumers | Redis |

## API Gateway Routing

Nginx routes browser/API traffic to internal services:

| Gateway path | Service path |
| --- | --- |
| `/api/users/*` | UserService `/users/*` |
| `/api/movies*` | CatalogueService `/movies*` |
| `/api/reviews*` | ReviewService `/reviews*` |
| `/api/follow` | SocialService `/follow` |
| `/api/users/{id}/followers` | SocialService `/users/{id}/followers` |
| `/api/users/{id}/following` | SocialService `/users/{id}/following` |
| `/api/history*` | ActivityService `/history*` |
| `/api/watchlist*` | ActivityService `/watchlist*` |
| `/api/playlists*` | ActivityService `/playlists*` |
| `/api/feed*` | FeedService `/feed*` |
| `/api/ws/feed` | FeedService `/ws/feed` |
| `/api/health/*` | service `/health` endpoints |
| `/api/swagger/*` | service Swagger UI |

The root path serves the React SPA. Unknown frontend routes fall back to `index.html`.

## Synchronous Communication

The system uses HTTP for direct service-to-service calls where the caller needs an immediate answer.

1. `ReviewService -> CatalogueService`
   - When a user creates a review, ReviewService calls CatalogueService with `GET /movies/{movieId}`.
   - Purpose: verify that the reviewed movie exists and copy stable movie title data into the review.
   - Resilience: Polly retry with exponential backoff and a circuit breaker.

2. `FeedService -> SocialService`
   - When FeedService consumes a feed-worthy event, it calls SocialService with `GET /users/{userId}/followers`.
   - Purpose: find which users should receive the feed item.
   - Resilience: Polly retry with exponential backoff. If follower lookup fails, FeedService logs the error and returns an empty follower list to avoid blocking the consumer forever.

## Asynchronous Communication

RabbitMQ is used for event-driven communication. Producers publish to fanout exchanges; consumers subscribe with durable queues.

| Event | Producer | Consumer | Purpose |
| --- | --- | --- | --- |
| `review.created` | ReviewService | CatalogueService, FeedService | Update movie rating read data and push review items into followers' feeds |
| `review.updated` | ReviewService | CatalogueService | Recalculate movie aggregate rating after rating changes |
| `review.deleted` | ReviewService | CatalogueService, FeedService | Remove rating contribution and remove review feed items |
| `movie.watched` | ActivityService | FeedService | Push watch events to followers' feeds |
| `movie.deleted` | CatalogueService | ReviewService | Remove reviews belonging to deleted movies |

This is intentionally eventually consistent. For example, after a review is created, the review write succeeds in ReviewService first. CatalogueService updates `AverageRating` and `ReviewCount` asynchronously after consuming the event. A short delay is acceptable because movie rating is a read-side projection, not the source of truth.

## Database Per Service and Migrations

Each stateful service owns its schema:

- `users` database for UserService.
- `catalogue` database for CatalogueService.
- `reviews` database for ReviewService.
- `social` database for SocialService.
- `activity` database for ActivityService.

Schemas are versioned with EF Core migrations in each service's `Migrations` folder. Migrations are applied automatically at service startup with `Database.Migrate()`.

Migration files were renamed to readable filenames such as `InitialCreate.cs` and `AddReviewImageUrl.cs`. EF Core still keeps the original migration IDs inside designer metadata, so already-applied database history remains compatible.

## CQRS and Read Models

The code separates command and query models at DTO level:

- Command DTOs describe writes, for example `RegisterUserCommand`, `LoginUserCommand`, `CreateMovieCommand`, `UpdateMovieCommand`, `CreateReviewCommand`, `AddToHistoryCommand`.
- Query DTOs describe reads, for example `UserProfileDto`, `MovieDto`, `ReviewDto`, `WatchHistoryDto`, `PlaylistDto`.

Read-optimized models:

- `FeedItem` is a Redis-backed feed read model. It is built asynchronously from events and optimized for `GET /feed`.
- CatalogueService caches `MovieDto` and the full movie list in Redis using cache-aside.

## Redis Usage

Redis is used in two ways:

1. Cache-aside in CatalogueService:
   - `GET /movies` reads `movies:all`.
   - `GET /movies/{id}` reads `movie:{id}`.
   - On cache miss, data is loaded from PostgreSQL and written to Redis with a TTL.
   - On movie create/update/delete, related cache keys are invalidated.

2. Distributed read model in FeedService:
   - Each user's feed is stored as a Redis list under `feed:{userId}`.
   - FeedService appends new items when RabbitMQ events arrive.
   - `GET /feed` reads directly from Redis.

## Error Handling

Services use centralized error-handling middleware. Expected domain errors map to correct HTTP status codes:

- `200 OK` for successful reads and normal idempotent responses.
- `201 Created` for successful creates.
- `204 No Content` for deletes.
- `400 Bad Request` for invalid input.
- `401 Unauthorized` for missing/invalid authentication.
- `403 Forbidden` for insufficient permissions.
- `404 Not Found` for missing resources.
- `409 Conflict` for duplicate domain actions.
- `500 Internal Server Error` for unexpected failures.

ASP.NET Core model validation also returns 400 for invalid command DTOs.

## Authentication and Authorization

UserService issues JWT tokens. Other services validate the same JWT signing secret from environment variables. Protected endpoints use `[Authorize]`; admin-only movie mutations use `[Authorize(Roles = "admin")]`.

Roles:

- `user`: normal authenticated user.
- `admin`: can manage catalogue movies and delete reviews where admin authorization is allowed.

Admin promotion is done through `POST /api/users/admin/promote` with `ADMIN_SETUP_SECRET` from `.env`.

## Logging and Correlation IDs

Services use Serilog with `CompactJsonFormatter`, writing structured JSON logs to stdout. Logs include:

- timestamp,
- level,
- service name,
- message template fields,
- correlation ID when available.

Nginx also writes JSON access logs and forwards `X-Correlation-ID` to backend services. If a request does not provide a correlation ID, Nginx or service middleware creates one and returns it in the response.

Current limitation: inbound correlation ID is handled consistently, but outgoing `HttpClient` calls do not yet explicitly copy the current correlation ID into downstream service requests. To make propagation complete, add a delegating handler that reads the current request header/context and sets `X-Correlation-ID` on `HttpClient` calls.

## Health Checks

Every backend service maps `/health`. Docker Compose uses these endpoints for container health checks. Nginx exposes them externally:

- `/api/health/users`
- `/api/health/catalogue`
- `/api/health/reviews`
- `/api/health/social`
- `/api/health/activity`
- `/api/health/feed`

Healthy services return `200 OK`.

## Graceful Shutdown

ASP.NET Core handles SIGTERM from Docker and stops accepting new requests while the host shuts down. EF Core contexts are DI-scoped and disposed by the container. RabbitMQ publishers and consumers implement disposal/close logic for channels and connections. This satisfies the local graceful shutdown requirement for Docker Compose.

For production-like hardening, add explicit cancellation-token handling in RabbitMQ consumer loops and WebSocket handling paths.

## Resilience

ReviewService uses Polly:

- retry policy with exponential backoff for transient HTTP errors;
- circuit breaker with Closed -> Open -> Half-Open behavior;
- fail-fast when CatalogueService repeatedly fails.

FeedService uses Polly retry with exponential backoff for SocialService calls.

Improvement suggestion: add circuit breaker to FeedService's SocialService client as well, mirroring ReviewService.

## Configuration and Secrets

Docker Compose reads secrets from `.env`, which is ignored by git. `.env.example` documents required variables:

- `POSTGRES_USER`
- `POSTGRES_PASSWORD`
- `RABBITMQ_USER`
- `RABBITMQ_PASS`
- `JWT_SECRET`
- `ADMIN_SETUP_SECRET`

Service URLs, database connection strings, broker settings, Redis settings, and JWT settings are supplied through environment variables in Compose.

Development `appsettings.Development.json` files contain local placeholder values. They are not suitable for shared environments.

## CI/CD

The repository includes `.github/workflows/ci.yml`.

It runs on push and pull requests:

- restore/build backend solution;
- install/lint/build frontend;
- validate Docker Compose config.

There are currently no automated test projects in the repository. When tests are added, extend the backend job with:

```yaml
- name: Test
  run: dotnet test backend/LeafFilms.slnx --configuration Release --no-build
```

### Dev Deploy Setup

Automatic deployment requires an external dev server or platform credentials, so it cannot be fully completed inside the repository without real infrastructure secrets.

Recommended simple dev deploy through GitHub Actions over SSH:

1. Prepare a dev server with Docker and Docker Compose.
2. Clone the repository on the server, for example `/opt/leaffilms`.
3. Create `/opt/leaffilms/.env` with production-like values.
4. In GitHub repository settings, add Actions secrets:
   - `DEV_HOST`
   - `DEV_USER`
   - `DEV_SSH_KEY`
   - optionally `DEV_PORT`
5. Add a deploy job after CI:

```yaml
deploy-dev:
  name: Deploy dev
  runs-on: ubuntu-latest
  needs: [backend, frontend, compose]
  if: github.ref == 'refs/heads/main' && github.event_name == 'push'
  environment: development
  steps:
    - name: Deploy over SSH
      uses: appleboy/ssh-action@v1.0.3
      with:
        host: ${{ secrets.DEV_HOST }}
        username: ${{ secrets.DEV_USER }}
        key: ${{ secrets.DEV_SSH_KEY }}
        port: ${{ secrets.DEV_PORT || 22 }}
        script: |
          cd /opt/leaffilms
          git pull
          docker compose up --build -d
          docker compose ps
```

This gives automatic dev deployment on every push to `main` after CI succeeds.

## Requirement Coverage

| Requirement | Status | Evidence |
| --- | --- | --- |
| Runs on one machine with Docker Compose | Done | `docker-compose.yml`; gateway image now builds frontend |
| Clean code and structure | Mostly done | service folders, controllers/services/repositories/DTOs; some comments in older files have encoding damage and should be cleaned later |
| REST API in at least 2 services | Done | all backend services expose REST controllers |
| Correct HTTP methods | Done | GET/POST/PUT/DELETE are used for resources |
| Synchronous service call | Done | ReviewService -> CatalogueService, FeedService -> SocialService |
| Error handling and status codes | Done | error middleware and controller response codes |
| README with launch instructions | Done | root `README.md` |
| API Gateway | Done | Nginx routes `/api/*` |
| Message broker | Done | RabbitMQ |
| Event-driven publish/subscribe | Done | review/movie activity events |
| Database per service | Done | separate PostgreSQL databases |
| DB migrations versioned | Done | EF Core migrations |
| Migrations auto-apply on startup | Done | `Database.Migrate()` |
| Eventual consistency | Done | rating projection and feed projection |
| Asynchronous data synchronization | Done | Catalogue rating and Feed read model |
| CQRS models | Done | command/query DTOs and Redis read models |
| Distributed cache | Done | Redis |
| Cache-aside/read-through | Done | CatalogueService cache-aside |
| Structured JSON logging | Done | Serilog compact JSON, Nginx JSON logs |
| Health endpoints | Done | `/health` per service and gateway routes |
| Graceful shutdown | Mostly done | ASP.NET Core host and disposable broker connections; consumer cancellation can be hardened |
| Correlation ID propagation | Partial | inbound/gateway/log context done; outgoing HttpClient propagation should be added |
| Circuit breaker | Done | ReviewService -> CatalogueService |
| Retry with exponential backoff | Done | ReviewService and FeedService HTTP clients |
| Env vars for configuration | Done for Compose | `.env.example`, Compose env interpolation |
| No committed `.env` | Done | `.gitignore` includes `.env` |
| `.env.example` | Done | root `.env.example` |
| Swagger/OpenAPI | Done | Swagger per service |
| JWT auth + authorization | Done | JWT bearer, `[Authorize]`, admin role |
| CI/CD pipeline | Partial | CI added; dev deploy needs real server secrets and job from the section above |
