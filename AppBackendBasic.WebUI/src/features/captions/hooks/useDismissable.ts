import { useEffect, useRef, useState } from 'react'

/** Open/close state for a click-to-open menu: closes on a click outside the returned ref, or on Escape. */
export function useDismissable<T extends HTMLElement = HTMLDivElement>() {
  const [open, setOpen] = useState(false)
  const ref = useRef<T>(null)
  useEffect(() => {
    if (!open) return undefined
    const onMouseDown = (e: MouseEvent) => { if (!ref.current?.contains(e.target as Node)) setOpen(false) }
    const onKeyDown = (e: KeyboardEvent) => { if (e.key === 'Escape') setOpen(false) }
    window.addEventListener('mousedown', onMouseDown)
    window.addEventListener('keydown', onKeyDown)
    return () => { window.removeEventListener('mousedown', onMouseDown); window.removeEventListener('keydown', onKeyDown) }
  }, [open])
  return { open, setOpen, ref }
}
