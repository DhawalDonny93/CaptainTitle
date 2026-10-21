import { useState, useEffect } from 'react';
import { useProject } from '../contexts/ProjectContext';
import { brandProfileService, Competitor } from '../services/brandProfileService';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Plus, X, AlertCircle } from 'lucide-react';
import { toast } from '../utils/toast';
import BrandSidebar from '../components/BrandSidebar';

const Competitors = () => {
  const { selectedProject } = useProject();
  const [loading, setLoading] = useState(false);
  const [competitors, setCompetitors] = useState<Competitor[]>([]);
  const [newCompetitorUrl, setNewCompetitorUrl] = useState('');
  const [adding, setAdding] = useState(false);

  useEffect(() => {
    if (selectedProject?.id) {
      loadCompetitors();
    }
  }, [selectedProject?.id]);

  const loadCompetitors = async () => {
    if (!selectedProject?.id) return;
    
    setLoading(true);
    try {
      const data = await brandProfileService.getCompetitors(selectedProject.id);
      setCompetitors(data);
    } catch (err: any) {
      console.error('Error loading competitors:', err);
      toast.error('Failed to load competitors');
    } finally {
      setLoading(false);
    }
  };

  const handleAddCompetitor = async () => {
    if (!selectedProject?.id || !newCompetitorUrl.trim()) {
      toast.error('Please enter a competitor URL');
      return;
    }

    setAdding(true);
    try {
      const newCompetitor = await brandProfileService.addCompetitor(selectedProject.id, newCompetitorUrl);
      setCompetitors([...competitors, newCompetitor]);
      setNewCompetitorUrl('');
      toast.success(`Added ${newCompetitor.name}`);
    } catch (err: any) {
      console.error('Error adding competitor:', err);
      toast.error(err.response?.data?.message || 'Failed to add competitor');
    } finally {
      setAdding(false);
    }
  };

  const handleDeleteCompetitor = async (competitorId: number, name: string) => {
    if (!confirm(`Remove ${name} from competitors?`)) return;

    try {
      await brandProfileService.deleteCompetitor(competitorId);
      setCompetitors(competitors.filter(c => c.id !== competitorId));
      toast.success(`Removed ${name}`);
    } catch (err: any) {
      console.error('Error deleting competitor:', err);
      toast.error('Failed to remove competitor');
    }
  };

  if (!selectedProject) {
    return (
      <div className="flex h-screen bg-gradient-to-br from-gray-50 via-blue-50/20 to-gray-50">
        <BrandSidebar />
        <div className="flex-1 flex items-center justify-center">
          <div className="text-center">
            <AlertCircle className="h-12 w-12 text-gray-400 mx-auto mb-4" />
            <h2 className="text-xl font-bold text-gray-900 mb-2">No Project Selected</h2>
            <p className="text-gray-600">Please select a project to manage competitors.</p>
          </div>
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
          <h1 className="text-2xl font-bold text-gray-900 mb-1">Competitors</h1>
          <p className="text-sm text-gray-600">Track competitors your brand is compared against across content and reports.</p>
        </div>

        {/* Add Competitor Input */}
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6 mb-6">
          <label className="block text-sm font-semibold text-gray-900 mb-3">
            Enter competitors URL (comma or newline separated)
          </label>
          <div className="flex gap-3">
            <Input
              value={newCompetitorUrl}
              onChange={(e) => setNewCompetitorUrl(e.target.value)}
              placeholder="e.g., competitor.com, example.com"
              className="flex-1"
              onKeyPress={(e) => {
                if (e.key === 'Enter') {
                  handleAddCompetitor();
                }
              }}
            />
            <Button
              onClick={handleAddCompetitor}
              disabled={adding || !newCompetitorUrl.trim()}
              className="bg-orange-500 hover:bg-orange-600 text-white px-6 whitespace-nowrap"
            >
              <Plus className="w-4 h-4 mr-2" />
              Add
            </Button>
          </div>
        </div>

        {/* Competitors List */}
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
          {loading ? (
            <div className="text-center py-12 text-gray-500">Loading competitors...</div>
          ) : competitors.length === 0 ? (
            <div className="text-center py-12">
              <div className="text-gray-400 mb-2">No competitors added yet</div>
              <p className="text-sm text-gray-500">Add competitor URLs above to start tracking them</p>
            </div>
          ) : (
            <div className="space-y-3">
              {competitors.map((competitor) => (
                <div
                  key={competitor.id}
                  className="flex items-center gap-4 p-4 rounded-lg border border-gray-200 hover:border-gray-300 transition-colors"
                >
                  {/* Logo */}
                  <div className="w-12 h-12 rounded-lg bg-gray-100 flex items-center justify-center flex-shrink-0 overflow-hidden">
                    {competitor.logoUrl ? (
                      <img
                        src={competitor.logoUrl}
                        alt={competitor.name}
                        className="w-full h-full object-contain"
                        onError={(e) => {
                          e.currentTarget.style.display = 'none';
                          e.currentTarget.parentElement!.innerHTML = `<div class="text-gray-400 font-semibold text-lg">${competitor.name.charAt(0).toUpperCase()}</div>`;
                        }}
                      />
                    ) : (
                      <div className="text-gray-400 font-semibold text-lg">
                        {competitor.name.charAt(0).toUpperCase()}
                      </div>
                    )}
                  </div>

                  {/* Info */}
                  <div className="flex-1 min-w-0">
                    <div className="font-semibold text-gray-900">{competitor.name}</div>
                    <div className="text-sm text-gray-500 truncate">{competitor.domain}</div>
                  </div>

                  {/* Delete Button */}
                  <button
                    onClick={() => handleDeleteCompetitor(competitor.id, competitor.name)}
                    className="w-8 h-8 rounded-lg hover:bg-red-50 text-gray-400 hover:text-red-600 flex items-center justify-center transition-colors"
                    title="Remove competitor"
                  >
                    <X className="w-5 h-5" />
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>
        </div>
      </div>
    </div>
  );
};

export default Competitors;
