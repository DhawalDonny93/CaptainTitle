import { useState, useEffect } from 'react';
import { useProject } from '../contexts/ProjectContext';
import { brandProfileService, BrandProfile as BrandProfileType } from '../services/brandProfileService';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Upload, X } from 'lucide-react';
import { toast } from '../utils/toast';
import BrandSidebar from '../components/BrandSidebar';

const AVAILABLE_TOPICS = [
  'Health', 'Entrepreneurship', 'Marketing', 'Sales', 'Education',
  'Finance', 'Technology', 'Wellness', 'Religion', 'Law',
  'Real Estate', 'Travel', 'Food', 'Fashion', 'Sports'
];

const MAX_TOPICS = 5;

const BrandProfile = () => {
  const { selectedProject } = useProject();
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [profile, setProfile] = useState<BrandProfileType>({
    projectId: selectedProject?.id || 0,
    overview: '',
    description: '',
    topics: [],
    logoUrl: undefined
  });
  const [logoFile, setLogoFile] = useState<File | null>(null);
  const [logoPreview, setLogoPreview] = useState<string | null>(null);

  useEffect(() => {
    if (selectedProject?.id) {
      loadProfile();
    }
  }, [selectedProject?.id]);

  const loadProfile = async () => {
    if (!selectedProject?.id) return;
    
    setLoading(true);
    try {
      const data = await brandProfileService.getBrandProfile(selectedProject.id);
      setProfile(data);
      if (data.logoUrl) {
        setLogoPreview(data.logoUrl);
      }
    } catch (err: any) {
      console.error('Error loading brand profile:', err);
      toast.error('Failed to load brand profile');
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    if (!selectedProject?.id) return;

    setSaving(true);
    try {
      // Upload logo first if changed
      if (logoFile) {
        const { logoUrl } = await brandProfileService.uploadLogo(selectedProject.id, logoFile);
        profile.logoUrl = logoUrl;
      }

      // Save profile data
      await brandProfileService.saveBrandProfile(selectedProject.id, {
        overview: profile.overview,
        description: profile.description,
        topics: profile.topics
      });

      toast.success('Brand profile saved successfully');
      await loadProfile();
    } catch (err: any) {
      console.error('Error saving brand profile:', err);
      toast.error(err.response?.data?.message || 'Failed to save brand profile');
    } finally {
      setSaving(false);
    }
  };

  const handleLogoChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      if (file.size > 5 * 1024 * 1024) {
        toast.error('File size must be less than 5MB');
        return;
      }
      setLogoFile(file);
      const reader = new FileReader();
      reader.onloadend = () => {
        setLogoPreview(reader.result as string);
      };
      reader.readAsDataURL(file);
    }
  };

  const toggleTopic = (topic: string) => {
    const currentTopics = profile.topics || [];
    if (currentTopics.includes(topic)) {
      setProfile({ ...profile, topics: currentTopics.filter(t => t !== topic) });
    } else {
      if (currentTopics.length >= MAX_TOPICS) {
        toast.error(`You can select up to ${MAX_TOPICS} topics`);
        return;
      }
      setProfile({ ...profile, topics: [...currentTopics, topic] });
    }
  };

  if (!selectedProject) {
    return (
      <div className="flex h-screen bg-gradient-to-br from-gray-50 via-blue-50/20 to-gray-50">
        <BrandSidebar />
        <div className="flex-1 flex items-center justify-center">
          <div className="text-center">
            <h2 className="text-xl font-bold text-gray-900 mb-2">No Project Selected</h2>
            <p className="text-gray-600">Please select a project to manage its brand profile.</p>
          </div>
        </div>
      </div>
    );
  }

  if (loading) {
    return (
      <div className="flex h-screen bg-gradient-to-br from-gray-50 via-blue-50/20 to-gray-50">
        <BrandSidebar />
        <div className="flex-1 flex items-center justify-center">
          <div className="text-gray-600">Loading brand profile...</div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex h-screen bg-gradient-to-br from-gray-50 via-blue-50/20 to-gray-50">
      <BrandSidebar />
      <div className="flex-1 overflow-y-auto">
        <div className="max-w-4xl mx-auto px-6 py-8">
        <div className="mb-6">
          <h1 className="text-2xl font-bold text-gray-900 mb-1">Profile</h1>
          <p className="text-sm text-gray-600">Manage your brand's public identity and core topics.</p>
        </div>

        <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6 space-y-6">
          {/* Logo Upload */}
          <div>
            <label className="block text-sm font-semibold text-gray-900 mb-3">Brand Logo</label>
            <div className="flex items-center gap-4">
              {logoPreview ? (
                <div className="relative">
                  <img src={logoPreview} alt="Logo" className="w-24 h-24 rounded-lg object-cover border border-gray-200" />
                  <button
                    onClick={() => {
                      setLogoFile(null);
                      setLogoPreview(null);
                    }}
                    className="absolute -top-2 -right-2 w-6 h-6 bg-red-500 text-white rounded-full flex items-center justify-center hover:bg-red-600"
                  >
                    <X className="w-4 h-4" />
                  </button>
                </div>
              ) : (
                <div className="w-24 h-24 rounded-lg border-2 border-dashed border-gray-300 flex items-center justify-center">
                  <Upload className="w-8 h-8 text-gray-400" />
                </div>
              )}
              <div>
                <label className="cursor-pointer">
                  <input
                    type="file"
                    accept="image/png,image/jpeg,image/jpg,image/svg+xml,image/webp"
                    onChange={handleLogoChange}
                    className="hidden"
                  />
                  <span className="inline-block px-4 py-2 bg-gray-100 hover:bg-gray-200 text-gray-700 rounded-lg text-sm font-medium transition-colors">
                    Choose File
                  </span>
                </label>
                <p className="text-xs text-gray-500 mt-1">PNG, JPG, SVG or WebP (max 5MB)</p>
              </div>
            </div>
          </div>

          {/* Overview */}
          <div>
            <label className="block text-sm font-semibold text-gray-900 mb-2">
              Overview <span className="text-gray-400 font-normal">({profile.overview?.length || 0}/120)</span>
            </label>
            <Input
              value={profile.overview || ''}
              onChange={(e) => setProfile({ ...profile, overview: e.target.value.slice(0, 120) })}
              placeholder="e.g. AI-Powered Marketing Platform for B2B Companies"
              className="w-full"
            />
            <p className="text-xs text-gray-500 mt-1">A short professional tagline displayed on your profile.</p>
          </div>

          {/* Description */}
          <div>
            <label className="block text-sm font-semibold text-gray-900 mb-2">
              Description <span className="text-gray-400 font-normal">({profile.description?.length || 0}/1500)</span>
            </label>
            <textarea
              value={profile.description || ''}
              onChange={(e) => setProfile({ ...profile, description: e.target.value.slice(0, 1500) })}
              placeholder="Describe your brand, mission, and what makes you unique..."
              rows={8}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-none"
            />
            <p className="text-xs text-gray-500 mt-1">This detailed bio helps the AI understand your background and speaks to your audience.</p>
          </div>

          {/* Topics */}
          <div>
            <label className="block text-sm font-semibold text-gray-900 mb-2">
              Topics <span className="text-gray-400 font-normal">({profile.topics?.length || 0}/{MAX_TOPICS} selected)</span>
            </label>
            <p className="text-xs text-gray-500 mb-3">Select up to {MAX_TOPICS} topics that define your expertise.</p>
            <div className="flex flex-wrap gap-2">
              {AVAILABLE_TOPICS.map((topic) => {
                const isSelected = profile.topics?.includes(topic);
                return (
                  <button
                    key={topic}
                    onClick={() => toggleTopic(topic)}
                    className={`px-4 py-2 rounded-full text-sm font-medium transition-colors ${
                      isSelected
                        ? 'bg-orange-500 text-white hover:bg-orange-600'
                        : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                    }`}
                  >
                    {topic}
                    {isSelected && <X className="w-3 h-3 inline ml-1" />}
                  </button>
                );
              })}
            </div>
          </div>

          {/* Save Button */}
          <div className="flex justify-end pt-4 border-t border-gray-200">
            <Button
              onClick={handleSave}
              disabled={saving}
              className="bg-gray-900 hover:bg-gray-800 text-white px-6"
            >
              {saving ? 'Saving...' : 'Save Profile'}
            </Button>
          </div>
        </div>
        </div>
      </div>
    </div>
  );
};

export default BrandProfile;
