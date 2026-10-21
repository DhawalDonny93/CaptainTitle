import { useState, useEffect } from 'react';
import { useProject } from '../contexts/ProjectContext';
import communityOpportunityService, { CommunityOpportunity } from '../services/communityOpportunityService';
import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { ExternalLink, MessageSquare, CheckCircle, XCircle, AlertCircle } from 'lucide-react';
import { toast } from '../utils/toast';
import { LoadingPage } from '../components/ui/LoadingSpinner';

const CommunityOpportunities = () => {
    const { selectedProject } = useProject();
    const [opportunities, setOpportunities] = useState<CommunityOpportunity[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        if (selectedProject?.id) {
            fetchOpportunities();
        } else {
            setLoading(false);
        }
    }, [selectedProject?.id]);

    const fetchOpportunities = async () => {
        if (!selectedProject?.id) return;
        
        try {
            setLoading(true);
            setError(null);
            const data = await communityOpportunityService.getOpportunities(selectedProject.id);
            setOpportunities(data);
        } catch (err: any) {
            console.error("Failed to fetch opportunities", err);
            setError(err.message || 'Failed to load opportunities');
            toast.error('Failed to load opportunities');
        } finally {
            setLoading(false);
        }
    };

    const handleStatusUpdate = async (opportunityId: number, newStatus: 'New' | 'Drafted' | 'Posted' | 'Ignored') => {
        try {
            await communityOpportunityService.updateOpportunity(opportunityId, { status: newStatus });
            setOpportunities(prev => prev.map(o => o.id === opportunityId ? { ...o, status: newStatus } : o));
            toast.success('Status updated successfully');
        } catch (err: any) {
            console.error("Failed to update status", err);
            toast.error(err.message || 'Failed to update status');
        }
    };

    if (loading) {
        return <LoadingPage />;
    }

    if (error) {
        return (
            <div className="text-center p-10">
                <AlertCircle className="h-12 w-12 text-red-600 mx-auto mb-4" />
                <h2 className="text-xl font-bold text-gray-900 mb-2">Failed to Load Opportunities</h2>
                <p className="text-gray-600 mb-4">{error}</p>
                <Button onClick={fetchOpportunities}>Try Again</Button>
            </div>
        );
    }

    if (!selectedProject) {
        return (
            <div className="text-center p-10">
                <AlertCircle className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                <h2 className="text-xl font-bold text-gray-900 mb-2">No Project Selected</h2>
                <p className="text-gray-600">Please select a project to view community opportunities</p>
            </div>
        );
    }

    if (opportunities.length === 0) {
        return (
            <div className="text-center p-10 border-2 border-dashed rounded-lg">
                <MessageSquare className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                <p className="text-gray-600 mb-2">No opportunities found yet</p>
                <p className="text-sm text-gray-500 mb-4">The background job runs daily to find new threads</p>
                <Button variant="outline" onClick={fetchOpportunities}>Refresh</Button>
            </div>
        );
    }

    return (
        <div className="space-y-4">
            <h2 className="text-xl font-semibold mb-4">Community Opportunities</h2>
            <div className="grid gap-4">
                {opportunities.map((opp) => (
                    <Card key={opp.id} className="p-4">
                        <div className="flex justify-between items-start">
                            <div>
                                <div className="flex items-center gap-2 mb-1">
                                    <span className="px-2 py-0.5 rounded text-xs font-medium bg-purple-100 text-purple-800">
                                        {opp.platform}
                                    </span>
                                    <span className={`px-2 py-0.5 rounded text-xs font-medium ${
                                        opp.status === 'New' ? 'bg-blue-100 text-blue-800' :
                                        opp.status === 'Drafted' ? 'bg-yellow-100 text-yellow-800' :
                                        opp.status === 'Posted' ? 'bg-green-100 text-green-800' :
                                        'bg-gray-100 text-gray-800'
                                    }`}>
                                        {opp.status}
                                    </span>
                                </div>
                                <h3 className="font-medium text-lg hover:underline">
                                    <a href={opp.url} target="_blank" rel="noopener noreferrer" className="flex items-center gap-1">
                                        {opp.title} <ExternalLink className="h-3 w-3" />
                                    </a>
                                </h3>
                                <p className="text-sm text-gray-600 mt-1">{opp.summary || 'No summary available'}</p>
                                <p className="text-xs text-gray-500 mt-2">Priority: {opp.priority} | Created: {new Date(opp.createdAt).toLocaleDateString()}</p>
                            </div>
                            <div className="flex flex-col gap-2">
                                {opp.status === 'New' && (
                                    <Button size="sm" onClick={() => handleStatusUpdate(opp.id, 'Drafted')}>
                                        <MessageSquare className="h-4 w-4 mr-2" /> Draft Reply
                                    </Button>
                                )}
                                {opp.status === 'Drafted' && (
                                    <Button size="sm" variant="default" onClick={() => handleStatusUpdate(opp.id, 'Posted')}>
                                        <CheckCircle className="h-4 w-4 mr-2" /> Mark Posted
                                    </Button>
                                )}
                                <Button size="sm" variant="ghost" className="text-gray-500" onClick={() => handleStatusUpdate(opp.id, 'Ignored')}>
                                    <XCircle className="h-4 w-4" />
                                </Button>
                            </div>
                        </div>
                    </Card>
                ))}
            </div>
        </div>
    );
};

export default CommunityOpportunities;
