import { useState, useEffect, useRef } from 'react';
import { useProject } from '../contexts/ProjectContext';
import { useSubscription } from '../contexts/SubscriptionContext';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { promptService, CreatePromptRequest } from '../services/promptService';
import { 
  Search, 
  Plus,
  Globe,
  MoreVertical,
  ChevronDown,
  FileText,
  Trash2,
  Edit,
  Copy,
  CheckCircle,
  Circle,
  Loader2,
  AlertCircle
} from 'lucide-react';
import { toast } from '../utils/toast';

interface Prompt {
  id: number;
  text: string;
  location: string;
  topic: string;
  status: 'Pending' | 'Scheduled' | 'Active' | 'Paused' | 'Completed';
  volume: number;
  citationCount?: number;
  platforms: string[];
  averageVisibilityScore?: number;
  lastCheckedAt?: string;
}

const YourPrompts = () => {
  const { selectedProject } = useProject();
  const { canCreatePrompt, promptLimit, openUpgradeModal, currentPlan } = useSubscription();
  
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedStatus, setSelectedStatus] = useState<'all' | 'Pending' | 'Active' | 'Paused' | 'Completed'>('all');
  const [selectedIsCompetitor, setSelectedIsCompetitor] = useState<'all' | 'own' | 'competitor'>('all');
  const [selectedPrompts, setSelectedPrompts] = useState<number[]>([]);
  const [showCreateMenu, setShowCreateMenu] = useState(false);
  const [showActionsMenu, setShowActionsMenu] = useState<number | null>(null);
  const [showManualModal, setShowManualModal] = useState(false);
  const [showAIModal, setShowAIModal] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [editingPrompt, setEditingPrompt] = useState<Prompt | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [prompts, setPrompts] = useState<Prompt[]>([]);
  const [submitting, setSubmitting] = useState(false);
  
  // Form states
  const [manualForm, setManualForm] = useState({ promptText: '', topic: '', platforms: [] as string[], isCompetitor: false });
  const [aiForm, setAIForm] = useState({ topic: '', count: 5, platforms: [] as string[], isCompetitor: false });
  const fileInputRef = useRef<HTMLInputElement>(null);
  
  const createMenuRef = useRef<HTMLDivElement>(null);
  const actionsMenuRef = useRef<HTMLDivElement>(null);

  // Fetch prompts from API
  useEffect(() => {
    const fetchPrompts = async () => {
      if (!selectedProject?.id) return;
      
      setLoading(true);
      setError(null);
      try {
        const statusFilter = selectedStatus === 'all' ? undefined : selectedStatus;
        const isCompetitorFilter = selectedIsCompetitor === 'all' ? undefined : selectedIsCompetitor === 'competitor';
        const data = await promptService.getPrompts(selectedProject.id, statusFilter, undefined, undefined, isCompetitorFilter);
        
        // Transform API data to match UI format
        const transformedPrompts: Prompt[] = data.map(p => ({
          id: p.id,
          text: p.promptText,
          location: 'Worldwide',
          topic: p.topic || 'Uncategorized',
          status: (p.status || 'Pending') as 'Pending' | 'Scheduled' | 'Active' | 'Paused' | 'Completed',
          volume: Math.round(p.averageVisibilityScore || 0),
          platforms: p.platformTrackings?.map(pt => pt.platform) || [],
          averageVisibilityScore: p.averageVisibilityScore,
          lastCheckedAt: p.lastCheckedAt
        }));
        
        setPrompts(transformedPrompts);
      } catch (err: any) {
        console.error('Error fetching prompts:', err);
        setError(err.message || 'Failed to load prompts');
        toast.error('Failed to load prompts');
      } finally {
        setLoading(false);
      }
    };

    fetchPrompts();
  }, [selectedProject, selectedStatus, selectedIsCompetitor]);

  // Close dropdowns when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (createMenuRef.current && !createMenuRef.current.contains(event.target as Node)) {
        setShowCreateMenu(false);
      }
      if (actionsMenuRef.current && !actionsMenuRef.current.contains(event.target as Node)) {
        setShowActionsMenu(null);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const filteredPrompts = prompts.filter(prompt => {
    const matchesSearch = prompt.text.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesStatus = selectedStatus === 'all' || prompt.status === selectedStatus;
    return matchesSearch && matchesStatus;
  });

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="w-8 h-8 animate-spin text-blue-600" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="text-center p-10">
        <AlertCircle className="h-12 w-12 text-red-600 mx-auto mb-4" />
        <h2 className="text-xl font-bold text-gray-900 mb-2">Failed to Load Prompts</h2>
        <p className="text-gray-600 mb-4">{error}</p>
        <Button onClick={() => window.location.reload()}>Try Again</Button>
      </div>
    );
  }

  if (!selectedProject) {
    return (
      <div className="text-center p-10">
        <AlertCircle className="h-12 w-12 text-gray-400 mx-auto mb-4" />
        <h2 className="text-xl font-bold text-gray-900 mb-2">No Project Selected</h2>
        <p className="text-gray-600">Please select a project to view prompts.</p>
      </div>
    );
  }

  const activeCount = prompts.filter(p => p.status === 'Active').length;
  const totalVolume = prompts.reduce((sum, p) => sum + p.volume, 0);
  const atPromptLimit = !canCreatePrompt(prompts.length);
  const promptLimitLabel = promptLimit === -1 ? 'Unlimited' : promptLimit;

  const handleAddPromptClick = (mode: 'manual' | 'ai') => {
    if (atPromptLimit) {
      openUpgradeModal(
        `You've reached the ${promptLimitLabel}-prompt limit per project on your ${currentPlan?.displayName ?? 'current'} plan. Upgrade to track more prompts.`
      );
      return;
    }
    if (mode === 'manual') setShowManualModal(true);
    else setShowAIModal(true);
    setShowCreateMenu(false);
  };

  const togglePromptSelection = (id: number) => {
    setSelectedPrompts(prev => 
      prev.includes(id) ? prev.filter(p => p !== id) : [...prev, id]
    );
  };

  const toggleAllPrompts = () => {
    if (selectedPrompts.length === filteredPrompts.length) {
      setSelectedPrompts([]);
    } else {
      setSelectedPrompts(filteredPrompts.map(p => p.id));
    }
  };

  const getVolumeBar = (volume: number) => {
    const maxVolume = 5;
    const percentage = (volume / maxVolume) * 100;
    return (
      <div className="flex items-center gap-2">
        <div className="flex-1 h-2 bg-gray-200 rounded-full overflow-hidden max-w-[100px]">
          <div 
            className="h-full bg-gradient-to-r from-green-500 to-emerald-500 transition-all"
            style={{ width: `${percentage}%` }}
          />
        </div>
        <span className="text-xs font-medium text-gray-600 min-w-[20px]">{volume}</span>
      </div>
    );
  };

  const handleDeleteSelected = async () => {
    try {
      await promptService.bulkAction({
        action: 'delete',
        promptIds: selectedPrompts
      });
      setPrompts(prompts.filter(p => !selectedPrompts.includes(p.id)));
      setSelectedPrompts([]);
    } catch (err: any) {
      console.error('Error deleting prompts:', err);
      toast.error(err.message || 'Failed to delete prompts');
    }
  };

  const handleDuplicateSelected = () => {
    const newPrompts = prompts.filter(p => selectedPrompts.includes(p.id)).map(p => ({
      ...p,
      id: Math.max(...prompts.map(pr => pr.id)) + 1 + Math.random()
    }));
    setPrompts([...prompts, ...newPrompts]);
    setSelectedPrompts([]);
  };

  const handleToggleStatus = async (id: number) => {
    const prompt = prompts.find(p => p.id === id);
    if (!prompt) return;
    
    const newStatus = prompt.status === 'Active' ? 'Paused' : 'Active';
    
    try {
      await promptService.updatePrompt(id, {
        promptText: prompt.text,
        topic: prompt.topic,
        status: newStatus
      });
      
      setPrompts(prompts.map(p => 
        p.id === id ? { ...p, status: newStatus as 'Pending' | 'Scheduled' | 'Active' | 'Paused' | 'Completed' } : p
      ));
      toast.success('Prompt status updated');
    } catch (err: any) {
      console.error('Error updating prompt status:', err);
      toast.error(err.message || 'Failed to update prompt status');
    }
  };

  const handleDeletePrompt = async (id: number) => {
    try {
      await promptService.deletePrompt(id);
      setPrompts(prompts.filter(p => p.id !== id));
      setShowActionsMenu(null);
      toast.success('Prompt deleted');
    } catch (err: any) {
      console.error('Error deleting prompt:', err);
      toast.error(err.message || 'Failed to delete prompt');
    }
  };

  const handleCreateManual = async () => {
    if (!selectedProject?.id || !manualForm.promptText.trim()) {
      toast.error('Please enter a prompt text');
      return;
    }
    if (atPromptLimit) {
      openUpgradeModal(`You've reached the ${promptLimitLabel}-prompt limit on your ${currentPlan?.displayName ?? 'current'} plan.`);
      return;
    }

    setSubmitting(true);
    try {
      const request: CreatePromptRequest = {
        projectId: selectedProject.id,
        promptText: manualForm.promptText,
        topic: manualForm.topic || undefined,
        platforms: manualForm.platforms.length > 0 ? manualForm.platforms : undefined,
        isCompetitor: manualForm.isCompetitor
      };
      
      const newPrompt = await promptService.createPrompt(request);
      
      const transformedPrompt: Prompt = {
        id: newPrompt.id,
        text: newPrompt.promptText,
        location: 'Worldwide',
        topic: newPrompt.topic || 'Uncategorized',
        status: (newPrompt.status || 'Pending') as 'Pending' | 'Scheduled' | 'Active' | 'Paused' | 'Completed',
        volume: Math.round(newPrompt.averageVisibilityScore || 0),
        platforms: newPrompt.platformTrackings?.map(pt => pt.platform) || [],
        averageVisibilityScore: newPrompt.averageVisibilityScore,
        lastCheckedAt: newPrompt.lastCheckedAt
      };
      
      setPrompts([transformedPrompt, ...prompts]);
      setShowManualModal(false);
      setManualForm({ promptText: '', topic: '', platforms: [], isCompetitor: false });
      toast.success('Prompt created successfully');
    } catch (err: any) {
      console.error('Error creating prompt:', err);
      if (err?.response?.status === 402 || err?.response?.data?.code === 'QUOTA_EXCEEDED') {
        openUpgradeModal('You have reached your prompt limit. Please upgrade your plan.');
      } else {
        toast.error(err.message || 'Failed to create prompt');
      }
    } finally {
      setSubmitting(false);
    }
  };

  const handleCreateWithAI = async () => {
    if (!selectedProject?.id || !aiForm.topic.trim()) {
      toast.error('Please enter a topic');
      return;
    }

    setSubmitting(true);
    try {
      // Generate multiple prompts based on topic
      const requests: CreatePromptRequest[] = [];
      for (let i = 0; i < aiForm.count; i++) {
        requests.push({
          projectId: selectedProject.id,
          promptText: `AI-generated prompt about ${aiForm.topic} (${i + 1})`,
          topic: aiForm.topic,
          platforms: aiForm.platforms.length > 0 ? aiForm.platforms : undefined,
          isCompetitor: aiForm.isCompetitor
        });
      }
      
      const newPrompts = await Promise.all(
        requests.map(req => promptService.createPrompt(req))
      );
      
      // Transform and add to list
      const transformedPrompts: Prompt[] = newPrompts.map(p => ({
        id: p.id,
        text: p.promptText,
        location: 'Worldwide',
        topic: p.topic || 'Uncategorized',
        status: (p.status || 'Pending') as 'Pending' | 'Scheduled' | 'Active' | 'Paused' | 'Completed',
        volume: Math.round(p.averageVisibilityScore || 0),
        platforms: p.platformTrackings?.map(pt => pt.platform) || [],
        averageVisibilityScore: p.averageVisibilityScore,
        lastCheckedAt: p.lastCheckedAt
      }));
      
      setPrompts([...transformedPrompts, ...prompts]);
      setShowAIModal(false);
      setAIForm({ topic: '', count: 5, platforms: [], isCompetitor: false });
      toast.success(`${newPrompts.length} prompts created successfully`);
    } catch (err: any) {
      console.error('Error creating prompts with AI:', err);
      toast.error(err.message || 'Failed to create prompts');
    } finally {
      setSubmitting(false);
    }
  };

  const handleImportCSV = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file || !selectedProject?.id) return;

    setSubmitting(true);
    try {
      const text = await file.text();
      const lines = text.split('\n').filter(line => line.trim());
      
      // Skip header if exists
      const startIndex = lines[0].toLowerCase().includes('prompt') ? 1 : 0;
      const requests: CreatePromptRequest[] = [];
      
      for (let i = startIndex; i < lines.length; i++) {
        const parts = lines[i].split(',').map(p => p.trim());
        if (parts[0]) {
          requests.push({
            projectId: selectedProject.id,
            promptText: parts[0],
            topic: parts[1] || undefined,
            platforms: parts[2] ? parts[2].split(';').map(p => p.trim()) : undefined
          });
        }
      }
      
      const newPrompts = await Promise.all(
        requests.map(req => promptService.createPrompt(req))
      );
      
      // Transform and add to list
      const transformedPrompts: Prompt[] = newPrompts.map(p => ({
        id: p.id,
        text: p.promptText,
        location: 'Worldwide',
        topic: p.topic || 'Uncategorized',
        status: (p.status || 'Pending') as 'Pending' | 'Scheduled' | 'Active' | 'Paused' | 'Completed',
        volume: Math.round(p.averageVisibilityScore || 0),
        platforms: p.platformTrackings?.map(pt => pt.platform) || [],
        averageVisibilityScore: p.averageVisibilityScore,
        lastCheckedAt: p.lastCheckedAt
      }));
      
      setPrompts([...transformedPrompts, ...prompts]);
      toast.success(`${newPrompts.length} prompts imported successfully`);
    } catch (err: any) {
      console.error('Error importing CSV:', err);
      toast.error(err.message || 'Failed to import prompts');
    } finally {
      setSubmitting(false);
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  };

  const handleEditPrompt = async () => {
    if (!editingPrompt) return;

    setSubmitting(true);
    try {
      await promptService.updatePrompt(editingPrompt.id, {
        promptText: editingPrompt.text,
        topic: editingPrompt.topic,
        status: editingPrompt.status
      });
      
      setPrompts(prompts.map(p => p.id === editingPrompt.id ? editingPrompt : p));
      setShowEditModal(false);
      setEditingPrompt(null);
      toast.success('Prompt updated successfully');
    } catch (err: any) {
      console.error('Error updating prompt:', err);
      toast.error(err.message || 'Failed to update prompt');
    } finally {
      setSubmitting(false);
    }
  };

  const openEditModal = (prompt: Prompt) => {
    setEditingPrompt(prompt);
    setShowEditModal(true);
    setShowActionsMenu(null);
  };

  const handleBulkEdit = () => {
    if (selectedPrompts.length === 0) return;
    const firstPrompt = prompts.find(p => p.id === selectedPrompts[0]);
    if (firstPrompt) {
      setEditingPrompt(firstPrompt);
      setShowEditModal(true);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-50 via-blue-50/20 to-gray-50">
      <div className="max-w-7xl mx-auto px-6 py-6">
        {/* Header */}
        <div className="mb-6">
          <div className="flex items-center justify-between mb-4">
            <div>
              <h1 className="text-2xl font-bold text-gray-900 mb-1">Your Prompts</h1>
              <p className="text-sm text-gray-600 flex items-center gap-2">
                Manage and track your prompts across AI platforms
                {promptLimit !== -1 && (
                  <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${
                    atPromptLimit
                      ? 'bg-red-100 text-red-700'
                      : 'bg-gray-100 text-gray-600'
                  }`}>
                    {prompts.length} / {promptLimitLabel} prompts
                  </span>
                )}
              </p>
            </div>
            <div className="flex items-center gap-2">
              <div className="relative" ref={createMenuRef}>
                <Button
                  onClick={() => atPromptLimit ? handleAddPromptClick('manual') : setShowCreateMenu(!showCreateMenu)}
                  className={atPromptLimit
                    ? 'bg-amber-500 hover:bg-amber-600 text-white shadow-lg shadow-amber-500/30'
                    : 'bg-gray-900 hover:bg-gray-800 text-white shadow-lg shadow-gray-900/30'
                  }
                >
                  {atPromptLimit
                    ? <><svg className="w-4 h-4 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" /></svg>Upgrade to Add More</>
                    : <><Plus className="w-4 h-4 mr-2" />Create prompts<ChevronDown className="w-4 h-4 ml-2" /></>
                  }
                </Button>
                
                {showCreateMenu && (
                  <div className="absolute right-0 top-full mt-2 w-56 bg-white rounded-xl border border-gray-200 shadow-xl z-50 overflow-hidden">
                    <button 
                      onClick={() => handleAddPromptClick('manual')}
                      className="w-full px-4 py-3 text-left hover:bg-gray-50 transition-colors flex items-start gap-3"
                    >
                      <div className="w-8 h-8 rounded-lg bg-gray-900 flex items-center justify-center flex-shrink-0">
                        <Plus className="w-4 h-4 text-white" />
                      </div>
                      <div>
                        <div className="text-sm font-semibold text-gray-900">Create manually</div>
                        <div className="text-xs text-gray-500">Add one at a time</div>
                      </div>
                    </button>
                    <button 
                      onClick={() => handleAddPromptClick('ai')}
                      className="w-full px-4 py-3 text-left hover:bg-gray-50 transition-colors flex items-start gap-3"
                    >
                      <div className="w-8 h-8 rounded-lg bg-purple-100 flex items-center justify-center flex-shrink-0">
                        <FileText className="w-4 h-4 text-purple-600" />
                      </div>
                      <div>
                        <div className="text-sm font-semibold text-gray-900">Create with AI</div>
                        <div className="text-xs text-gray-500">Generate automatically</div>
                      </div>
                    </button>
                    <button 
                      onClick={() => {
                        fileInputRef.current?.click();
                        setShowCreateMenu(false);
                      }}
                      className="w-full px-4 py-3 text-left hover:bg-gray-50 transition-colors flex items-start gap-3"
                    >
                      <div className="w-8 h-8 rounded-lg bg-blue-100 flex items-center justify-center flex-shrink-0">
                        <FileText className="w-4 h-4 text-blue-600" />
                      </div>
                      <div>
                        <div className="text-sm font-semibold text-gray-900">Import from CSV</div>
                        <div className="text-xs text-gray-500">Bulk upload</div>
                      </div>
                    </button>
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* Stats Bar */}
          <div className="bg-white rounded-xl border border-gray-200/50 shadow-lg shadow-gray-200/50 p-4 mb-4">
            <div className="flex items-center gap-6">
              <div>
                <div className="text-2xl font-bold text-gray-900">{prompts.length}</div>
                <div className="text-xs text-gray-500">Total Prompts</div>
              </div>
              <div className="h-10 w-px bg-gray-200" />
              <div>
                <div className="text-2xl font-bold text-green-600">{activeCount}</div>
                <div className="text-xs text-gray-500">Active</div>
              </div>
              <div className="h-10 w-px bg-gray-200" />
              <div>
                <div className="text-2xl font-bold text-gray-900">{totalVolume}</div>
                <div className="text-xs text-gray-500">Total Volume</div>
              </div>
            </div>
          </div>

          {/* Filters */}
          <div className="bg-white rounded-xl border border-gray-200/50 shadow-lg shadow-gray-200/50 p-4">
            <div className="flex items-center gap-3 flex-wrap">
              {/* Search */}
              <div className="flex-1 min-w-[300px]">
                <div className="relative">
                  <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
                  <Input
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    placeholder="Search prompts..."
                    className="pl-10 h-9 text-sm border-2 border-gray-200 focus:border-gray-900 focus:ring-2 focus:ring-gray-900/20 rounded-lg"
                  />
                </div>
              </div>

              {/* Status Filter */}
              <select
                value={selectedStatus}
                onChange={(e) => setSelectedStatus(e.target.value as any)}
                className="px-3 py-2 text-xs font-medium border-2 border-gray-200 rounded-lg hover:border-gray-300 focus:border-gray-900 focus:ring-2 focus:ring-gray-900/20 bg-white"
              >
                <option value="all">All Status</option>
                <option value="Pending">Pending</option>
                <option value="Active">Active</option>
                <option value="Paused">Paused</option>
                <option value="Completed">Completed</option>
              </select>

              {/* Brand Ownership Filter */}
              <select
                value={selectedIsCompetitor}
                onChange={(e) => setSelectedIsCompetitor(e.target.value as any)}
                className="px-3 py-2 text-xs font-medium border-2 border-gray-200 rounded-lg hover:border-gray-300 focus:border-gray-900 focus:ring-2 focus:ring-gray-900/20 bg-white"
              >
                <option value="all">All Brands</option>
                <option value="own">Own Brand</option>
                <option value="competitor">Competitor</option>
              </select>

              {/* All Tracked Filter */}
              <button className="px-3 py-2 text-xs font-medium border-2 border-gray-200 rounded-lg hover:border-gray-300 bg-white flex items-center gap-2">
                All tracked
                <ChevronDown className="w-3.5 h-3.5" />
              </button>
            </div>
          </div>
        </div>

        {/* Prompts Table */}
        <div className="bg-white rounded-xl border border-gray-200/50 shadow-lg shadow-gray-200/50 overflow-visible">
          {/* Table Header */}
          <div className="bg-gray-50 border-b border-gray-200 px-6 py-3">
            <div className="flex items-center gap-4">
              <button
                onClick={toggleAllPrompts}
                className="w-5 h-5 rounded border-2 border-gray-300 flex items-center justify-center hover:border-gray-900 transition-colors"
              >
                {selectedPrompts.length === filteredPrompts.length && filteredPrompts.length > 0 && (
                  <CheckCircle className="w-4 h-4 text-gray-900" />
                )}
              </button>
              <div className="flex-1 grid grid-cols-12 gap-4 text-xs font-semibold text-gray-600">
                <div className="col-span-5">Prompts ({filteredPrompts.length})</div>
                <div className="col-span-2">Locations</div>
                <div className="col-span-2">Topic</div>
                <div className="col-span-2">Volume</div>
                <div className="col-span-1"></div>
              </div>
            </div>
          </div>

          {/* Table Body */}
          <div className="divide-y divide-gray-100">
            {filteredPrompts.map((prompt) => (
              <div
                key={prompt.id}
                className={`px-6 py-4 hover:bg-gray-50 transition-colors ${
                  selectedPrompts.includes(prompt.id) ? 'bg-blue-50/50' : ''
                }`}
              >
                <div className="flex items-center gap-4">
                  {/* Checkbox */}
                  <button
                    onClick={() => togglePromptSelection(prompt.id)}
                    className="w-5 h-5 rounded border-2 border-gray-300 flex items-center justify-center hover:border-gray-900 transition-colors flex-shrink-0"
                  >
                    {selectedPrompts.includes(prompt.id) && (
                      <CheckCircle className="w-4 h-4 text-gray-900" />
                    )}
                  </button>

                  <div className="flex-1 grid grid-cols-12 gap-4 items-center">
                    {/* Prompt Text */}
                    <div className="col-span-5">
                      <div className="flex items-start gap-2">
                       <div className="flex-1">
                          <p className="text-sm font-medium text-gray-900 mb-1">
                            {prompt.text}
                          </p>
                          <div className="flex items-center gap-1.5">
                            {prompt.status === 'Active' && (
                              <span className="inline-flex items-center gap-1 px-2 py-0.5 bg-green-100 text-green-700 rounded text-xs font-semibold">
                                <Circle className="w-2 h-2 fill-current" />
                                Active
                              </span>
                            )}
                            {prompt.status === 'Pending' && (
                              <span className="inline-flex items-center gap-1 px-2 py-0.5 bg-yellow-100 text-yellow-700 rounded text-xs font-semibold">
                                <Circle className="w-2 h-2 fill-current" />
                                Pending
                              </span>
                            )}
                            {prompt.status === 'Paused' && (
                              <span className="inline-flex items-center gap-1 px-2 py-0.5 bg-gray-100 text-gray-600 rounded text-xs font-semibold">
                                <Circle className="w-2 h-2 fill-current" />
                                Paused
                              </span>
                            )}
                            {prompt.status === 'Completed' && (
                              <span className="inline-flex items-center gap-1 px-2 py-0.5 bg-blue-100 text-blue-700 rounded text-xs font-semibold">
                                <Circle className="w-2 h-2 fill-current" />
                                Completed
                              </span>
                            )}
                          </div>
                        </div>
                      </div>
                    </div>

                    {/* Location */}
                    <div className="col-span-2">
                      <div className="flex items-center gap-2">
                        <Globe className="w-4 h-4 text-gray-400" />
                        <span className="text-sm text-gray-600">{prompt.location}</span>
                      </div>
                    </div>

                    {/* Topic */}
                    <div className="col-span-2">
                      {prompt.topic && prompt.topic !== 'Uncategorized' ? (
                        <span className="px-2 py-1 bg-blue-50 text-blue-700 rounded text-xs font-medium">
                          {prompt.topic}
                        </span>
                      ) : (
                        <span className="text-xs text-gray-400">No topic</span>
                      )}
                    </div>

                    {/* Volume */}
                    <div className="col-span-2">
                      {getVolumeBar(prompt.volume)}
                    </div>

                    {/* Actions */}
                    <div className="col-span-1 flex items-center justify-end gap-2">
                      <div className="relative">
                        <button 
                          onClick={() => setShowActionsMenu(showActionsMenu === prompt.id ? null : prompt.id)}
                          className="w-7 h-7 rounded-lg hover:bg-gray-100 flex items-center justify-center transition-colors"
                        >
                          <MoreVertical className="w-4 h-4 text-gray-600" />
                        </button>
                        {showActionsMenu === prompt.id && (
                          <div className="absolute right-0 bottom-full mb-1 w-40 bg-white rounded-lg border border-gray-200 shadow-xl z-[9999] py-1" ref={actionsMenuRef}>
                            <button
                              onClick={() => openEditModal(prompt)}
                              className="w-full px-3 py-2 text-left text-sm hover:bg-gray-50 flex items-center gap-2"
                            >
                              <Edit className="w-3.5 h-3.5 text-gray-600" />
                              Edit
                            </button>
                            <button
                              onClick={() => {
                                const newPrompt = { ...prompt, id: Math.max(...prompts.map(p => p.id)) + 1 };
                                setPrompts([...prompts, newPrompt]);
                                setShowActionsMenu(null);
                              }}
                              className="w-full px-3 py-2 text-left text-sm hover:bg-gray-50 flex items-center gap-2"
                            >
                              <Copy className="w-3.5 h-3.5 text-gray-600" />
                              Duplicate
                            </button>
                            <button
                              onClick={() => handleToggleStatus(prompt.id)}
                              className="w-full px-3 py-2 text-left text-sm hover:bg-gray-50 flex items-center gap-2"
                            >
                              <Circle className="w-3.5 h-3.5 text-gray-600" />
                              Toggle Status
                            </button>
                            <button
                              onClick={async () => {
                                try {
                                  await promptService.checkVisibility(prompt.id);
                                  toast.success('Visibility check initiated');
                                  setShowActionsMenu(null);
                                } catch (err: any) {
                                  toast.error(err.message || 'Failed to check visibility');
                                }
                              }}
                              className="w-full px-3 py-2 text-left text-sm hover:bg-gray-50 flex items-center gap-2"
                            >
                              <CheckCircle className="w-3.5 h-3.5 text-gray-600" />
                              Check Visibility
                            </button>
                            <div className="border-t border-gray-100 my-1" />
                            <button
                              onClick={() => handleDeletePrompt(prompt.id)}
                              className="w-full px-3 py-2 text-left text-sm hover:bg-red-50 flex items-center gap-2 text-red-600"
                            >
                              <Trash2 className="w-3.5 h-3.5" />
                              Delete
                            </button>
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>

          {/* Bulk Actions Bar */}
          {selectedPrompts.length > 0 && (
            <div className="border-t border-gray-200 bg-gray-50 px-6 py-3">
              <div className="flex items-center justify-between">
                <div className="text-sm font-medium text-gray-900">
                  {selectedPrompts.length} prompt{selectedPrompts.length > 1 ? 's' : ''} selected
                </div>
                <div className="flex items-center gap-2">
                  <Button
                    size="sm"
                    onClick={handleBulkEdit}
                    className="bg-white border border-gray-300 text-gray-700 hover:bg-gray-50"
                  >
                    <Edit className="w-3.5 h-3.5 mr-1.5" />
                    Edit
                  </Button>
                  <Button
                    size="sm"
                    onClick={handleDuplicateSelected}
                    className="bg-white border border-gray-300 text-gray-700 hover:bg-gray-50"
                  >
                    <Copy className="w-3.5 h-3.5 mr-1.5" />
                    Duplicate
                  </Button>
                  <Button
                    size="sm"
                    onClick={() => {
                      if (confirm(`Are you sure you want to delete ${selectedPrompts.length} prompt(s)?`)) {
                        handleDeleteSelected();
                      }
                    }}
                    className="bg-white border border-gray-300 text-red-600 hover:bg-red-50"
                  >
                    <Trash2 className="w-3.5 h-3.5 mr-1.5" />
                    Delete
                  </Button>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Empty State */}
        {filteredPrompts.length === 0 && (
          <div className="bg-white rounded-xl border border-gray-200/50 shadow-lg shadow-gray-200/50 p-12 text-center">
            <div className="w-16 h-16 rounded-xl bg-gray-100 flex items-center justify-center mx-auto mb-4">
              <Search className="w-8 h-8 text-gray-400" />
            </div>
            <h3 className="text-lg font-bold text-gray-900 mb-2">No prompts found</h3>
            <p className="text-sm text-gray-600 mb-4">
              Try adjusting your search or filters
            </p>
            <Button
              onClick={() => setSearchQuery('')}
              className="bg-gray-900 hover:bg-gray-800 text-white"
            >
              Clear filters
            </Button>
          </div>
        )}

        {/* Hidden File Input for CSV Import */}
        <input
          ref={fileInputRef}
          type="file"
          accept=".csv"
          onChange={handleImportCSV}
          className="hidden"
        />

        {/* Create Manual Modal */}
        {showManualModal && (
          <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
            <div className="bg-white rounded-xl shadow-2xl max-w-lg w-full p-6">
              <h2 className="text-xl font-bold text-gray-900 mb-4">Create Prompt Manually</h2>
              
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Prompt Text <span className="text-red-500">*</span>
                  </label>
                  <textarea
                    value={manualForm.promptText}
                    onChange={(e) => setManualForm({ ...manualForm, promptText: e.target.value })}
                    placeholder="Enter your prompt text..."
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 min-h-[100px]"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Topic</label>
                  <Input
                    value={manualForm.topic}
                    onChange={(e) => setManualForm({ ...manualForm, topic: e.target.value })}
                    placeholder="e.g., Running Shoes, AI Tools"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Platforms</label>
                  <div className="flex flex-wrap gap-2">
                    {['ChatGPT', 'Claude', 'Gemini', 'Perplexity', 'Copilot'].map(platform => (
                      <button
                        key={platform}
                        onClick={() => {
                          const platforms = manualForm.platforms.includes(platform)
                            ? manualForm.platforms.filter(p => p !== platform)
                            : [...manualForm.platforms, platform];
                          setManualForm({ ...manualForm, platforms });
                        }}
                        className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-colors ${
                          manualForm.platforms.includes(platform)
                            ? 'bg-blue-600 text-white'
                            : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                        }`}
                      >
                        {platform}
                      </button>
                    ))}
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Brand Ownership</label>
                  <div className="flex gap-2">
                    <button
                      onClick={() => setManualForm({ ...manualForm, isCompetitor: false })}
                      className={`flex-1 px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                        !manualForm.isCompetitor
                          ? 'bg-blue-600 text-white'
                          : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                      }`}
                    >
                      Own Brand
                    </button>
                    <button
                      onClick={() => setManualForm({ ...manualForm, isCompetitor: true })}
                      className={`flex-1 px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                        manualForm.isCompetitor
                          ? 'bg-orange-600 text-white'
                          : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                      }`}
                    >
                      Competitor
                    </button>
                  </div>
                </div>
              </div>

              <div className="flex items-center gap-3 mt-6">
                <Button
                  onClick={handleCreateManual}
                  disabled={submitting || !manualForm.promptText.trim()}
                  className="flex-1 bg-gray-900 hover:bg-gray-800 text-white disabled:opacity-50"
                >
                  {submitting ? (
                    <>
                      <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                      Creating...
                    </>
                  ) : (
                    'Create Prompt'
                  )}
                </Button>
                <Button
                  onClick={() => {
                    setShowManualModal(false);
                    setManualForm({ promptText: '', topic: '', platforms: [], isCompetitor: false });
                  }}
                  disabled={submitting}
                  className="px-6 bg-white border border-gray-300 text-gray-700 hover:bg-gray-50"
                >
                  Cancel
                </Button>
              </div>
            </div>
          </div>
        )}

        {/* Create with AI Modal */}
        {showAIModal && (
          <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
            <div className="bg-white rounded-xl shadow-2xl max-w-lg w-full p-6">
              <h2 className="text-xl font-bold text-gray-900 mb-4">Generate Prompts with AI</h2>
              
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Topic <span className="text-red-500">*</span>
                  </label>
                  <Input
                    value={aiForm.topic}
                    onChange={(e) => setAIForm({ ...aiForm, topic: e.target.value })}
                    placeholder="e.g., Running Shoes, AI Tools"
                  />
                  <p className="text-xs text-gray-500 mt-1">AI will generate prompts related to this topic</p>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Number of Prompts
                  </label>
                  <Input
                    type="number"
                    min="1"
                    max="20"
                    value={aiForm.count}
                    onChange={(e) => setAIForm({ ...aiForm, count: parseInt(e.target.value) || 5 })}
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Target Platforms</label>
                  <div className="flex flex-wrap gap-2">
                    {['ChatGPT', 'Claude', 'Gemini', 'Perplexity', 'Copilot'].map(platform => (
                      <button
                        key={platform}
                        onClick={() => {
                          const platforms = aiForm.platforms.includes(platform)
                            ? aiForm.platforms.filter(p => p !== platform)
                            : [...aiForm.platforms, platform];
                          setAIForm({ ...aiForm, platforms });
                        }}
                        className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-colors ${
                          aiForm.platforms.includes(platform)
                            ? 'bg-purple-600 text-white'
                            : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                        }`}
                      >
                        {platform}
                      </button>
                    ))}
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Brand Ownership</label>
                  <div className="flex gap-2">
                    <button
                      onClick={() => setAIForm({ ...aiForm, isCompetitor: false })}
                      className={`flex-1 px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                        !aiForm.isCompetitor
                          ? 'bg-blue-600 text-white'
                          : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                      }`}
                    >
                      Own Brand
                    </button>
                    <button
                      onClick={() => setAIForm({ ...aiForm, isCompetitor: true })}
                      className={`flex-1 px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                        aiForm.isCompetitor
                          ? 'bg-orange-600 text-white'
                          : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                      }`}
                    >
                      Competitor
                    </button>
                  </div>
                </div>
              </div>

              <div className="flex items-center gap-3 mt-6">
                <Button
                  onClick={handleCreateWithAI}
                  disabled={submitting || !aiForm.topic.trim()}
                  className="flex-1 bg-purple-600 hover:bg-purple-700 text-white disabled:opacity-50"
                >
                  {submitting ? (
                    <>
                      <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                      Generating...
                    </>
                  ) : (
                    'Generate Prompts'
                  )}
                </Button>
                <Button
                  onClick={() => {
                    setShowAIModal(false);
                    setAIForm({ topic: '', count: 5, platforms: [], isCompetitor: false });
                  }}
                  disabled={submitting}
                  className="px-6 bg-white border border-gray-300 text-gray-700 hover:bg-gray-50"
                >
                  Cancel
                </Button>
              </div>
            </div>
          </div>
        )}

        {/* Edit Prompt Modal */}
        {showEditModal && editingPrompt && (
          <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
            <div className="bg-white rounded-xl shadow-2xl max-w-lg w-full p-6">
              <h2 className="text-xl font-bold text-gray-900 mb-4">Edit Prompt</h2>
              
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Prompt Text <span className="text-red-500">*</span>
                  </label>
                  <textarea
                    value={editingPrompt.text}
                    onChange={(e) => setEditingPrompt({ ...editingPrompt, text: e.target.value })}
                    placeholder="Enter your prompt text..."
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 min-h-[100px]"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Topic</label>
                  <Input
                    value={editingPrompt.topic}
                    onChange={(e) => setEditingPrompt({ ...editingPrompt, topic: e.target.value })}
                    placeholder="e.g., Running Shoes, AI Tools"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Status</label>
                  <select
                    value={editingPrompt.status}
                    onChange={(e) => setEditingPrompt({ ...editingPrompt, status: e.target.value as 'Pending' | 'Scheduled' | 'Active' | 'Paused' | 'Completed' })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  >
                    <option value="Pending">Pending</option>
                    <option value="Active">Active</option>
                    <option value="Paused">Paused</option>
                    <option value="Completed">Completed</option>
                  </select>
                </div>
              </div>

              <div className="flex items-center gap-3 mt-6">
                <Button
                  onClick={handleEditPrompt}
                  disabled={submitting || !editingPrompt.text.trim()}
                  className="flex-1 bg-gray-900 hover:bg-gray-800 text-white disabled:opacity-50"
                >
                  {submitting ? (
                    <>
                      <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                      Updating...
                    </>
                  ) : (
                    'Update Prompt'
                  )}
                </Button>
                <Button
                  onClick={() => {
                    setShowEditModal(false);
                    setEditingPrompt(null);
                  }}
                  disabled={submitting}
                  className="px-6 bg-white border border-gray-300 text-gray-700 hover:bg-gray-50"
                >
                  Cancel
                </Button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default YourPrompts;
