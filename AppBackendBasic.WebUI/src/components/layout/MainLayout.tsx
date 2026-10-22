import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import { Bell, Film, CreditCard, History, Settings as SettingsIcon } from 'lucide-react';
import UserDropdown from '../UserDropdown';
import NotificationDropdown from '../NotificationDropdown';

const navItems = [
  { path: '/studio', label: 'Caption Studio', icon: Film },
  { path: '/notifications', label: 'Notifications', icon: Bell },
  { path: '/pricing', label: 'Plans & pricing', icon: CreditCard },
  { path: '/login-history', label: 'Login history', icon: History },
  { path: '/settings', label: 'Settings', icon: SettingsIcon },
];

/** Account shell (sidebar + header) for the signed-in pages other than the full-screen studio. */
const MainLayout = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const isActive = (path: string) => location.pathname === path;

  return (
    <div className="min-h-screen bg-gray-50">
      <aside className="fixed left-0 top-0 h-full w-64 bg-gray-50 border-r border-gray-200 px-2.5 py-3 flex flex-col">
        <div className="mb-4 px-2.5 py-1.5 text-sm font-extrabold tracking-wide text-gray-900">CAPTAIN TITLE</div>

        <nav className="flex-1 overflow-y-auto">
          <div className="space-y-0.5">
            {navItems.map(({ path, label, icon: Icon }) => (
              <button
                key={path}
                onClick={() => navigate(path)}
                className={`w-full flex items-center gap-2.5 px-2.5 py-1.5 rounded-md text-sm transition-all ${
                  isActive(path) ? 'bg-gray-900 text-white font-medium' : 'text-gray-700 hover:bg-gray-100'
                }`}
              >
                <Icon className="w-4 h-4" />
                <span>{label}</span>
              </button>
            ))}
          </div>
        </nav>

        <div className="border-t border-gray-200 pt-3 mt-3">
          <UserDropdown />
        </div>
      </aside>

      <main className="ml-64">
        <header className="sticky top-0 z-40 bg-white border-b border-gray-100 px-5 py-2.5">
          <div className="flex items-center justify-end">
            <NotificationDropdown />
          </div>
        </header>

        <div className="p-5 bg-white min-h-screen">
          <Outlet />
        </div>
      </main>
    </div>
  );
};

export default MainLayout;
