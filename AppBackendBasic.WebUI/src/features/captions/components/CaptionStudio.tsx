import { Fragment, useEffect, useMemo, useRef, useState } from "react";
import { BookmarkSimple, ChatCircle, DotsThreeVertical, DownloadSimple, FileText, FloppyDisk, FrameCorners, Heart, Play, Share, SpinnerGap } from "@phosphor-icons/react";
import { toast } from "@/utils/toast";
import { absoluteApiUrl as absoluteUrl, captionsApi, errorMessage, saveBlob } from "../captionsApi";
import { useCaptionCatalog } from "../catalogContext";
import { bakeWordStyle, cssFamily, DEFAULT_WORD_SIZE, DOODLE_MARKER, STREET_GLOW } from "../lib/styles";
import { playSfx } from "../lib/sfx";
import { ThemePanel, type ThemeTab } from "./ThemePanel";
import { TranscriptPanel } from "./TranscriptPanel";
import { Timeline } from "./Timeline";
import { LeftRail, type LeftTab } from "./LeftRail";
import { MediaPanel } from "./MediaPanel";
import { AudioPanel } from "./AudioPanel";
import { ElementsPanel } from "./ElementsPanel";
import { TextPanel } from "./TextPanel";
import { AdjustmentPanel } from "./AdjustmentPanel";
import { AiToolsPanel } from "./AiToolsPanel";
import { MomentPopover } from "./MomentPopover";
import type {
  CaptionElement, CaptionPosition, CaptionProject, CaptionTier, CaptionWord, KineticStyle, ProjectEditPayload, RenderQuality, ScriptMode, SfxMarker, TextStyle,
} from "../types";

type Tiers = Record<string, CaptionTier> | null;
type Handle = "tl" | "tc" | "tr" | "ml" | "mr" | "bl" | "bc" | "br";

// Mirrors backend/captions.py's _auto_line_breaks/_resolve_line_breaks: the
// automatic default (a tier change for a tiered template, the midpoint for
// a plain two-line template) with any word's explicit new_line (set by a
// manual merge/split edit) taking precedence over it.
function autoLineBreaks(groupWords: CaptionWord[], tiers: Tiers, captionLines: number): boolean[] {
  if (tiers) {
    let previousTier: string | null = null;
    return groupWords.map((word, index) => {
      const tier = tiers[word.emphasis] ? word.emphasis : "support";
      const brk = index > 0 && tier !== previousTier;
      previousTier = tier;
      return brk;
    });
  }
  if (captionLines === 2 && groupWords.length > 1) {
    const mid = Math.ceil(groupWords.length / 2);
    return groupWords.map((_, index) => index === mid);
  }
  return groupWords.map(() => false);
}

function splitIntoRows(groupWords: CaptionWord[], tiers: Tiers, captionLines: number) {
  const auto = autoLineBreaks(groupWords, tiers, captionLines);
  const breaks = groupWords.map((word, index) => word.new_line ?? auto[index]);
  const rows: CaptionWord[][] = [];
  let current: CaptionWord[] = [];
  groupWords.forEach((word, index) => {
    if (index > 0 && breaks[index]) {
      rows.push(current);
      current = [];
    }
    current.push(word);
  });
  if (current.length) rows.push(current);
  return rows;
}

const MIN_DISPLAY_SECONDS = 0.35;
const MAX_WORDS_PER_LINE = 3;
const MAX_GROUP_GAP = 0.4;
const SENTENCE_END_CHARS = ".!?।॥"; // includes Devanagari danda/double danda
// Percentages of the 1080x1920 canvas the whole caption block is centred on - mirrors the server's CaptionLayout.PositionDefaults exactly.
const POSITION_DEFAULTS = { top: { x: 50, y: 14 }, middle: { x: 50, y: 50 }, bottom: { x: 50, y: 80 } };
// Fixed CSS line-height for stacked caption rows - calibrated (not guessed) against the export's own native `\N` line spacing in libass: the
// multiplier that makes the browser's line-box leading match libass's native gap at every font-size pair tested (~1.46-1.50). Not an editable
// value, since nothing on the export side reads a line-height multiplier (libass's \N spacing is native, not computed).
const NATIVE_LINE_HEIGHT = 1.47;

// Per-handle resize behaviour: which sign of the mouse delta on each axis grows the box, and whether that axis is touched at all.
// Edge handles (tc/bc/ml/mr) touch exactly one axis; corner handles touch both. The box is resized symmetrically around its fixed centre, so a
// signed per-axis delta - never an unsigned distance - keeps a drag past the centre from "inverting" the box.
const RESIZE_HANDLES: Record<Handle, { x: number; y: number }> = {
  tl: { x: -1, y: -1 }, tc: { x: 0, y: -1 }, tr: { x: 1, y: -1 },
  ml: { x: -1, y: 0 }, mr: { x: 1, y: 0 },
  bl: { x: -1, y: 1 }, bc: { x: 0, y: 1 }, br: { x: 1, y: 1 },
};

// Mirrors the server's AssSubtitleWriter.BoldCapableFonts: only Poppins and Caveat are bundled with a real separate bold weight; asking the
// browser for font-weight:800 on a single-weight font would synthesize a bolder face that differs from what libass renders, so never ask for it.
function isBoldCapableFont(family: string) {
  return family.includes("Poppins") || family.includes("Caveat");
}

function hexToRgba(hex: string | null | undefined, opacity: number) {
  const value = (hex || "#000000").replace("#", "");
  const r = parseInt(value.slice(0, 2), 16) || 0;
  const g = parseInt(value.slice(2, 4), 16) || 0;
  const b = parseInt(value.slice(4, 6), 16) || 0;
  return `rgba(${r},${g},${b},${opacity})`;
}

// Mirrors the server's AssSubtitleWriter.ShadowOffset: clockwise degrees from straight down (0=down, 90=left, 180=up, 270=right).
function shadowOffset(angleDeg: number, distance: number): [number, number] {
  const radians = (angleDeg * Math.PI) / 180;
  return [distance * Math.sin(radians), distance * Math.cos(radians)];
}

function gcd(a: number, b: number): number { return b === 0 ? a : gcd(b, a % b); }
function aspectLabel(width: number, height: number) {
  const divisor = gcd(width, height) || 1;
  return `${width / divisor}:${height / divisor}`;
}

function endsSentence(word: CaptionWord) {
  const original = (word.original || "").trim();
  return !!original && SENTENCE_END_CHARS.includes(original[original.length - 1]);
}

