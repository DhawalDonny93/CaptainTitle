import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Smartphone, Monitor, Tablet, MapPin, Clock, AlertCircle, Shield } from 'lucide-react'
import axios from 'axios'
import { authService } from '../services/authService'

const API_BASE_URL = import.meta.env.VITE_API_URL

interface LoginSession {
  id: string
  deviceModel: string
  platform: string
  appVersion: string
  createdAt: string
  lastActiveAt: string
  ipAddress?: string
  location?: string
  isCurrent: boolean
}

export default function LoginHistory() {
  const navigate = useNavigate()
  const [sessions, setSessions] = useState<LoginSession[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    fetchLoginHistory()
  }, [])

  const fetchLoginHistory = async () => {
    setLoading(true)
    setError('')

    try {
      const response = await axios.get(`${API_BASE_URL}/user/sessions`, {
        withCredentials: true,
        headers: {
          Authorization: `Bearer ${authService.getAccessToken()}`,
        },
      })

      if (response.data.success) {
        setSessions(response.data.sessions || [])
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load login history')
    } finally {
      setLoading(false)
    }
  }

  const handleRevokeSession = async (sessionId: string) => {
    if (!window.confirm('Are you sure you want to revoke this session? The device will be logged out.')) {
      return
    }

    try {
      await axios.delete(`${API_BASE_URL}/user/sessions/${sessionId}`, {
        withCredentials: true,
        headers: {
          Authorization: `Bearer ${authService.getAccessToken()}`,
        },
      })

      // Refresh the list
      fetchLoginHistory()
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to revoke session')
    }
  }

  const getDeviceIcon = (platform: string) => {
    const lower = platform?.toLowerCase() || ''
    if (lower.includes('ios') || lower.includes('iphone')) {
      return <Smartphone className="w-6 h-6" />
    } else if (lower.includes('android')) {
      return <Smartphone className="w-6 h-6" />
    } else if (lower.includes('ipad') || lower.includes('tablet')) {
      return <Tablet className="w-6 h-6" />
    } else {
      return <Monitor className="w-6 h-6" />
    }
  }

  const formatDate = (dateString: string) => {
    const date = new Date(dateString)
    const now = new Date()
    const diffMs = now.getTime() - date.getTime()
    const diffMins = Math.floor(diffMs / 60000)
    const diffHours = Math.floor(diffMs / 3600000)
    const diffDays = Math.floor(diffMs / 86400000)

    if (diffMins < 1) return 'Just now'
    if (diffMins < 60) return `${diffMins} minute${diffMins > 1 ? 's' : ''} ago`
    if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`
    if (diffDays < 7) return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`
    
    return date.toLocaleDateString('en-US', { 
      month: 'short', 
      day: 'numeric', 
      year: date.getFullYear() !== now.getFullYear() ? 'numeric' : undefined 
    })
  }

  return (
    <div className="container mx-auto max-w-4xl space-y-6">
      {/* Security Notice */}
      <div className="bg-blue-50 border border-blue-200 rounded-2xl p-4 flex items-start gap-3">
        <Shield className="w-5 h-5 text-blue-600 flex-shrink-0 mt-0.5" />
        <div className="text-sm">
          <p className="text-blue-900 font-medium mb-1">Security Tip</p>
          <p className="text-blue-800 font-light">
            If you see any suspicious activity, revoke the session immediately and change your password.
          </p>
        </div>
      </div>

      <div>
        {loading ? (
          <div className="flex items-center justify-center py-12">
            <svg className="w-8 h-8 animate-spin text-gray-400" fill="none" viewBox="0 0 24 24">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
          </div>
        ) : error ? (
          <div className="p-6 bg-red-50 border border-red-200 rounded-2xl flex items-start gap-3">
            <AlertCircle className="w-5 h-5 text-red-600 flex-shrink-0 mt-0.5" />
            <div>
              <p className="text-sm text-red-800 font-medium mb-1">Error Loading Sessions</p>
              <p className="text-sm text-red-700 font-light">{error}</p>
            </div>
          </div>
        ) : sessions.length === 0 ? (
          <div className="text-center py-12">
            <Monitor className="w-16 h-16 text-gray-300 mx-auto mb-4" />
            <p className="text-gray-600 font-light">No active sessions found</p>
          </div>
        ) : (
          <div className="space-y-4">
            {sessions.map((session) => (
              <div
                key={session.id}
                className={`p-6 border-2 rounded-2xl transition-all ${
                  session.isCurrent
                    ? 'border-black bg-gray-50'
                    : 'border-gray-200 hover:border-gray-300'
                }`}
              >
                <div className="flex items-start justify-between">
                  <div className="flex items-start gap-4 flex-1">
                    <div className="p-3 bg-white rounded-xl border border-gray-200">
                      {getDeviceIcon(session.platform)}
                    </div>
                    <div className="flex-1">
                      <div className="flex items-center gap-2 mb-1">
                        <h3 className="font-medium text-gray-900">
                          {session.deviceModel || session.platform || 'Unknown Device'}
                        </h3>
                        {session.isCurrent && (
                          <span className="px-2 py-0.5 bg-black text-white text-xs rounded-full font-medium">
                            Current
                          </span>
                        )}
                      </div>
                      <div className="space-y-1 text-sm text-gray-600 font-light">
                        <div className="flex items-center gap-2">
                          <Smartphone className="w-4 h-4" />
                          <span>{session.platform} • {session.appVersion}</span>
                        </div>
                        {session.location && (
                          <div className="flex items-center gap-2">
                            <MapPin className="w-4 h-4" />
                            <span>{session.location}</span>
                          </div>
                        )}
                        <div className="flex items-center gap-2">
                          <Clock className="w-4 h-4" />
                          <span>
                            Last active {formatDate(session.lastActiveAt)}
                          </span>
                        </div>
                        <div className="flex items-center gap-2 text-xs text-gray-500">
                          <span>Signed in {formatDate(session.createdAt)}</span>
                        </div>
                      </div>
                    </div>
                  </div>
                  {!session.isCurrent && (
                    <button
                      onClick={() => handleRevokeSession(session.id)}
                      className="px-4 py-2 text-sm font-medium text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                    >
                      Revoke
                    </button>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Actions */}
        {sessions.length > 0 && (
          <div className="mt-8 p-6 bg-gray-50 rounded-2xl">
            <h3 className="font-medium text-gray-900 mb-2">Need to secure your account?</h3>
            <p className="text-sm text-gray-600 font-light mb-4">
              If you don't recognize any of these sessions, we recommend changing your password immediately.
            </p>
            <button
              onClick={() => navigate('/forgot-password')}
              className="px-6 py-3 bg-black text-white rounded-full hover:bg-gray-800 transition-all font-medium"
            >
              Change Password
            </button>
          </div>
        )}
      </div>
    </div>
  )
}
