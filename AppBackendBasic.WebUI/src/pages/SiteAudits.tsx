import React, { useState, useEffect } from 'react';
import siteAuditService, { SiteAudit, AuditIssue } from '../services/siteAuditService';
import { useProject } from '../contexts/ProjectContext';
import { useUserProfile } from '../contexts/UserProfileContext';
import { Button } from '@/components/ui/Button';
import { AlertCircle, RefreshCw, CheckCircle, Sparkles, Copy } from 'lucide-react';
import { LoadingPage } from '../components/ui/LoadingSpinner';
import { toast } from '../utils/toast';
import ScanningAnimation from '../components/ScanningAnimation';

const SiteAudits: React.FC = () => {
    const { selectedProject } = useProject();
    const { hasSiteAudit, updateSiteAuditStatus } = useUserProfile();
    const [, setAudits] = useState<SiteAudit[]>([]);
    const [issues, setIssues] = useState<AuditIssue[]>([]);
    const [selectedIssueIndex, setSelectedIssueIndex] = useState(0);
    const [loading, setLoading] = useState(true);
    const [isScanning, setIsScanning] = useState(false);
    const [hasRunAudit, setHasRunAudit] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [showAllIssues, setShowAllIssues] = useState(false);
    const [, setTechnicalScore] = useState(53);
    const [, setAeoScore] = useState(31);

    useEffect(() => {
        if (selectedProject?.id) {
            // Check session data first to minimize DB calls
            const hasAuditInSession = hasSiteAudit(selectedProject.id);
            
            if (hasAuditInSession) {
                // User has run audit before, fetch data
                setHasRunAudit(true);
                fetchAuditsAndIssues();
            } else {
                // No audit in session, check DB once
                checkAndFetchAudits();
            }
        } else {
            setLoading(false);
        }
    }, [selectedProject?.id]);

    const checkAndFetchAudits = async () => {
        if (!selectedProject?.id) return;

        try {
            setLoading(true);
            setError(null);
            
            // Single DB call to check if audits exist
            const auditsData = await siteAuditService.getAuditsByProject(selectedProject.id);
            
            if (auditsData.length > 0) {
                setHasRunAudit(true);
                // Update session to avoid future DB calls
                updateSiteAuditStatus(selectedProject.id, true);
                // Now fetch issues
                const issuesData = await siteAuditService.getProjectAuditIssues(selectedProject.id);
                setAudits(auditsData);
                setIssues(issuesData);
                
                // Update scores from latest audit
                const latestAudit = auditsData[0];
                setTechnicalScore(latestAudit.performanceScore || 53);
                setAeoScore(latestAudit.seoScore || 31);
                
                // Auto-select first issue if available
                if (issuesData.length > 0) {
                    setSelectedIssueIndex(0);
                }
            } else {
                setHasRunAudit(false);
            }
        } catch (err: any) {
            console.error("Failed to check audits", err);
            setError(err.message || 'Failed to load audits');
            toast.error('Failed to load site audits');
        } finally {
            setLoading(false);
        }
    };

    const fetchAuditsAndIssues = async () => {
        if (!selectedProject?.id) return;

        try {
            setLoading(true);
            setError(null);
            
            // Fetch audits and issues in parallel
            const [auditsData, issuesData] = await Promise.all([
                siteAuditService.getAuditsByProject(selectedProject.id),
                siteAuditService.getProjectAuditIssues(selectedProject.id)
            ]);
            
            setAudits(auditsData);
            setIssues(issuesData);
            
            if (auditsData.length > 0) {
                setHasRunAudit(true);
                // Update scores from latest audit
                const latestAudit = auditsData[0];
                setTechnicalScore(latestAudit.performanceScore || 53);
                setAeoScore(latestAudit.seoScore || 31);
            }
            
            // Auto-select first issue if available
            if (issuesData.length > 0) {
                setSelectedIssueIndex(0);
            }
        } catch (err: any) {
            console.error("Failed to fetch audits and issues", err);
            setError(err.message || 'Failed to load audits');
            toast.error('Failed to load site audits');
        } finally {
            setLoading(false);
        }
    };

    const fetchAudits = async () => {
        if (!selectedProject?.id) return;

        try {
            setLoading(true);
            setError(null);
            const data = await siteAuditService.getAuditsByProject(selectedProject.id);
            setAudits(data);
            if (data.length > 0) {
                setHasRunAudit(true);
                // Update scores from latest audit
                const latestAudit = data[0];
                setTechnicalScore(latestAudit.performanceScore || 53);
                setAeoScore(latestAudit.seoScore || 31);
            }
        } catch (err: any) {
            console.error("Failed to fetch audits", err);
            setError(err.message || 'Failed to load audits');
            toast.error('Failed to load site audits');
        } finally {
            setLoading(false);
        }
    };

    const handleStartScan = () => {
        setIsScanning(true);
    };

    const handleScanComplete = async () => {
        if (!selectedProject?.id) {
            toast.error('Please select a project first');
            return;
        }

        try {
            // Use project domain or default URL
            const projectUrl = selectedProject.domain || 'https://example.com';
            await siteAuditService.startAudit({
                projectId: selectedProject.id,
                url: projectUrl
            });
            
            toast.success('Audit completed successfully!');
            setHasRunAudit(true);
            
            // Update session to avoid future DB calls
            updateSiteAuditStatus(selectedProject.id, true);
            
            // Refresh audits and issues
            await fetchAuditsAndIssues();
            setIsScanning(false);
        } catch (err: any) {
            console.error("Failed to run audit", err);
            toast.error(err.message || 'Failed to complete audit');
            setIsScanning(false);
        }
    };

    const handleMarkFixed = async (issueId: number) => {
        try {
            await siteAuditService.markIssueCompleted(issueId);
            toast.success('Marked as fixed!');
            
            // Update local state
            setIssues(prevIssues => 
                prevIssues.map(issue => 
                    issue.id === issueId 
                        ? { ...issue, isCompleted: true, completedAt: new Date() }
                        : issue
                )
            );
        } catch (err: any) {
            console.error("Failed to mark issue as fixed", err);
            toast.error('Failed to mark issue as fixed');
        }
    };

    const getSeverityBadgeClass = (severity: string) => {
        switch (severity) {
            case 'Critical':
                return 'bg-red-100 text-red-700 border border-red-200';
            case 'High':
                return 'bg-orange-100 text-orange-700 border border-orange-200';
            case 'Medium':
                return 'bg-yellow-100 text-yellow-700 border border-yellow-200';
            default:
                return 'bg-blue-100 text-blue-700 border border-blue-200';
        }
    };

    const getCategoryBadgeClass = (category: string) => {
        switch (category) {
            case 'Content':
                return 'bg-purple-100 text-purple-700';
            case 'AEO':
                return 'bg-green-100 text-green-700';
            case 'Technical':
                return 'bg-blue-100 text-blue-700';
            default:
                return 'bg-gray-100 text-gray-700';
        }
    };

    if (loading) {
        return <LoadingPage />;
    }

    if (!selectedProject) {
        return (
            <div className="text-center p-10">
                <AlertCircle className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                <h2 className="text-xl font-bold text-gray-900 mb-2">No Project Selected</h2>
                <p className="text-gray-600">Please select a project to view site audits.</p>
            </div>
        );
    }

    if (error) {
        return (
            <div className="text-center p-10">
                <AlertCircle className="h-12 w-12 text-red-600 mx-auto mb-4" />
                <h2 className="text-xl font-bold text-gray-900 mb-2">Failed to Load Audits</h2>
                <p className="text-gray-600 mb-4">{error}</p>
                <Button onClick={fetchAudits}>Try Again</Button>
            </div>
        );
    }

    if (isScanning) {
        return (
            <div className="min-h-screen bg-gradient-to-br from-gray-50 via-blue-50/30 to-gray-50">
                <div className="max-w-5xl mx-auto px-6 py-8">
                    <div className="text-center mb-8">
                        <h2 className="text-2xl font-bold text-gray-900 mb-3">
                            Running Site Audit
                        </h2>
                        <p className="text-sm text-gray-600 max-w-2xl mx-auto">
                            Analyzing your website for technical issues and optimization opportunities.
                        </p>
                    </div>
                    <ScanningAnimation onComplete={handleScanComplete} />
                </div>
            </div>
        );
    }

    if (!hasRunAudit) {
        return (
            <div className="min-h-screen bg-gradient-to-br from-gray-50 via-blue-50/30 to-gray-50">
                <div className="max-w-5xl mx-auto px-6 py-8">
                    <div className="text-center mb-8">
                        <div className="inline-flex items-center gap-2 px-3 py-1.5 bg-gray-900 text-white rounded-full text-xs font-semibold mb-4 animate-pulse">
                            <Sparkles className="w-3 h-3" />
                            AI-Powered Analysis
                        </div>
                        <h2 className="text-2xl font-bold text-gray-900 mb-3">
                            Run Your First Site Audit
                        </h2>
                        <p className="text-sm text-gray-600 max-w-2xl mx-auto mb-8">
                            Discover technical issues, SEO opportunities, and content gaps to improve your website's performance.
                        </p>
                        <Button
                            onClick={handleStartScan}
                            className="px-8 py-3 bg-gray-900 hover:bg-gray-800 text-white font-semibold rounded-lg shadow-lg shadow-gray-900/30 hover:shadow-xl hover:shadow-gray-900/40 transition-all transform hover:scale-105 active:scale-95"
                        >
                            <Sparkles className="w-4 h-4 mr-2" />
                            Start Scan
                        </Button>
                    </div>
                    <div className="bg-white rounded-xl border border-gray-200/50 shadow-xl shadow-gray-200/50 p-8 max-w-3xl mx-auto">
                        <div className="flex items-center justify-center gap-8 text-sm text-gray-600">
                            <div className="flex items-center gap-2">
                                <CheckCircle className="w-5 h-5 text-green-500" />
                                <span>Technical SEO</span>
                            </div>
                            <div className="flex items-center gap-2">
                                <CheckCircle className="w-5 h-5 text-green-500" />
                                <span>Content Analysis</span>
                            </div>
                            <div className="flex items-center gap-2">
                                <CheckCircle className="w-5 h-5 text-green-500" />
                                <span>AEO Optimization</span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="min-h-screen bg-gray-50">
            <div className="max-w-7xl mx-auto px-8 py-8">
                {/* Header with Scores and Rerun */}
                <div className="mb-8">
                    <div className="flex items-center justify-between mb-6">
                        <div>
                            <h1 className="text-base font-normal text-gray-900">
                                All Issues <span className="text-gray-400">({issues.length})</span>
                            </h1>
                        </div>
                        <div className="flex items-center gap-4">
                            {/* Severity Dropdown */}
                            <select className="px-3 py-1.5 text-sm text-gray-700 border border-gray-300 rounded-md bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-gray-900 focus:border-transparent">
                                <option>Severity</option>
                                <option>Critical</option>
                                <option>High</option>
                                <option>Medium</option>
                                <option>Low</option>
                            </select>

                            <button
                                onClick={handleStartScan}
                                className="flex items-center gap-1.5 px-3 py-1.5 text-sm text-gray-700 hover:text-gray-900 border border-gray-300 rounded-md hover:bg-gray-50 transition-colors"
                            >
                                <RefreshCw className="w-3.5 h-3.5" />
                                Re-run
                            </button>
                        </div>
                    </div>
                </div>

                {/* Issues List */}
                <div className="space-y-3 mb-8">
                    {(showAllIssues ? issues : issues.slice(0, 5)).map((issue, index) => (
                        <div
                            key={issue.id}
                            onClick={() => setSelectedIssueIndex(index)}
                            className={`bg-white rounded-lg p-4 cursor-pointer transition-all duration-150 ${
                                selectedIssueIndex === index
                                    ? 'ring-2 ring-gray-900 shadow-sm'
                                    : 'hover:shadow-md'
                            }`}
                        >
                            <div className="flex items-center gap-4">
                                <div className="flex-shrink-0 w-7 h-7 rounded-full bg-gray-100 flex items-center justify-center text-sm font-normal text-gray-600">
                                    {index + 1}
                                </div>
                                <div className="flex-1 min-w-0">
                                    <h3 className="text-[15px] font-normal text-gray-900">
                                        {issue.title}
                                    </h3>
                                </div>
                                <div className="flex items-center gap-2">
                                    <span className={`px-2.5 py-1 text-xs font-medium rounded ${getCategoryBadgeClass(issue.category)}`}>
                                        {issue.category}
                                    </span>
                                    <span className={`px-2.5 py-1 text-xs font-medium rounded ${getCategoryBadgeClass('AEO')}`}>
                                        AEO
                                    </span>
                                </div>
                                <span className={`px-3 py-1 text-xs font-medium rounded ${getSeverityBadgeClass(issue.severity)}`}>
                                    {issue.severity}
                                </span>
                            </div>
                        </div>
                    ))}
                </div>

                {/* View All Link */}
                <div className="text-center mb-8">
                    <button 
                        onClick={() => setShowAllIssues(!showAllIssues)}
                        className="text-sm text-gray-600 hover:text-gray-900 transition-colors"
                    >
                        {showAllIssues ? 'Show less' : `View all ${issues.length} issues`}
                    </button>
                </div>

                {/* Issue Details Section */}
                <div className="mb-6">
                    <div className="flex items-center justify-between mb-6">
                        <h2 className="text-base font-normal text-gray-900">
                            Issue Details <span className="text-gray-400">1 of {issues.length} fixed</span>
                        </h2>
                        <div className="flex items-center gap-2">
                            <button 
                                onClick={() => setSelectedIssueIndex(Math.max(0, selectedIssueIndex - 1))}
                                disabled={selectedIssueIndex === 0}
                                className="p-1.5 hover:bg-gray-100 rounded disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
                            >
                                <svg className="w-4 h-4 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
                                </svg>
                            </button>
                            <span className="text-sm text-gray-600">{selectedIssueIndex + 1} / {issues.length}</span>
                            <button 
                                onClick={() => setSelectedIssueIndex(Math.min(issues.length - 1, selectedIssueIndex + 1))}
                                disabled={selectedIssueIndex === issues.length - 1}
                                className="p-1.5 hover:bg-gray-100 rounded disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
                            >
                                <svg className="w-4 h-4 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                                </svg>
                            </button>
                        </div>
                    </div>
                </div>

                {/* Detail Cards */}
                {issues.length > 0 && (
                    <div className="grid grid-cols-2 gap-6">
                        {/* Left Card - Selected Issue */}
                        <div className="bg-white rounded-lg p-6">
                            <div className="flex items-center gap-2 mb-4">
                                <span className={`px-2.5 py-1 text-xs font-medium rounded ${getSeverityBadgeClass(issues[selectedIssueIndex].severity)}`}>
                                    {issues[selectedIssueIndex].severity}
                                </span>
                                <span className={`px-2.5 py-1 text-xs font-medium rounded ${getCategoryBadgeClass(issues[selectedIssueIndex].category)}`}>
                                    {issues[selectedIssueIndex].category}
                                </span>
                                {issues[selectedIssueIndex].category !== 'AEO' && (
                                    <span className={`px-2.5 py-1 text-xs font-medium rounded ${getCategoryBadgeClass('AEO')}`}>
                                        AEO
                                    </span>
                                )}
                                <span className="ml-auto text-xs text-gray-500">{selectedIssueIndex + 1}/{issues.length}</span>
                            </div>
                            <h3 className="text-[15px] font-medium text-gray-900 mb-4">
                                {issues[selectedIssueIndex].title}
                            </h3>
                            <p className="text-sm text-gray-600 leading-relaxed mb-6">
                                {issues[selectedIssueIndex].description}
                            </p>
                            <div className="flex items-center gap-3 mb-6">
                                <button className="text-xs text-purple-600 hover:text-purple-700 font-medium">
                                    +{Math.floor(Math.random() * 10) + 10} Content
                                </button>
                                <button className="text-xs text-green-600 hover:text-green-700 font-medium">
                                    +{Math.floor(Math.random() * 5) + 3} AEO
                                </button>
                            </div>
                            <div className="flex items-center gap-2">
                                <button
                                    className="flex items-center gap-1.5 px-3 py-1.5 text-sm text-gray-700 hover:text-gray-900 bg-white border border-gray-300 rounded-md hover:bg-gray-50 transition-colors"
                                    onClick={() => handleMarkFixed(issues[selectedIssueIndex].id)}
                                    disabled={issues[selectedIssueIndex].isCompleted}
                                >
                                    <CheckCircle className="w-3.5 h-3.5" />
                                    {issues[selectedIssueIndex].isCompleted ? 'Completed' : 'Mark Fixed'}
                                </button>
                                <button
                                    className="flex items-center gap-1.5 px-3 py-1.5 text-sm text-gray-700 hover:text-gray-900 bg-white border border-gray-300 rounded-md hover:bg-gray-50 transition-colors"
                                    onClick={() => toast.info('Ask Agent feature coming soon')}
                                >
                                    <Sparkles className="w-3.5 h-3.5" />
                                    Ask Agent
                                </button>
                                <button
                                    className="flex items-center gap-1.5 px-3 py-1.5 text-sm text-gray-700 hover:text-gray-900 bg-white border border-gray-300 rounded-md hover:bg-gray-50 transition-colors"
                                    onClick={() => {
                                        navigator.clipboard.writeText(issues[selectedIssueIndex].description);
                                        toast.success('Copied to clipboard!');
                                    }}
                                >
                                    <Copy className="w-3.5 h-3.5" />
                                    Copy Prompt
                                </button>
                            </div>
                        </div>

                        {/* Right Card - Next Issue */}
                        {selectedIssueIndex < issues.length - 1 && (
                            <div 
                                className="bg-white rounded-lg p-6 cursor-pointer hover:shadow-md transition-shadow"
                                onClick={() => setSelectedIssueIndex(selectedIssueIndex + 1)}
                            >
                                <div className="flex items-center gap-2 mb-4">
                                    <span className={`px-2.5 py-1 text-xs font-medium rounded ${getSeverityBadgeClass(issues[selectedIssueIndex + 1].severity)}`}>
                                        {issues[selectedIssueIndex + 1].severity}
                                    </span>
                                    <span className={`px-2.5 py-1 text-xs font-medium rounded ${getCategoryBadgeClass(issues[selectedIssueIndex + 1].category)}`}>
                                        {issues[selectedIssueIndex + 1].category}
                                    </span>
                                    {issues[selectedIssueIndex + 1].category !== 'AEO' && (
                                        <span className={`px-2.5 py-1 text-xs font-medium rounded ${getCategoryBadgeClass('AEO')}`}>
                                            AEO
                                        </span>
                                    )}
                                    <span className="ml-auto text-xs text-gray-500">{selectedIssueIndex + 2}/{issues.length}</span>
                                </div>
                                <h3 className="text-[15px] font-medium text-gray-900 mb-4">
                                    {issues[selectedIssueIndex + 1].title}
                                </h3>
                                <p className="text-sm text-gray-600 leading-relaxed mb-6">
                                    {issues[selectedIssueIndex + 1].description}
                                </p>
                                <div className="flex items-center gap-3 mb-6">
                                    <button className="text-xs text-purple-600 hover:text-purple-700 font-medium">
                                        +{Math.floor(Math.random() * 10) + 10} Content
                                    </button>
                                    <button className="text-xs text-green-600 hover:text-green-700 font-medium">
                                        +{Math.floor(Math.random() * 5) + 3} AEO
                                    </button>
                                </div>
                                <div className="flex items-center gap-2">
                                    <button
                                        className="flex items-center gap-1.5 px-3 py-1.5 text-sm text-gray-700 hover:text-gray-900 bg-white border border-gray-300 rounded-md hover:bg-gray-50 transition-colors"
                                        onClick={(e) => { e.stopPropagation(); toast.success('Marked as fixed!'); }}
                                    >
                                        <CheckCircle className="w-3.5 h-3.5" />
                                        Mark Fixed
                                    </button>
                                    <button
                                        className="flex items-center gap-1.5 px-3 py-1.5 text-sm text-gray-700 hover:text-gray-900 bg-white border border-gray-300 rounded-md hover:bg-gray-50 transition-colors"
                                        onClick={(e) => { e.stopPropagation(); toast.info('Ask Agent feature coming soon'); }}
                                    >
                                        <Sparkles className="w-3.5 h-3.5" />
                                        Ask Agent
                                    </button>
                                    <button
                                        className="flex items-center gap-1.5 px-3 py-1.5 text-sm text-gray-700 hover:text-gray-900 bg-white border border-gray-300 rounded-md hover:bg-gray-50 transition-colors"
                                        onClick={(e) => {
                                            e.stopPropagation();
                                            navigator.clipboard.writeText(issues[selectedIssueIndex + 1].description);
                                            toast.success('Copied to clipboard!');
                                        }}
                                    >
                                        <Copy className="w-3.5 h-3.5" />
                                        Copy Prompt
                                    </button>
                                </div>
                            </div>
                        )}
                    </div>
                )}
            </div>
        </div>
    );
};

export default SiteAudits;
