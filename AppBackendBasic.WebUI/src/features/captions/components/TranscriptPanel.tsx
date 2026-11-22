import { useEffect, useMemo, useRef, useState } from "react";
import { ArrowClockwise, CaretDown, CaretUp, ListBullets, MagnifyingGlass, Scissors, TextAa, X } from "@phosphor-icons/react";
import type { CaptionWord, ScriptMode } from "../types";

const SENTENCE_END_CHARS = ".!?।॥"; // includes Devanagari danda/double danda
const CHUNK_SIZE = 4; // short, scannable cards instead of one tall sentence block

interface IndexedWord { word: CaptionWord; index: number }

function endsSentence(word: CaptionWord) {
  const original = (word.original || word.text || "").trim();
  return !!original && SENTENCE_END_CHARS.includes(original[original.length - 1]);
}

// Captik's left panel reads like a script, but in short, scannable chunks -
// a card breaks every CHUNK_SIZE words, or earlier at a sentence end (so a
// short sentence still gets its own clean card instead of bleeding into the
// next one). Groups independently from the on-screen caption/timeline cue
// groups, straight off the flat words array using real array position as
// the index (same addressing every other editor control already relies on).
function groupIntoChunks(words: CaptionWord[]) {
  const chunks: IndexedWord[][] = [];
  let current: IndexedWord[] = [];
  words.forEach((word, index) => {
    current.push({ word, index });
    if (endsSentence(word) || current.length >= CHUNK_SIZE) {
      chunks.push(current);
      current = [];
    }
  });
  if (current.length) chunks.push(current);
  return chunks;
}

const EMPHASIS_TIERS = [
  { id: "support", label: "Support" },
  { id: "feature", label: "Feature" },
  { id: "key", label: "Key" },
];

interface WordPopoverProps {
  word: CaptionWord
  index: number
  anchor: { x: number; y: number }
  onClose: () => void
  onText: (index: number, text: string) => void
  onNewLine: (index: number) => void
  onSplitWord: (index: number, pos: number) => void
  onEmphasis: (index: number, tier: string) => void
}