// Mirrors the server's CaptionLayout.GroupWordsForDisplay: fast/short consecutive words get shown together instead of flashing past one at a
// time. With an explicit wordsPerCaption (not "Auto") always pack up to that many words. A sentence-ending word is always the last of its group.
// A word's own new_caption overrides all of that: true always starts a new group at that word (manual "Split"), false always keeps it in the
// previous group (manual "Merge up") even past maxWords / the gap / a sentence end.
// Both fixed templates need several words flowing per cue (Street Glow's headline + subline, Doodle Marker's flowing line), so they get more
// generous thresholds than the old rapid-fire single-word chips; an explicit wordsPerCaption still overrides via strict.
function groupWordsForDisplay(words: CaptionWord[], wordsPerCaption: number | null, theme: string) {
  const isFixedTemplate = theme === STREET_GLOW || theme === DOODLE_MARKER;
  const maxWords = wordsPerCaption || (isFixedTemplate ? 6 : MAX_WORDS_PER_LINE);
  const minDisplay = isFixedTemplate ? 1.5 : MIN_DISPLAY_SECONDS;
  const strict = wordsPerCaption != null;
  const groups: CaptionWord[][] = [];
  let index = 0;
  while (index < words.length) {
    const group = [words[index]];
    let groupEnd = words[index].end;
    let next = index + 1;
    while (next < words.length) {
      const nextWord = words[next];
      const forcedSplit = nextWord.new_caption === true;
      const forcedMerge = nextWord.new_caption === false;
      if (forcedSplit) break;
      if (!forcedMerge && !(group.length < maxWords && !endsSentence(group[group.length - 1])
            && nextWord.start - groupEnd < MAX_GROUP_GAP
            && (strict || groupEnd - words[index].start < minDisplay))) break;
      group.push(nextWord);
      groupEnd = nextWord.end;
      next += 1;
    }
    groups.push(group);
    index = next;
  }
  return groups;
}

interface StreetGlowOverlayProps {
  visibleWords: CaptionWord[]; scriptMode: ScriptMode; boxX: number; boxY: number; boxRotation: number; previewScale: number
  onMouseDown: (e: React.MouseEvent) => void
  onResize: (handle: Handle) => (e: React.MouseEvent) => void
}

// Street Glow is LINE-based (two fixed, independently-animated roles: a condensed glow headline + a cursive subline), not word-based - it never
// goes through the generic tiers-driven caption-overlay render (see the server's EmitStreetGlowGroup for the export side). Sizes/gap are fixed
// constants (72/44/-8, scaled by previewScale) mirrored from FixedTemplates.StreetGlow*, not read from word.font_size/word.color. Each line's
// key (its first word's index) changes exactly when the visible group changes, remounting the div so its CSS entrance animation replays per cue.
function StreetGlowOverlay({ visibleWords, scriptMode, boxX, boxY, boxRotation, previewScale, onMouseDown, onResize }: StreetGlowOverlayProps) {
  const rows = splitIntoRows(visibleWords, null, 2);
  const line1Words = rows[0] || [];
  const line2Words = rows[1] || [];
  const lineText = (rowWords: CaptionWord[], upper: boolean) => {
    const joined = rowWords.map((word) => (scriptMode === "native" && word.original) ? word.original : word.text).join(" ");
    return upper ? joined.toUpperCase() : joined;
  };
  return (
    <div className="caption-overlay street-glow-overlay"
      style={{ left: `${boxX}%`, top: `${boxY}%`, transform: `translate(-50%,-50%) rotate(${boxRotation}deg)` }}
      onMouseDown={onMouseDown} data-testid="live-caption-preview">
      <div className="street-glow-stack">
        <div key={`line1-${line1Words[0]?.index}`} className="street-glow-line1" style={{ fontSize: `${72 * previewScale}px` }}>{lineText(line1Words, true)}</div>
        {line2Words.length > 0 && <div key={`line2-${line2Words[0]?.index}`} className="street-glow-line2" style={{ fontSize: `${44 * previewScale}px`, marginTop: `${-8 * previewScale}px` }}>{lineText(line2Words, false)}</div>}
      </div>
      <div className="selection-box">
        {(["tl", "tc", "tr", "ml", "mr", "bl", "bc", "br"] as Handle[]).map((pos) => <i key={pos} className={`resize-handle handle-${pos}`} onMouseDown={onResize(pos)} data-testid={`caption-resize-handle-${pos}`} />)}
      </div>
    </div>
  );
}

interface CaptionStudioProps { initialProject: CaptionProject; onNew: () => void }

