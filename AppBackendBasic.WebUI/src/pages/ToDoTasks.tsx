import { useState, useEffect } from 'react';
import { CheckCircle2, Circle, Plus, Trash2, Loader2, ChevronDown, ChevronRight, AlertCircle, ExternalLink } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import taskService, { UserTask } from '../services/taskService';
import { useProject } from '../contexts/ProjectContext';
import { toast } from '../utils/toast';

type TaskStatus = 'Pending' | 'InProgress' | 'Completed' | 'Cancelled';
type DisplayStatus = 'TO-DO' | 'IN PROGRESS' | 'DONE' | 'CANCELLED';
type ViewMode = 'list' | 'board';
type TabMode = 'all' | 'my' | 'activity';

const ToDoTasks = () => {
  const { selectedProject } = useProject();
  const [loading, setLoading] = useState(true);
  const [tasks, setTasks] = useState<UserTask[]>([]);
  const [newTask, setNewTask] = useState('');
  const [viewMode, setViewMode] = useState<ViewMode>('list');
  const [tabMode, setTabMode] = useState<TabMode>('all');
  const [error, setError] = useState<string | null>(null);
  const [expandedSections, setExpandedSections] = useState<Record<DisplayStatus, boolean>>({
    'TO-DO': true,
    'IN PROGRESS': true,
    'DONE': true,
    'CANCELLED': false,
  });

  useEffect(() => {
    fetchTasks();
  }, [selectedProject]);

  const fetchTasks = async () => {
    if (!selectedProject?.id) {
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const data = await taskService.getTasks(selectedProject.id);
      setTasks(data);
    } catch (err: any) {
      console.error('Failed to fetch tasks:', err);
      setError(err.message || 'Failed to load tasks');
      toast.error('Failed to load tasks');
    } finally {
      setLoading(false);
    }
  };

  const handleAddTask = async () => {
    if (!newTask.trim() || !selectedProject?.id) return;
    
    try {
      await taskService.createTask({
        projectId: selectedProject.id,
        title: newTask,
        priority: 'Medium',
        sourceUrl: window.location.href,
      });
      setNewTask('');
      toast.success('Task created successfully');
      await fetchTasks();
    } catch (err: any) {
      console.error('Failed to create task:', err);
      toast.error(err.message || 'Failed to create task');
    }
  };

  const handleToggleTask = async (id: number) => {
    try {
      await taskService.completeTask(id);
      toast.success('Task completed');
      await fetchTasks();
    } catch (err: any) {
      console.error('Failed to toggle task:', err);
      toast.error(err.message || 'Failed to complete task');
    }
  };

  const handleDeleteTask = async (id: number) => {
    if (!confirm('Are you sure you want to delete this task?')) return;
    try {
      await taskService.deleteTask(id);
      toast.success('Task deleted');
      await fetchTasks();
    } catch (err: any) {
      console.error('Failed to delete task:', err);
      toast.error(err.message || 'Failed to delete task');
    }
  };

  const handleStatusChange = async (taskId: number, newStatus: TaskStatus) => {
    try {
      await taskService.updateTask(taskId, {
        status: newStatus,
      });
      toast.success('Task status updated');
      await fetchTasks();
    } catch (err: any) {
      console.error('Failed to update task status:', err);
      toast.error(err.message || 'Failed to update task');
    }
  };

  const toggleSection = (status: DisplayStatus) => {
    setExpandedSections(prev => ({
      ...prev,
      [status]: !prev[status],
    }));
  };

  const mapStatusToDisplay = (status: TaskStatus): DisplayStatus => {
    switch (status) {
      case 'Pending': return 'TO-DO';
      case 'InProgress': return 'IN PROGRESS';
      case 'Completed': return 'DONE';
      case 'Cancelled': return 'CANCELLED';
      default: return 'TO-DO';
    }
  };

  const mapDisplayToStatus = (display: DisplayStatus): TaskStatus => {
    switch (display) {
      case 'TO-DO': return 'Pending';
      case 'IN PROGRESS': return 'InProgress';
      case 'DONE': return 'Completed';
      case 'CANCELLED': return 'Cancelled';
      default: return 'Pending';
    }
  };

  const getTasksByStatus = (displayStatus: DisplayStatus) => {
    const backendStatus = mapDisplayToStatus(displayStatus);
    return tasks.filter(task => task.status === backendStatus);
  };

  const getStatusColor = (status: DisplayStatus) => {
    switch (status) {
      case 'TO-DO': return 'text-gray-600';
      case 'IN PROGRESS': return 'text-yellow-600';
      case 'DONE': return 'text-green-600';
      case 'CANCELLED': return 'text-gray-400';
      default: return 'text-gray-600';
    }
  };

  const getStatusIcon = (status: DisplayStatus) => {
    switch (status) {
      case 'TO-DO': return <Circle className="w-4 h-4" />;
      case 'IN PROGRESS': return <div className="w-4 h-4 rounded-full border-2 border-yellow-600 border-t-transparent animate-spin" />;
      case 'DONE': return <CheckCircle2 className="w-4 h-4" />;
      case 'CANCELLED': return <Circle className="w-4 h-4" />;
      default: return <Circle className="w-4 h-4" />;
    }
  };

  const displayStatuses: DisplayStatus[] = ['TO-DO', 'IN PROGRESS', 'DONE', 'CANCELLED'];
  const backendStatuses: TaskStatus[] = ['Pending', 'InProgress', 'Completed', 'Cancelled'];
  const highPriorityCount = tasks.filter(t => t.priority === 'High' && t.status !== 'Completed').length;
  // const completedCount = tasks.filter(t => t.status === 'DONE').length;
  // const totalCount = tasks.length;

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
        <h2 className="text-xl font-bold text-gray-900 mb-2">Failed to Load Tasks</h2>
        <p className="text-gray-600 mb-4">{error}</p>
        <Button onClick={fetchTasks}>Try Again</Button>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-gray-900">Tasks</h1>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={() => setViewMode('list')}
            className={`px-3 py-1.5 text-sm rounded-lg transition-all ${viewMode === 'list' ? 'bg-gray-100 text-gray-900' : 'text-gray-600 hover:bg-gray-50'}`}
          >
            List
          </button>
          <button
            onClick={() => setViewMode('board')}
            className={`px-3 py-1.5 text-sm rounded-lg transition-all ${viewMode === 'board' ? 'bg-gray-100 text-gray-900' : 'text-gray-600 hover:bg-gray-50'}`}
          >
            Board
          </button>
        </div>
      </div>

      {/* Outstanding Tasks Alert */}
      {highPriorityCount > 0 && (
        <div className="bg-orange-50 border border-orange-200 rounded-xl p-4">
          <h3 className="text-sm font-semibold text-orange-900">You have {highPriorityCount} outstanding task{highPriorityCount > 1 ? 's' : ''}</h3>
          <p className="text-xs text-orange-700 mt-1">{highPriorityCount} high priority. Focus on what matters most.</p>
        </div>
      )}

      {/* Tabs */}
      <div className="flex items-center gap-4 border-b border-gray-200">
        <button
          onClick={() => setTabMode('all')}
          className={`pb-2 px-1 text-sm font-medium transition-all ${tabMode === 'all' ? 'border-b-2 border-gray-900 text-gray-900' : 'text-gray-600 hover:text-gray-900'}`}
        >
          All Tasks
        </button>
        <button
          onClick={() => setTabMode('my')}
          className={`pb-2 px-1 text-sm font-medium transition-all ${tabMode === 'my' ? 'border-b-2 border-gray-900 text-gray-900' : 'text-gray-600 hover:text-gray-900'}`}
        >
          My Tasks
        </button>
        <button
          onClick={() => setTabMode('activity')}
          className={`pb-2 px-1 text-sm font-medium transition-all ${tabMode === 'activity' ? 'border-b-2 border-gray-900 text-gray-900' : 'text-gray-600 hover:text-gray-900'}`}
        >
          Activity
        </button>
      </div>

      {/* Add Task Input */}
      <div className="flex gap-2">
        <Input
          value={newTask}
          onChange={(e) => setNewTask(e.target.value)}
          onKeyPress={(e) => e.key === 'Enter' && handleAddTask()}
          placeholder="Add a new task..."
          className="flex-1"
          disabled={!selectedProject}
        />
        <Button onClick={handleAddTask} disabled={!selectedProject}>
          <Plus className="h-4 w-4" />
        </Button>
      </div>

      {!selectedProject && (
        <div className="text-center py-8 text-gray-400 bg-gray-50 rounded-lg">
          <p>Please select a project to manage tasks</p>
        </div>
      )}

      {/* Task Sections */}
      {selectedProject && (
        <div className="space-y-2">
          {displayStatuses.map((status) => {
            const statusTasks = getTasksByStatus(status);
            const isExpanded = expandedSections[status];

            return (
              <div key={status} className="bg-white rounded-lg border border-gray-200">
                {/* Section Header */}
                <button
                  onClick={() => toggleSection(status)}
                  className="w-full flex items-center justify-between p-3 hover:bg-gray-50 transition-all"
                >
                  <div className="flex items-center gap-2">
                    {isExpanded ? <ChevronDown className="w-4 h-4 text-gray-400" /> : <ChevronRight className="w-4 h-4 text-gray-400" />}
                    <span className={`flex items-center gap-2 text-sm font-medium ${getStatusColor(status)}`}>
                      {getStatusIcon(status)}
                      {status}
                    </span>
                    <span className="text-xs text-gray-500">{statusTasks.length}</span>
                  </div>
                </button>

                {/* Task List */}
                {isExpanded && (
                  <div className="border-t border-gray-100">
                    {statusTasks.length === 0 ? (
                      <div className="p-4 text-center text-sm text-gray-400">No tasks</div>
                    ) : (
                      statusTasks.map((task) => (
                        <div
                          key={task.id}
                          className="flex items-center gap-2 px-3 py-2 border-b border-gray-50 last:border-0 hover:bg-gray-50 transition-all group"
                        >
                          {/* Checkbox */}
                          <button
                            onClick={() => handleToggleTask(task.id)}
                            className="flex-shrink-0"
                          >
                            {task.status === 'Completed' ? (
                              <CheckCircle2 className="w-4 h-4 text-green-600" />
                            ) : (
                              <Circle className="w-4 h-4 text-gray-400" />
                            )}
                          </button>

                          {/* Task ID */}
                          <span className={`text-xs font-mono flex-shrink-0 ${task.status === 'Completed' ? 'line-through text-gray-400' : 'text-gray-500'}`}>
                            TSK-{task.id.toString().padStart(4, '0')}
                          </span>

                          {/* Task Title */}
                          <p className={`text-sm flex-1 min-w-0 truncate ${task.status === 'Completed' ? 'line-through text-gray-400' : 'text-gray-900'}`} title={task.title}>
                            {task.title}
                          </p>

                          {/* Priority Badge */}
                          <span className={`text-[10px] px-2 py-0.5 rounded font-medium flex-shrink-0 ${
                            task.priority === 'High' ? 'bg-red-100 text-red-800' :
                            task.priority === 'Medium' ? 'bg-yellow-100 text-yellow-800' : 'bg-green-100 text-green-800'
                          }`}>
                            {task.priority}
                          </span>

                          {/* Date */}
                          <span className="text-xs text-gray-500 flex-shrink-0">
                            {new Date(task.createdAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
                          </span>

                          {/* Source URL Link */}
                          {task.sourceUrl && (
                            <a 
                              href={task.sourceUrl} 
                              target="_blank" 
                              rel="noopener noreferrer"
                              className="flex-shrink-0 p-1 hover:bg-blue-50 rounded transition-all"
                              onClick={(e) => e.stopPropagation()}
                              title="View source page"
                            >
                              <ExternalLink className="w-3.5 h-3.5 text-blue-600" />
                            </a>
                          )}

                          {/* Actions (visible on hover) */}
                          <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity flex-shrink-0">
                            <select
                              value={task.status}
                              onChange={(e) => handleStatusChange(task.id, e.target.value as TaskStatus)}
                              className="text-xs border border-gray-200 rounded px-2 py-1 bg-white"
                              onClick={(e) => e.stopPropagation()}
                            >
                              {backendStatuses.map(s => (
                                <option key={s} value={s}>{mapStatusToDisplay(s)}</option>
                              ))}
                            </select>
                            <button
                              onClick={(e) => {
                                e.stopPropagation();
                                handleDeleteTask(task.id);
                              }}
                              className="p-1 hover:bg-red-50 rounded transition-all"
                            >
                              <Trash2 className="w-3 h-3 text-gray-400 hover:text-red-600" />
                            </button>
                          </div>
                        </div>
                      ))
                    )}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};

export default ToDoTasks;
