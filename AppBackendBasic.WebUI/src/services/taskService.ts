import { api } from '../lib/api';

export interface UserTask {
  id: number;
  userId: number;
  projectId?: number;
  title: string;
  description?: string;
  priority: 'Low' | 'Medium' | 'High' | 'Critical';
  status: 'Pending' | 'InProgress' | 'Completed' | 'Cancelled';
  dueDate?: Date;
  completedAt?: Date;
  sourceUrl?: string;
  createdAt: Date;
  updatedAt: Date;
}

export interface CreateTaskRequest {
  projectId?: number;
  title: string;
  description?: string;
  priority: 'Low' | 'Medium' | 'High' | 'Critical';
  dueDate?: Date;
  sourceUrl?: string;
}

export interface UpdateTaskRequest {
  title?: string;
  description?: string;
  priority?: 'Low' | 'Medium' | 'High' | 'Critical';
  status?: 'Pending' | 'InProgress' | 'Completed' | 'Cancelled';
  dueDate?: Date;
}

const taskService = {
  // Get all tasks for the current user
  async getTasks(projectId?: number): Promise<UserTask[]> {
    if (projectId) {
      // Get tasks by project
      const response = await api.get(`/usertasks/project/${projectId}`);
      return response.data;
    } else {
      // Get all user tasks
      const response = await api.get('/usertasks/user');
      return response.data;
    }
  },

  // Get task by ID
  async getTaskById(taskId: number): Promise<UserTask> {
    const response = await api.get(`/usertasks/${taskId}`);
    return response.data;
  },

  // Create a new task
  async createTask(request: CreateTaskRequest): Promise<UserTask> {
    const response = await api.post('/usertasks', request);
    return response.data;
  },

  // Update a task
  async updateTask(taskId: number, request: UpdateTaskRequest): Promise<UserTask> {
    const response = await api.put(`/usertasks/${taskId}`, request);
    return response.data;
  },

  // Mark task as complete
  async completeTask(taskId: number): Promise<UserTask> {
    const response = await api.put(`/usertasks/${taskId}/complete`);
    return response.data;
  },

  // Delete a task
  async deleteTask(taskId: number): Promise<void> {
    await api.delete(`/usertasks/${taskId}`);
  },
};

export default taskService;
