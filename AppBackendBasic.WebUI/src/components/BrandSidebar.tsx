import { NavLink } from 'react-router-dom';
import { User, Users } from 'lucide-react';

const BrandSidebar = () => {
  const navItems = [
    { path: '/brand-profile', label: 'Profile', icon: User },
    { path: '/competitors', label: 'Competitors', icon: Users }
  ];

  return (
    <div className="w-64 bg-white border-r border-gray-200 flex-shrink-0">
      <div className="p-6">
        <h2 className="text-lg font-bold text-gray-900 mb-1">BRAND</h2>
      </div>
      
      <nav className="px-3 space-y-1">
        {navItems.map((item) => {
          const Icon = item.icon;
          return (
            <NavLink
              key={item.path}
              to={item.path}
              className={({ isActive }) =>
                `flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors ${
                  isActive
                    ? 'bg-gray-100 text-gray-900'
                    : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900'
                }`
              }
            >
              <Icon className="w-4 h-4" />
              <span>{item.label}</span>
            </NavLink>
          );
        })}
      </nav>
    </div>
  );
};

export default BrandSidebar;
