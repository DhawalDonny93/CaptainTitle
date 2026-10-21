import { useEffect, useRef, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { authService } from '../services/authService'
import { useAuth } from '../contexts/AuthContext'

interface SessionManagerProps {
  children: React.ReactNode
  idleTimeout?: number // in minutes
}

export default function SessionManager({ 
  children, 
  idleTimeout = 30 // 30 minutes default
}: SessionManagerProps) {
  const navigate = useNavigate()
  const { isAuthenticated } = useAuth()
  const timeoutRef = useRef<number | null>(null)
  const lastActivityRef = useRef<number>(Date.now())
  const isLoggingOutRef = useRef(false)

  const logout = useCallback(async () => {
    // Prevent multiple logout calls
    if (isLoggingOutRef.current) return
    isLoggingOutRef.current = true

    try {
      await authService.logout()
    } catch (error) {
      console.error('Logout error:', error)
    } finally {
      navigate('/login?reason=session_expired')
      isLoggingOutRef.current = false
    }
  }, [navigate])

  useEffect(() => {
    // Only track session for authenticated users
    if (!isAuthenticated) {
      // Clear any existing timers when user logs out
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current)
        timeoutRef.current = null
      }
      return
    }

    const startTimer = () => {
      // Clear existing timer
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current)
      }

      // Only set timer if user is authenticated
      if (!authService.isAuthenticated()) return

      // Set logout timer
      const timeoutMs = idleTimeout * 60 * 1000
      timeoutRef.current = window.setTimeout(() => {
        logout()
      }, timeoutMs) as unknown as number
    }

    const handleActivity = () => {
      // Only track activity if authenticated
      if (!authService.isAuthenticated()) return
      lastActivityRef.current = Date.now()
      startTimer()
    }

    const handleVisibilityChange = () => {
      if (!document.hidden && authService.isAuthenticated()) {
        // Tab is visible again, check if session expired
        const now = Date.now()
        const elapsed = now - lastActivityRef.current
        const maxIdle = idleTimeout * 60 * 1000

        if (elapsed > maxIdle) {
          logout()
        } else {
          startTimer()
        }
      }
    }

    // Activity events to track
    const events = ['mousedown', 'keydown', 'scroll', 'touchstart', 'click']

    // Add event listeners
    events.forEach(event => {
      document.addEventListener(event, handleActivity, { passive: true })
    })
    document.addEventListener('visibilitychange', handleVisibilityChange)

    // Initialize timer
    startTimer()

    // Cleanup
    return () => {
      events.forEach(event => {
        document.removeEventListener(event, handleActivity)
      })
      document.removeEventListener('visibilitychange', handleVisibilityChange)
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current)
        timeoutRef.current = null
      }
    }
  }, [idleTimeout, logout, isAuthenticated])

  return <>{children}</>
}
