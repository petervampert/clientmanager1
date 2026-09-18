# Client Manager

A full-stack client management application built with **Angular 22** (frontend) and **ASP.NET Core 10** (backend), using SQLite for persistence, JWT for authentication, and QuestPDF for PDF report generation.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Running with Docker (recommended)](#running-with-docker-recommended)
  - [Running locally (development)](#running-locally-development)
- [API Reference](#api-reference)
  - [Authentication](#authentication)
  - [Clients](#clients)
- [Frontend Routes](#frontend-routes)
- [Configuration](#configuration)
- [Testing](#testing)
- [Docker Details](#docker-details)

---

## Architecture Overview

```
┌─────────────────────────────────────────┐
│           Browser (port 80)             │
│         Angular SPA (nginx)             │
│                                         │
│  /           → Angular app              │
│  /api/*      → proxy to API container   │
└──────────────────┬──────────────────────┘
                   │ HTTP (internal Docker network)
┌──────────────────▼──────────────────────┐
│       ASP.NET Core API (port 8080)      │
│   Auth · Clients CRUD · PDF Reports     │
│                                         │
│  SQLite database (Docker volume)        │
└─────────────────────────────────────────┘
```

In development the Angular devserver (`ng serve`) runs on `:4200` and calls the API directly on `:5093`.

---

## Tech Stack

| Layer       | Technology                                      |
|-------------|-------------------------------------------------|
| Frontend    | Angular 22, Angular Material 22, RxJS, Signals  |
| Backend     | ASP.NET Core 10, C#                             |
| Database    | SQLite via Entity Framework Core 10 (Code-First)|
| Auth        | JWT Bearer tokens, BCrypt password hashing      |
| PDF         | QuestPDF (Community license)                    |
| API Docs    | Swagger UI (Swashbuckle 10)                     |
| Container   | Docker, Docker Compose, nginx:alpine            |

---

## Project Structure

```
/
├── ClientManager.API/          # ASP.NET Core Web API
│   ├── Controllers/
│   │   ├── AuthController.cs       # POST /api/auth/login, /register
│   │   └── ClientsController.cs    # CRUD + PDF report
│   ├── Data/
│   │   └── AppDbContext.cs          # EF Core DbContext
│   ├── DTOs/
│   │   ├── AuthDtos.cs              # RegisterRequest, LoginRequest, AuthResponse
│   │   └── ClientDtos.cs            # ClientRequest, ClientResponse, PagedResult
│   ├── Migrations/                  # EF Core auto-generated migrations
│   ├── Models/
│   │   ├── Client.cs
│   │   └── User.cs
│   ├── Services/
│   │   ├── PdfReportService.cs      # QuestPDF A4 report
│   │   └── TokenService.cs          # JWT generation
│   ├── appsettings.json
│   ├── Dockerfile
│   └── .dockerignore
│
├── client-manager/             # Angular 22 SPA
│   ├── src/app/
│   │   ├── core/
│   │   │   ├── guards/              # authGuard, noAuthGuard
│   │   │   ├── interceptors/        # authInterceptor (JWT header)
│   │   │   ├── models/              # auth.models.ts, client.models.ts
│   │   │   └── services/            # AuthService, ClientService
│   │   ├── features/
│   │   │   ├── auth/
│   │   │   │   ├── login/           # LoginComponent
│   │   │   │   └── register/        # RegisterComponent
│   │   │   └── clients/
│   │   │       ├── detail/          # ClientDetailComponent
│   │   │       ├── form/            # ClientFormComponent (create + edit)
│   │   │       └── list/            # ClientsListComponent
│   │   └── shared/components/
│   │       ├── confirm-dialog/      # Reusable delete dialog
│   │       └── layout/shell/        # App shell (toolbar + outlet)
│   ├── src/environments/
│   │   ├── environment.ts           # Production: apiUrl = '/api'
│   │   └── environment.development.ts  # Dev: apiUrl = 'http://localhost:5093/api'
│   ├── nginx.conf
│   ├── Dockerfile
│   └── .dockerignore
│
├── docker-compose.yml
├── .gitignore
└── README.md
```

---

## Getting Started

### Prerequisites

| Tool | Version |
|------|---------|
| Docker | 20+ |
| Docker Compose | v2+ |
| .NET SDK | 10.0 (local dev only) |
| Node.js | 24 (local dev only) |

### Running with Docker (recommended)

```bash
# Clone the repository
git clone https://github.com/petervampert/clientmanager1.git
cd clientmanager1

# Build and start both containers
docker compose up --build

# Run in background
docker compose up --build -d
```

| Service  | URL                                      |
|----------|------------------------------------------|
| Angular  | http://localhost                         |
| API      | http://localhost:5000                    |
| Swagger  | http://localhost:5000/swagger            |

Stop everything:
```bash
docker compose down
```

Stop and remove the database volume (⚠️ destroys all data):
```bash
docker compose down -v
```

### Running locally (development)

**Backend:**
```bash
cd ClientManager.API
dotnet run
# API available at http://localhost:5093
# Swagger UI at http://localhost:5093/swagger
```

**Frontend:**
```bash
cd client-manager
npm install
npm start
# App available at http://localhost:4200
```

> The Angular dev environment (`environment.development.ts`) points to `http://localhost:5093/api` automatically when running `npm start`.

---

## API Reference

All protected endpoints require a `Bearer` JWT token in the `Authorization` header.

### Authentication

#### `POST /api/auth/register`

Registers a new user and returns a JWT token.

**Request body:**
```json
{
  "username": "john",       // required, min 3 chars
  "password": "secret123"   // required, min 6 chars
}
```

**Responses:**
| Status | Description |
|--------|-------------|
| `201 Created` | Registration successful, returns `AuthResponse` |
| `400 Bad Request` | Validation failed |
| `409 Conflict` | Username already taken |

---

#### `POST /api/auth/login`

Authenticates a user and returns a JWT token.

**Request body:**
```json
{
  "username": "john",
  "password": "secret123"
}
```

**Responses:**
| Status | Description |
|--------|-------------|
| `200 OK` | Login successful, returns `AuthResponse` |
| `400 Bad Request` | Validation failed |
| `401 Unauthorized` | Invalid credentials |

**`AuthResponse` shape:**
```json
{
  "token": "eyJhbGci...",
  "username": "john",
  "expiresAt": "2026-09-17T10:00:00Z"
}
```

---

### Clients

All endpoints require `Authorization: Bearer <token>`.

#### `GET /api/clients`

Returns a paginated, searchable list of clients.

**Query parameters:**
| Param | Type | Default | Description |
|-------|------|---------|-------------|
| `page` | int | 1 | Page number (≥ 1) |
| `pageSize` | int | 10 | Items per page (1–100) |
| `search` | string | — | Searches FirstName, LastName, Email, Phone |

**Response `200 OK`:**
```json
{
  "items": [ { "id": 1, "firstName": "Jane", ... } ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 10
}
```

---

#### `GET /api/clients/{id}`

Returns a single client by ID.

| Status | Description |
|--------|-------------|
| `200 OK` | Returns `ClientResponse` |
| `404 Not Found` | Client does not exist |

---

#### `POST /api/clients`

Creates a new client.

**Request body:**
```json
{
  "firstName": "Jane",       // required
  "lastName": "Doe",         // required
  "email": "jane@example.com", // required, valid email
  "phone": "+1 555 0100",    // optional
  "address": "123 Main St"   // optional
}
```

| Status | Description |
|--------|-------------|
| `201 Created` | Returns the created `ClientResponse` |
| `400 Bad Request` | Validation failed |
| `409 Conflict` | Email already in use |

---

#### `PUT /api/clients/{id}`

Updates an existing client. Same request body as `POST /api/clients`.

| Status | Description |
|--------|-------------|
| `200 OK` | Returns updated `ClientResponse` |
| `400 Bad Request` | Validation failed |
| `404 Not Found` | Client does not exist |
| `409 Conflict` | Email already used by another client |

---

#### `DELETE /api/clients/{id}`

Deletes a client permanently.

| Status | Description |
|--------|-------------|
| `204 No Content` | Deleted successfully |
| `404 Not Found` | Client does not exist |

---

#### `GET /api/clients/report/pdf`

Generates and downloads a PDF report of all clients.

| Status | Description |
|--------|-------------|
| `200 OK` | Returns `application/pdf` binary |

---

## Frontend Routes

| Route | Guard | Component | Description |
|-------|-------|-----------|-------------|
| `/auth/login` | `noAuthGuard` | `LoginComponent` | Sign in |
| `/auth/register` | `noAuthGuard` | `RegisterComponent` | Create account |
| `/clients` | `authGuard` | `ClientsListComponent` | Paginated client table |
| `/clients/new` | `authGuard` | `ClientFormComponent` | Create new client |
| `/clients/:id` | `authGuard` | `ClientDetailComponent` | View client details |
| `/clients/:id/edit` | `authGuard` | `ClientFormComponent` | Edit existing client |

- `authGuard` — redirects to `/auth/login` if not authenticated
- `noAuthGuard` — redirects to `/clients` if already authenticated

---

## Configuration

### Backend (`ClientManager.API/appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/app/data/clientmanager.db"
  },
  "Jwt": {
    "Key": "super-secret-jwt-key-change-in-production-min32chars!!",
    "Issuer": "ClientManagerAPI",
    "Audience": "ClientManagerApp",
    "ExpiresInHours": 8
  }
}
```

> ⚠️ Change `Jwt:Key` to a strong secret before deploying to production. Minimum 32 characters.

### Frontend environments

| File | Used when | `apiUrl` |
|------|-----------|----------|
| `environment.development.ts` | `ng serve` | `http://localhost:5093/api` |
| `environment.ts` | `ng build --configuration=production` | `/api` |

---

## Testing

### API tests

```bash
cd ClientManager.API.Tests
dotnet test
```

Tests cover: `AuthController`, `ClientsController`, `TokenService`, `PdfReportService`.

### Angular tests

```bash
cd client-manager
npx ng test --run          # single run with Karma
# or
npx vitest run             # if configured
```

Tests cover: `AuthService`, `ClientService`, `authGuard`, `noAuthGuard`, `authInterceptor`.

---

## Docker Details

### Images

| Service | Base images |
|---------|-------------|
| `api` | Build: `mcr.microsoft.com/dotnet/sdk:10.0` → Runtime: `mcr.microsoft.com/dotnet/aspnet:10.0` |
| `frontend` | Build: `node:24-alpine` → Runtime: `nginx:alpine` |

### Volumes

| Volume | Purpose |
|--------|---------|
| `db-data` | Persists the SQLite database across container restarts |

### Networking

Both containers join the `clientmanager-net` bridge network. The nginx container resolves the API by the Docker DNS name `api` (the service name in `docker-compose.yml`).

### Port mapping

| Container port | Host port | Service |
|---------------|-----------|---------|
| `8080` | `5000` | ASP.NET Core API |
| `80` | `80` | nginx / Angular |