export const CaptionStudio = ({ initialProject, onNew }: CaptionStudioProps) => {
  const { themeById, fontById, sfxById } = useCaptionCatalog();
  const [project, setProject] = useState<CaptionProject>(initialProject);
  const [words, setWords] = useState(initialProject.words);
  const [theme, setTheme] = useState(initialProject.theme);
  const [fontId, setFontId] = useState(initialProject.font_id || "poppins");
  const [wordsPerCaption, setWordsPerCaption] = useState<number | null>(initialProject.words_per_caption ?? null);
  const [captionLines, setCaptionLines] = useState(initialProject.caption_lines || 1);
  const [captionPosition, setCaptionPosition] = useState<CaptionPosition>(initialProject.caption_position || "bottom");
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [themeTab, setThemeTab] = useState<ThemeTab>("templates");
  // Selecting a word from the Captions list or the bottom timeline should
  // land you straight on its edit controls - requiring a separate click
  // over to the right panel's Text tab every time was exactly the kind of
  // hidden-UI friction that made editing feel clunky. It should also seek
  // the video there, matching how a real script/transcript editor works -
  // clicking a line is how you navigate the footage, not just an editing
  // affordance.
  const selectWord = (index: number) => {
    setSelectedIndex(index);
    setThemeTab("text");
    const word = words[index];
    if (word && videoRef.current) {
      videoRef.current.currentTime = word.start;
      setCurrentTime(word.start);
    }
  };
  // "FOR THIS MOMENT": a single click on a word - from the Script Panel, the
  // Timeline, or directly on the canvas - seeks/selects it exactly as
  // before, and additionally opens the moment popover for it. anchorEl, when given, is the clicked chip/span itself - the popover opens right
  // next to IT instead of always next to the video frame.
  const openMoment = (index: number, anchorEl?: HTMLElement) => {
    selectWord(index);
    setMomentPopover(index);
    setMomentAnchor(anchorEl ? anchorEl.getBoundingClientRect() : null);
  };
  const [currentTime, setCurrentTime] = useState(0);
  const [playing, setPlaying] = useState(false);
  const [rendering, setRendering] = useState(false);
  const [relistening, setRelistening] = useState(false);
  // Short-lived signed media URL (the <video> element cannot send the bearer token); refreshed on error/expiry.
  const [mediaUrl, setMediaUrl] = useState<string | null>(null);
  const [mediaRetry, setMediaRetry] = useState(0);
  // Shown right after a direct drag/resize on the canvas box releases - the move/resize has ALREADY been committed to this caption by then,
  // so this is purely an optional one-tap shortcut to also broadcast it to every caption, not a gate. Dismissing it just closes it.
  const [scopePrompt, setScopePrompt] = useState(false);
  const [leftTab, setLeftTab] = useState<LeftTab>("captions");
  const [elements, setElements] = useState(initialProject.elements || []);
  const [selectedElementId, setSelectedElementId] = useState<string | null>(null);
  const [audioVolume, setAudioVolume] = useState(initialProject.audio_volume ?? 100);
  const [audioMuted, setAudioMuted] = useState(initialProject.audio_muted ?? false);
  const [brightness, setBrightness] = useState(initialProject.brightness ?? 0);
  const [contrast, setContrast] = useState(initialProject.contrast ?? 100);
  const [saturation, setSaturation] = useState(initialProject.saturation ?? 100);
  const [renderQuality, setRenderQuality] = useState<RenderQuality>(initialProject.render_quality ?? "high");
  const [sfxMarkers, setSfxMarkers] = useState<SfxMarker[]>(initialProject.sfx_markers || []);
  const [momentPopover, setMomentPopover] = useState<number | null>(null); // word index, or null when closed
  const [momentAnchor, setMomentAnchor] = useState<DOMRect | null>(null); // clicked chip/span's rect, or null to anchor by the video frame
  // Dual-script toggle: which text shows on canvas / Timeline / exports - the Romanized Hinglish word.text, or the native-script word.original.
  const [scriptMode, setScriptMode] = useState<ScriptMode>(initialProject.script_mode || "roman");
  // Project-wide Outline / Shadow / Glow / Background controls plus Bold / Italic - field names match the API 1:1 so the payload spreads straight through.
  const [textStyle, setTextStyle] = useState<TextStyle>({
    outline_enabled: initialProject.outline_enabled ?? false,
    outline_width: initialProject.outline_width ?? 2,
    outline_color: initialProject.outline_color || "#000000",
    shadow_enabled: initialProject.shadow_enabled ?? true,
    shadow_angle: initialProject.shadow_angle ?? 0,
    shadow_distance: initialProject.shadow_distance ?? 2,
    shadow_blur: initialProject.shadow_blur ?? 2,
    shadow_opacity: initialProject.shadow_opacity ?? 0.5,
    shadow_color: initialProject.shadow_color || "#000000",
    glow_enabled: initialProject.glow_enabled ?? false,
    glow_color: initialProject.glow_color ?? null,
    glow_blur: initialProject.glow_blur ?? 8,
    glow_opacity: initialProject.glow_opacity ?? 0.6,
    background_enabled: initialProject.background_enabled ?? false,
    background_color: initialProject.background_color || "#000000",
    background_opacity: initialProject.background_opacity ?? 0.6,
  });
  const updateTextStyle = (change: Partial<TextStyle>) => setTextStyle((current) => ({ ...current, ...change }));
  const [bold, setBold] = useState(initialProject.bold ?? true);
  const [italic, setItalic] = useState(initialProject.italic ?? false);
  const [reassigning, setReassigning] = useState(false);
  const [safeZone, setSafeZone] = useState(false);
  const [frameWidth, setFrameWidth] = useState(0);
  const [frameBox, setFrameBox] = useState({ w: 360, h: 640 });
  const videoRef = useRef<HTMLVideoElement>(null);
  const videoFrameRef = useRef<HTMLDivElement>(null);
  const stageRef = useRef<HTMLDivElement>(null);
  // Set true mid-drag once the cursor has actually moved - read (and reset) by a word span's onClick to swallow the spurious click a
  // drag-release fires on whatever word is still under the cursor (the caption box follows the mouse).
  const dragMovedRef = useRef(false);

  useEffect(() => {
    let cancelled = false;
    captionsApi.mediaUrl(project.project_id)
      .then((relative) => { if (!cancelled) setMediaUrl(absoluteUrl(relative)); })
      .catch(() => { if (!cancelled) setMediaUrl(null); });
    return () => { cancelled = true; };
  }, [project.project_id, mediaRetry]);

  // The 9:16 video box should be sized to the largest rectangle that fits
  // the actual available area (not a flat vh guess, which left a lot of
  // dead black space around a too-small box on most screens) - whichever
  // dimension is the binding constraint, width or height, depends on the
  // stage's own shape, so this has to be measured rather than assumed.
  useEffect(() => {
    const node = stageRef.current;
    if (!node || typeof ResizeObserver === "undefined") return undefined;
    const observer = new ResizeObserver((entries) => {
      const { width, height } = entries[0].contentRect;
      const unit = Math.max(0, Math.min(width / 9, height / 16));
      setFrameBox({ w: Math.floor(unit * 9), h: Math.floor(unit * 16) });
    });
    observer.observe(node);
    return () => observer.disconnect();
  }, []);

  // The <video> element's own volume tops out at 1.0 (100%) - boosting past
  // that only happens in the exported render's ffmpeg volume filter, not here.
  useEffect(() => {
    if (!videoRef.current) return;
    videoRef.current.volume = Math.min(1, Math.max(0, audioVolume / 100));
    videoRef.current.muted = audioMuted;
  }, [audioVolume, audioMuted]);

  // Caption/element font sizes are defined against the 1080x1920 canvas the
  // backend bakes into the ASS script (see captions.py's PlayResX/PlayResY),
  // so the preview has to know its own on-screen pixel width to scale a
  // font_size down to a matching on-screen size - a fixed guess here was the
  // actual bug behind "caption looks fine in the editor but tiny in the
  // rendered video": the preview box's real size depends on the viewer's
  // window/screen, not a constant.
  useEffect(() => {
    const node = videoFrameRef.current;
    if (!node || typeof ResizeObserver === "undefined") return undefined;
    const observer = new ResizeObserver((entries) => setFrameWidth(entries[0].contentRect.width));
    observer.observe(node);
    return () => observer.disconnect();
  }, []);
  const previewScale = frameWidth ? frameWidth / 1080 : 0.62;

  // The video element's own timeupdate event fires at a coarse, inconsistent
  // rate (often just 4-10x/sec), which visibly lags captions behind the
  // actual audio. Poll currentTime every animation frame instead while
  // playing, for tight word-level sync.
  useEffect(() => {
    if (!playing) return undefined;
    let frameId = 0;
    const tick = () => {
      if (videoRef.current) setCurrentTime(videoRef.current.currentTime);
      frameId = requestAnimationFrame(tick);
    };
    frameId = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(frameId);
  }, [playing]);

  // Fire each SFX marker's (synthesized) tone once as playback crosses its
  // timestamp - a separate AudioContext mixed alongside the <video>'s own
  // audio, so the main video audio is never touched. lastTimeRef detects a
  // seek backward (or a loop) to let markers re-trigger on a later pass,
  // and the played set keeps a forward pass from re-firing the same one.
  const playedSfxRef = useRef(new Set<string>());
  const lastTimeRef = useRef(0);
  useEffect(() => {
    if (currentTime < lastTimeRef.current - 0.25) playedSfxRef.current.clear();
    lastTimeRef.current = currentTime;
    if (!playing) return;
    for (const marker of sfxMarkers) {
      if (playedSfxRef.current.has(marker.id)) continue;
      if (currentTime >= marker.time && currentTime - marker.time < 0.5) {
        playedSfxRef.current.add(marker.id);
        playSfx(sfxById[marker.type]);
      }
    }
  }, [currentTime, playing, sfxMarkers, sfxById]);

  const activeIndex = words.findIndex((word) => currentTime >= word.start && currentTime <= word.end);
  const wordGroups = useMemo(() => groupWordsForDisplay(words, wordsPerCaption, theme), [words, wordsPerCaption, theme]);
  // The Captions list and the Timeline's "Lines" view both show the same
  // cue grouping the live preview and the final render use, each word
  // paired with its real position in `words` (not reconstructed by identity
  // lookup) so selecting/editing a word from either place stays correct
  // even after words get deleted (e.g. by the AI tools filler cleanup).
  const indexedWordGroups = useMemo(() => {
    let position = 0;
    return wordGroups.map((group) => group.map((word) => ({ word, index: position++ })));
  }, [wordGroups]);
  const visibleWords = useMemo(() => {
    const group = wordGroups.find((candidate) =>
      currentTime >= candidate[0].start && currentTime <= candidate[candidate.length - 1].end);
    return group || [];
  }, [wordGroups, currentTime]);
  const anchorWord = visibleWords[0];
  const tiers: Tiers = themeById[theme]?.tiers ?? null;
  // A real browser already blurs text-shadow independently of the glyph fill (unlike libass's single \blur), so the preview needs no two-layer
  // trick: one WebkitTextStroke (crisp) + one text-shadow (independently blurred) gives the same crisp-text-plus-soft-shadow look the export builds
  // from separate blur-mask layers.
  const [shadowOffsetX, shadowOffsetY] = shadowOffset(textStyle.shadow_angle, textStyle.shadow_distance);
  const outlineStroke = textStyle.outline_enabled && !textStyle.background_enabled
    ? `${Math.max(0.5, textStyle.outline_width * previewScale)}px ${textStyle.outline_color}` : "0px transparent";
  const dropShadow = textStyle.shadow_enabled && !textStyle.background_enabled
    ? `${(shadowOffsetX * previewScale).toFixed(2)}px ${(shadowOffsetY * previewScale).toFixed(2)}px ${(textStyle.shadow_blur * previewScale).toFixed(2)}px ${hexToRgba(textStyle.shadow_color, textStyle.shadow_opacity)}`
    : "none";
  // The "active word" pop briefly blurs the SAME configured shadow a little more (the export's pop animation does the same) instead of carrying a
  // hardcoded black shadow that ignores Shadow-off / Background-on.
  const dropShadowActive = textStyle.shadow_enabled && !textStyle.background_enabled
    ? `${(shadowOffsetX * previewScale).toFixed(2)}px ${(shadowOffsetY * previewScale).toFixed(2)}px ${((textStyle.shadow_blur + 3) * previewScale).toFixed(2)}px ${hexToRgba(textStyle.shadow_color, textStyle.shadow_opacity)}`
    : "none";
  // Glow is a non-directional (0,0-offset) soft light, independent of Shadow. glow_color=null means "this word's own colour", which must be
  // resolved PER WORD (including the Emphasize accent override while active), so it can't be computed once for the whole overlay.
  const glowShadowFor = (word: CaptionWord, active: boolean) => {
    if (!textStyle.glow_enabled || textStyle.background_enabled) return null;
    const blur = (active ? textStyle.glow_blur + 3 : textStyle.glow_blur) * previewScale;
    const displayColor = (active && word.emphasize) ? "#EAB308" : word.color;
    return `0px 0px ${blur.toFixed(2)}px ${hexToRgba(textStyle.glow_color || displayColor, textStyle.glow_opacity)}`;
  };
  const basePos = POSITION_DEFAULTS[captionPosition] || POSITION_DEFAULTS.bottom;
  const boxX = anchorWord?.pos_x ?? basePos.x;
  const boxY = anchorWord?.pos_y ?? basePos.y;
  const boxFontSize = anchorWord?.font_size ?? DEFAULT_WORD_SIZE;
  const boxRotation = anchorWord?.rotation ?? 0;

  const updateWord = (index: number, changes: Partial<CaptionWord>) => setWords((current) => current.map((word, i) => i === index ? { ...word, ...changes } : word));
  // Script panel popover: toggle a manual caption-line break before this word.
  const toggleWordNewLine = (index: number) => setWords((current) => current.map((word, i) => i === index ? { ...word, new_line: !word.new_line } : word));
  // "Split Caption" / Split at Playhead: force a brand-new caption group to start right at this word (same "before this word" convention new_line
  // uses one level down, for the caption boundary groupWordsForDisplay computes instead of a line wrap within one).
  const splitCaptionBeforeWord = (index: number) => setWords((current) => current.map((word, i) => i === index ? { ...word, new_caption: true } : word));
  // "Merge Up": force this word to join the PREVIOUS caption group even past the auto-grouping heuristics - the inverse of the split above, and the
  // way to merge segment S with S+1 is to call this on S+1's own first word.
  const mergeCaptionUp = (index: number) => setWords((current) => current.map((word, i) => i === index ? { ...word, new_caption: false } : word));
  // Timeline's "Split at Playhead": splits the caption on screen in two at the word playing under the scrubber - disabled when the playhead sits on
  // the first word of its group (nothing to split off).
  const canSplitAtPlayhead = activeIndex >= 0 && !!anchorWord && anchorWord.index !== activeIndex;
  const splitAtPlayhead = () => { if (canSplitAtPlayhead) splitCaptionBeforeWord(activeIndex); };
  // Script panel popover: split one word into two adjacent words, time-sliced
  // proportionally to where the character split falls, inserted in place.
  const splitWord = (index: number, splitPos?: number) => setWords((current) => {
    const word = current[index];
    if (!word) return current;
    const text = word.text;
    const pos = Math.min(Math.max(1, splitPos ?? Math.ceil(text.length / 2)), text.length - 1);
    const left = text.slice(0, pos).trim();
    const right = text.slice(pos).trim();
    if (!left || !right) return current;
    const mid = word.start + (word.end - word.start) * (pos / text.length);
    const wordLeft = { ...word, text: left, original: left, end: mid };
    const wordRight = { ...word, text: right, original: right, start: mid, new_line: null, new_caption: null, box_width: null, box_height: null };
    const next = [...current.slice(0, index), wordLeft, wordRight, ...current.slice(index + 1)];
    // .index is a dense array-position id (see caption_ai.py's transcript
    // builder) that deleteWords() matches against - an insert has to
    // renumber everything after it so no two words ever share one.
    return next.map((item, i) => ({ ...item, index: i }));
  });
  // Script panel popover: set one word's emphasis tier, recoloring it the
  // same way a full theme switch would (tiered theme's tier palette, or the
  // plain important/not-important palette for a non-tiered theme).
  const setWordEmphasis = (index: number, emphasis: string) => setWords((current) => current.map((word, i) => {
    if (i !== index) return word;
    const important = emphasis !== "support";
    // Only a tiered template recolours/resizes by tier; Street Glow ignores word colour/size entirely.
    return themeById[theme]?.tiers ? { ...word, emphasis, important, ...bakeWordStyle(themeById[theme], emphasis) } : { ...word, emphasis, important };
  }));
  // "FOR THIS MOMENT" popover: a standalone accent highlight, independent of
  // the tiered-template emphasis above - just an on/off boolean the canvas
  // renders as a fixed accent color + scale while the word is active.
  const toggleWordEmphasize = (index: number) => setWords((current) => current.map((word, i) => i === index ? { ...word, emphasize: !word.emphasize } : word));
  // "FOR THIS MOMENT" popover: the kinetic animation tag the canvas plays
  // while this word is active (null/"none" pill clears it).
  const setWordKinetic = (index: number, kinetic: KineticStyle | null) => setWords((current) => current.map((word, i) => i === index ? { ...word, kinetic } : word));
  // "FOR THIS MOMENT" popover: merge this word into its previous/next
  // neighbor - combined text and time span, the other word removed. Mirrors
  // splitWord's reindex (word.index stays a dense array-position id).
  const mergeWord = (index: number, direction: "previous" | "next") => setWords((current) => {
    const otherIndex = direction === "previous" ? index - 1 : index + 1;
    if (otherIndex < 0 || otherIndex >= current.length) return current;
    const lowIndex = Math.min(index, otherIndex);
    const a = current[lowIndex], b = current[lowIndex + 1];
    const merged = { ...a, text: `${a.text} ${b.text}`.trim(), original: `${a.original} ${b.original}`.trim(),
                      start: Math.min(a.start, b.start), end: Math.max(a.end, b.end) };
    const next = [...current.slice(0, lowIndex), merged, ...current.slice(lowIndex + 2)];
    return next.map((item, i) => ({ ...item, index: i }));
  });
  const chooseTheme = (next: string) => {
    setTheme(next);
    const nextTheme = themeById[next];
    // Street Glow (no tiers) ignores word.color/font_size in preview and export - its two lines are a fixed design - so only a tiered template re-bakes.
    if (nextTheme?.tiers) setWords((current) => current.map((word) => ({ ...word, ...bakeWordStyle(nextTheme, word.emphasis) })));
  };
  // What a word's canvas/Timeline label shows: falls back to the Roman text when native mode is on but the word has no native transcription.
  const scriptText = (word: CaptionWord) => (scriptMode === "native" && word.original) ? word.original : word.text;
  // Top/Middle/Bottom presets also clear every word's own manual pos_x/pos_y, since a dragged position otherwise always wins over the preset.
  const choosePosition = (position: CaptionPosition) => {
    setCaptionPosition(position);
    setWords((current) => current.map((word) => ({ ...word, pos_x: null, pos_y: null })));
  };
  const payload = (): ProjectEditPayload => ({ theme, font_id: fontId, words_per_caption: wordsPerCaption,
                           caption_lines: captionLines, caption_position: captionPosition, words,
                           audio_volume: audioVolume, audio_muted: audioMuted,
                           brightness, contrast, saturation, render_quality: renderQuality, elements,
                           sfx_markers: sfxMarkers, script_mode: scriptMode,
                           ...textStyle, bold, italic });

  const addSfxMarker = (type: string, time?: number, wordIndex?: number) => {
    const id = `sfx_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`;
    const markerTime = Math.round((time ?? currentTime) * 100) / 100;
    setSfxMarkers((current) => [...current, { id, type, time: markerTime, word_index: wordIndex ?? null }]);
  };
  const moveSfxMarker = (id: string, time: number) => setSfxMarkers((current) => current.map((marker) => marker.id === id ? { ...marker, time } : marker));
  const deleteSfxMarker = (id: string) => setSfxMarkers((current) => current.filter((marker) => marker.id !== id));

  const addElement = (char: string, kind: "sticker" | "text" = "sticker") => {
    const id = `el_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`;
    const start = currentTime;
    const end = Math.min(project.duration, currentTime + 2);
    const fontSize = kind === "text" ? 48 : 120;
    setElements((current) => [...current, { id, kind, char, font_id: fontId, color: "#FFFFFF", font_size: fontSize, start, end, pos_x: 50, pos_y: 30, layer: "front" }]);
    setSelectedElementId(id);
  };
  const updateElement = (id: string, changes: Partial<CaptionElement>) => setElements((current) => current.map((item) => item.id === id ? { ...item, ...changes } : item));
  const deleteElement = (id: string) => {
    setElements((current) => current.filter((item) => item.id !== id));
    setSelectedElementId((current) => current === id ? null : current);
  };
  const deleteWords = (indices: number[]) => {
    const indexSet = new Set(indices);
    setWords((current) => current.filter((word) => !indexSet.has(word.index)));
  };
  const reassignEmphasis = async () => {
    setReassigning(true);
    try {
      const data = await captionsApi.reassignEmphasis(project.project_id);
      setWords(data.words);
      toast.success("Emphasis re-tagged by AI");
    } catch (error) { toast.error(errorMessage(error)); }
    finally { setReassigning(false); }
  };
  const save = async () => {
    try {
      await captionsApi.save(project.project_id, payload());
      toast.success("Caption edits saved");
    } catch (error) { toast.error(errorMessage(error)); }
  };
  const render = async () => {
    setRendering(true);
    try {
      await captionsApi.save(project.project_id, payload());
      // The export runs as a background job on the server; poll until it finishes.
      let data = await captionsApi.render(project.project_id);
      while (data.status === "rendering") {
        await new Promise((resolve) => window.setTimeout(resolve, 2500));
        data = await captionsApi.get(project.project_id);
      }
      if (data.status !== "rendered") throw new Error(data.error_message || "Video rendering failed. Please try again.");
      setProject((current) => ({ ...current, status: "rendered" }));
      toast.success("Your captioned video is ready");
    } catch (error) { toast.error(errorMessage(error)); }
    finally { setRendering(false); }
  };
  const relisten = async () => {
    setRelistening(true);
    try {
      let data = await captionsApi.retranscribe(project.project_id, "auto");
      while (["queued", "processing"].includes(data.status)) {
        await new Promise((resolve) => window.setTimeout(resolve, 2500));
        data = await captionsApi.get(project.project_id);
      }
      if (data.status === "failed") throw new Error(data.error_message || "Re-listen failed");
      setWords(data.words);
      setProject((current) => ({ ...current, ...data }));
      setSelectedIndex(0);
      toast.success("A fresh trusted transcript is ready");
    } catch (error) { toast.error(errorMessage(error)); }
    finally { setRelistening(false); }
  };
  const download = async () => {
    try {
      saveBlob(await captionsApi.downloadBlob(project.project_id), "captain-title.mp4");
    } catch (error) { toast.error(errorMessage(error)); }
  };
  const downloadSrt = async () => {
    try {
      await captionsApi.save(project.project_id, payload());
      saveBlob(await captionsApi.srtBlob(project.project_id), "captain-title.srt");
    } catch (error) { toast.error(errorMessage(error)); }
  };
  const togglePlayback = () => { const video = videoRef.current; if (!video) return; if (video.paused) void video.play(); else video.pause(); };
  const seek = (time: number) => { if (videoRef.current) { videoRef.current.currentTime = time; setCurrentTime(time); } };

  // The Transform & Style panel's sliders write straight into `words` for the CURRENTLY VISIBLE caption (the group on screen at the playhead) -
  // a slider's value IS the committed value the instant it changes, so no drag preview/commit step is needed.
  const transformThisCaption = (changes: Partial<CaptionWord>) => {
    if (!visibleWords.length) return;
    const indices = new Set(visibleWords.map((word) => word.index));
    setWords((current) => current.map((word) => indices.has(word.index) ? { ...word, ...changes } : word));
  };
  const setCaptionPos = ({ x, y }: { x?: number; y?: number }) => transformThisCaption({ pos_x: x ?? boxX, pos_y: y ?? boxY });
  // Scaling (not flattening) every word's own font_size by the ratio between the new and old anchor size keeps a tiered template's relative word
  // sizes intact instead of forcing every word in the caption to one size. Clamped to the server's [16,120] bounds.
  const setCaptionScale = (size: number) => {
    const ratio = size / (boxFontSize || 1);
    if (!visibleWords.length) return;
    const indices = new Set(visibleWords.map((word) => word.index));
    setWords((current) => current.map((word) => indices.has(word.index)
      ? { ...word, font_size: Math.min(120, Math.max(16, Math.round(word.font_size * ratio))) } : word));
  };
  const setCaptionRotation = (deg: number) => transformThisCaption({ rotation: deg });
  // Direct drag-to-move on the caption box, alongside the Transform panel's Position sliders - commits straight to this caption's pos_x/pos_y on
  // every move; the optional "also apply to all captions" offer appears when the drag is released.
  const startCaptionDrag = (e: React.MouseEvent) => {
    if (!visibleWords.length || !videoFrameRef.current) return;
    e.preventDefault();
    const frameRect = videoFrameRef.current.getBoundingClientRect();
    const startClientX = e.clientX, startClientY = e.clientY;
    const startX = boxX, startY = boxY;
    const indices = new Set(visibleWords.map((word) => word.index));
    dragMovedRef.current = false;
    const onMove = (moveEvent: MouseEvent) => {
      // The box follows the cursor, so the word under the pointer at mousedown is still under it at mouseup and the browser fires a native click
      // on it; flagging a real drag here lets the word's onClick swallow that one spurious click.
      if (Math.abs(moveEvent.clientX - startClientX) > 3 || Math.abs(moveEvent.clientY - startClientY) > 3) dragMovedRef.current = true;
      const dx = ((moveEvent.clientX - startClientX) / frameRect.width) * 100;
      const dy = ((moveEvent.clientY - startClientY) / frameRect.height) * 100;
      const nextX = Math.min(100, Math.max(0, startX + dx));
      const nextY = Math.min(95, Math.max(5, startY + dy));
      setWords((current) => current.map((word) => indices.has(word.index) ? { ...word, pos_x: nextX, pos_y: nextY } : word));
    };
    const onUp = () => {
      window.removeEventListener("mousemove", onMove);
      window.removeEventListener("mouseup", onUp);
      if (dragMovedRef.current) setScopePrompt(true);
    };
    window.addEventListener("mousemove", onMove);
    window.addEventListener("mouseup", onUp);
  };
  // Direction-aware resize handles - tc/bc only touch height, ml/mr only width, corners both. Resizing scales this caption's font_size (the same
  // field and clamp the Transform panel's Scale slider writes), so the two controls always agree on what "size" means.
  const startResize = (handle: Handle) => (e: React.MouseEvent) => {
    if (!visibleWords.length) return;
    e.preventDefault(); e.stopPropagation();
    const overlayEl = (e.currentTarget as HTMLElement).closest(".caption-overlay");
    if (!overlayEl) return;
    const overlayRect = overlayEl.getBoundingClientRect();
    const startWidthPx = overlayRect.width, startHeightPx = overlayRect.height;
    const startClientX = e.clientX, startClientY = e.clientY;
    const startFontSize = boxFontSize;
    const dir = RESIZE_HANDLES[handle];
    let moved = false;
    const onMove = (moveEvent: MouseEvent) => {
      moved = true;
      const dx = moveEvent.clientX - startClientX;
      const dy = moveEvent.clientY - startClientY;
      const nextWidthPx = dir.x !== 0 ? Math.max(40, startWidthPx + dx * dir.x * 2) : startWidthPx;
      const nextHeightPx = dir.y !== 0 ? Math.max(20, startHeightPx + dy * dir.y * 2) : startHeightPx;
      const scale = Math.sqrt((nextWidthPx * nextHeightPx) / (startWidthPx * startHeightPx));
      setCaptionScale(Math.round(startFontSize * scale));
    };
    const onUp = () => {
      window.removeEventListener("mousemove", onMove);
      window.removeEventListener("mouseup", onUp);
      if (moved) setScopePrompt(true);
    };
    window.addEventListener("mousemove", onMove);
    window.addEventListener("mouseup", onUp);
  };
  // Broadcasts THIS caption's current position / rotation / size to every caption in the project.
  const applyTransformToAll = () => {
    const changes = { pos_x: boxX, pos_y: boxY, rotation: boxRotation || null, font_size: boxFontSize };
    setWords((current) => current.map((word) => ({ ...word, ...changes })));
  };
  const resetTransform = () => {
    if (!visibleWords.length) return;
    const indices = new Set(visibleWords.map((word) => word.index));
    setWords((current) => current.map((word) => indices.has(word.index)
      ? { ...word, pos_x: null, pos_y: null, rotation: null, box_width: null, box_height: null, font_size: word.important ? 76 : DEFAULT_WORD_SIZE }
      : word));
  };
  // Escape or a click outside the prompt/box just dismisses it - the drag/resize already committed, there is nothing to revert.
  useEffect(() => {
    if (!scopePrompt) return undefined;
    const onKeyDown = (e: KeyboardEvent) => { if (e.key === "Escape") setScopePrompt(false); };
    const onMouseDown = (e: MouseEvent) => {
      const target = e.target as HTMLElement;
      if (!target.closest(".position-scope-prompt") && !target.closest(".caption-overlay")) setScopePrompt(false);
    };
    window.addEventListener("keydown", onKeyDown);
    window.addEventListener("mousedown", onMouseDown);
    return () => { window.removeEventListener("keydown", onKeyDown); window.removeEventListener("mousedown", onMouseDown); };
  }, [scopePrompt]);

  // Elements are standalone stickers (not word-synced cues), so a drag or
  // resize applies straight to that one element - no "this/all" scope
  // prompt needed, unlike the shared caption box above.
  const startMoveElement = (id: string) => (e: React.MouseEvent) => {
    e.preventDefault(); e.stopPropagation();
    const element = elements.find((item) => item.id === id);
    if (!element || !videoFrameRef.current) return;
    const frameRect = videoFrameRef.current.getBoundingClientRect();
    const startClientX = e.clientX, startClientY = e.clientY;
    const startX = element.pos_x, startY = element.pos_y;
    const onMove = (moveEvent: MouseEvent) => {
      const dx = ((moveEvent.clientX - startClientX) / frameRect.width) * 100;
      const dy = ((moveEvent.clientY - startClientY) / frameRect.height) * 100;
      updateElement(id, { pos_x: Math.min(100, Math.max(0, startX + dx)), pos_y: Math.min(100, Math.max(0, startY + dy)) });
    };
    const onUp = () => {
      window.removeEventListener("mousemove", onMove);
      window.removeEventListener("mouseup", onUp);
    };
    window.addEventListener("mousemove", onMove);
    window.addEventListener("mouseup", onUp);
  };

  // An element is a single glyph/short text, not a wrapping box, so there's
  // no separate width/height to track - but it shares the same flip bug fix:
  // a signed delta projected onto the handle's own outward direction (never
  // an unsigned radius), clamped to a floor, so no handle can ever drive the
  // size through zero and out the other side.
  const startResizeElement = (id: string, handle: Handle) => (e: React.MouseEvent) => {
    e.preventDefault(); e.stopPropagation();
    const element = elements.find((item) => item.id === id);
    if (!element) return;
    const dir = RESIZE_HANDLES[handle];
    const norm = Math.hypot(dir.x, dir.y) || 1;
    const unitX = dir.x / norm, unitY = dir.y / norm;
    const startClientX = e.clientX, startClientY = e.clientY;
    const startSize = element.font_size;
    const onMove = (moveEvent: MouseEvent) => {
      const dx = moveEvent.clientX - startClientX;
      const dy = moveEvent.clientY - startClientY;
      const signedDelta = (dx * unitX + dy * unitY) * 1.6;
      updateElement(id, { font_size: Math.min(280, Math.max(20, Math.round(startSize + signedDelta))) });
    };
    const onUp = () => {
      window.removeEventListener("mousemove", onMove);
      window.removeEventListener("mouseup", onUp);
    };
    window.addEventListener("mousemove", onMove);
    window.addEventListener("mouseup", onUp);
  };

  return (
    <main className="studio" data-testid="caption-editor-workspace">
      <div className="studio-topbar" data-testid="project-command-bar">
        <div><button onClick={onNew} data-testid="new-project-button">← New session</button><h1 data-testid="current-project-name">{project.name}</h1></div>
        <div className="project-actions">
          {project.width && project.height && <span className="resolution-badge" data-testid="resolution-badge">{project.width}×{project.height} · {aspectLabel(project.width, project.height)}</span>}
          <select className="quality-select" value={renderQuality} onChange={(e) => setRenderQuality(e.target.value as RenderQuality)} title="Export quality" data-testid="render-quality-select">
            <option value="high">High quality</option>
            <option value="draft">Draft (fast)</option>
          </select>
          <button className="utility-chip" onClick={save} data-testid="save-project-button"><FloppyDisk size={16} /> Save</button>
          <button className="utility-chip" onClick={downloadSrt} data-testid="download-srt-button"><FileText size={16} /> SRT</button>
          {project.status === "rendered" ? <button className="export-button" onClick={download} data-testid="download-video-button"><DownloadSimple size={19} weight="bold" /> Download MP4</button> :
            <button className="export-button" onClick={render} disabled={rendering} data-testid="render-video-button">{rendering ? <SpinnerGap className="spin" size={19} /> : <DownloadSimple size={19} weight="bold" />} {rendering ? "Exporting…" : "Export Video"}</button>}
        </div>
      </div>
      <div className="studio-grid">
        <LeftRail active={leftTab} onSelect={setLeftTab} />
        {leftTab === "captions" && <TranscriptPanel words={words} originalTranscript={project.source_transcript} transcriptionMode={project.transcription_mode} activeIndex={activeIndex} selectedIndex={selectedIndex} onSelect={openMoment} onText={(index, text) => updateWord(index, { text })} onNewLine={toggleWordNewLine} onSplitWord={splitWord} onEmphasis={setWordEmphasis} onRelisten={relisten} relistening={relistening} scriptMode={scriptMode} onScriptMode={setScriptMode} />}
        {leftTab === "media" && <MediaPanel project={project} />}
        {leftTab === "audio" && <AudioPanel volume={audioVolume} onVolume={setAudioVolume} muted={audioMuted} onMuted={setAudioMuted} />}
        {leftTab === "elements" && <ElementsPanel elements={elements.filter((item) => item.kind !== "text")} selectedId={selectedElementId} onSelect={setSelectedElementId} onAdd={(char) => addElement(char, "sticker")} onUpdate={updateElement} onDelete={deleteElement} currentTime={currentTime} duration={project.duration} />}
        {leftTab === "text" && <TextPanel elements={elements} selectedId={selectedElementId} onSelect={setSelectedElementId} onAdd={(text) => addElement(text, "text")} onUpdate={updateElement} onDelete={deleteElement} currentTime={currentTime} duration={project.duration} />}
        {leftTab === "adjustment" && <AdjustmentPanel brightness={brightness} contrast={contrast} saturation={saturation} onBrightness={setBrightness} onContrast={setContrast} onSaturation={setSaturation} onReset={() => { setBrightness(0); setContrast(100); setSaturation(100); }} />}
        {leftTab === "ai" && <AiToolsPanel words={words} onDeleteWords={deleteWords} onReassignEmphasis={reassignEmphasis} reassigning={reassigning} />}
        <section className="preview-panel" data-testid="video-preview-panel">
          <div className="video-stage" ref={stageRef}>
          <div className="video-frame" ref={videoFrameRef} style={{ width: `${frameBox.w}px`, height: `${frameBox.h}px` }}>
            <video ref={videoRef} src={mediaUrl ?? undefined} style={{ filter: `brightness(${100 + brightness}%) contrast(${contrast}%) saturate(${saturation}%)` }} onError={() => { if (mediaRetry < 2) window.setTimeout(() => setMediaRetry((n) => n + 1), 1200); }} onTimeUpdate={(e) => setCurrentTime(e.currentTarget.currentTime)} onPlay={() => setPlaying(true)} onPause={() => setPlaying(false)} playsInline data-testid="caption-video-player" />
            <button className="preview-play" onClick={togglePlayback} data-testid="video-preview-play-button"><Play size={28} weight="fill" /></button>
            <button className={`preview-safe-zone-toggle ${safeZone ? "active" : ""}`} onClick={() => setSafeZone((current) => !current)} title="Toggle Reels/Shorts safe-zone guide" data-testid="safe-zone-toggle-button"><FrameCorners size={18} weight={safeZone ? "fill" : "regular"} /></button>
            {safeZone && <div className="safe-zone-overlay" data-testid="safe-zone-overlay">
              <div className="safe-zone-icons">
                <i><Heart size={20} weight="fill" /></i>
                <i><ChatCircle size={20} weight="fill" /></i>
                <i><BookmarkSimple size={20} weight="fill" /></i>
                <i><Share size={20} weight="fill" /></i>
                <i><DotsThreeVertical size={20} weight="fill" /></i>
              </div>
              <div className="safe-zone-meta" />
            </div>}
            {visibleWords.length > 0 && theme === STREET_GLOW && <StreetGlowOverlay
              visibleWords={visibleWords} scriptMode={scriptMode}
              boxX={boxX} boxY={boxY} boxRotation={boxRotation} previewScale={previewScale}
              onMouseDown={startCaptionDrag} onResize={startResize} />}
            {visibleWords.length > 0 && theme !== STREET_GLOW && <div className={`caption-overlay theme-${theme} ${(captionLines === 2 || tiers) ? "lines-2" : ""}`}
              style={{ left: `${boxX}%`, top: `${boxY}%`,
                      transform: `translate(-50%,-50%) rotate(${boxRotation}deg)`, lineHeight: NATIVE_LINE_HEIGHT,
                      fontStyle: italic ? "italic" : "normal",
                      // WebkitTextStroke / textShadow are always set explicitly (never left to the stylesheet's default rule) so every Outline/
                      // Shadow combination - including both off - renders exactly what the controls say, matching the export 1:1.
                      WebkitTextStroke: outlineStroke, textShadow: dropShadow }}
              onMouseDown={startCaptionDrag} data-testid="live-caption-preview">
              {/* Plain text flow with a literal line break between rows - no per-row wrapper with hand-computed margins. The browser lays out
                  stacked lines with each line's own natural metrics, exactly the way libass stacks \N-separated lines in the export. Doodle
                  Marker uses ONE size for every tier, so words flow on one line (null tiers = only an explicit two-line midpoint or a word's own
                  new_line breaks), mirroring the server's break_tiers override. */}
              {splitIntoRows(visibleWords, theme === DOODLE_MARKER ? null : tiers, captionLines).map((row, rowIndex) => <Fragment key={rowIndex}>
                {rowIndex > 0 && <br />}
                {/* BorderStyle=3's opaque box in libass draws around each \N-separated VISUAL LINE on its own, so the background lives on a per-row wrapper. */}
                <span style={textStyle.background_enabled
                  ? { backgroundColor: hexToRgba(textStyle.background_color, textStyle.background_opacity),
                      padding: "2px 10px", borderRadius: "3px", boxDecorationBreak: "clone", WebkitBoxDecorationBreak: "clone" } as React.CSSProperties
                  : undefined}>
                {row.map((word, wordIndex) => {
                  // Colour/size always come from the word itself; a tiered theme only decides the font.
                  const fontFamily = tiers
                    ? tiers[tiers[word.emphasis] ? word.emphasis : "support"].css_font
                    : cssFamily(fontById[fontId]?.family ?? "Poppins");
                  const isActiveWord = activeIndex === word.index;
                  // Glow can differ per word, so it is combined into an explicit per-word textShadow together with the Shadow entry.
                  const glowEntry = glowShadowFor(word, isActiveWord);
                  const style: React.CSSProperties = { color: word.color, fontSize: `${Math.max(20, word.font_size * previewScale)}px`, fontFamily,
                    fontWeight: bold && isBoldCapableFont(fontFamily) ? 800 : 400,
                    ...((isActiveWord || glowEntry) ? { textShadow: [isActiveWord ? dropShadowActive : dropShadow, glowEntry].filter((v) => v && v !== "none").join(", ") || "none" } : {}) };
                  const className = [
                    isActiveWord ? "active" : "",
                    isActiveWord && word.kinetic ? `kinetic-${word.kinetic}` : "",
                    isActiveWord && word.emphasize ? "emphasize-accent" : "",
                  ].filter(Boolean).join(" ");
                  const doodleTier = theme === DOODLE_MARKER && (word.emphasis === "key" || word.emphasis === "feature") ? word.emphasis : null;
                  const relOnMs = Math.max(0, Math.round((word.start - visibleWords[0].start) * 1000));
                  return <span key={word.index} className={doodleTier ? `doodle-decorated doodle-${doodleTier}` : undefined}>
                    {wordIndex > 0 && " "}
                    {doodleTier === "feature" && <svg className="doodle-loop" viewBox="0 0 100 40" style={{ animationDelay: `${relOnMs}ms` }}>
                      <path d="M 10 20 C 10 8, 90 6, 92 20 C 94 34, 8 36, 14 18" fill="none" stroke="#EF4444" strokeWidth="3" strokeLinecap="round" pathLength="1" style={{ animationDelay: `${relOnMs}ms` }} />
                    </svg>}
                    <span className={[className, doodleTier === "key" ? "doodle-pill" : ""].filter(Boolean).join(" ")}
                      style={{ ...style, ...(doodleTier === "key" ? { animationDelay: `${relOnMs}ms` } : {}) }}
                      onClick={(e) => { e.stopPropagation(); if (dragMovedRef.current) { dragMovedRef.current = false; return; } openMoment(word.index); }}
                      data-testid={`preview-caption-word-${word.index}`}>{scriptText(word)}</span>
                  </span>;
                })}
                </span>
              </Fragment>)}
              <div className="selection-box">
                {(["tl", "tc", "tr", "ml", "mr", "bl", "bc", "br"] as Handle[]).map((pos) => <i key={pos} className={`resize-handle handle-${pos}`} onMouseDown={startResize(pos)} data-testid={`caption-resize-handle-${pos}`} />)}
              </div>
            </div>}
            {scopePrompt && <div className="position-scope-prompt" style={{ left: `${boxX}%`, top: `${Math.min(92, boxY + 8)}%` }} data-testid="position-scope-prompt">
              <button onClick={() => setScopePrompt(false)} data-testid="apply-this-caption-button">This caption</button>
              <button onClick={() => { applyTransformToAll(); setScopePrompt(false); }} data-testid="apply-all-captions-button">All captions</button>
            </div>}
            {elements.filter((item) => currentTime >= item.start && currentTime <= item.end).map((item) => {
              const interactive = leftTab === "elements" || leftTab === "text";
              return <div key={item.id} className={`element-overlay ${interactive && selectedElementId === item.id ? "selected" : ""}`}
                style={{ left: `${item.pos_x}%`, top: `${item.pos_y}%`, fontSize: `${item.font_size * previewScale}px`, color: item.color,
                        fontFamily: item.kind === "text" ? cssFamily(fontById[item.font_id]?.family ?? "Poppins") : undefined,
                        fontWeight: item.kind === "text" ? 700 : undefined,
                        // plain z-staging vs the caption overlay (z-index 3); mirrors the export's ASS Layer (no subject matting)
                        zIndex: item.layer === "behind" ? 1 : 4,
                        cursor: interactive ? "grab" : "default", pointerEvents: interactive ? "auto" : "none" }}
                onMouseDown={interactive ? (e) => { setSelectedElementId(item.id); startMoveElement(item.id)(e); } : undefined}
                data-testid={`element-overlay-${item.id}`}>
                {item.char}
                {interactive && selectedElementId === item.id && <div className="selection-box">
                  {(["tl", "tc", "tr", "ml", "mr", "bl", "bc", "br"] as Handle[]).map((pos) => <i key={pos} className={`resize-handle handle-${pos}`} onMouseDown={startResizeElement(item.id, pos)} data-testid={`element-resize-handle-${pos}`} />)}
                </div>}
              </div>;
            })}
          </div>
          </div>
          <div className="preview-meta"><span data-testid="preview-format-label">9:16 PREVIEW</span><span data-testid="caption-engine-label">WORD SYNC · {theme.toUpperCase()}</span></div>
        </section>
        <ThemePanel theme={theme} onTheme={chooseTheme} fontId={fontId} onFont={setFontId}
          wordsPerCaption={wordsPerCaption} onWordsPerCaption={setWordsPerCaption}
          captionLines={captionLines} onCaptionLines={setCaptionLines}
          captionPosition={captionPosition} onCaptionPosition={choosePosition}
          selectedWord={words[selectedIndex]} onWordStyle={(change) => updateWord(selectedIndex, change)}
          tab={themeTab} onTab={setThemeTab}
          textStyle={textStyle} onTextStyle={updateTextStyle} bold={bold} onBold={setBold} italic={italic} onItalic={setItalic}
          hasActiveCaption={visibleWords.length > 0} boxX={boxX} boxY={boxY} boxFontSize={boxFontSize} boxRotation={boxRotation}
          onCaptionPos={setCaptionPos} onCaptionScale={setCaptionScale} onCaptionRotation={setCaptionRotation}
          onApplyTransformToAll={applyTransformToAll} onResetTransform={resetTransform} />
      </div>
      {momentPopover != null && words[momentPopover] && <MomentPopover
        word={words[momentPopover]} index={momentPopover}
        frameRect={videoFrameRef.current?.getBoundingClientRect() ?? null}
        anchorRect={momentAnchor}
        onClose={() => setMomentPopover(null)}
        onText={(index, text) => updateWord(index, { text })}
        onNewLine={toggleWordNewLine}
        onMerge={(index, direction) => { mergeWord(index, direction); setMomentPopover(null); }}
        onKinetic={setWordKinetic}
        onEmphasize={toggleWordEmphasize}
        onAddSfx={addSfxMarker}
        canMergePrevious={momentPopover > 0}
        canMergeNext={momentPopover < words.length - 1}
        onSplitCaption={(index) => { splitCaptionBeforeWord(index); setMomentPopover(null); }}
        onMergeCaptionUp={(index) => { mergeCaptionUp(index); setMomentPopover(null); }}
        canSplitCaption={momentPopover > 0}
        canMergeCaptionUp={momentPopover > 0}
      />}
      <Timeline words={words} groups={indexedWordGroups} duration={project.duration} currentTime={currentTime} playing={playing} onToggle={togglePlayback} onSeek={seek} selectedIndex={selectedIndex} onSelect={openMoment} onRetimeWord={updateWord} mediaUrl={mediaUrl} sfxMarkers={sfxMarkers} onAddSfx={addSfxMarker} onMoveSfx={moveSfxMarker} onDeleteSfx={deleteSfxMarker} scriptMode={scriptMode} onSplitAtPlayhead={splitAtPlayhead} canSplitAtPlayhead={canSplitAtPlayhead} />
    </main>
  );
};