# AppBackendBasic - .NET 9 Backend Framework

A comprehensive .NET 9 backend framework for building modern web applications with authentication, logging, and more.

## Features

- **Authentication & Authorization**
  - JWT-based authentication
  - Email/password login
  - Social logins (Google, Apple)
  - Phone number verification with OTP
  - Refresh tokens

- **Logging & Monitoring**
  - Request/response logging
  - Structured logging with Serilog
  - Global exception handling

- **API Documentation**
  - Swagger/OpenAPI integration
  - API versioning

## Prerequisites

- .NET 9 SDK
- SQL Server or PostgreSQL
- Visual Studio Code

## Getting Started

### 1. Clone the repository

```bash
git clone <repository-url>
cd BasicFrameworkBackend
```

### 2. Update the connection string

Edit the `appsettings.json` file in the `AppBackendBasic.API` project and update the connection string to match your database setup:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=AppBackendBasic;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
}
```

### 3. Apply database migrations

Make the migration script executable:

```bash
chmod +x migrate.sh
```

Create and apply the initial migration:

```bash
./migrate.sh InitialCreate
```

### 4. Run the application

```bash
cd AppBackendBasic.API
dotnet run
```

The API will be available at `https://localhost:5001` and `http://localhost:5000`.

Swagger documentation will be available at `https://localhost:5001/swagger`.

## Project Structure

- **AppBackendBasic.API**: API controllers, middleware, and configuration
- **AppBackendBasic.Core**: Domain models, interfaces, and DTOs
- **AppBackendBasic.Infrastructure**: Data access, repositories, and database context
- **AppBackendBasic.Services**: Business logic and services

## API Endpoints

### Authentication

- `POST /api/Auth/register` - Register a new user
- `POST /api/Auth/login` - Login with email and password
- `POST /api/Auth/google` - Login with Google
- `POST /api/Auth/apple` - Login with Apple
- `POST /api/Auth/send-otp` - Send OTP to phone number
- `POST /api/Auth/verify-otp` - Verify OTP
- `POST /api/Auth/refresh-token` - Refresh access token
- `POST /api/Auth/revoke-token` - Revoke refresh token

### User Management

- `GET /api/User/profile` - Get current user profile
- `PUT /api/User/profile` - Update user profile
- `GET /api/User/sessions` - Get user sessions

## License

This project is licensed under the MIT License - see the LICENSE file for details.
