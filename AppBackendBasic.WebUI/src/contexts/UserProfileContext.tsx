import React, { createContext, useContext, useState } from 'react';

interface SiteAuditStatus {
  [projectId: string]: boolean;
}

interface ExtendedProperties {
  siteAuditStatus?: SiteAuditStatus;
  [key: string]: any;
}

interface UserProfile {
  language: string;
  theme: string;
  timezone: string;
  extendedProperties: string;
}

interface UserProfileContextType {
  profile: UserProfile | null;
  setProfile: (profile: UserProfile | null) => void;
  hasSiteAudit: (projectId: number) => boolean;
  updateSiteAuditStatus: (projectId: number, hasAudit: boolean) => void;
  clearProfile: () => void;
}

const UserProfileContext = createContext<UserProfileContextType | undefined>(undefined);

const STORAGE_KEY = 'userProfile';

export const UserProfileProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [profile, setProfileState] = useState<UserProfile | null>(() => {
    // Load from sessionStorage on init
    const stored = sessionStorage.getItem(STORAGE_KEY);
    if (stored) {
      try {
        return JSON.parse(stored);
      } catch {
        return null;
      }
    }
    return null;
  });

  const setProfile = (newProfile: UserProfile | null) => {
    setProfileState(newProfile);
    if (newProfile) {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(newProfile));
    } else {
      sessionStorage.removeItem(STORAGE_KEY);
    }
  };

  const hasSiteAudit = (projectId: number): boolean => {
    if (!profile?.extendedProperties) return false;
    
    try {
      const extended: ExtendedProperties = JSON.parse(profile.extendedProperties);
      return extended.siteAuditStatus?.[projectId.toString()] ?? false;
    } catch {
      return false;
    }
  };

  const updateSiteAuditStatus = (projectId: number, hasAudit: boolean) => {
    if (!profile) return;

    try {
      const extended: ExtendedProperties = profile.extendedProperties 
        ? JSON.parse(profile.extendedProperties) 
        : {};
      
      if (!extended.siteAuditStatus) {
        extended.siteAuditStatus = {};
      }
      
      extended.siteAuditStatus[projectId.toString()] = hasAudit;
      
      const updatedProfile = {
        ...profile,
        extendedProperties: JSON.stringify(extended)
      };
      
      setProfile(updatedProfile);
    } catch (error) {
      console.error('Error updating site audit status:', error);
    }
  };

  const clearProfile = () => {
    setProfile(null);
  };

  // Clear profile when user logs out (when accessToken is removed)
  React.useEffect(() => {
    const handleStorageChange = () => {
      const token = localStorage.getItem('accessToken');
      if (!token && profile) {
        // User logged out, clear profile
        clearProfile();
      }
    };

    // Listen for storage changes (logout in another tab)
    window.addEventListener('storage', handleStorageChange);
    
    // Also check periodically in case logout happens in same tab
    const interval = setInterval(() => {
      const token = localStorage.getItem('accessToken');
      if (!token && profile) {
        clearProfile();
      }
    }, 1000);

    return () => {
      window.removeEventListener('storage', handleStorageChange);
      clearInterval(interval);
    };
  }, [profile]);

  return (
    <UserProfileContext.Provider 
      value={{ 
        profile, 
        setProfile, 
        hasSiteAudit, 
        updateSiteAuditStatus,
        clearProfile 
      }}
    >
      {children}
    </UserProfileContext.Provider>
  );
};

export const useUserProfile = () => {
  const context = useContext(UserProfileContext);
  if (context === undefined) {
    throw new Error('useUserProfile must be used within a UserProfileProvider');
  }
  return context;
};
