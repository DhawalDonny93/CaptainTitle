import { createContext, useContext, useState, ReactNode } from 'react'
import WaitlistModal from '../components/WaitlistModal'

interface WaitlistContextType {
  openWaitlist: () => void
}

const WaitlistContext = createContext<WaitlistContextType | undefined>(undefined)

export function WaitlistProvider({ children }: { children: ReactNode }) {
  const [open, setOpen] = useState(false)

  return (
    <WaitlistContext.Provider value={{ openWaitlist: () => setOpen(true) }}>
      {children}
      <WaitlistModal open={open} onClose={() => setOpen(false)} />
    </WaitlistContext.Provider>
  )
}

export function useWaitlist() {
  const ctx = useContext(WaitlistContext)
  if (!ctx) throw new Error('useWaitlist must be used within WaitlistProvider')
  return ctx
}
