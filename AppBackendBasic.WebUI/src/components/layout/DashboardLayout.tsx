import { useState, useEffect } from 'react';
import { Outlet, NavLink, useNavigate, useLocation } from 'react-router-dom';
import { aiSeoService, Notification } from '../../services/aiSeoService';
import {
    LayoutDashboard,
    Bell,
    User,
    LogOut,
    Atom,
    FolderKanban,
    CreditCard,
    History
} from 'lucide-react';
import { Button } from '@/components/ui/Button';

const DashboardLayout = () => {
    const navigate = useNavigate();
    const location = useLocation();
    const [notifications, setNotifications] = useState<Notification[]>([]);

    useEffect(() => {
        fetchNotifications();
        // In a real app, use WebSockets or polling
        const interval = setInterval(fetchNotifications, 60000);
        return () => clearInterval(interval);
    }, []);

    const fetchNotifications = async () => {
        try {
            const data = await aiSeoService.getNotifications();
            setNotifications(data);
        } catch (error) {
            console.error(error);
        }
    };

    const handleLogout = () => {
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        navigate('/login');
    };

    const unreadCount = notifications.filter(n => !n.isRead).length;

    const getPageTitle = () => {
        const path = location.pathname;
        if (path === '/dashboard') return 'Dashboard';
        if (path === '/projects') return 'Projects';
        if (path.includes('/content')) return 'Content Writer';
        if (path.includes('/visibility')) return 'AI Visibility';
        if (path.includes('/community')) return 'Community Opportunities';
        if (path.includes('/audits')) return 'Agentic Auditor';
        if (path.includes('/action-plan')) return 'Action Plan';
        if (path.startsWith('/projects/')) return 'Project Dashboard';
        if (path === '/notifications') return 'Notifications';
        if (path === '/subscription') return 'Subscription';
        if (path === '/login-history') return 'Login History';
        return 'Dashboard';
    };

    return (
        <div className="flex h-screen bg-background">
            {/* Sidebar */}
            <aside className="w-64 border-r bg-card flex flex-col">
                <div className="p-6 border-b flex items-center gap-2">
                    <Atom className="h-6 w-6 text-primary animate-pulse" />
                    <h1 className="text-xl font-bold tracking-tight">ProQuest AI</h1>
                </div>

                <nav className="flex-1 p-4 space-y-1 overflow-y-auto">
                    {/* Main Navigation */}
                    <NavLink to="/dashboard" className={({ isActive }) => `flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium transition-colors ${isActive ? 'bg-primary/10 text-primary' : 'text-muted-foreground hover:bg-muted hover:text-foreground'}`}>
                        <LayoutDashboard className="h-4 w-4" /> Dashboard
                    </NavLink>

                    <NavLink to="/projects" className={({ isActive }) => `flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium transition-colors ${isActive ? 'bg-primary/10 text-primary' : 'text-muted-foreground hover:bg-muted hover:text-foreground'}`}>
                        <FolderKanban className="h-4 w-4" /> Projects
                    </NavLink>

                    {/* Project Tools Section */}
                    {/*{location.pathname.startsWith('/projects/') && (
                        <>
                            <div className="pt-4 pb-2">
                                <p className="px-3 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Project Tools</p>
                            </div>

                            <NavLink to={`${location.pathname.split('/').slice(0, 3).join('/')}/content`} className={({ isActive }) => `flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium transition-colors ${isActive ? 'bg-primary/10 text-primary' : 'text-muted-foreground hover:bg-muted hover:text-foreground'}`}>
                                <PenTool className="h-4 w-4" /> Content Writer
                            </NavLink>

                            <NavLink to={`${location.pathname.split('/').slice(0, 3).join('/')}/visibility`} className={({ isActive }) => `flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium transition-colors ${isActive ? 'bg-primary/10 text-primary' : 'text-muted-foreground hover:bg-muted hover:text-foreground'}`}>
                                <BarChart3 className="h-4 w-4" /> AI Visibility
                            </NavLink>

                            <NavLink to={`${location.pathname.split('/').slice(0, 3).join('/')}/community`} className={({ isActive }) => `flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium transition-colors ${isActive ? 'bg-primary/10 text-primary' : 'text-muted-foreground hover:bg-muted hover:text-foreground'}`}>
                                <MessageSquare className="h-4 w-4" /> Community
                            </NavLink>

                            <NavLink to={`${location.pathname.split('/').slice(0, 3).join('/')}/audits`} className={({ isActive }) => `flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium transition-colors ${isActive ? 'bg-primary/10 text-primary' : 'text-muted-foreground hover:bg-muted hover:text-foreground'}`}>
                                <AlertCircle className="h-4 w-4" /> Agentic Auditor
                            </NavLink>

                            <NavLink to={`${location.pathname.split('/').slice(0, 3).join('/')}/action-plan`} className={({ isActive }) => `flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium transition-colors ${isActive ? 'bg-primary/10 text-primary' : 'text-muted-foreground hover:bg-muted hover:text-foreground'}`}>
                                <CheckSquare className="h-4 w-4" /> Action Plan
                            </NavLink>
                        </>
                    )}
*/}
                    {/* Workspace Section */}
                    <div className="pt-4 pb-2">
                        <p className="px-3 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Workspace</p>
                    </div>

                    <NavLink to="/notifications" className={({ isActive }) => `flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium transition-colors ${isActive ? 'bg-primary/10 text-primary' : 'text-muted-foreground hover:bg-muted hover:text-foreground'}`}>
                        <Bell className="h-4 w-4" /> Notifications
                        {unreadCount > 0 && <span className="ml-auto bg-red-500 text-white text-[10px] px-1.5 py-0.5 rounded-full">{unreadCount}</span>}
                    </NavLink>

                    {/* Account Section */}
                    <div className="pt-4 pb-2">
                        <p className="px-3 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Account</p>
                    </div>

                    <NavLink to="/subscription" className={({ isActive }) => `flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium transition-colors ${isActive ? 'bg-primary/10 text-primary' : 'text-muted-foreground hover:bg-muted hover:text-foreground'}`}>
                        <CreditCard className="h-4 w-4" /> Subscription
                    </NavLink>

                    <NavLink to="/login-history" className={({ isActive }) => `flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium transition-colors ${isActive ? 'bg-primary/10 text-primary' : 'text-muted-foreground hover:bg-muted hover:text-foreground'}`}>
                        <History className="h-4 w-4" /> Login History
                    </NavLink>
                </nav>

                <div className="p-4 border-t">
                    <Button variant="ghost" className="w-full justify-start text-muted-foreground" onClick={handleLogout}>
                        <LogOut className="h-4 w-4 mr-2" /> Logout
                    </Button>
                </div>
            </aside>

            {/* Main Content */}
            <main className="flex-1 overflow-auto">
                <header className="h-16 border-b bg-card flex items-center justify-between px-6 sticky top-0 z-10">
                    <h2 className="text-lg font-semibold">{getPageTitle()}</h2>
                    <div className="flex items-center gap-4">
                        <Button variant="ghost" size="sm" className="relative" onClick={() => navigate('/notifications')}>
                            <Bell className="h-5 w-5" />
                            {unreadCount > 0 && <span className="absolute top-1 right-1 h-2 w-2 bg-red-500 rounded-full"></span>}
                        </Button>
                        <div className="h-8 w-8 rounded-full bg-primary/20 flex items-center justify-center">
                            <User className="h-5 w-5 text-primary" />
                        </div>
                    </div>
                </header>

                <div className="p-6">
                    <Outlet />
                </div>
            </main>
        </div>
    );
};

export default DashboardLayout;
