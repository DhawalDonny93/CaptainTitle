# AppBackend Basic - WebUI

A beautiful, modern web interface for the AppBackend Basic platform, featuring an elegant landing page, smooth onboarding flow, authentication, and dashboard.

## Features

- 🎨 **Beautiful Landing Page** - Modern, gradient-rich design inspired by lovable.dev
- 🚀 **Smooth Onboarding Flow** - Multi-step wizard with progress tracking
- 🔐 **Authentication** - Login and registration with social auth options
- 💳 **Pricing/Paywall** - Flexible pricing plans with monthly/yearly billing
- 📊 **Dashboard** - Comprehensive dashboard with stats and activity tracking
- 🎯 **Responsive Design** - Works perfectly on all devices
- ⚡ **Fast Performance** - Built with Vite for lightning-fast development and builds

## Tech Stack

- **React 18** - Modern React with hooks
- **TypeScript** - Type-safe development
- **Vite** - Next-generation frontend tooling
- **TailwindCSS** - Utility-first CSS framework
- **React Router** - Client-side routing
- **Lucide React** - Beautiful icon library
- **Axios** - HTTP client for API calls
- **Framer Motion** - Smooth animations

## Getting Started

### Prerequisites

- Node.js 16+ and npm

### Installation

```bash
# Install dependencies
npm install

# Start development server
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview
```

The development server will start at `http://localhost:3000`

## Project Structure

```
src/
├── components/
│   └── ui/           # Reusable UI components
│       ├── Button.tsx
│       ├── Input.tsx
│       └── Card.tsx
├── pages/            # Page components
│   ├── LandingPage.tsx
│   ├── OnboardingFlow.tsx
│   ├── Login.tsx
│   ├── Paywall.tsx
│   └── Dashboard.tsx
├── lib/              # Utilities and helpers
│   ├── utils.ts      # Utility functions
│   └── api.ts        # API client configuration
├── App.tsx           # Main app component with routing
├── main.tsx          # Application entry point
└── index.css         # Global styles and Tailwind imports
```

## Pages

### Landing Page (`/`)
- Hero section with call-to-action
- Feature showcase
- Benefits section
- Pricing CTA
- Footer with links

### Onboarding Flow (`/onboarding`)
- Multi-step wizard (3 steps)
- Progress indicator
- Form validation
- Smooth transitions

### Login (`/login`)
- Email/password authentication
- Social login options (Google, GitHub)
- Toggle between login/register
- Forgot password link

### Paywall (`/paywall`)
- Three pricing tiers (Starter, Professional, Enterprise)
- Monthly/yearly billing toggle
- Feature comparison
- FAQ section

### Dashboard (`/dashboard`)
- Sidebar navigation
- Stats cards with metrics
- Recent activity feed
- Quick actions
- Active projects overview

## API Integration

The app is configured to connect to the backend API at `http://localhost:5082api` by default. You can change this by setting the `VITE_API_URL` environment variable.

Create a `.env` file in the root:

```env
VITE_API_URL=http://localhost:5082/api
```

## Customization

### Colors

The app uses a purple-blue gradient theme. You can customize colors in `tailwind.config.js` and `src/index.css`.

### Components

All UI components are in `src/components/ui/` and can be easily customized or extended.

## Building for Production

```bash
npm run build
```

The built files will be in the `dist/` directory, ready to be deployed to any static hosting service.

## Deployment

The app can be deployed to:
- Netlify
- Vercel
- GitHub Pages
- Any static hosting service

## License

MIT
