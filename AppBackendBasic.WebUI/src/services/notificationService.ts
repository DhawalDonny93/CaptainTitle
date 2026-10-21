import { api } from '../lib/api';

export interface Notification {
  id: number;
  userId: number;
  type: 'Info' | 'Success' | 'Warning' | 'Error';
  title: string;
  message: string;
  isRead: boolean;
  actionUrl?: string;
  createdAt: Date;
  readAt?: Date;
}

export interface NotificationPreferences {
  emailNotifications: boolean;
  pushNotifications: boolean;
  smsNotifications: boolean;
  notificationTypes: string[];
}

const notificationService = {
  // Get all notifications for the current user
  async getNotifications(unreadOnly?: boolean): Promise<Notification[]> {
    const params = new URLSearchParams();
    if (unreadOnly) params.append('unreadOnly', 'true');
    
    const response = await api.get(`/notifications?${params.toString()}`);
    return response.data;
  },

  // Get unread notification count
  async getUnreadCount(): Promise<number> {
    const response = await api.get('/notifications/unread/count');
    return response.data;
  },

  // Mark notification as read
  async markAsRead(notificationId: number): Promise<void> {
    await api.put(`/notifications/${notificationId}/read`);
  },

  // Mark all notifications as read
  async markAllAsRead(): Promise<void> {
    await api.put('/notifications/read-all');
  },

  // Delete a notification
  async deleteNotification(notificationId: number): Promise<void> {
    await api.delete(`/notifications/${notificationId}`);
  },

  // Get notification preferences
  async getPreferences(): Promise<NotificationPreferences> {
    const response = await api.get('/notifications/preferences');
    return response.data;
  },

  // Update notification preferences
  async updatePreferences(preferences: NotificationPreferences): Promise<NotificationPreferences> {
    const response = await api.put('/notifications/preferences', preferences);
    return response.data;
  },
};

export default notificationService;
