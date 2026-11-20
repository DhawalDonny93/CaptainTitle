import { createContext, useContext, type ReactNode } from 'react'
import { createPortal } from 'react-dom'

/** The studio header lives in StudioShell, but its project-specific parts (back link, name, save state, Export) belong to the open
 *  project. The header exposes two DOM slots and the project screens portal their content into them. */
interface Slots { left: HTMLElement | null; right: HTMLElement | null }
const HeaderSlotsContext = createContext<Slots>({ left: null, right: null })
export const HeaderSlotsProvider = HeaderSlotsContext.Provider

export const HeaderLeft = ({ children }: { children: ReactNode }) => {
  const { left } = useContext(HeaderSlotsContext)
  return left ? createPortal(children, left) : null
}

export const HeaderRight = ({ children }: { children: ReactNode }) => {
  const { right } = useContext(HeaderSlotsContext)
  return right ? createPortal(children, right) : null
}
