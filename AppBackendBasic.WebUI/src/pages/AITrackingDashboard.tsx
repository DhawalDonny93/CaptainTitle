import { useState, useEffect } from 'react';
import { useProject } from '../contexts/ProjectContext';
import aiTrackingService, { AIInteraction, AIUsageStats } from '../services/aiTrackingService';
import { TrendingUp, DollarSign, Zap, Clock, Loader2, AlertCircle, Download } from 'lucide-react';
import { toast } from '../utils/toast';

const AITrackingDashboard = () => {
  const { selectedProject } = useProject();
  const [interactions, setInteractions] = useState<AIInteraction[]>([]);
  const [stats, setStats] = useState<AIUsageStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dateRange, setDateRange] = useState<'7d' | '30d' | '90d'>('30d');

  useEffect(() => {
    if (selectedProject?.id) {
      fetchTrackingData();
    } else {
      setLoading(false);
    }
  }, [selectedProject?.id, dateRange]);

  const fetchTrackingData = async () => {
    if (!selectedProject?.id) return;

    try {
      setLoading(true);
      setError(null);

      const endDate = new Date();
      const startDate = new Date();
      if (dateRange === '7d') startDate.setDate(startDate.getDate() - 7);
      else if (dateRange === '30d') startDate.setDate(startDate.getDate() - 30);
      else startDate.setDate(startDate.getDate() - 90);

      const [interactionsData, statsData] = await Promise.all([
        aiTrackingService.getInteractions(selectedProject.id, startDate, endDate),
        aiTrackingService.getUsageStats(selectedProject.id, startDate, endDate),
      ]);

      setInteractions(interactionsData);
      setStats(statsData);
    } catch (err: any) {
      console.error('Failed to fetch AI tracking data:', err);
      setError(err.message || 'Failed to load AI tracking data');
      toast.error('Failed to load AI tracking data');
    } finally {
      setLoading(false);
    }
  };

  const handleExport = async () => {
    if (!selectedProject?.id) return;

    try {
      const csv = interactions.map(i => 
        `${i.createdAt},${i.model},${i.prompt},${i.duration}`
      ).join('\n');
      const blob = new Blob([csv], { type: 'text/csv' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'ai-interactions.csv';
      a.click();
    } catch (err) {
      toast.error('Failed to export interactions');
    }
  };

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
        <h2 className="text-xl font-bold text-gray-900 mb-2">Failed to Load AI Tracking Data</h2>
        <p className="text-gray-600 mb-4">{error}</p>
        <button onClick={fetchTrackingData} className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700">
          Try Again
        </button>
      </div>
    );
  }

  if (!selectedProject) {
    return (
      <div className="text-center p-10">
        <AlertCircle className="h-12 w-12 text-gray-400 mx-auto mb-4" />
        <h2 className="text-xl font-bold text-gray-900 mb-2">No Project Selected</h2>
        <p className="text-gray-600">Please select a project to view AI tracking data</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">AI Tracking Dashboard</h1>
          <p className="text-gray-600 mt-1">Monitor AI interactions and usage metrics</p>
        </div>
        <div className="flex items-center gap-3">
          <select
            value={dateRange}
            onChange={(e) => setDateRange(e.target.value as '7d' | '30d' | '90d')}
            className="px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          >
            <option value="7d">Last 7 days</option>
            <option value="30d">Last 30 days</option>
            <option value="90d">Last 90 days</option>
          </select>
          <button
            onClick={handleExport}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 flex items-center gap-2"
          >
            <Download className="w-4 h-4" />
            Export
          </button>
        </div>
      </div>

      {/* Stats Cards */}
      {stats && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <div className="flex items-center justify-between mb-2">
              <Zap className="w-8 h-8 text-blue-600" />
            </div>
            <p className="text-2xl font-bold text-gray-900">{stats.totalInteractions.toLocaleString()}</p>
            <p className="text-sm text-gray-600">Total Interactions</p>
          </div>

          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <div className="flex items-center justify-between mb-2">
              <TrendingUp className="w-8 h-8 text-green-600" />
            </div>
            <p className="text-2xl font-bold text-gray-900">{stats.totalTokensUsed.toLocaleString()}</p>
            <p className="text-sm text-gray-600">Total Tokens</p>
          </div>

          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <div className="flex items-center justify-between mb-2">
              <DollarSign className="w-8 h-8 text-purple-600" />
            </div>
            <p className="text-2xl font-bold text-gray-900">${stats.totalCost.toFixed(2)}</p>
            <p className="text-sm text-gray-600">Total Cost</p>
          </div>

          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <div className="flex items-center justify-between mb-2">
              <Clock className="w-8 h-8 text-orange-600" />
            </div>
            <p className="text-2xl font-bold text-gray-900">{Math.floor(stats.averageDuration / 1000)}s</p>
            <p className="text-sm text-gray-600">Avg Response Time: {stats.averageDuration}ms</p>
          </div>
        </div>
      )}

      {/* Recent Interactions */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-6">Recent Interactions</h2>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="border-b border-gray-200">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Timestamp</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Model</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Prompt</th>
                <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Tokens</th>
                <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Cost</th>
                <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Duration</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200">
              {interactions.slice(0, 20).map((interaction) => (
                <tr key={interaction.id} className="border-b border-gray-100 hover:bg-gray-50">
                  <td className="px-4 py-3 text-sm text-gray-600">{new Date(interaction.createdAt).toLocaleString()}</td>
                  <td className="px-4 py-3 text-sm text-gray-900">{interaction.model}</td>
                  <td className="px-4 py-3 text-sm text-gray-600 max-w-xs truncate">
                    {interaction.prompt}
                  </td>
                  <td className="px-4 py-3 text-sm text-gray-900 text-right">{interaction.tokensUsed.toLocaleString()}</td>
                  <td className="px-4 py-3 text-sm text-gray-900 text-right">${interaction.cost.toFixed(4)}</td>
                  <td className="px-4 py-3 text-sm text-gray-900 text-right">{interaction.duration}ms</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

export default AITrackingDashboard;
