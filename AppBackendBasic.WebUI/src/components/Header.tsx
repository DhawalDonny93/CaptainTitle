import { useState, useRef, useEffect } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { Eye, ChevronDown, FileText, Shield } from 'lucide-react'
import { useAuth } from '../contexts/AuthContext'
import { useWaitlist } from '../contexts/WaitlistContext'

const FREE_TOOLS = [
  { label: 'llms.txt Generator', path: '/tools/llms-txt-generator', icon: FileText, desc: 'Generate your llms.txt file' },
  { label: 'AI Crawlability Checker', path: '/tools/ai-crawlability-checker', icon: Shield, desc: 'Check who can crawl your site' },
]

export default function Header() {
  const navigate = useNavigate()
  const location = useLocation()
  const { isAuthenticated } = useAuth()
  const { openWaitlist } = useWaitlist()
  const [toolsOpen, setToolsOpen] = useState(false)
  const toolsRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (toolsRef.current && !toolsRef.current.contains(e.target as Node)) {
        setToolsOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClick)
    return () => document.removeEventListener('mousedown', handleClick)
  }, [])

  const navLinks = [
    { label: 'Home',     path: '/' },
    { label: 'About',    path: '/about' },
    { label: 'Features', path: '/features' },
  ]

  return (
    <div className="fixed top-0 left-0 right-0 z-50 flex justify-center pt-4 px-6 pointer-events-none">
      <nav
        className="pointer-events-auto flex items-center justify-between w-full max-w-4xl rounded-full px-3 py-2"
        style={{
          background: 'rgba(255, 255, 255, 0.05)',
          backdropFilter: 'blur(20px)',
          WebkitBackdropFilter: 'blur(20px)',
          boxShadow: '0 4px 32px 0 rgba(0,0,0,0.18), inset 0 1px 0 rgba(255,255,255,0.08)',
          border: '1px solid rgba(255,255,255,0.10)',
        }}
      >
        {/* Logo */}
        <button
          onClick={() => navigate('/')}
          className="flex items-center gap-2 pl-1 flex-shrink-0"
        >
          <div
            className="w-8 h-8 rounded-full flex items-center justify-center"
            style={{ background: 'rgba(0, 0, 0, 0.12)', border: '1px solid rgba(255,255,255,0.18)' }}
          >
            <Eye className="w-4 h-4 text-white" />
          </div>
          <span className="text-black font-bold text-sm tracking-tight select-none">ProQuest AI</span>
        </button>

        {/* Nav links */}
        <div className="hidden md:flex items-center gap-1">
          {navLinks.map(link => {
            const active = location.pathname === link.path
            return (
              <button
                key={link.path}
                onClick={() => navigate(link.path)}
                className="px-4 py-1.5 rounded-full text-sm font-medium transition-all"
                style={{
                  color: active ? '#ffffff' : 'rgba(1, 1, 1, 0.65)',
                  background: active ? 'rgba(0, 0, 0, 0.12)' : 'transparent',
                }}
                onMouseEnter={e => {
                  if (!active) (e.currentTarget as HTMLButtonElement).style.color = '#717171'
                }}
                onMouseLeave={e => {
                  if (!active) (e.currentTarget as HTMLButtonElement).style.color = 'rgba(0, 0, 0, 0.65)'
                }}
              >
                {link.label}
              </button>
            )
          })}

          {/* Free Tools dropdown */}
          <div ref={toolsRef} className="relative">
            <button
              onClick={() => setToolsOpen(o => !o)}
              className="flex items-center gap-1 px-4 py-1.5 rounded-full text-sm font-medium transition-all"
              style={{
                color: location.pathname.startsWith('/tools') ? '#ffffff' : 'rgba(1, 1, 1, 0.65)',
                background: location.pathname.startsWith('/tools') ? 'rgba(0, 0, 0, 0.12)' : 'transparent',
              }}
            >
              Free Tools
              <ChevronDown className={`w-3.5 h-3.5 transition-transform ${toolsOpen ? 'rotate-180' : ''}`} />
            </button>

            {toolsOpen && (
              <div
                className="absolute top-full left-1/2 -translate-x-1/2 mt-2 w-64 rounded-2xl overflow-hidden"
                style={{
                  background: 'rgba(255,255,255,0.96)',
                  backdropFilter: 'blur(20px)',
                  boxShadow: '0 8px 32px rgba(0,0,0,0.14)',
                  border: '1px solid rgba(0,0,0,0.08)',
                }}
              >
                {FREE_TOOLS.map(tool => (
                  <button
                    key={tool.path}
                    onClick={() => { navigate(tool.path); setToolsOpen(false) }}
                    className="w-full flex items-start gap-3 px-4 py-3.5 hover:bg-gray-50 transition-colors text-left"
                  >
                    <div className="w-7 h-7 rounded-lg bg-gray-100 flex items-center justify-center flex-shrink-0 mt-0.5">
                      <tool.icon className="w-3.5 h-3.5 text-gray-700" />
                    </div>
                    <div>
                      <p className="text-sm font-semibold text-gray-900">{tool.label}</p>
                      <p className="text-xs text-gray-500 mt-0.5">{tool.desc}</p>
                    </div>
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* CTA */}
        {isAuthenticated ? (
          <button
            onClick={() => navigate('/dashboard')}
            className="flex-shrink-0 px-5 py-2 rounded-full text-sm font-semibold bg-white text-gray-900 hover:bg-gray-100 transition-all"
          >
            Dashboard
          </button>
        ) : (
          <button
            onClick={openWaitlist}
            className="flex-shrink-0 px-5 py-2 rounded-full text-sm font-semibold bg-white text-gray-900 hover:bg-gray-100 transition-all"
          >
            Get Started
          </button>
        )}
      </nav>
    </div>
  )
}
