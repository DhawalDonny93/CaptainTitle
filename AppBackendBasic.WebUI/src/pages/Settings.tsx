import { useState, useEffect } from 'react';
import {
  User,
  Mail,
  Bell,
  Lock,
  Globe,
  Moon,
  Sun,
  Smartphone,
  Save,
  CheckCircle,
  AlertCircle,
} from 'lucide-react';
import axios from 'axios';
import { authService } from '../services/authService';

const API_BASE_URL = import.meta.env.VITE_API_URL;

interface UserProfile {
  email: string;
  name?: string;
  phoneNumber?: string;
}

interface NotificationSettings {
  emailNotifications: boolean;
  pushNotifications: boolean;
  weeklyReports: boolean;
  productUpdates: boolean;
}

export default function Settings() {
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  // Profile Settings
  const [profile, setProfile] = useState<UserProfile>({
    email: '',
    name: '',
    phoneNumber: '',
  });

  // Notification Settings
  const [notifications, setNotifications] = useState<NotificationSettings>({
    emailNotifications: true,
    pushNotifications: true,
    weeklyReports: false,
    productUpdates: true,
  });

  // Appearance Settings
  const [theme, setTheme] = useState<'light' | 'dark'>('light');
  const [language, setLanguage] = useState('en');

  // Password Change
  const [passwordData, setPasswordData] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  });

  useEffect(() => {
    fetchUserProfile();
    fetchSettings();
  }, []);

  const fetchUserProfile = async () => {
    try {
      const response = await axios.get(`${API_BASE_URL}/user/profile`, {
        withCredentials: true,
        headers: {
          Authorization: `Bearer ${authService.getAccessToken()}`,
        },
      });
      setProfile(response.data);
    } catch (err) {
      console.error('Failed to fetch profile:', err);
    } finally {
      setLoading(false);
    }
  };

  const fetchSettings = async () => {
    try {
      const response = await axios.get(`${API_BASE_URL}/user/settings`, {
        withCredentials: true,
        headers: {
          Authorization: `Bearer ${authService.getAccessToken()}`,
        },
      });
      if (response.data.notifications) {
        setNotifications(response.data.notifications);
      }
      if (response.data.theme) {
        setTheme(response.data.theme);
      }
      if (response.data.language) {
        setLanguage(response.data.language);
      }
    } catch (err) {
      console.error('Failed to fetch settings:', err);
    }
  };

  const handleProfileUpdate = async () => {
    setSaving(true);
    setMessage(null);
    try {
      await axios.put(
        `${API_BASE_URL}/user/profile`,
        profile,
        {
          withCredentials: true,
          headers: {
            Authorization: `Bearer ${authService.getAccessToken()}`,
          },
        }
      );
      setMessage({ type: 'success', text: 'Profile updated successfully!' });
    } catch (err: any) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Failed to update profile' });
    } finally {
      setSaving(false);
    }
  };

  const handleNotificationUpdate = async () => {
    setSaving(true);
    setMessage(null);
    try {
      await axios.put(
        `${API_BASE_URL}/user/settings/notifications`,
        notifications,
        {
          withCredentials: true,
          headers: {
            Authorization: `Bearer ${authService.getAccessToken()}`,
          },
        }
      );
      setMessage({ type: 'success', text: 'Notification settings updated!' });
    } catch (err: any) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Failed to update settings' });
    } finally {
      setSaving(false);
    }
  };

  const handlePasswordChange = async () => {
    if (passwordData.newPassword !== passwordData.confirmPassword) {
      setMessage({ type: 'error', text: 'Passwords do not match' });
      return;
    }

    if (passwordData.newPassword.length < 8) {
      setMessage({ type: 'error', text: 'Password must be at least 8 characters' });
      return;
    }

    setSaving(true);
    setMessage(null);
    try {
      await axios.post(
        `${API_BASE_URL}/user/change-password`,
        {
          currentPassword: passwordData.currentPassword,
          newPassword: passwordData.newPassword,
        },
        {
          withCredentials: true,
          headers: {
            Authorization: `Bearer ${authService.getAccessToken()}`,
          },
        }
      );
      setMessage({ type: 'success', text: 'Password changed successfully!' });
      setPasswordData({ currentPassword: '', newPassword: '', confirmPassword: '' });
    } catch (err: any) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Failed to change password' });
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <svg className="w-10 h-10 animate-spin text-gray-400" fill="none" viewBox="0 0 24 24">
          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
          <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="mb-12">
        <h1 className="text-4xl font-light tracking-tight text-gray-900 mb-2">Settings</h1>
        <p className="text-gray-600 font-light">Manage your account preferences and settings</p>
      </div>

      {/* Message Alert */}
      {message && (
        <div
          className={`p-4 rounded-2xl flex items-start gap-3 ${
            message.type === 'success' ? 'bg-green-50 border border-green-200' : 'bg-red-50 border border-red-200'
          }`}
        >
          {message.type === 'success' ? (
            <CheckCircle className="w-5 h-5 text-green-600 flex-shrink-0 mt-0.5" />
          ) : (
            <AlertCircle className="w-5 h-5 text-red-600 flex-shrink-0 mt-0.5" />
          )}
          <p className={`text-sm font-medium ${message.type === 'success' ? 'text-green-800' : 'text-red-800'}`}>
            {message.text}
          </p>
        </div>
      )}

      {/* Profile Settings */}
      <div className="bg-white rounded-3xl p-8 border border-gray-100">
        <div className="flex items-center gap-3 mb-6">
          <div className="w-12 h-12 bg-gray-100 rounded-2xl flex items-center justify-center">
            <User className="w-6 h-6 text-gray-600" />
          </div>
          <div>
            <h2 className="text-2xl font-normal text-gray-900">Profile Information</h2>
            <p className="text-sm text-gray-600 font-light">Update your personal details</p>
          </div>
        </div>

        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Name</label>
            <input
              type="text"
              value={profile.name || ''}
              onChange={(e) => setProfile({ ...profile, name: e.target.value })}
              className="w-full px-4 py-3 border border-gray-200 rounded-2xl focus:outline-none focus:ring-2 focus:ring-gray-900 focus:border-transparent"
              placeholder="Your name"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Email</label>
            <div className="relative">
              <Mail className="absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-400" />
              <input
                type="email"
                value={profile.email}
                onChange={(e) => setProfile({ ...profile, email: e.target.value })}
                className="w-full pl-12 pr-4 py-3 border border-gray-200 rounded-2xl focus:outline-none focus:ring-2 focus:ring-gray-900 focus:border-transparent"
                placeholder="your.email@example.com"
              />
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Phone Number</label>
            <div className="relative">
              <Smartphone className="absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-400" />
              <input
                type="tel"
                value={profile.phoneNumber || ''}
                onChange={(e) => setProfile({ ...profile, phoneNumber: e.target.value })}
                className="w-full pl-12 pr-4 py-3 border border-gray-200 rounded-2xl focus:outline-none focus:ring-2 focus:ring-gray-900 focus:border-transparent"
                placeholder="+1 (555) 123-4567"
              />
            </div>
          </div>

          <button
            onClick={handleProfileUpdate}
            disabled={saving}
            className="px-6 py-3 bg-black text-white rounded-full hover:bg-gray-800 transition-all font-medium disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
          >
            {saving ? (
              <>
                <svg className="w-5 h-5 animate-spin" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                Saving...
              </>
            ) : (
              <>
                <Save className="w-5 h-5" />
                Save Changes
              </>
            )}
          </button>
        </div>
      </div>

      {/* Notification Settings */}
      <div className="bg-white rounded-3xl p-8 border border-gray-100">
        <div className="flex items-center gap-3 mb-6">
          <div className="w-12 h-12 bg-gray-100 rounded-2xl flex items-center justify-center">
            <Bell className="w-6 h-6 text-gray-600" />
          </div>
          <div>
            <h2 className="text-2xl font-normal text-gray-900">Notifications</h2>
            <p className="text-sm text-gray-600 font-light">Manage how you receive updates</p>
          </div>
        </div>

        <div className="space-y-4">
          {[
            { key: 'emailNotifications', label: 'Email Notifications', description: 'Receive notifications via email' },
            { key: 'pushNotifications', label: 'Push Notifications', description: 'Receive push notifications in browser' },
            { key: 'weeklyReports', label: 'Weekly Reports', description: 'Get weekly summary of your progress' },
            { key: 'productUpdates', label: 'Product Updates', description: 'Stay informed about new features' },
          ].map((item) => (
            <div key={item.key} className="flex items-center justify-between p-4 bg-gray-50 rounded-2xl">
              <div>
                <p className="font-medium text-gray-900">{item.label}</p>
                <p className="text-sm text-gray-600 font-light">{item.description}</p>
              </div>
              <button
                onClick={() =>
                  setNotifications({
                    ...notifications,
                    [item.key]: !notifications[item.key as keyof NotificationSettings],
                  })
                }
                className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${
                  notifications[item.key as keyof NotificationSettings] ? 'bg-gray-900' : 'bg-gray-300'
                }`}
              >
                <span
                  className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
                    notifications[item.key as keyof NotificationSettings] ? 'translate-x-6' : 'translate-x-1'
                  }`}
                />
              </button>
            </div>
          ))}

          <button
            onClick={handleNotificationUpdate}
            disabled={saving}
            className="px-6 py-3 bg-black text-white rounded-full hover:bg-gray-800 transition-all font-medium disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
          >
            {saving ? (
              <>
                <svg className="w-5 h-5 animate-spin" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                Saving...
              </>
            ) : (
              <>
                <Save className="w-5 h-5" />
                Save Preferences
              </>
            )}
          </button>
        </div>
      </div>

      {/* Security Settings */}
      <div className="bg-white rounded-3xl p-8 border border-gray-100">
        <div className="flex items-center gap-3 mb-6">
          <div className="w-12 h-12 bg-gray-100 rounded-2xl flex items-center justify-center">
            <Lock className="w-6 h-6 text-gray-600" />
          </div>
          <div>
            <h2 className="text-2xl font-normal text-gray-900">Security</h2>
            <p className="text-sm text-gray-600 font-light">Update your password and security settings</p>
          </div>
        </div>

        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Current Password</label>
            <input
              type="password"
              value={passwordData.currentPassword}
              onChange={(e) => setPasswordData({ ...passwordData, currentPassword: e.target.value })}
              className="w-full px-4 py-3 border border-gray-200 rounded-2xl focus:outline-none focus:ring-2 focus:ring-gray-900 focus:border-transparent"
              placeholder="Enter current password"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">New Password</label>
            <input
              type="password"
              value={passwordData.newPassword}
              onChange={(e) => setPasswordData({ ...passwordData, newPassword: e.target.value })}
              className="w-full px-4 py-3 border border-gray-200 rounded-2xl focus:outline-none focus:ring-2 focus:ring-gray-900 focus:border-transparent"
              placeholder="Enter new password"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Confirm New Password</label>
            <input
              type="password"
              value={passwordData.confirmPassword}
              onChange={(e) => setPasswordData({ ...passwordData, confirmPassword: e.target.value })}
              className="w-full px-4 py-3 border border-gray-200 rounded-2xl focus:outline-none focus:ring-2 focus:ring-gray-900 focus:border-transparent"
              placeholder="Confirm new password"
            />
          </div>

          <button
            onClick={handlePasswordChange}
            disabled={saving || !passwordData.currentPassword || !passwordData.newPassword || !passwordData.confirmPassword}
            className="px-6 py-3 bg-black text-white rounded-full hover:bg-gray-800 transition-all font-medium disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
          >
            {saving ? (
              <>
                <svg className="w-5 h-5 animate-spin" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                Changing...
              </>
            ) : (
              <>
                <Lock className="w-5 h-5" />
                Change Password
              </>
            )}
          </button>
        </div>
      </div>

      {/* Appearance Settings */}
      <div className="bg-white rounded-3xl p-8 border border-gray-100">
        <div className="flex items-center gap-3 mb-6">
          <div className="w-12 h-12 bg-gray-100 rounded-2xl flex items-center justify-center">
            {theme === 'dark' ? (
              <Moon className="w-6 h-6 text-gray-600" />
            ) : (
              <Sun className="w-6 h-6 text-gray-600" />
            )}
          </div>
          <div>
            <h2 className="text-2xl font-normal text-gray-900">Appearance</h2>
            <p className="text-sm text-gray-600 font-light">Customize how ProQuest AI looks</p>
          </div>
        </div>

        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-3">Theme</label>
            <div className="grid grid-cols-2 gap-4">
              <button
                onClick={() => setTheme('light')}
                className={`p-4 border-2 rounded-2xl transition-all ${
                  theme === 'light' ? 'border-gray-900 bg-gray-50' : 'border-gray-200 hover:border-gray-300'
                }`}
              >
                <Sun className="w-6 h-6 mx-auto mb-2 text-gray-900" />
                <p className="text-sm font-medium text-gray-900">Light</p>
              </button>
              <button
                onClick={() => setTheme('dark')}
                className={`p-4 border-2 rounded-2xl transition-all ${
                  theme === 'dark' ? 'border-gray-900 bg-gray-50' : 'border-gray-200 hover:border-gray-300'
                }`}
              >
                <Moon className="w-6 h-6 mx-auto mb-2 text-gray-900" />
                <p className="text-sm font-medium text-gray-900">Dark</p>
              </button>
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Language</label>
            <div className="relative">
              <Globe className="absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-400" />
              <select
                value={language}
                onChange={(e) => setLanguage(e.target.value)}
                className="w-full pl-12 pr-4 py-3 border border-gray-200 rounded-2xl focus:outline-none focus:ring-2 focus:ring-gray-900 focus:border-transparent appearance-none bg-white"
              >
                <option value="en">English</option>
                <option value="es">Spanish</option>
                <option value="fr">French</option>
                <option value="de">German</option>
                <option value="ja">Japanese</option>
              </select>
            </div>
          </div>
        </div>
      </div>

      {/* Danger Zone */}
      <div className="bg-red-50 rounded-3xl p-8 border-2 border-red-200">
        <h2 className="text-2xl font-normal text-red-900 mb-2">Danger Zone</h2>
        <p className="text-sm text-red-700 font-light mb-6">
          Irreversible actions that affect your account
        </p>
        <button
          onClick={() => {
            if (window.confirm('Are you sure you want to delete your account? This action cannot be undone.')) {
              // Handle account deletion
              console.log('Account deletion requested');
            }
          }}
          className="px-6 py-3 bg-red-600 text-white rounded-full hover:bg-red-700 transition-all font-medium"
        >
          Delete Account
        </button>
      </div>
    </div>
  );
}
