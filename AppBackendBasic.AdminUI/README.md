# Admin Dashboard UI

A modern, responsive admin dashboard for managing the BasicFramework Backend.

## Features

- ✅ **Authentication** - Secure login with JWT tokens
- ✅ **Dashboard** - Overview with key metrics and charts
- ✅ **User Management** - View and manage users (Coming soon)
- ✅ **Analytics** - User growth, subscriptions, revenue (Coming soon)
- ✅ **Notifications** - Send push notifications (Coming soon)
- ✅ **Settings** - System configuration (Coming soon)
- ✅ **Audit Logs** - View system audit logs (Coming soon)

## Technology Stack

- **Framework**: ASP.NET Core 9 (Razor Pages)
- **UI**: Bootstrap 5
- **Charts**: Chart.js
- **Icons**: Font Awesome
- **HTTP Client**: HttpClient with session-based authentication

## Getting Started

### Prerequisites

- .NET 9 SDK
- Backend API running on https://localhost:7001

### Running the Application

1. **Start the Backend API** (in a separate terminal):
   ```bash
   cd ../AppBackendBasic.API
   dotnet run
   ```

2. **Start the Admin UI**:
   ```bash
   dotnet run
   ```

3. **Access the Dashboard**:
   - Open browser to: https://localhost:5001 (or the port shown in terminal)
   - Login with default credentials:
     - Email: `admin@yourapp.com`
     - Password: `Admin@123456`

## Project Structure

```
AppBackendBasic.AdminUI/
├── Pages/
│   ├── Dashboard/
│   │   └── Index.cshtml          # Dashboard home page
│   ├── Users/                     # User management (coming soon)
│   ├── Analytics/                 # Analytics pages (coming soon)
│   ├── Notifications/             # Notification management (coming soon)
│   ├── Settings/                  # System settings (coming soon)
│   ├── Audit/                     # Audit logs (coming soon)
│   ├── Shared/
│   │   └── _Layout.cshtml         # Main layout with sidebar
│   ├── Login.cshtml               # Login page
│   └── Logout.cshtml              # Logout handler
├── Services/
│   ├── ApiClient.cs               # HTTP client for API calls
│   └── AuthService.cs             # Authentication service
├── Models/                        # View models (coming soon)
├── wwwroot/                       # Static files
├── appsettings.json               # Configuration
└── Program.cs                     # Application startup
```

## Configuration

Edit `appsettings.json` to configure:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:7001/api",
    "Timeout": 30
  },
  "Authentication": {
    "CookieName": "AdminAuth",
    "LoginPath": "/Login",
    "LogoutPath": "/Logout",
    "ExpireTimeSpan": 60
  }
}
```

## Features Implemented

### ✅ Task 9.1: Project Setup
- [x] ASP.NET Core Web App created
- [x] Dependencies installed
- [x] Project structure organized
- [x] API client configured

### ✅ Task 9.2: Authentication & Login
- [x] Login page with modern UI
- [x] JWT token storage in session
- [x] Session management
- [x] Logout functionality

### ✅ Task 9.3: Dashboard Home Page (Partial)
- [x] Dashboard layout with sidebar
- [x] Stats cards (Users, Subscriptions, Revenue)
- [x] User growth chart
- [x] Responsive design

## Next Steps

Follow the tasks in `../TASKS.md` Phase 9 to implement:

- [ ] **Task 9.4**: User Management Interface
- [ ] **Task 9.5**: Analytics Dashboard
- [ ] **Task 9.6**: Notification Management
- [ ] **Task 9.7**: System Settings
- [ ] **Task 9.8**: Audit Logs Viewer
- [ ] **Task 9.9**: UI/UX Enhancements
- [ ] **Task 9.10**: Deployment & Documentation

## API Endpoints Used

The Admin UI consumes the following backend API endpoints:

### Authentication
- `POST /api/auth/login` - Admin login

### Dashboard
- `GET /api/admin/dashboard/summary` - Dashboard statistics

### Coming Soon
- User management endpoints
- Analytics endpoints
- Notification endpoints
- Settings endpoints
- Audit log endpoints

## Development

### Adding New Pages

1. Create a new folder in `Pages/` (e.g., `Pages/NewFeature/`)
2. Add `.cshtml` and `.cshtml.cs` files
3. Use the `_Layout.cshtml` for consistent UI
4. Add navigation link in `_Layout.cshtml` sidebar

### Making API Calls

Use the `ApiClient` service:

```csharp
// In your PageModel
private readonly ApiClient _apiClient;

public async Task OnGetAsync()
{
    var data = await _apiClient.GetAsync<MyModel>("/admin/endpoint");
}
```

## Troubleshooting

### Cannot Connect to API
- Ensure backend API is running on https://localhost:7001
- Check `appsettings.json` for correct API URL
- Verify CORS is configured in backend

### Login Fails
- Verify admin account exists in database
- Check backend logs for authentication errors
- Ensure JWT token is being returned

### Session Expires Quickly
- Increase `ExpireTimeSpan` in `appsettings.json`
- Check session middleware is configured in `Program.cs`

## Screenshots

### Login Page
Modern gradient design with secure authentication

### Dashboard
Clean, intuitive dashboard with key metrics and charts

### Sidebar Navigation
Easy access to all admin features

## License

Part of BasicFramework Backend project.

## Support

For issues or questions, refer to the main project documentation.
