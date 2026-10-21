import { useNavigate } from 'react-router-dom'
import { Atom } from 'lucide-react'

export default function Footer() {
  const navigate = useNavigate()

  return (
    <footer className="bg-black text-gray-400 py-16 px-6 border-t border-gray-900">
      <div className="max-w-7xl mx-auto">
        <div className="grid md:grid-cols-4 gap-12 mb-12">
          <div>
            <div className="flex items-center gap-2 mb-4">
              <div className="w-8 h-8 bg-gradient-to-br from-blue-600 to-purple-600 rounded-2xl flex items-center justify-center">
                <Atom className="w-5 h-5 text-white" />
              </div>
              <span className="text-lg font-semibold text-white">ProQuest AI</span>
            </div>
            <p className="text-sm font-light">Optimize for the AI era. Get recommended by LLMs.</p>
          </div>
          <div>
            <h3 className="font-medium text-white mb-4">Product</h3>
            <ul className="space-y-3 text-sm font-light">
              <li><button onClick={() => navigate('/features')} className="hover:text-white transition-colors">Features</button></li>
              <li><button onClick={() => navigate('/pricing')} className="hidden hover:text-white transition-colors">Pricing</button></li>
              <li><button onClick={() => navigate('/login')} className="hidden hover:text-white transition-colors">Get Started</button></li>
            </ul>
          </div>
          <div>
            <h3 className="font-medium text-white mb-4">Company</h3>
            <ul className="space-y-3 text-sm font-light">
              <li><button onClick={() => navigate('/about')} className="hover:text-white transition-colors">About</button></li>
              <li><a href="#" className="hidden hover:text-white transition-colors">Blog</a></li>
              <li><a href="#" className="hidden hover:text-white transition-colors">Careers</a></li>
            </ul>
          </div>
          <div>
            <h3 className="font-medium text-white mb-4">Legal</h3>
            <ul className="space-y-3 text-sm font-light">
              <li><button onClick={() => window.open('/privacy', '_blank')} className="hover:text-white transition-colors">Privacy Policy</button></li>
              <li><button onClick={() => window.open('/terms', '_blank')} className="hover:text-white transition-colors">Terms of Service</button></li>
              <li><a href="#" className="hover:text-white transition-colors">Security</a></li>
            </ul>
          </div>
        </div>
        <div className="border-t border-gray-900 pt-8 flex flex-col md:flex-row justify-between items-center gap-4">
          <p className="text-sm font-light">&copy; {new Date().getFullYear()} ProQuest AI. All rights reserved.</p>
          <div className="flex gap-6">
            <a href="#" className="text-sm font-light hover:text-white transition-colors">Twitter</a>
            <a href="#" className="text-sm font-light hover:text-white transition-colors">Instagram</a>
            <a href="#" className="text-sm font-light hover:text-white transition-colors">LinkedIn</a>
          </div>
        </div>
      </div>
    </footer>
  )
}
