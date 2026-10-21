import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  TrendingUp,
  CheckCircle2,
  BarChart3,
  PenTool,
  FileSearch,
  Circle,
  ArrowRight,
  Sparkles,
  Activity,
  Target,
} from 'lucide-react'
import { useAuth } from '../contexts/AuthContext'
import { useProject } from '../contexts/ProjectContext'
import visibilityService, { DashboardOverview } from '../services/visibilityService'
import taskService, { UserTask } from '../services/taskService'
import { LoadingPage } from '../components/ui/LoadingSpinner'

export default function Dashboard() {
  const navigate = useNavigate()
  const { user } = useAuth()
  const { selectedProject } = useProject()
  const [dashboardData, setDashboardData] = useState<DashboardOverview | null>(null)
  const [tasks, setTasks] = useState<UserTask[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (selectedProject?.id) {
      loadDashboardData()
      loadTasks()
    }
  }, [selectedProject])

  const loadDashboardData = async () => {
    if (!selectedProject?.id) return
    try {
      setLoading(true)
      const data = await visibilityService.getDashboardOverview(selectedProject.id)
      setDashboardData(data)
    } catch (err) {
      console.error('Error loading dashboard:', err)
    } finally {
      setLoading(false)
    }
  }

  const loadTasks = async () => {
    if (!selectedProject?.id) return
    try {
      const data = await taskService.getTasks(selectedProject.id)
      setTasks(data.filter(t => t.status !== 'Completed').slice(0, 6))
    } catch (err) {
      console.error('Error loading tasks:', err)
    }
  }

  const handleToggleTask = async (id: number) => {
    try {
      await taskService.completeTask(id)
      await loadTasks()
    } catch (err) {
      console.error('Failed to toggle task:', err)
    }
  }

  const getGreeting = () => {
    const hour = new Date().getHours()
    if (hour < 12) return 'Good morning'
    if (hour < 18) return 'Good afternoon'
    return 'Good evening'
  }

  const getTaskCategoryColor = (sourceUrl?: string) => {
    if (sourceUrl?.includes('technical')) return 'text-red-600'
    if (sourceUrl?.includes('site') || sourceUrl?.includes('audit')) return 'text-orange-600'
    if (sourceUrl?.includes('platform')) return 'text-blue-600'
    return 'text-gray-600'
  }
  
  const getTaskCategory = (sourceUrl?: string) => {
    if (sourceUrl?.includes('technical')) return 'Technical'
    if (sourceUrl?.includes('site') || sourceUrl?.includes('audit')) return 'Site Health'
    if (sourceUrl?.includes('platform')) return 'Platform'
    return 'General'
  }

  const previousScore = 13.3
  const currentScore = 14.3

  const quickNavCards = [
    {
      title: 'Monitor Analytics',
      description: 'Track visibility and traffic across AI platforms',
      icon: BarChart3,
      color: 'bg-orange-500',
      textColor: 'text-white',
      action: () => navigate('/visibility-dashboards'),
    },
    {
      title: 'Create Content',
      description: 'Generate articles optimized for AI search',
      icon: PenTool,
      color: 'bg-gray-100',
      action: () => {
        if (selectedProject?.id) {
          navigate(`/projects/${selectedProject.id}/content/create`)
        }
      },
    },
    {
      title: 'Audit your Website',
      description: 'Find and fix issues holding back performance',
      icon: FileSearch,
      color: 'bg-gray-100',
      action: () => {
        if (selectedProject?.id) {
          navigate(`/projects/${selectedProject.id}/audits`)
        }
      },
    },
  ]

  if (loading) {
    return <LoadingPage minHeight="h-96" />
  }

  const stats = [
    {
      title: 'AI Visibility',
      value: dashboardData?.overallVisibilityScore?.toString() || '87',
      change: '+12%',
      icon: TrendingUp,
      trend: 'up',
    },
    {
      title: 'Citations',
      value: dashboardData?.totalCitations?.toString() || '142',
      change: '+23',
      icon: Activity,
      trend: 'up',
    },
    {
      title: 'Opportunities',
      value: dashboardData?.totalOpportunities?.toString() || '38',
      change: '+8',
      icon: CheckCircle2,
      trend: 'up',
    },
    {
      title: 'Prompts',
      value: dashboardData?.totalPrompts?.toString() || '3',
      change: 'Active',
      icon: Target,
      trend: 'neutral',
    },
  ]

  return (
    <div className="max-w-6xl mx-auto">
      {/* Greeting Header */}
      <div className="mb-6">
        <div className="flex items-center gap-3">
          <div className="w-11 h-11 bg-gray-900 rounded-xl flex items-center justify-center">
            <Sparkles className="w-5 h-5 text-white" />
          </div>
          <div>
            <h1 className="text-xl font-bold text-gray-900">{getGreeting()}, {user?.firstName || 'there'}.</h1>
            <p className="text-xs text-gray-600">Back for more? Good. There's work to do.</p>
          </div>
        </div>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-3 mb-6">
        {stats.map((stat, index) => (
          <div
            key={index}
            className="bg-white rounded-xl p-4 border border-gray-200 hover:shadow-sm transition-all"
          >
            <div className="flex items-start justify-between mb-2">
              <div className="w-8 h-8 bg-gray-900 rounded-lg flex items-center justify-center text-white">
                <stat.icon className="w-4 h-4" />
              </div>
              {stat.trend === 'up' && (
                <div className="px-1.5 py-0.5 bg-green-50 text-green-600 rounded text-[10px] font-medium">
                  ↑
                </div>
              )}
            </div>
            <p className="text-xs text-gray-500 mb-0.5">{stat.title}</p>
            <p className="text-xl font-bold text-gray-900">{stat.value}</p>
            <p className="text-[10px] text-gray-500 mt-0.5">{stat.change}</p>
          </div>
        ))}
      </div>

      {/* Quick Navigation */}
      <div className="mb-6">
        <h2 className="text-xs font-medium text-gray-500 mb-3">Quick navigation</h2>
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-3">
          {quickNavCards.map((card, index) => (
            <button
              key={index}
              onClick={card.action}
              className={`${card.color} ${card.textColor || 'text-gray-900'} rounded-xl p-4 text-left hover:shadow-sm transition-all group`}
            >
              <div className="flex items-start justify-between mb-2">
                <div className={`w-8 h-8 ${card.textColor ? 'bg-white/20' : 'bg-white'} rounded-lg flex items-center justify-center`}>
                  <card.icon className={`w-4 h-4 ${card.textColor || 'text-gray-700'}`} />
                </div>
              </div>
              <h3 className="text-sm font-bold mb-0.5">{card.title}</h3>
              <p className={`text-xs ${card.textColor ? 'text-white/80' : 'text-gray-600'} line-clamp-2`}>
                {card.description}
              </p>
            </button>
          ))}
        </div>
      </div>

      {/* Your Briefing */}
      <div className="mb-6">
        <div className="flex items-center justify-between mb-2">
          <h2 className="text-xs font-medium text-gray-500">Your briefing</h2>
          <span className="text-[10px] text-gray-400">Just now</span>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-4">
          <p className="text-sm text-gray-900 leading-relaxed">
            Your visibility increased from {previousScore}% to {currentScore}% 
            <span className="inline-flex items-center gap-0.5 text-green-600 font-medium">
              +{((currentScore - previousScore) / previousScore * 100).toFixed(0)}%
              <TrendingUp className="w-3 h-3" />
            </span>
            {' '}—that's a solid gain. However, the critical issue of 'No prominent call-to-action' on your homepage is likely hindering further growth.
          </p>
          <button
            onClick={() => {
              if (selectedProject?.id) {
                navigate(`/projects/${selectedProject.id}/audits`)
              }
            }}
            className="mt-3 inline-flex items-center gap-1.5 text-xs text-gray-600 hover:text-gray-900 transition-colors group"
          >
            <span className="font-medium">Fix the call-to-action</span>
            <ArrowRight className="w-3 h-3 group-hover:translate-x-0.5 transition-transform" />
          </button>
        </div>
      </div>

      {/* Today's Priorities */}
      <div>
        <div className="flex items-center justify-between mb-2">
          <h2 className="text-xs font-medium text-gray-500">Today's priorities</h2>
          <span className="text-[10px] text-gray-400">{tasks.length} items</span>
        </div>
        <div className="space-y-1.5">
          {tasks.length === 0 ? (
            <div className="bg-white rounded-xl border border-gray-200 p-6 text-center">
              <CheckCircle2 className="w-8 h-8 text-gray-300 mx-auto mb-2" />
              <p className="text-sm text-gray-600">No pending tasks. Great job!</p>
            </div>
          ) : (
            tasks.map((task) => (
              <div
                key={task.id}
                className="bg-white rounded-xl border border-gray-200 p-3 hover:shadow-sm transition-all group"
              >
                <div className="flex items-start gap-3">
                  <button
                    onClick={() => handleToggleTask(task.id)}
                    className="mt-0.5 text-gray-300 hover:text-gray-600 transition-colors"
                  >
                    {task.status === 'Completed' ? (
                      <CheckCircle2 className="w-4 h-4 text-green-600" />
                    ) : (
                      <Circle className="w-4 h-4" />
                    )}
                  </button>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-start justify-between gap-3">
                      <div className="flex-1">
                        <div className="flex items-center gap-1.5 mb-0.5">
                          <span className={`inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-medium ${
                            task.priority === 'High' ? 'bg-red-50 text-red-700' :
                            task.priority === 'Medium' ? 'bg-yellow-50 text-yellow-700' :
                            'bg-gray-50 text-gray-600'
                          }`}>
                            {task.priority === 'High' ? 'EIN-001' : 'Suggested'}
                          </span>
                          <Circle className="w-0.5 h-0.5 fill-current text-gray-300" />
                          <span className="text-xs font-medium text-gray-900">{task.title}</span>
                        </div>
                        {task.description && (
                          <p className="text-xs text-gray-500 line-clamp-1">{task.description}</p>
                        )}
                      </div>
                      <span className={`text-[10px] font-medium ${getTaskCategoryColor(task.sourceUrl)}`}>
                        {getTaskCategory(task.sourceUrl)}
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            ))  )}
        </div>
        {tasks.length > 0 && (
          <button
            onClick={() => navigate('/todo-tasks')}
            className="mt-3 text-xs text-gray-600 hover:text-gray-900 font-medium transition-colors"
          >
            View all tasks →
          </button>
        )}
      </div>

    </div>
  )
}
