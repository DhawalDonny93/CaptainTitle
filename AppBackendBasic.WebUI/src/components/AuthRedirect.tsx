import { useEffect, useRef } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'

/**
 * AuthRedirect component handles automatic redirects based on authentication and onboarding status
 * - Only redirects from login/verify-email pages (not landing page)
 * - Allows authenticated users to browse public pages
 * - Prevents redirect loops after logout
 */
export default function AuthRedirect() {
  const { user, isAuthenticated, loading } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const hasRedirected = useRef(false)
  const isRedirecting = useRef(false)
  const lastPathRef = useRef(location.pathname)

  useEffect(() => {
    // Don't redirect while loading
    if (loading) return

    // Reset redirect flag when authentication state changes or path changes
    if (!isAuthenticated) {
      hasRedirected.current = false
      isRedirecting.current = false
      return
    }

    // Reset redirect flag when path changes
    if (lastPathRef.current !== location.pathname) {
      hasRedirected.current = false
      lastPathRef.current = location.pathname
    }

    // Prevent multiple simultaneous redirects
    if (isRedirecting.current || hasRedirected.current) return
    
    if (!user) return

    const autoRedirectPages = ['/login', '/verify-email']
    const shouldAutoRedirect = autoRedirectPages.includes(location.pathname)

    if (shouldAutoRedirect) {
      hasRedirected.current = true
      isRedirecting.current = true
      
      // Check onboarding status from user object (from DB)
      const targetPath = !user.onboardingCompleted ? '/onboarding' : '/dashboard'
      
      navigate(targetPath, { replace: true })
      
      // Reset redirecting flag after navigation
      setTimeout(() => {
        isRedirecting.current = false
      }, 100)
    }
  }, [loading, isAuthenticated, user, location.pathname, navigate])

  return null // This component doesn't render anything
}
