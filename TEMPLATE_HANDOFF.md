# SaaS Template Handoff

This repository is a reusable .NET and React SaaS foundation. The existing product features remain in place so a new project can selectively keep, rename, or remove them after the template is copied.

## Kept for every SaaS project

### Backend

- `AppBackendBasic.Core`: entities, DTOs, interfaces, validation contracts, and shared domain primitives.
- `AppBackendBasic.Infrastructure`: EF Core/PostgreSQL data access, repositories, migrations, converters, caching, email, media, and provider integrations.
- `AppBackendBasic.Services/Authentication`: registration, password login, refresh/revocation tokens, email verification, password reset, Google and Apple sign-in, OTP, device/session management, and user-provider mappings.
- `AppBackendBasic.Services/Security` and `AppBackendBasic.API/Middleware`: rate limiting, quota enforcement, session timeout, security headers, HTTPS enforcement, request logging, exception handling, and authorization helpers.
- `AppBackendBasic.API`: JWT bearer validation, CORS, antiforgery configuration, Swagger security, health checks, Hangfire background jobs, and API controllers.
- `AppBackendBasic.AdminUI`: cookie authentication, authorization, protected Razor Pages, session handling, and the API client boundary.

### Frontend

- `AppBackendBasic.WebUI/src/services/authService.ts`: token lifecycle, refresh coordination, logout, current-user loading, email verification, password reset, and social login flows.
- `AppBackendBasic.WebUI/src/lib/api.ts`: shared Axios client, bearer-token attachment, and unauthorized-response handling.
- React routing, protected-page patterns, shared UI components, form handling, loading states, and error boundaries.
- `.env.example`: frontend configuration contract. Keep real values in local or deployment environment variables.

## Before starting the next project

1. Copy `AppBackendBasic.API/appsettings.example.json` to an environment-specific configuration source. Set `ConnectionStrings:DefaultConnection`, `JWT:Secret`, `JWT:Issuer`, and `JWT:Audience`; never commit secrets.
2. Set `VITE_API_URL` in the WebUI environment. The local default is `http://localhost:5000/api`.
3. Replace application names, URLs, support email addresses, CORS origins, OAuth redirect URIs, and branding strings.
4. Review `AppBackendBasic.Infrastructure/Migrations` and keep only migrations required by the chosen schema. Do not edit an applied migration; add a new migration instead.
5. Decide which domain modules to retain. AI/LLM, SEO, analytics, payments, onboarding, notifications, and team modules are available but are product-specific extensions, not required by the authentication foundation.
6. Configure external providers through environment variables or a secret manager. The checked-in provider configuration contains placeholders only.
7. Rotate any credential that may previously have been exposed outside the local environment.

## Security baseline

- JWT validation checks issuer, audience, lifetime, and signing key with zero clock skew.
- Refresh tokens are generated with a cryptographic random number generator and persisted for revocation.
- Passwords and reset/verification flows are handled by backend services; the frontend never receives provider secrets.
- CORS uses an explicit allow-list and credentials rather than wildcard origins.
- Antiforgery, HTTPS enforcement, security headers, rate limiting, session timeout, quota checks, and global exception handling are registered in the API pipeline.
- Production configuration and secret files are ignored. Use user secrets, environment variables, or a managed secret store.

## Validation commands

From the repository root:

```bash
dotnet build AppBackendBasic.sln
cd AppBackendBasic.WebUI
npm ci
npm run build
npm run lint
```

The integration-test projects are included in the solution. Run them only after providing a test database and the required provider configuration.

## Cleanup policy

Generated `bin/`, `obj/`, frontend `dist/`, local uploads, `.DS_Store`, environment files, and OAuth client-secret JSON files are not template source and must not be committed. Keep migrations, security code, shared services, tests, and documentation until the new project's required surface has been verified.
