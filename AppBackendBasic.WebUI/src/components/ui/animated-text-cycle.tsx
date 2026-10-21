import { useState, useEffect, useRef, useCallback } from "react"
import { motion, AnimatePresence } from "framer-motion"

interface TextItem {
  word: string
  color?: string
  logo?: string
  isImageLogo?: boolean
}

interface AnimatedTextCycleProps {
  items: (string | TextItem)[]
  interval?: number
  className?: string
}

export default function AnimatedTextCycle({
  items = [],
  interval = 5000,
  className = "",
}: AnimatedTextCycleProps) {
  const [currentIndex, setCurrentIndex] = useState(0)
  const [width, setWidth] = useState("auto")
  const measureRef = useRef<HTMLSpanElement>(null)
  // Pre-measured widths for all items, so width updates are synchronous
  const widthsRef = useRef<number[]>([])

  const normalizedItems = (items || []).map(item =>
    typeof item === 'string' ? { word: item } : item
  )

  // Measure all words once after mount (and after images settle)
  const measureAll = useCallback(() => {
    if (!measureRef.current) return
    const els = measureRef.current.children
    const measured: number[] = []
    for (let i = 0; i < els.length; i++) {
      measured.push(els[i].getBoundingClientRect().width + 4)
    }
    widthsRef.current = measured
    // Set initial width synchronously
    if (measured[0] !== undefined) setWidth(`${measured[0]}px`)
  }, [])

  useEffect(() => {
    // Measure after a short delay so image logos have loaded
    const t = setTimeout(measureAll, 150)
    return () => clearTimeout(t)
  }, [measureAll])

  // On index change, update width immediately from the pre-measured cache
  useEffect(() => {
    const w = widthsRef.current[currentIndex]
    if (w !== undefined) setWidth(`${w}px`)
  }, [currentIndex])

  useEffect(() => {
    const timer = setInterval(() => {
      setCurrentIndex(prev => (prev + 1) % normalizedItems.length)
    }, interval)
    return () => clearInterval(timer)
  }, [interval, normalizedItems.length])

  const containerVariants = {
    hidden: { y: -20, opacity: 0, filter: "blur(8px)" },
    visible: {
      y: 0, opacity: 1, filter: "blur(0px)",
      transition: { duration: 0.35, ease: "easeOut" },
    },
    exit: {
      y: 20, opacity: 0, filter: "blur(8px)",
      transition: { duration: 0.25, ease: "easeIn" },
    },
  }

  if (!normalizedItems.length) return null

  const currentItem = normalizedItems[currentIndex]

  const renderLogo = (item: TextItem) => {
    if (!item.logo) return null
    if (item.isImageLogo) {
      return (
        <img
          src={item.logo}
          alt={item.word}
          className="inline-block mr-1.5 object-contain flex-shrink-0"
          style={{ width: '1em', height: '1em', verticalAlign: 'middle' }}
        />
      )
    }
    return <span className="mr-1 align-middle">{item.logo}</span>
  }

  return (
    <>
      {/* Hidden measurement span — all items rendered side-by-side so we can read individual widths */}
      <span
        ref={measureRef}
        aria-hidden="true"
        style={{ position: 'absolute', visibility: 'hidden', pointerEvents: 'none', whiteSpace: 'nowrap', display: 'block' }}
      >
        {normalizedItems.map((item, i) => (
          <span key={i} className={`font-bold ${className}`} style={{ display: 'inline-flex', alignItems: 'baseline' }}>
            {item.logo && item.isImageLogo ? (
              <img src={item.logo} alt="" style={{ width: '1em', height: '1em', marginRight: '0.375rem', verticalAlign: 'middle', objectFit: 'contain' }} />
            ) : item.logo ? (
              <span className="mr-1">{item.logo}</span>
            ) : null}
            {item.word}
          </span>
        ))}
      </span>

      {/* Width wrapper — instant snap, no animation delay */}
      <motion.span
        className="relative inline-block"
        animate={{ width }}
        transition={{ type: "tween", ease: "easeInOut", duration: 0.18 }}
        style={{ verticalAlign: "baseline" }}
      >
        <AnimatePresence mode="wait" initial={false}>
          <motion.span
            key={currentIndex}
            className={`inline-flex items-baseline font-bold ${className}`}
            variants={containerVariants}
            initial="hidden"
            animate="visible"
            exit="exit"
            style={{ whiteSpace: "nowrap", color: currentItem?.color || "inherit" }}
          >
            {renderLogo(currentItem)}
            {currentItem?.word}
          </motion.span>
        </AnimatePresence>
      </motion.span>
    </>
  )
}
