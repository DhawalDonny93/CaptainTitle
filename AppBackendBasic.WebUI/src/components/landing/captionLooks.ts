import { useEffect, useState } from 'react'

/** Caption looks and timers shared by the illustrated landing-page mock-ups (text and shapes only, no real videos). */

export type Word = { t: string; em?: boolean }
export type Look = { font: string; color: string; emFont: string; emColor: string; emSize: number; size: number; upper?: boolean; marker?: boolean; glow?: boolean }

export const LOOKS: Record<string, Look> = {
  viral: { font: 'Poppins', color: '#FFFFFF', emFont: 'Anton', emColor: '#FF3B30', size: 18, emSize: 34, upper: true },
  cinematic: { font: 'Poppins', color: '#FFF7DB', emFont: 'Caveat', emColor: '#FFB800', size: 18, emSize: 32 },
  meme: { font: 'Poppins', color: '#FFFFFF', emFont: 'Permanent Marker', emColor: '#FFEB3B', size: 18, emSize: 30 },
  vogue: { font: 'Poppins', color: '#E4E4E7', emFont: 'Bebas Neue', emColor: '#FF8FC7', size: 18, emSize: 36 },
  glow: { font: 'Poppins', color: '#FFFFFF', emFont: 'Anton', emColor: '#FF2E2E', size: 18, emSize: 34, upper: true, glow: true },
  doodle: { font: 'Caveat', color: '#FFFFFF', emFont: 'Caveat', emColor: '#111111', size: 24, emSize: 28, marker: true },
}

export function useReducedMotion() {
  const [reduced, setReduced] = useState(false)
  useEffect(() => {
    const mq = window.matchMedia('(prefers-reduced-motion: reduce)')
    setReduced(mq.matches)
    const on = () => setReduced(mq.matches)
    mq.addEventListener('change', on)
    return () => mq.removeEventListener('change', on)
  }, [])
  return reduced
}

/** Steps an index 0..count-1 on a timer (stops while paused or when the visitor prefers reduced motion). */
export function useTicker(count: number, ms: number, paused = false) {
  const reduced = useReducedMotion()
  const [i, setI] = useState(0)
  useEffect(() => {
    if (paused || reduced) return
    const id = window.setInterval(() => setI((v) => (v + 1) % count), ms)
    return () => window.clearInterval(id)
  }, [count, ms, paused, reduced])
  return reduced ? count - 1 : i
}
