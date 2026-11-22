import { Fragment, useCallback, useEffect, useMemo, useRef, useState } from "react";
import { ArrowCounterClockwise, ArrowClockwise, ArrowLeft, BookmarkSimple, ChatCircle, CheckCircle, DotsThreeVertical, Export, FloppyDisk, FrameCorners, Heart, Keyboard, Play, Share, SpinnerGap, WarningCircle } from "@phosphor-icons/react";
import { toast } from "@/utils/toast";
import { absoluteApiUrl as absoluteUrl, captionsApi, errorMessage, saveBlob } from "../captionsApi";
import { useCaptionCatalog } from "../catalogContext";
import { bakeWordStyle, cssFamily, DEFAULT_WORD_SIZE, DOODLE_MARKER, STREET_GLOW } from "../lib/styles";
import { playSfx } from "../lib/sfx";
import { useEditorStatePersistence } from "../hooks/useEditorStatePersistence";
import { useProjectAutosave } from "../hooks/useProjectAutosave";
import { useUndoHistory } from "../hooks/useUndoHistory";
import { useStudioShortcuts, type ShortcutHandlers } from "../hooks/useShortcuts";
import { comboText, SHORTCUT_BINDINGS, type ShortcutId } from "../lib/shortcuts";
import { ShortcutsDialog } from "./ShortcutsDialog";
import { VersionHistoryMenu } from "./VersionHistoryMenu";
import { HeaderLeft, HeaderRight } from "./HeaderSlots";
import { ProjectNameEditor } from "./ProjectNameEditor";
import { ExportDialog, type CaptionFileFormat } from "./ExportDialog";
import { isCaptionStyleTab, isLeftTab, isThemeTab, themeTabOf } from "../lib/editorTabs";
import { ThemePanel, type ThemeTab } from "./ThemePanel";
import { TranscriptPanel } from "./TranscriptPanel";
import { Timeline, type TimelineMode } from "./Timeline";
import { Switch } from "@/components/ui/Switch";
import { Panel, PanelGroup, PanelResizeHandle } from "react-resizable-panels";
import { useMediaQuery } from "../lib/useMediaQuery";
import { LeftRail, type LeftTab } from "./LeftRail";
import { MediaPanel } from "./MediaPanel";
import { AudioPanel } from "./AudioPanel";
import { ElementsPanel } from "./ElementsPanel";
import { TextPanel } from "./TextPanel";
import { ColorPanel } from "./ColorPanel";
import { RatioPanel } from "./RatioPanel";
import { BlurFill, WhiteBalanceFilter, WHITE_BALANCE_FILTER_ID } from "./FramePreview";
import { fadeGain, outputSize, type AspectRatio, type ColorValues, type FrameFit, type RenderFormat, type RenderResolution } from "../lib/frame";
import { AiToolsPanel } from "./AiToolsPanel";
import { MomentPopover } from "./MomentPopover";
import type {
  CaptionElement, CaptionPosition, CaptionProject, CaptionTier, CaptionWord, EditorState, KineticStyle, ProjectEditPayload, RenderQuality, ScriptMode, SfxMarker, TextStyle,
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
// Keyboard stepping: one frame at 30 fps, preview volume in 10% steps, and the playback speeds Slower/Faster walk through.
const FRAME_SECONDS = 1 / 30;
const PLAYBACK_RATES = [0.25, 0.5, 0.75, 1, 1.25, 1.5, 1.75, 2];
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

interface CaptionStudioProps {
  initialProject: CaptionProject
  onNew: () => void
  /** The server's copy replaced this editor's content (restore, or a failed re-listen): reload the editor from it. */
  onReplaced: (project: CaptionProject) => void
}

export const CaptionStudio = ({ initialProject, onNew, onReplaced }: CaptionStudioProps) => {
  const savedState = initialProject.editor_state;
  const { themeById, fontById, sfxById } = useCaptionCatalog();
  const [project, setProject] = useState<CaptionProject>(initialProject);
  const [words, setWords] = useState(initialProject.words);
  const [theme, setTheme] = useState(initialProject.theme);
  const [fontId, setFontId] = useState(initialProject.font_id || "poppins");
  const [wordsPerCaption, setWordsPerCaption] = useState<number | null>(initialProject.words_per_caption ?? null);
  const [captionLines, setCaptionLines] = useState(initialProject.caption_lines || 1);
  const [captionPosition, setCaptionPosition] = useState<CaptionPosition>(initialProject.caption_position || "bottom");
  const [selectedIndex, setSelectedIndex] = useState(() => Math.min(savedState?.selected_index ?? 0, Math.max(0, initialProject.words.length - 1)));
  // The caption style tool last opened when this project was saved; kept in the saved editor state while another left tab is open.
  const [savedThemeTab] = useState<ThemeTab>(isThemeTab(savedState?.theme_tab) ? savedState.theme_tab : "templates");
  // Selecting a word from the Captions list or the bottom timeline should
  // land you straight on its edit controls: while a caption style tool
  // (Templates / Customize / Transform) is open in the left column, it
  // switches to the Word tool. The Captions list itself stays open, so
  // clicking through the transcript keeps working. It should also seek
  // the video there, matching how a real script/transcript editor works -
  // clicking a line is how you navigate the footage, not just an editing
  // affordance.
  const selectWord = (index: number) => {
    setSelectedIndex(index);
    setWordPicked(true);
    setLeftTab((tab) => (isCaptionStyleTab(tab) ? "word" : tab));
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
  const [currentTime, setCurrentTime] = useState(savedState?.playhead ?? 0);
  const [playing, setPlaying] = useState(false);
  const [rendering, setRendering] = useState(initialProject.status === "rendering");
  const [relistening, setRelistening] = useState(false);
  // Short-lived signed media URL (the <video> element cannot send the bearer token); refreshed on error/expiry.
  const [mediaUrl, setMediaUrl] = useState<string | null>(null);
  const [mediaRetry, setMediaRetry] = useState(0);
  const [mediaFailed, setMediaFailed] = useState(false);
  // Where to seek once the video's metadata is known (the saved playhead on open, the current time after a media URL refresh).
  const pendingSeekRef = useRef(savedState?.playhead ?? 0);
  // Shown right after a direct drag/resize on the canvas box releases - the move/resize has ALREADY been committed to this caption by then,
  // so this is purely an optional one-tap shortcut to also broadcast it to every caption, not a gate. Dismissing it just closes it.
  const [scopePrompt, setScopePrompt] = useState(false);
  const [leftTab, setLeftTab] = useState<LeftTab>(isLeftTab(savedState?.left_tab) ? savedState.left_tab : "captions");
  const themeTab: ThemeTab = isCaptionStyleTab(leftTab) ? themeTabOf(leftTab) : savedThemeTab;
  const [elements, setElements] = useState(initialProject.elements || []);
  const [selectedElementId, setSelectedElementId] = useState<string | null>(null);
  const [audioVolume, setAudioVolume] = useState(initialProject.audio_volume ?? 100);
  const [audioMuted, setAudioMuted] = useState(initialProject.audio_muted ?? false);
  const [brightness, setBrightness] = useState(initialProject.brightness ?? 0);
  const [contrast, setContrast] = useState(initialProject.contrast ?? 100);
  const [saturation, setSaturation] = useState(initialProject.saturation ?? 100);
  const [warmth, setWarmth] = useState(initialProject.warmth ?? 0);
  const [tint, setTint] = useState(initialProject.tint ?? 0);
  const [audioFadeIn, setAudioFadeIn] = useState(initialProject.audio_fade_in ?? 0);
  const [audioFadeOut, setAudioFadeOut] = useState(initialProject.audio_fade_out ?? 0);
  const [audioNormalize, setAudioNormalize] = useState(initialProject.audio_normalize ?? false);
  const [aspectRatio, setAspectRatio] = useState<AspectRatio>(initialProject.aspect_ratio ?? "original");
  const [frameFit, setFrameFit] = useState<FrameFit>(initialProject.frame_fit ?? "blur");
  const [frameColor, setFrameColor] = useState(initialProject.frame_color ?? "#000000");
  const [renderQuality, setRenderQuality] = useState<RenderQuality>(initialProject.render_quality ?? "high");
  const [renderResolution, setRenderResolution] = useState<RenderResolution>(initialProject.render_resolution ?? "original");
  const [renderFormat, setRenderFormat] = useState<RenderFormat>(initialProject.render_format ?? "mp4");
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
  const [safeZone, setSafeZone] = useState(savedState?.safe_zone ?? false);
  const [exportOpen, setExportOpen] = useState(false);
  // Keyboard shortcuts: the dialog, plus the editor state only the shortcuts touch.
  const [shortcutsOpen, setShortcutsOpen] = useState(false);
  // selectedIndex always points at some word (it drives the Text tab), so "a word is picked" - what Delete cuts and Esc lets go of - is tracked
  // separately: true once the user picks a word, false again after Esc.
  const [wordPicked, setWordPicked] = useState(false);
  const [timelineMode, setTimelineMode] = useState<TimelineMode>("lines");
  const [revealToken, setRevealToken] = useState(0);
  const [searchFocusToken, setSearchFocusToken] = useState(0);
  const [searchQuery, setSearchQuery] = useState("");
  // Preview-only: never saved and never part of the export (the export volume is the Audio panel's).
  const [previewVolume, setPreviewVolume] = useState(1);
  const [playbackRate, setPlaybackRate] = useState(1);
  const [hud, setHud] = useState<{ text: string; key: number } | null>(null);
  const hudTimerRef = useRef(0);
  const prevLeftTabRef = useRef<LeftTab>("captions");
  const [restoring, setRestoring] = useState(false);
  const [renderProgress, setRenderProgress] = useState(initialProject.status === "rendering" ? initialProject.progress ?? 0 : 0);
  const [frameWidth, setFrameWidth] = useState(0);
  const [stageSize, setStageSize] = useState({ w: 360, h: 640 });
  const videoRef = useRef<HTMLVideoElement>(null);
  const videoFrameRef = useRef<HTMLDivElement>(null);
  const stageRef = useRef<HTMLDivElement>(null);
  // Phones get the stacked layout (see studio.css @media 760px); the resizable panels are desktop/tablet only.
  // Switching between the two remounts the preview, so the ResizeObservers below re-attach on it.
  const narrowLayout = useMediaQuery("(max-width: 760px)");
  // Set true mid-drag once the cursor has actually moved - read (and reset) by a word span's onClick to swallow the spurious click a
  // drag-release fires on whatever word is still under the cursor (the caption box follows the mouse).
  const dragMovedRef = useRef(false);

  useEffect(() => {
    let cancelled = false;
    captionsApi.mediaUrl(project.project_id)
      .then((relative) => { if (!cancelled) setMediaUrl(absoluteUrl(relative)); })
      .catch(() => { if (!cancelled) { setMediaUrl(null); setMediaFailed(true); } });
    return () => { cancelled = true; };
  }, [project.project_id, mediaRetry]);

  // The video box is the largest rectangle with the EXPORTED frame's shape (Ratio panel; the source's own shape for "original") that fits the
  // stage - measured, because whether width or height is the binding constraint depends on the stage's own shape.
  useEffect(() => {
    const node = stageRef.current;
    if (!node || typeof ResizeObserver === "undefined") return undefined;
    const observer = new ResizeObserver((entries) => {
      const { width, height } = entries[0].contentRect;
      setStageSize({ w: width, h: height });
    });
    observer.observe(node);
    return () => observer.disconnect();
  }, [narrowLayout]);
  const [outputWidth, outputHeight] = outputSize(aspectRatio, project.width, project.height);
  const frameAspect = outputWidth / outputHeight;
  const frameBox = useMemo(() => {
    const w = Math.max(0, Math.min(stageSize.w, stageSize.h * frameAspect));
    return { w: Math.floor(w), h: Math.floor(w / frameAspect) };
  }, [stageSize, frameAspect]);
  const reframed = aspectRatio !== "original";

  // The <video> element's own volume tops out at 1.0 (100%) - boosting past
  // that only happens in the exported render's ffmpeg volume filter, not here.
  // Fades follow the playhead (currentTime updates every animation frame while playing), the same linear ramp as ffmpeg's afade.
  useEffect(() => {
    if (!videoRef.current) return;
    const gain = fadeGain(currentTime, project.duration, audioFadeIn, audioFadeOut);
    videoRef.current.volume = Math.min(1, Math.max(0, audioVolume / 100)) * gain * previewVolume;
    videoRef.current.muted = audioMuted;
  }, [audioVolume, audioMuted, currentTime, project.duration, audioFadeIn, audioFadeOut, previewVolume]);
  useEffect(() => { if (videoRef.current) videoRef.current.playbackRate = playbackRate; }, [playbackRate, mediaUrl, narrowLayout]);
  useEffect(() => () => window.clearTimeout(hudTimerRef.current), []);

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
  }, [narrowLayout]);
  const previewScale = frameWidth ? frameWidth / 1080 : 0.62;
  // Same order as the export: eq (brightness, contrast, saturation) then the white-balance channel mixer.
  const videoFilter = `brightness(${100 + brightness}%) contrast(${contrast}%) saturate(${saturation}%)${warmth || tint ? ` url(#${WHITE_BALANCE_FILTER_ID})` : ""}`;

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
  // Optional render flags (templates converted from tscaps); all off for the older templates.
  const themeCard = themeById[theme]?.card;
  const flowLines = !!themeCard?.flow;
  const upperCase = !!themeCard?.uppercase;
  const activeColor = themeCard?.active_color ?? null;
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
    // Templates that carry their own text-style preset (the ones converted from tscaps) set outline/shadow/glow/background/bold/italic to
    // their design defaults; the user can still change every control afterwards. Older templates have no preset and leave these alone.
    const preset = nextTheme?.card?.text_style;
    if (preset) {
      const { bold: presetBold, italic: presetItalic, ...style } = preset;
      updateTextStyle(style);
      if (typeof presetBold === "boolean") setBold(presetBold);
      if (typeof presetItalic === "boolean") setItalic(presetItalic);
    }
  };
  // What a word's canvas/Timeline label shows: falls back to the Roman text when native mode is on but the word has no native transcription.
  const scriptText = (word: CaptionWord) => (scriptMode === "native" && word.original) ? word.original : word.text;
  // Top/Middle/Bottom presets also clear every word's own manual pos_x/pos_y, since a dragged position otherwise always wins over the preset.
  const choosePosition = (position: CaptionPosition) => {
    setCaptionPosition(position);
    setWords((current) => current.map((word) => ({ ...word, pos_x: null, pos_y: null })));
  };
  const payload = useMemo((): ProjectEditPayload => ({ theme, font_id: fontId, words_per_caption: wordsPerCaption,
                           caption_lines: captionLines, caption_position: captionPosition, words,
                           audio_volume: audioVolume, audio_muted: audioMuted,
                           brightness, contrast, saturation, render_quality: renderQuality, render_resolution: renderResolution, render_format: renderFormat, elements,
                           warmth, tint, audio_fade_in: audioFadeIn, audio_fade_out: audioFadeOut, audio_normalize: audioNormalize,
                           aspect_ratio: aspectRatio, frame_fit: frameFit, frame_color: frameColor,
                           sfx_markers: sfxMarkers, script_mode: scriptMode,
                           ...textStyle, bold, italic }),
    [theme, fontId, wordsPerCaption, captionLines, captionPosition, words, audioVolume, audioMuted, brightness, contrast, saturation, renderQuality,
      renderResolution, renderFormat, elements, sfxMarkers, scriptMode, textStyle, bold, italic, warmth, tint, audioFadeIn, audioFadeOut, audioNormalize, aspectRatio, frameFit, frameColor]);
  // Jobs that replace the words on the server (and the restore flow) own the document while they run; autosaving over them would only conflict.
  const autosave = useProjectAutosave({ projectId: initialProject.project_id, initialRevision: initialProject.revision, payload,
    paused: relistening || reassigning || restoring });
  const { flush: flushEdits } = autosave;
  // Edits autosave on their own; the header Save button (and Mod+S) just writes them now instead of after the debounce.
  const saveNow = useCallback(async () => {
    try { await flushEdits(); }
    catch (error) { toast.error(errorMessage(error)); }
  }, [flushEdits]);
  // Undo / redo over the whole editable document (the autosave payload): putting a snapshot back just sets every piece of editor state from it.
  const applyDocument = useCallback((doc: ProjectEditPayload) => {
    setTheme(doc.theme); setFontId(doc.font_id); setWordsPerCaption(doc.words_per_caption); setCaptionLines(doc.caption_lines);
    setCaptionPosition(doc.caption_position); setWords(doc.words); setElements(doc.elements); setSfxMarkers(doc.sfx_markers);
    setAudioVolume(doc.audio_volume); setAudioMuted(doc.audio_muted); setAudioFadeIn(doc.audio_fade_in); setAudioFadeOut(doc.audio_fade_out);
    setAudioNormalize(doc.audio_normalize); setBrightness(doc.brightness); setContrast(doc.contrast); setSaturation(doc.saturation);
    setWarmth(doc.warmth); setTint(doc.tint); setAspectRatio(doc.aspect_ratio); setFrameFit(doc.frame_fit); setFrameColor(doc.frame_color);
    setRenderQuality(doc.render_quality); setRenderResolution(doc.render_resolution ?? "original"); setRenderFormat(doc.render_format ?? "mp4");
    setScriptMode(doc.script_mode); setBold(doc.bold); setItalic(doc.italic);
    setTextStyle({
      outline_enabled: doc.outline_enabled, outline_width: doc.outline_width, outline_color: doc.outline_color,
      shadow_enabled: doc.shadow_enabled, shadow_angle: doc.shadow_angle, shadow_distance: doc.shadow_distance, shadow_blur: doc.shadow_blur,
      shadow_opacity: doc.shadow_opacity, shadow_color: doc.shadow_color, glow_enabled: doc.glow_enabled, glow_color: doc.glow_color,
      glow_blur: doc.glow_blur, glow_opacity: doc.glow_opacity, background_enabled: doc.background_enabled,
      background_color: doc.background_color, background_opacity: doc.background_opacity,
    });
  }, []);
  const history = useUndoHistory(payload, applyDocument, !(relistening || reassigning || restoring));
  const exportIsCurrent = project.rendered_revision != null && project.rendered_revision === autosave.revision && autosave.status === "saved";

  // Reads the React state, not the <video>: until the saved position has been seeked to, the element itself still reports 0.
  const getEditorState = useCallback((): EditorState => ({ playhead: Math.round(currentTime * 100) / 100, left_tab: leftTab, theme_tab: themeTab,
    selected_index: selectedIndex, safe_zone: safeZone }), [currentTime, leftTab, themeTab, selectedIndex, safeZone]);
  useEditorStatePersistence(initialProject.project_id, getEditorState);

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
      await flushEdits();
      const data = await captionsApi.reassignEmphasis(project.project_id);
      setWords(data.words);
      autosave.rebase(data.revision);
      toast.success("Emphasis re-tagged by AI");
    } catch (error) { toast.error(errorMessage(error)); }
    finally { setReassigning(false); }
  };
  // The export runs as a background job on the server, so it may already be running when the project is opened: this also picks that back up.
  const aliveRef = useRef(true);
  useEffect(() => { aliveRef.current = true; return () => { aliveRef.current = false; }; }, []);
  const followingRenderRef = useRef(false);
  const followRender = useCallback(async (started: CaptionProject) => {
    if (followingRenderRef.current) return;
    followingRenderRef.current = true;
    setRendering(true);
    try {
      let data = started;
      setRenderProgress(data.progress ?? 0);
      while (data.status === "rendering" && aliveRef.current) {
        await new Promise((resolve) => window.setTimeout(resolve, 2500));
        data = await captionsApi.get(started.project_id);
        setRenderProgress(data.progress ?? 0);
      }
      if (!aliveRef.current) return;
      if (data.status !== "rendered") throw new Error(data.error_message || "Video rendering could not finish. Please try again, and contact the admin if it keeps happening.");
      setProject((current) => ({ ...current, status: "rendered", rendered_revision: data.rendered_revision }));
      toast.success("Your captioned video is ready");
    } catch (error) { toast.error(errorMessage(error)); }
    finally { followingRenderRef.current = false; setRendering(false); }
  }, []);
  useEffect(() => {
    if (initialProject.status === "rendering") void followRender(initialProject);
    // Only for the state the project was opened in; exports started here call followRender themselves.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);
  const render = async () => {
    setRendering(true);
    setRenderProgress(0);
    try {
      await flushEdits();
      await followRender(await captionsApi.render(project.project_id));
    } catch (error) { toast.error(errorMessage(error)); setRendering(false); }
  };
  const relisten = async () => {
    setRelistening(true);
    try {
      await flushEdits();
      let data = await captionsApi.retranscribe(project.project_id, "auto");
      while (["queued", "processing"].includes(data.status)) {
        await new Promise((resolve) => window.setTimeout(resolve, 2500));
        data = await captionsApi.get(project.project_id);
      }
      // The server's words are gone at this point, so the editor can't stay open on them: the project page offers retry and restore.
      if (data.status === "failed") { toast.error(data.error_message || "Re-listen failed"); onReplaced(data); return; }
      setWords(data.words);
      setProject((current) => ({ ...current, ...data }));
      autosave.rebase(data.revision);
      setSelectedIndex(0);
      toast.success("A fresh trusted transcript is ready");
    } catch (error) { toast.error(errorMessage(error)); }
    finally { setRelistening(false); }
  };
  const restoreVersion = async (versionId: number) => {
    setRestoring(true);
    try {
      await flushEdits(); // the unsaved edits become part of the restore point the server keeps
      const restored = await captionsApi.restoreVersion(project.project_id, versionId);
      toast.success("Version restored. Your previous edits were kept in History.");
      onReplaced(restored);
    } catch (error) { toast.error(errorMessage(error)); setRestoring(false); }
  };
  const download = async () => {
    try {
      const { blob, extension } = await captionsApi.downloadBlob(project.project_id);
      saveBlob(blob, `captain-title.${extension}`);
    } catch (error) { toast.error(errorMessage(error)); }
  };
  const downloadCaptions = async (format: CaptionFileFormat) => {
    try {
      await flushEdits();
      saveBlob(await captionsApi.subtitlesBlob(project.project_id, format), `captain-title.${format}`);
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

  // --- Keyboard shortcuts (lib/shortcuts.ts lists them; useStudioShortcuts only runs them on /studio URLs) ---
  const searchHits = useMemo(() => {
    const query = searchQuery.trim().toLowerCase();
    if (!query) return [];
    return words.flatMap((word, index) => `${word.text} ${word.original ?? ""}`.toLowerCase().includes(query) ? [index] : []);
  }, [words, searchQuery]);
  const searchHitSet = useMemo(() => new Set(searchHits), [searchHits]);
  const showHud = (text: string) => {
    window.clearTimeout(hudTimerRef.current);
    setHud({ text, key: Date.now() });
    hudTimerRef.current = window.setTimeout(() => setHud(null), 900);
  };
  // The <video> is ahead of the React state while playing, so read it directly.
  const now = () => videoRef.current?.currentTime ?? currentTime;
  const groupOfWord = (index: number) => indexedWordGroups.findIndex((group) => group.some((item) => item.index === index));
  // The caption under the playhead, or in a gap the last one that started before it.
  const groupAtTime = (time: number) => {
    let found = -1;
    indexedWordGroups.forEach((group, i) => { if (group[0].word.start <= time + 0.001) found = i; });
    return found < 0 && indexedWordGroups.length ? 0 : found;
  };
  // "The caption being edited": the picked word's caption, otherwise the one under the playhead.
  const editedGroup = () => {
    const picked = wordPicked ? groupOfWord(selectedIndex) : -1;
    return picked >= 0 ? picked : groupAtTime(now());
  };
  const groupStarts = () => indexedWordGroups.map((group) => group[0].word.start);
  const seekPaused = (time: number) => { videoRef.current?.pause(); seek(Math.min(project.duration, Math.max(0, time))); };
  const editGroup = (offset: 1 | -1) => {
    const target = editedGroup() + offset;
    if (target < 0 || target >= indexedWordGroups.length) return false;
    setLeftTab("captions");
    selectWord(indexedWordGroups[target][0].index);
    setRevealToken((n) => n + 1);
  };
  const deleteWordAt = (position: number) => {
    setWords((current) => current.filter((_, i) => i !== position).map((word, i) => ({ ...word, index: i })));
    setSelectedIndex(Math.max(0, Math.min(position, words.length - 2)));
    setMomentPopover(null);
  };
  const shortcutHandlers: ShortcutHandlers = {
    "shortcuts.show": () => setShortcutsOpen(true),
    save: () => { void saveNow(); },
    undo: () => { if (!history.canUndo) return false; history.undo(); },
    redo: () => { if (!history.canRedo) return false; history.redo(); },
    "element.remove": () => {
      if (!selectedElementId || (leftTab !== "elements" && leftTab !== "text")) return false;
      deleteElement(selectedElementId);
    },
    "play.toggle": () => { if (!videoRef.current) return false; togglePlayback(); },
    play: () => { if (!videoRef.current) return false; void videoRef.current.play(); },
    pause: () => { if (!videoRef.current) return false; videoRef.current.pause(); },
    "frame.prev": () => seekPaused(now() - FRAME_SECONDS),
    "frame.next": () => seekPaused(now() + FRAME_SECONDS),
    "word.prev": () => {
      const t = now();
      const word = [...words].reverse().find((w) => w.start < t - 0.05);
      if (!word) return false;
      seek(word.start);
    },
    "word.next": () => {
      const t = now();
      const word = words.find((w) => w.start > t + 0.01);
      if (!word) return false;
      seek(word.start);
    },
    // Like a media player: from inside a caption "previous" goes back to its start first, then to the caption before.
    "caption.prev": () => {
      const t = now();
      const start = [...groupStarts()].reverse().find((s) => s < t - 0.25);
      seek(start ?? 0);
    },
    "caption.next": () => {
      const t = now();
      const start = groupStarts().find((s) => s > t + 0.01);
      if (start == null) return false;
      seek(start);
    },
    "volume.up": () => { const v = Math.min(1, Math.round((previewVolume + 0.1) * 10) / 10); setPreviewVolume(v); showHud(`Volume ${Math.round(v * 100)}%`); },
    "volume.down": () => { const v = Math.max(0, Math.round((previewVolume - 0.1) * 10) / 10); setPreviewVolume(v); showHud(`Volume ${Math.round(v * 100)}%`); },
    "speed.down": () => { const r = PLAYBACK_RATES[Math.max(0, PLAYBACK_RATES.indexOf(playbackRate) - 1)]; setPlaybackRate(r); showHud(`Speed ${r}×`); },
    "speed.up": () => { const r = PLAYBACK_RATES[Math.min(PLAYBACK_RATES.length - 1, PLAYBACK_RATES.indexOf(playbackRate) + 1)]; setPlaybackRate(r); showHud(`Speed ${r}×`); },
    "caption.search": () => { setLeftTab("captions"); setSearchFocusToken((n) => n + 1); },
    "caption.reveal": () => {
      const group = groupAtTime(now());
      if (group < 0) return false;
      setLeftTab("captions");
      setSelectedIndex(indexedWordGroups[group][0].index);
      setWordPicked(true);
      setRevealToken((n) => n + 1);
    },
    "caption.editPrev": () => editGroup(-1),
    "caption.editNext": () => editGroup(1),
    "caption.playFromStart": () => {
      const group = editedGroup();
      if (group < 0 || !videoRef.current) return false;
      seek(indexedWordGroups[group][0].word.start);
      void videoRef.current.play();
    },
    "caption.split": () => {
      const group = wordPicked ? groupOfWord(selectedIndex) : -1;
      if (group >= 0 && indexedWordGroups[group][0].index !== selectedIndex) splitCaptionBeforeWord(selectedIndex);
      else if (canSplitAtPlayhead) splitAtPlayhead();
      else return false;
    },
    "caption.joinPrev": () => {
      const group = editedGroup();
      if (group <= 0) return false;
      mergeCaptionUp(indexedWordGroups[group][0].index);
    },
    "caption.joinNext": () => {
      const group = editedGroup();
      if (group < 0 || group >= indexedWordGroups.length - 1) return false;
      mergeCaptionUp(indexedWordGroups[group + 1][0].index);
    },
    // Popovers and the drag scope prompt close themselves on Esc; this only lets go of what is picked, one level at a time.
    "selection.clear": () => {
      if (momentPopover != null || scopePrompt) return false;
      if (selectedElementId) setSelectedElementId(null);
      else if (wordPicked) setWordPicked(false);
      else if (timelineMode === "words") setTimelineMode("lines");
      else return false;
    },
    "selection.cut": () => {
      if (!wordPicked || !words[selectedIndex]) return false;
      deleteWordAt(selectedIndex);
    },
    "assistant.toggle": () => {
      if (leftTab === "ai") setLeftTab(prevLeftTabRef.current);
      else { prevLeftTabRef.current = leftTab; setLeftTab("ai"); }
    },
  };
  useStudioShortcuts(shortcutHandlers);
  const shortcutHint = (id: ShortcutId) => {
    const combo = SHORTCUT_BINDINGS[id][0];
    return combo ? comboText(combo) : undefined;
  };

  // The studio's three resizable regions, built once and placed by the layout below (resizable panels on desktop, stacked on phones).
  const toolPanelNode = (<>
    {leftTab === "captions" && <TranscriptPanel words={words} originalTranscript={project.source_transcript} transcriptionMode={project.transcription_mode} activeIndex={activeIndex} selectedIndex={selectedIndex} onSelect={openMoment} onText={(index, text) => updateWord(index, { text })} onNewLine={toggleWordNewLine} onSplitWord={splitWord} onEmphasis={setWordEmphasis} onRelisten={relisten} relistening={relistening} scriptMode={scriptMode} onScriptMode={setScriptMode} searchQuery={searchQuery} onSearchQuery={setSearchQuery} searchHits={searchHits} onJumpTo={selectWord} searchFocusToken={searchFocusToken} onSearchFocused={() => setSearchFocusToken(0)} revealToken={revealToken} />}
    {leftTab === "media" && <MediaPanel project={project} />}
    {leftTab === "audio" && <AudioPanel duration={project.duration}
      value={{ volume: audioVolume, muted: audioMuted, fadeIn: audioFadeIn, fadeOut: audioFadeOut, normalize: audioNormalize }}
      onChange={(c) => {
        if (c.volume !== undefined) setAudioVolume(c.volume);
        if (c.muted !== undefined) setAudioMuted(c.muted);
        if (c.fadeIn !== undefined) setAudioFadeIn(c.fadeIn);
        if (c.fadeOut !== undefined) setAudioFadeOut(c.fadeOut);
        if (c.normalize !== undefined) setAudioNormalize(c.normalize);
      }} />}
    {leftTab === "ratio" && <RatioPanel sourceWidth={project.width} sourceHeight={project.height}
      value={{ ratio: aspectRatio, fit: frameFit, color: frameColor }}
      onChange={(c) => {
        if (c.ratio !== undefined) setAspectRatio(c.ratio);
        if (c.fit !== undefined) setFrameFit(c.fit);
        if (c.color !== undefined) setFrameColor(c.color);
      }} />}
    {leftTab === "elements" && <ElementsPanel elements={elements.filter((item) => item.kind !== "text")} selectedId={selectedElementId} onSelect={setSelectedElementId} onAdd={(char) => addElement(char, "sticker")} onUpdate={updateElement} onDelete={deleteElement} currentTime={currentTime} duration={project.duration} />}
    {leftTab === "text" && <TextPanel elements={elements} selectedId={selectedElementId} onSelect={setSelectedElementId} onAdd={(text) => addElement(text, "text")} onUpdate={updateElement} onDelete={deleteElement} currentTime={currentTime} duration={project.duration} />}
    {leftTab === "adjustment" && <ColorPanel value={{ brightness, contrast, saturation, warmth, tint }}
      onChange={(c: Partial<ColorValues>) => {
        if (c.brightness !== undefined) setBrightness(c.brightness);
        if (c.contrast !== undefined) setContrast(c.contrast);
        if (c.saturation !== undefined) setSaturation(c.saturation);
        if (c.warmth !== undefined) setWarmth(c.warmth);
        if (c.tint !== undefined) setTint(c.tint);
      }} />}
    {leftTab === "ai" && <AiToolsPanel words={words} onDeleteWords={deleteWords} onReassignEmphasis={reassignEmphasis} reassigning={reassigning} />}
    {isCaptionStyleTab(leftTab) && <ThemePanel theme={theme} onTheme={chooseTheme} fontId={fontId} onFont={setFontId}
      wordsPerCaption={wordsPerCaption} onWordsPerCaption={setWordsPerCaption}
      captionLines={captionLines} onCaptionLines={setCaptionLines}
      captionPosition={captionPosition} onCaptionPosition={choosePosition}
      selectedWord={words[selectedIndex]} onWordStyle={(change) => updateWord(selectedIndex, change)}
      tab={themeTab}
      textStyle={textStyle} onTextStyle={updateTextStyle} bold={bold} onBold={setBold} italic={italic} onItalic={setItalic}
      hasActiveCaption={visibleWords.length > 0} boxX={boxX} boxY={boxY} boxFontSize={boxFontSize} boxRotation={boxRotation}
      onCaptionPos={setCaptionPos} onCaptionScale={setCaptionScale} onCaptionRotation={setCaptionRotation}
      onApplyTransformToAll={applyTransformToAll} onResetTransform={resetTransform} />}
  </>);
  const previewNode = (
    <section className="preview-panel" data-testid="video-preview-panel">
      <div className="video-stage" ref={stageRef}>
      <div className="video-frame" ref={videoFrameRef} data-ratio={aspectRatio}
        style={{ width: `${frameBox.w}px`, height: `${frameBox.h}px`, background: reframed && frameFit === "color" ? frameColor : undefined }}>
        <WhiteBalanceFilter warmth={warmth} tint={tint} />
        {reframed && frameFit === "blur" && <BlurFill videoRef={videoRef} aspect={frameAspect}
          filter={`${videoFilter} blur(${Math.max(4, Math.round(48 * previewScale / 1.1))}px)`} />}
        <video ref={videoRef} src={mediaUrl ?? undefined} style={{ filter: videoFilter, objectFit: reframed && frameFit === "crop" ? "cover" : "contain" }} onLoadedMetadata={(e) => { if (pendingSeekRef.current > 0) { e.currentTarget.currentTime = Math.min(pendingSeekRef.current, e.currentTarget.duration || pendingSeekRef.current); pendingSeekRef.current = 0; } setMediaFailed(false); }} onError={() => { pendingSeekRef.current = currentTime; if (mediaRetry < 2) window.setTimeout(() => setMediaRetry((n) => n + 1), 1200); else setMediaFailed(true); }} onTimeUpdate={(e) => setCurrentTime(e.currentTarget.currentTime)} onPlay={() => setPlaying(true)} onPause={() => setPlaying(false)} playsInline data-testid="caption-video-player" />
        {hud && <div key={hud.key} className="preview-hud" role="status" data-testid="preview-shortcut-hud">{hud.text}</div>}
        {mediaFailed && <div className="preview-media-error" data-testid="video-unavailable-message">This video couldn't be loaded. Your caption edits are safe. Try reloading the page.</div>}
        <button className="preview-play" onClick={togglePlayback} data-testid="video-preview-play-button"><Play size={28} weight="fill" /></button>
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
                  fontStyle: italic ? "italic" : "normal", textTransform: upperCase ? "uppercase" : undefined,
                  // WebkitTextStroke / textShadow are always set explicitly (never left to the stylesheet's default rule) so every Outline/
                  // Shadow combination - including both off - renders exactly what the controls say, matching the export 1:1.
                  WebkitTextStroke: outlineStroke, textShadow: dropShadow }}
          onMouseDown={startCaptionDrag} data-testid="live-caption-preview">
          {/* Plain text flow with a literal line break between rows - no per-row wrapper with hand-computed margins. The browser lays out
              stacked lines with each line's own natural metrics, exactly the way libass stacks \N-separated lines in the export. Doodle
              Marker uses ONE size for every tier, so words flow on one line (null tiers = only an explicit two-line midpoint or a word's own
              new_line breaks), mirroring the server's break_tiers override. */}
          {splitIntoRows(visibleWords, theme === DOODLE_MARKER || flowLines ? null : tiers, captionLines).map((row, rowIndex) => <Fragment key={rowIndex}>
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
              // Karaoke colour (export: an ASS colour switch for the word's spoken time); the manual Emphasize accent still wins.
              const style: React.CSSProperties = { color: isActiveWord && activeColor && !word.emphasize ? activeColor : word.color, fontSize: `${Math.max(20, word.font_size * previewScale)}px`, fontFamily,
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
      <div className="preview-meta"><span data-testid="preview-format-label">9:16 PREVIEW</span>
        <label className="preview-safe-zone-switch" title="Show the Reels/Shorts safe-zone guide"><FrameCorners size={13} weight={safeZone ? "fill" : "regular"} /> SAFE ZONE
          <Switch size="sm" checked={safeZone} onCheckedChange={setSafeZone} data-testid="safe-zone-toggle-button" /></label><span data-testid="caption-engine-label">WORD SYNC · {theme.toUpperCase()}</span></div>
    </section>
  );
  const timelineNode = (
    <Timeline words={words} groups={indexedWordGroups} duration={project.duration} currentTime={currentTime} playing={playing} onToggle={togglePlayback} onSeek={seek} selectedIndex={selectedIndex} onSelect={openMoment} onRetimeWord={updateWord} mediaUrl={mediaUrl} sfxMarkers={sfxMarkers} onAddSfx={addSfxMarker} onMoveSfx={moveSfxMarker} onDeleteSfx={deleteSfxMarker} scriptMode={scriptMode} onSplitAtPlayhead={splitAtPlayhead} canSplitAtPlayhead={canSplitAtPlayhead} mode={timelineMode} onModeChange={setTimelineMode} revealToken={revealToken} searchHits={searchHitSet} playShortcut={shortcutHint("play.toggle")} />
  );

  return (
    <main className="studio" data-testid="caption-editor-workspace">
      <HeaderLeft>
        <button className="header-back" onClick={onNew} data-testid="new-project-button"><ArrowLeft size={15} weight="bold" /> All projects</button>
        <span className="header-divider" aria-hidden="true" />
        <ProjectNameEditor projectId={project.project_id} name={project.name} onRenamed={(name) => setProject((current) => ({ ...current, name }))} />
      </HeaderLeft>
      <HeaderRight>
        <div className="header-tools" data-testid="editor-history-tools">
          <button className="header-icon-button" onClick={history.undo} disabled={!history.canUndo} title={`Undo${shortcutHint("undo") ? ` (${shortcutHint("undo")})` : ""}`} aria-label="Undo" data-testid="undo-button">
            <ArrowCounterClockwise size={18} />
          </button>
          <button className="header-icon-button" onClick={history.redo} disabled={!history.canRedo} title={`Redo${shortcutHint("redo") ? ` (${shortcutHint("redo")})` : ""}`} aria-label="Redo" data-testid="redo-button">
            <ArrowClockwise size={18} />
          </button>
          <span className="header-tools-divider" aria-hidden="true" />
          <VersionHistoryMenu projectId={project.project_id} busy={restoring} onRestore={restoreVersion} />
          <span className="header-tools-divider" aria-hidden="true" />
          <button className="header-icon-button" onClick={() => setShortcutsOpen(true)} aria-haspopup="dialog" aria-expanded={shortcutsOpen}
            title={`Keyboard shortcuts${shortcutHint("shortcuts.show") ? ` (${shortcutHint("shortcuts.show")})` : ""}`} aria-label="Keyboard shortcuts"
            data-testid="open-shortcuts-dialog-button">
            <Keyboard size={18} />
          </button>
        </div>
        <button className={`export-button save-button is-${autosave.status}`} onClick={() => void saveNow()}
          disabled={autosave.status === "saving" || autosave.status === "conflict"}
          title={`${autosave.status === "error" ? `Not saved: ${autosave.error ?? "try again"}` : "Save now"}${shortcutHint("save") ? ` (${shortcutHint("save")})` : ""}`}
          data-testid="save-project-button">
          {autosave.status === "saving" ? <SpinnerGap className="spin" size={18} />
            : autosave.status === "saved" ? <CheckCircle size={18} weight="bold" />
            : autosave.status === "error" ? <WarningCircle size={18} weight="bold" />
            : <FloppyDisk size={18} weight="bold" />}
          {autosave.status === "saving" ? "Saving…" : autosave.status === "saved" ? "Saved" : autosave.status === "error" ? "Retry" : "Save"}
        </button>
        <button className="export-button" onClick={() => setExportOpen(true)} data-testid="open-export-dialog-button">
          {rendering ? <SpinnerGap className="spin" size={18} /> : <Export size={18} weight="bold" />} {rendering ? `Exporting ${Math.round(renderProgress)}%` : "Export"}
        </button>
      </HeaderRight>
      <ExportDialog open={exportOpen} onOpenChange={setExportOpen}
        frameSize={reframed || (project.width && project.height) ? [outputWidth, outputHeight] : null}
        resolution={renderResolution} onResolution={setRenderResolution} format={renderFormat} onFormat={setRenderFormat}
        quality={renderQuality} onQuality={setRenderQuality} rendering={rendering} renderProgress={renderProgress}
        exportIsCurrent={exportIsCurrent} hasExport={project.rendered_revision != null} onExport={render} onDownload={download} onDownloadCaptions={downloadCaptions} />
      <ShortcutsDialog open={shortcutsOpen} onOpenChange={setShortcutsOpen} />
      {autosave.status === "conflict" && <div className="studio-conflict-banner" role="alert" data-testid="save-conflict-banner">
        <span>{autosave.error}</span>
        <button onClick={() => window.location.reload()} data-testid="save-conflict-reload-button">Reload</button>
      </div>}
      {/* Rail runs the full height on the left; the tool panel + preview sit above the timeline, which starts just right of the rail. */}
      <div className="studio-body">
        <LeftRail active={leftTab} onSelect={setLeftTab} />
        {narrowLayout ? <div className="studio-grid">{toolPanelNode}{previewNode}</div> : (
          // Desktop: drag the tool panel's right edge to change its width; the timeline keeps a fixed height under the preview.
          // Same approach as tscaps' editor (react-resizable-panels; the width is saved in localStorage via autoSaveId).
          <PanelGroup direction="horizontal" autoSaveId="captain-title:studio-columns" className="studio-panels" style={{ overflow: "visible" }}>
            <Panel id="studio-tools" order={1} defaultSize={30} minSize={20} maxSize={50} className="studio-tools-slot">
              {toolPanelNode}
            </Panel>
            <PanelResizeHandle className="ct-resize-handle ct-resize-handle-col" title="Drag to resize the panel" data-testid="tool-panel-resize-handle"><i /></PanelResizeHandle>
            {/* overflow visible: the fixed-height timeline runs out to the window's right edge, as it always did. */}
            <Panel id="studio-stage" order={2} minSize={35} className="studio-stage" style={{ overflow: "visible" }}>
              {previewNode}
              {timelineNode}
            </Panel>
          </PanelGroup>
        )}
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
        {narrowLayout && timelineNode}
      </div>
    </main>
  );
};