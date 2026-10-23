import { useState, useEffect } from 'react';
import notificationService, { Notification } from '../services/notificationService';
import { Card } from '@/components/ui/Card';
import { Loader2, Bell, Check, AlertCircle, Trash2 } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { toast } from '../utils/toast';

const NotificationsPage = () => {
    const [notifications, setNotifications] = useState<Notification[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [unreadOnly, setUnreadOnly] = useState(false);

    useEffect(() => {
        fetchNotifications();
    }, []);

    const fetchNotifications = async () => {
        try {
            setLoading(true);
            setError(null);
            const data = await notificationService.getNotifications(unreadOnly);
            setNotifications(data);
        } catch (err: any) {
            console.error("Failed to fetch notifications", err);
            setError(err.message || 'Failed to load notifications');
            toast.error('Failed to load notifications');
        } finally {
            setLoading(false);
        }
    };

    const handleMarkAllRead = async () => {
        try {
            await notificationService.markAllAsRead();
            setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));
            toast.success('All notifications marked as read');
        } catch (err: any) {
            console.error('Failed to mark notifications as read', err);
            toast.error(err.message || 'Failed to mark notifications as read');
        }
    };

    const handleMarkAsRead = async (id: number) => {
        try {
            await notificationService.markAsRead(id);
            setNotifications(prev => prev.map(n => n.id === id ? { ...n, isRead: true } : n));
        } catch (err: any) {
            console.error('Failed to mark notification as read', err);
            toast.error(err.message || 'Failed to mark notification as read');
        }
    };

    const handleDelete = async (id: number) => {
        try {
            await notificationService.deleteNotification(id);
            setNotifications(prev => prev.filter(n => n.id !== id));
            toast.success('Notification deleted');
        } catch (err: any) {
            console.error('Failed to delete notification', err);
            toast.error(err.message || 'Failed to delete notification');
        }
    };

    useEffect(() => {
        fetchNotifications();
    }, [unreadOnly]);

    if (loading) return <div className="flex justify-center p-10"><Loader2 className="animate-spin" /></div>;

    if (error) {
        return (
            <div className="text-center p-10">
                <AlertCircle className="h-12 w-12 text-red-600 mx-auto mb-4" />
                <h2 className="text-xl font-bold text-gray-900 mb-2">Failed to Load Notifications</h2>
                <p className="text-gray-600 mb-4">{error}</p>
                <Button onClick={fetchNotifications}>Try Again</Button>
            </div>
        );
    }

    return (
        <div className="container mx-auto max-w-2xl space-y-6">
            <div className="flex justify-between items-center">
                <h1 className="text-2xl font-bold flex items-center gap-2">
                    <Bell className="h-6 w-6" /> Notifications
                </h1>
                <div className="flex items-center gap-2">
                    <Button 
                        variant="outline" 
                        size="sm" 
                        onClick={() => setUnreadOnly(!unreadOnly)}
                    >
                        {unreadOnly ? 'Show All' : 'Unread Only'}
                    </Button>
                    {notifications.some(n => !n.isRead) && (
                        <Button variant="outline" size="sm" onClick={handleMarkAllRead}>
                            <Check className="h-4 w-4 mr-2" /> Mark all read
                        </Button>
                    )}
                </div>
            </div>

            <div className="grid gap-3">
                {notifications.length === 0 ? (
                    <p className="text-center text-muted-foreground py-10">No notifications yet.</p>
                ) : (
                    notifications.map(notification => (
                        <Card key={notification.id} className={`p-4 ${!notification.isRead ? 'border-primary/50 bg-primary/5' : ''}`}>
                            <div className="flex justify-between items-start gap-4">
                                <div className="flex-1 cursor-pointer" onClick={() => !notification.isRead && handleMarkAsRead(notification.id)}>
                                    <h3 className="text-sm font-semibold">{notification.title}</h3>
                                    <p className="text-sm text-gray-600 mt-1">{notification.message}</p>
                                    <p className="text-xs text-muted-foreground mt-2">{new Date(notification.createdAt).toLocaleString()}</p>
                                </div>
                                <div className="flex items-start gap-2">
                                    <span className={`text-[10px] uppercase font-bold px-1.5 py-0.5 rounded whitespace-nowrap ${notification.type === 'Warning' ? 'bg-yellow-100 text-yellow-800' :
                                            notification.type === 'Error' ? 'bg-red-100 text-red-800' :
                                            notification.type === 'Success' ? 'bg-green-100 text-green-800' :
                                                'bg-butter text-cherry'
                                        }`}>
                                        {notification.type}
                                    </span>
                                    <button
                                        onClick={() => handleDelete(notification.id)}
                                        className="text-gray-400 hover:text-red-600 transition-colors"
                                    >
                                        <Trash2 className="h-4 w-4" />
                                    </button>
                                </div>
                            </div>
                        </Card>
                    ))
                )}
            </div>
        </div>
    );
};

export default NotificationsPage;
