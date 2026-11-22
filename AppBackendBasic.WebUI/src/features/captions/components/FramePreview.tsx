import { useEffect, useRef, type RefObject } from 'react'
import { whiteBalance } from '../lib/frame'

export const WHITE_BALANCE_FILTER_ID = 'ct-white-balance'

/**
 * Warmth/Tint as an SVG colour matrix the preview <video> references with filter: url(#ct-white-balance). It is the same per-channel gain the
 * export applies with ffmpeg's colorchannelmixer, in sRGB space like ffmpeg (not the linearRGB SVG default).
 */
export const WhiteBalanceFilter = ({ warmth, tint }: { warmth: number; tint: number }) => {
  const [r, g, b] = whiteBalance(warmth, tint)
  return (
    <svg width="0" height="0" style={{ position: 'absolute' }} aria-hidden="true">
      <filter id={WHITE_BALANCE_FILTER_ID} colorInterpolationFilters="sRGB">
        <feColorMatrix type="matrix" values={`${r} 0 0 0 0  0 ${g} 0 0 0  0 0 ${b} 0 0  0 0 0 1 0`} />
      </filter>
    </svg>
  )
}

interface BlurFillProps {
  videoRef: RefObject<HTMLVideoElement>
  /** frame aspect (width / height) */
  aspect: number
  /** CSS filter: the video's colour grading plus the blur */
  filter: string
}

/**
 * The "Blur fill" background: the playing video drawn every frame into a tiny canvas (cover-fit to the frame) and CSS-blurred. A second <video>
 * would have to be kept in sync; a 64px-wide canvas costs next to nothing. The export builds the same look with ffmpeg (see ReframeSteps).
 */
export const BlurFill = ({ videoRef, aspect, filter }: BlurFillProps) => {
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const width = 64
  const height = Math.max(1, Math.round(width / aspect))

  useEffect(() => {
    let raf = 0
    const draw = () => {
      const video = videoRef.current, canvas = canvasRef.current
      if (video && canvas && video.readyState >= 2 && video.videoWidth) {
        const ctx = canvas.getContext('2d')
        const scale = Math.max(canvas.width / video.videoWidth, canvas.height / video.videoHeight)
        const w = video.videoWidth * scale, h = video.videoHeight * scale
        ctx?.drawImage(video, (canvas.width - w) / 2, (canvas.height - h) / 2, w, h)
      }
      raf = requestAnimationFrame(draw)
    }
    raf = requestAnimationFrame(draw)
    return () => cancelAnimationFrame(raf)
  }, [videoRef])

  return <canvas ref={canvasRef} className="frame-blur-fill" width={width} height={height} style={{ filter }} aria-hidden="true" data-testid="frame-blur-fill" />
}