const WordPopover = ({ word, index, anchor, onClose, onText, onNewLine, onSplitWord, onEmphasis }: WordPopoverProps) => {
  const [draft, setDraft] = useState(word.text);
  const inputRef = useRef<HTMLInputElement>(null);
  const popoverRef = useRef<HTMLDivElement>(null);

  useEffect(() => { setDraft(word.text); }, [word.text]);
  useEffect(() => { inputRef.current?.focus(); inputRef.current?.select(); }, []);

  useEffect(() => {
    const onDocMouseDown = (e: MouseEvent) => { if (popoverRef.current && !popoverRef.current.contains(e.target as Node)) onClose(); };
    const onKeyDown = (e: KeyboardEvent) => { if (e.key === "Escape") onClose(); };
    window.addEventListener("mousedown", onDocMouseDown);
    window.addEventListener("keydown", onKeyDown);
    return () => { window.removeEventListener("mousedown", onDocMouseDown); window.removeEventListener("keydown", onKeyDown); };
  }, [onClose]);

  const commitText = () => { if (draft.trim() && draft !== word.text) onText(index, draft.trim()); };
  const splitHere = () => {
    const pos = inputRef.current?.selectionStart ?? Math.ceil(draft.length / 2);
    commitText();
    onSplitWord(index, pos);
    onClose();
  };

  const left = Math.min(anchor.x, Math.max(8, window.innerWidth - 272));
  const top = Math.min(anchor.y, window.innerHeight - 220);

  return (
    <div ref={popoverRef} className="fixed z-50 w-64 rounded-lg border border-[var(--ct-border)] bg-[var(--ct-panel)] p-3 shadow-xl"
      style={{ left, top }} data-testid={`script-word-popover-${index}`}>
      <div className="flex items-center justify-between mb-2">
        <span className="text-[10px] font-semibold tracking-[.14em] text-[var(--ct-muted)]">EDIT WORD</span>
        <button onClick={onClose} className="text-[var(--ct-muted)] hover:text-[var(--ct-text)]" data-testid="script-popover-close"><X size={13} weight="bold" /></button>
      </div>
      <input ref={inputRef} value={draft} onChange={(e) => setDraft(e.target.value)}
        onKeyDown={(e) => { if (e.key === "Enter") { commitText(); onClose(); } }}
        onBlur={commitText}
        className="w-full rounded border border-[var(--ct-border)] bg-[var(--ct-field)] px-2 py-1.5 text-sm text-[var(--ct-text)] outline-none focus:border-[var(--ct-accent)]"
        data-testid={`script-popover-input-${index}`} />
      <div className="mt-2 grid grid-cols-2 gap-1.5">
        <button onClick={splitHere} className="flex items-center justify-center gap-1 rounded bg-[var(--ct-field)] px-2 py-1.5 text-[11px] font-semibold text-[var(--ct-text-2)] hover:bg-[var(--ct-hover)]"
          title="Split this word into two, at the cursor" data-testid={`script-split-word-${index}`}>
          <Scissors size={12} /> Split word
        </button>
        <button onClick={() => { onNewLine(index); onClose(); }} className="flex items-center justify-center gap-1 rounded bg-[var(--ct-field)] px-2 py-1.5 text-[11px] font-semibold text-[var(--ct-text-2)] hover:bg-[var(--ct-hover)]"
          title="Toggle a caption line break before this word" data-testid={`script-split-line-${index}`}>
          <TextAa size={12} /> Split line
        </button>
      </div>
      <div className="mt-2">
        <span className="block mb-1 text-[10px] font-semibold tracking-[.1em] text-[var(--ct-muted)]">EMPHASIS</span>
        <div className="grid grid-cols-3 gap-1.5">
          {EMPHASIS_TIERS.map((tier) => <button key={tier.id} onClick={() => { onEmphasis(index, tier.id); onClose(); }}
            className={`rounded px-1.5 py-1 text-[11px] font-semibold ${word.emphasis === tier.id ? "bg-[var(--ct-accent)] text-[var(--ct-on-accent)]" : "bg-[var(--ct-field)] text-[var(--ct-text-2)] hover:bg-[var(--ct-hover)]"}`}
            data-testid={`script-emphasis-${tier.id}-${index}`}>{tier.label}</button>)}
        </div>
      </div>
    </div>
  );
};

// One caption line/cue per card (same grouping the live preview and the
// final render use), with its words flowing inline - matches how a cue
// actually reads on screen, instead of one disconnected row per word.
interface TranscriptPanelProps {
  words: CaptionWord[]
  originalTranscript: string
  transcriptionMode: string | null
  activeIndex: number
  selectedIndex: number
  onSelect: (index: number, anchorEl?: HTMLElement) => void
  onText: (index: number, text: string) => void
  onNewLine: (index: number) => void
  onSplitWord: (index: number, pos: number) => void
  onEmphasis: (index: number, tier: string) => void
  onRelisten: () => void
  relistening: boolean
  scriptMode?: ScriptMode
  onScriptMode?: (mode: ScriptMode) => void
  /** Caption search (also highlighted on the Timeline): the query, and the matching word indices in order. */
  searchQuery: string
  onSearchQuery: (query: string) => void
  searchHits: number[]
  /** Selects and seeks to a word without opening its popover - used to step through search matches. */
  onJumpTo: (index: number) => void
  /** Changes when the search shortcut asks for the search box to take focus. */
  searchFocusToken?: number
  onSearchFocused?: () => void
  /** Changes when the "go to the caption under the playhead" shortcut asks to scroll the picked word into view. */
  revealToken?: number
}

