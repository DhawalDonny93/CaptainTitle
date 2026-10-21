# LLM Provider Admin Endpoint Authentication Guide

## 🔐 Issue: 401 Unauthorized on `/api/admin/llm-providers`

The 401 error is **EXPECTED BEHAVIOR** - this is a security feature, not a bug.

---

## ✅ Why You're Getting 401

The `AdminController` has this authorization requirement:

```csharp
[ApiController]
[Authorize(Roles = "Admin")] // ← Admin role required for ALL endpoints
[Route("api/admin")]
public class AdminController : ControllerBase
```

**All endpoints under `/api/admin/*` require:**
1. Valid JWT token in the `Authorization` header
2. User must have `Role = "Admin"`

---

## 🚀 SOLUTION: Authenticate as Admin

### **Step 1: Check if Admin User Exists**

Run this SQL query:
```sql
SELECT "Id", "Email", "Role", "IsActive" 
FROM "User" 
WHERE "Role" = 'Admin';
```

If no admin exists, the `AdminSeedService` should create one on startup.

### **Step 2: Login as Admin**

**Request:**
```bash
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "email": "admin@example.com",
  "password": "Admin@123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "...",
  "user": {
    "id": 1,
    "email": "admin@example.com",
    "role": "Admin"
  }
}
```

### **Step 3: Use Token to Access Admin Endpoints**

**Request:**
```bash
GET http://localhost:5000/api/admin/llm-providers
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response:**
```json
[
  {
    "id": 1,
    "name": "OpenAI",
    "displayName": "OpenAI (ChatGPT)",
    "apiEndpoint": "https://api.openai.com/v1",
    "isActive": true,
    "priority": 100,
    "configs": [
      {
        "id": 1,
        "providerId": 1,
        "configKey": "ApiKey",
        "configValue": "[ENCRYPTED]",
        "isEncrypted": true
      }
    ]
  }
]
```

---

## 🔧 ALTERNATIVE: Temporarily Disable Auth (Development Only)

**⚠️ WARNING: NEVER do this in production!**

If you need to test without authentication in development:

**Option 1: Comment out authorization on specific endpoint**
```csharp
/// <summary>
/// Get all LLM providers (admin)
/// </summary>
[HttpGet("llm-providers")]
[AllowAnonymous] // ← Add this for testing only
public async Task<IActionResult> GetLLMProviders()
```

**Option 2: Comment out controller-level authorization**
```csharp
[ApiController]
// [Authorize(Roles = "Admin")] // ← Comment out for testing
[Route("api/admin")]
public class AdminController : ControllerBase
```

**Remember to remove these changes before committing!**

---

## 📋 Admin User Configuration

Check your `appsettings.json` for admin seed settings:

```json
{
  "AdminSeed": {
    "Email": "admin@example.com",
    "Password": "Admin@123",
    "FirstName": "Admin",
    "LastName": "User"
  }
}
```

The admin user is created automatically on application startup by `AdminSeedService`.

---

## 🧪 Testing with Postman/Insomnia

### **1. Create Login Request**
- Method: `POST`
- URL: `http://localhost:5000/api/auth/login`
- Body (JSON):
  ```json
  {
    "email": "admin@example.com",
    "password": "Admin@123"
  }
  ```

### **2. Extract Token**
Copy the `token` value from the response.

### **3. Create Admin Request**
- Method: `GET`
- URL: `http://localhost:5000/api/admin/llm-providers`
- Headers:
  - `Authorization`: `Bearer <paste-token-here>`

---

## 🔍 Verify JWT Configuration

Check `appsettings.json`:

```json
{
  "JWT": {
    "Secret": "d4f7a9b2c3e5f6a1d8b3c4e7f9a0b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a",
    "Issuer": "MyAwesomeApp",
    "Audience": "MyAwesomeAppUsers",
    "ExpirationMinutes": "60",
    "RefreshTokenExpirationDays": "7"
  }
}
```

The JWT configuration in `Program.cs` (lines 115-188) validates:
- Issuer: `MyAwesomeApp`
- Audience: `MyAwesomeAppUsers`
- Signing key from `JWT:Secret`

---

## 🐛 Troubleshooting

### **Issue: "Invalid token"**
- Token might be expired (60 minutes default)
- Login again to get a fresh token

### **Issue: "User not in Admin role"**
```sql
-- Update user role to Admin
UPDATE "User" 
SET "Role" = 'Admin' 
WHERE "Email" = 'your-email@example.com';
```

### **Issue: "Admin user not found"**
Check application logs for:
```
Admin user seeded successfully
```

If not found, manually create admin user:
```sql
INSERT INTO "User" (
    "Email", "PasswordHash", "Role", "IsActive", 
    "EmailVerified", "CreatedAt", "UpdatedAt"
)
VALUES (
    'admin@example.com',
    '[HASHED_PASSWORD]', -- Use proper password hashing
    'Admin',
    TRUE,
    TRUE,
    NOW(),
    NOW()
);
```

---

## 📊 Database Schema Fix

If you're getting `column ProviderId does not exist` error, the database column mapping has been fixed in `ApplicationDbContext.cs`:

```csharp
entity.Property(e => e.ProviderId).IsRequired().HasColumnName("ProviderId");
```

**Restart the application** after this fix to apply the new EF Core mapping.

---

## ✅ Summary

**The 401 error is correct behavior.** To access `/api/admin/llm-providers`:

1. ✅ Login as admin user
2. ✅ Get JWT token from response
3. ✅ Include token in `Authorization: Bearer <token>` header
4. ✅ Make request to admin endpoint

**Default Admin Credentials:**
- Email: `admin@example.com`
- Password: `Admin@123`

Change these in production!
