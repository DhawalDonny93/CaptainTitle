/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_GITHUB_CLIENT_ID: string
  readonly VITE_GOOGLE_CLIENT_ID: string
  readonly VITE_API_BASE_URL: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}

// Google OAuth types
interface Google {
  accounts: {
    id: {
      initialize: (config: any) => void;
      prompt: () => void;
      renderButton: (element: HTMLElement, config: any) => void;
    };
  };
}

interface Window {
  google?: Google;
}