export const TranscriptPanel = ({ words, originalTranscript, transcriptionMode, activeIndex, selectedIndex,
                                  onSelect, onText, onNewLine, onSplitWord, onEmphasis, onRelisten, relistening,
                                  scriptMode = "roman", onScriptMode, searchQuery, onSearchQuery, searchHits, onJumpTo,
                                  searchFocusToken = 0, onSearchFocused, revealToken = 0 }: TranscriptPanelProps) => {
  const sentences = useMemo(() => groupIntoChunks(words), [words]);
  const [popover, setPopover] = useState<{ index: number; x: number; y: number } | null>(null);
  const cardRefs = useRef<Record<number, HTMLDivElement | null>>({});
  const searchRef = useRef<HTMLInputElement>(null);
  const hitSet = useMemo(() => new Set(searchHits), [searchHits]);
  const hitPosition = searchHits.indexOf(selectedIndex);

  // Handled once, then cleared by the studio, so reopening the Script tab later does not grab focus again.
  useEffect(() => {
    if (!searchFocusToken) return;
    searchRef.current?.focus();
    searchRef.current?.select();
    onSearchFocused?.();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchFocusToken]);
  useEffect(() => {
    if (!revealToken) return;
    const sentenceIndex = sentences.findIndex((sentence) => sentence.some(({ index }) => index === selectedIndex));
    cardRefs.current[sentenceIndex]?.scrollIntoView({ block: "center", behavior: "smooth" });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [revealToken]);

  // Steps from the picked word to the next/previous match, wrapping around, so Enter keeps walking through the results.
  const stepHit = (direction: 1 | -1) => {
    if (!searchHits.length) return;
    const next = direction === 1 ? searchHits.find((index) => index > selectedIndex) ?? searchHits[0]
      : [...searchHits].reverse().find((index) => index < selectedIndex) ?? searchHits[searchHits.length - 1];
    onJumpTo(next);
    const sentenceIndex = sentences.findIndex((sentence) => sentence.some(({ index }) => index === next));
    cardRefs.current[sentenceIndex]?.scrollIntoView({ block: "center", behavior: "smooth" });
  };

  const activeSentenceIndex = sentences.findIndex((sentence) => sentence.some(({ index }) => index === activeIndex));

  // Keep whichever card is currently being spoken centered in view, without
  // fighting a manual scroll on every currentTime tick - only re-run when
  // the active sentence itself changes.
  useEffect(() => {
    if (activeSentenceIndex < 0) return;
    cardRefs.current[activeSentenceIndex]?.scrollIntoView({ block: "center", behavior: "smooth" });
  }, [activeSentenceIndex]);

  const openPopover = (index: number, target: HTMLElement) => {
    const rect = target.getBoundingClientRect();
    setPopover({ index, x: rect.left, y: rect.bottom + 6 });
  };

  return (
    <aside className="transcript-panel" data-testid="transcript-editor-panel">
      <div className="panel-heading"><ListBullets size={18} /><div><span>HINGLISH TRANSCRIPT</span><h2 data-testid="transcript-panel-heading">Script</h2></div></div>
      <div className="script-mode-toggle" data-testid="script-mode-toggle">
        <button className={scriptMode === "roman" ? "active" : ""} onClick={() => onScriptMode?.("roman")} data-testid="script-mode-roman-button">English Letters</button>
        <button className={scriptMode === "native" ? "active" : ""} onClick={() => onScriptMode?.("native")} data-testid="script-mode-native-button">Native Script</button>
      </div>
      <div className="raw-transcript" data-testid="whisper-raw-transcript">
        <div className="raw-transcript-head">
          <span data-testid="whisper-raw-transcript-label">TRANSCRIPT · {transcriptionMode === "hi" ? "HINDI PASS" : "ADAPTIVE PASS"}</span>
          <button onClick={onRelisten} disabled={relistening} data-testid="retranscribe-project-button"><ArrowClockwise size={13} className={relistening ? "spin" : ""} /> {relistening ? "Listening…" : "Re-listen"}</button>
        </div>
        <p data-testid="whisper-raw-transcript-text">{originalTranscript || words.map((word) => word.original).join(" ")}</p>
      </div>
      <div className="script-search" data-testid="script-search">
        <MagnifyingGlass size={14} />
        <input ref={searchRef} value={searchQuery} onChange={(e) => onSearchQuery(e.target.value)} placeholder="Search captions"
          onKeyDown={(e) => {
            if (e.key === "Enter") { e.preventDefault(); stepHit(e.shiftKey ? -1 : 1); }
            else if (e.key === "Escape") { e.preventDefault(); onSearchQuery(""); e.currentTarget.blur(); }
          }}
          aria-label="Search captions" data-testid="script-search-input" />
        {searchQuery.trim() && <>
          <span data-testid="script-search-count">{searchHits.length ? `${hitPosition >= 0 ? hitPosition + 1 : "–"}/${searchHits.length}` : "0"}</span>
          <button onClick={() => stepHit(-1)} disabled={!searchHits.length} title="Previous match (Shift+Enter)" aria-label="Previous match"><CaretUp size={12} /></button>
          <button onClick={() => stepHit(1)} disabled={!searchHits.length} title="Next match (Enter)" aria-label="Next match"><CaretDown size={12} /></button>
          <button onClick={() => onSearchQuery("")} title="Clear search" aria-label="Clear search"><X size={11} /></button>
        </>}
      </div>
      <div className="transcript-scroll" data-testid="editable-transcript-list">
        {sentences.map((sentence, sentenceIndex) => {
          const start = sentence[0].word.start, end = sentence[sentence.length - 1].word.end;
          const isActive = sentenceIndex === activeSentenceIndex;
          return <div key={sentence[0].index} ref={(el) => { cardRefs.current[sentenceIndex] = el; }}
            className={`rounded-md border p-2.5 transition-colors ${isActive ? "border-[var(--ct-accent-line)] bg-[var(--ct-accent-soft)]" : "border-[var(--ct-border)] bg-[var(--ct-field)]"}`}
            data-testid={`script-sentence-${sentenceIndex}`}>
            <div className="mb-1.5 flex items-center justify-between">
              <span className="grid h-[18px] w-[18px] place-items-center rounded-full bg-[var(--ct-field)] text-[10px] font-semibold text-[var(--ct-muted)]" data-testid={`script-sentence-number-${sentenceIndex}`}>{sentenceIndex + 1}</span>
              <span className="font-mono text-[9px] text-[var(--ct-muted)]" data-testid={`script-sentence-time-${sentenceIndex}`}>{start.toFixed(1)}s → {end.toFixed(1)}s</span>
            </div>
            <div className="flex flex-wrap items-baseline gap-y-1">
              {sentence.map(({ word, index }) => <button key={index} type="button"
                onClick={(e) => onSelect(index, e.currentTarget)}
                onDoubleClick={(e) => { e.preventDefault(); openPopover(index, e.currentTarget); }}
                onContextMenu={(e) => { e.preventDefault(); openPopover(index, e.currentTarget); }}
                className={`mr-1 flex flex-col items-start rounded px-2 py-1 leading-none transition-colors ${
                  activeIndex === index ? "bg-[var(--ct-accent)] text-[var(--ct-on-accent)]"
                    : selectedIndex === index ? "bg-[var(--ct-accent-soft)] text-[var(--ct-text)] ring-1 ring-[var(--ct-accent)]"
                    : "bg-[var(--ct-field)] text-[var(--ct-text-2)] hover:bg-[var(--ct-hover)] hover:text-[var(--ct-text)]"
                } ${word.important || word.emphasis === "key" ? "font-bold" : ""} ${hitSet.has(index) ? "search-hit" : ""}`}
                title={word.uncertain ? "Uncertain transcription - double-click to fix the spelling" : "Click to seek · double-click or right-click to edit"}
                data-testid={`script-word-${index}`} data-uncertain={word.uncertain ? "true" : undefined}>
                <span className="text-[13px]" data-testid={`script-word-text-${index}`}>
                  {scriptMode === "native" ? (word.original || word.text) : word.text}
                </span>
              </button>)}
            </div>
          </div>;
        })}
      </div>
      {popover && <WordPopover word={words[popover.index]} index={popover.index} anchor={popover} onClose={() => setPopover(null)}
        onText={onText} onNewLine={onNewLine} onSplitWord={onSplitWord} onEmphasis={onEmphasis} />}
    </aside>
  );
};
