#!/usr/bin/env python3
"""Generate golden fixtures from the ORIGINAL Python Captain Title backend.

Usage:  python3 -I scripts/old-sync/generate_golden.py <path-to-old>/backend <out-dir>

Stubs the third-party modules (fastapi, openai, sarvamai, imageio_ffmpeg, auth) so the pure functions in
caption_ai.py / captions.py can be imported without the old dependency stack, then dumps their outputs
for a fixed set of inputs. The C# test-suite (AppBackendBasic.Captions.Tests) asserts the port produces
byte-identical output. Re-run this whenever the old project's caption logic changes.
"""
import json, sys, types, random
sys.dont_write_bytecode = True
from pathlib import Path

old_backend, out_dir = Path(sys.argv[1]).resolve(), Path(sys.argv[2]).resolve()
sys.path.insert(0, str(old_backend))

def stub(name, **attrs):
    m = types.ModuleType(name)
    for k, v in attrs.items(): setattr(m, k, v)
    sys.modules[name] = m
    return m

class _Router:
    def __init__(self, *a, **k): pass
    def __getattr__(self, _):
        return lambda *a, **k: (lambda f: f)
class _HTTPException(Exception):
    def __init__(self, status_code, detail=None): self.status_code, self.detail = status_code, detail
stub("fastapi", APIRouter=_Router, Depends=lambda *a, **k: None, File=lambda *a, **k: None, Form=lambda *a, **k: None,
     Header=lambda *a, **k: None, HTTPException=_HTTPException, Request=object, Response=object, UploadFile=object)
stub("imageio_ffmpeg", get_ffmpeg_exe=lambda: "ffmpeg")
stub("openai", AsyncOpenAI=object)
stub("sarvamai", AsyncSarvamAI=object)
stub("auth", current_user=lambda: None)

import caption_ai, captions  # noqa: E402

out = {}
# ---- static tables (seed fidelity) ----
out["tables"] = {"themes": captions.THEMES, "fonts": captions.FONTS, "variants": captions.VARIANTS,
                 "sfx": captions.SFX_TONE_PARAMS, "positions": captions.POSITION_PRESETS,
                 "quality": captions.RENDER_QUALITY_PRESETS}

# ---- difflib ratios + alignment/stitching ----
pairs = [("hello", "hallo"), ("kya", "kyaa"), ("", ""), ("a", ""), ("matlab", "matlab"), ("toh", "to"),
         ("iPhone", "iphone"), ("bahut", "bohot"), ("abcde", "xabcdey"), ("नमस्ते", "namaste")]
out["ratio"] = [{"a": a, "b": b, "ratio": caption_ai._similarity(a, b)} for a, b in pairs]

def w(i, t, s, e): return {"index": i, "original": t, "text": t, "start": s, "end": e}
cases = [
    ([w(0, "Hello", 0.0, 0.4), w(1, "world", 0.5, 0.9), w(2, "this", 1.0, 1.2), w(3, "is", 1.3, 1.4)],
     "Hello duniya this is great"),
    ([w(0, "kya", 0.1, 0.3), w(1, "haal", 0.4, 0.7), w(2, "hai", 0.8, 1.0)], "kya haal hai bhai"),
    ([w(0, "extra", 0.0, 0.2), w(1, "kya", 0.3, 0.5), w(2, "haal", 0.6, 0.9)], "kya haal"),
    ([w(0, "a", 0.0, 0.1)], "completely different many words here now"),
    ([w(0, "one", 0, 1), w(1, "two", 1, 2), w(2, "three", 2, 3), w(3, "four", 3, 4), w(4, "five", 4, 5)], "one three five six seven"),
]
out["stitch"] = [{"whisper": ww, "sarvam": s, "result": caption_ai._stitch_chunk([dict(x) for x in ww], s)} for ww, s in cases]
out["align"] = [{"whisper": [x["original"] for x in ww], "sarvam": s.split(),
                 "ops": caption_ai._align_words([x["original"] for x in ww], s.split())} for ww, s in cases]

# ---- repetition / tokens ----
token_inputs = ["Hello, world! Yeh kya HAI?", "नमस्ते दुनिया kya", "a a a a a a a a a a a a a a",
                "one two one two one two one two one two one two one two", "x", ""]
out["tokens"] = [{"text": t, "tokens": caption_ai._tokens(t), "metrics": caption_ai._repetition_metrics(caption_ai._tokens(t))} for t in token_inputs]

# ---- chunk boundaries ----
out["chunks"] = [{"total": t, "silence": s, "seconds": sec, "snap": sn, "result": caption_ai._chunk_boundaries(t, s, sec, sn)}
                 for t, s, sec, sn in [(65.0, [10.2, 19.5, 21.0, 39.0, 44.4, 61.0], 20, 2.0), (12.0, [], 20, 2.0), (45.5, [18.1, 22.3, 41.7], 20, 2.0)]]
out["silence_midpoints"] = {"stderr": "silence_start: 1.5\nsilence_end: 2.5 | silence_duration: 1\nsilence_start: 10.25\nsilence_end: 11.75",
                            "result": [2.0, 11.0]}

# ---- normalize timings ----
nw = [w(0, "a", 0.5, 0.5), w(1, "b", 0.5, 0.55), w(2, "c", 0.4, 0.9), w(3, "d", 1.0, 1.0)]
out["normalize"] = {"input": nw, "result": caption_ai._normalize_word_timings([dict(x) for x in nw])}

# ---- whisper candidate ----
resp = {"words": [{"word": " Hello", "start": 0.0, "end": 0.4}, {"word": "world.", "start": 0.5, "end": 0.9}, {"word": "late", "start": 9.0, "end": 9.4}],
        "segments": [{"text": " Hello world.", "start": 0.0, "end": 1.0, "avg_logprob": -0.3, "no_speech_prob": 0.9},
                     {"text": "", "start": 1.0, "end": 2.0}, {"text": "ignored", "start": 3, "end": 4, "avg_logprob": -0.5, "no_speech_prob": 0.8}]}
out["whisper"] = [{"response": r, "skip": s, "result": caption_ai._build_whisper_candidate(r, s)}
                  for r, s in [(resp, 0.0), (resp, 0.45), ({"words": [{"word": "x", "start": 1, "end": 2}]}, 0.0),
                               ({"segments": [{"text": "alpha beta gamma", "start": 0, "end": 3, "avg_logprob": -0.2}]}, 0.0),
                               ({"words": [], "segments": [{"text": "bad", "start": 0, "end": 1, "avg_logprob": -2.0}]}, 0.0)]]

# ---- layout / ass / srt ----
def cw(i, text, s, e, **kw):
    d = {"index": i, "text": text, "original": kw.pop("original", text), "start": s, "end": e, "important": False, "emphasis": "support",
         "uncertain": False, "color": "#FFFFFF", "font_size": 52, "pos_x": None, "pos_y": None, "new_line": None,
         "box_width": None, "box_height": None, "kinetic": None, "emphasize": False}
    d.update(kw); return d

words = [cw(0, "Yeh", 0.0, 0.2, emphasis="support"), cw(1, "BAHUT", 0.25, 0.6, emphasis="key", important=True, color="#FF3B30", font_size=62),
         cw(2, "acchi", 0.65, 0.9, emphasis="feature", important=True), cw(3, "video", 0.95, 1.3, original="video."),
         cw(4, "hai", 1.35, 1.6, new_line=True), cw(5, "{bold}", 2.5, 2.9, emphasize=True), cw(6, "x", 3.0, 3.2, pos_x=40.5, pos_y=70.25, font_size=80),
         cw(7, "y", 3.25, 3.5), cw(8, "z", 3.55, 3.9), cw(9, "w", 4.0, 4.2)]
out["group"] = [{"words_per_caption": n, "groups": [[x["index"] for x in g] for g in captions._group_words_for_display(words, max_words=n or 3, strict_count=n is not None)]} for n in (None, 1, 2, 4)]
elements = [{"id": "e1", "kind": "sticker", "char": "★", "font_id": "poppins", "color": "#FF00AA", "font_size": 120, "start": 0, "end": 2, "pos_x": 50, "pos_y": 30},
            {"id": "e2", "kind": "text", "char": "Hi {x}", "font_id": "anton", "color": "#00FF00", "font_size": 48, "start": 1, "end": 1, "pos_x": 10, "pos_y": 90},
            {"id": "e3", "kind": "text", "char": "Title", "font_id": "bogus", "color": "#12", "font_size": 60, "start": 1.5, "end": 3.25, "pos_x": 33.3, "pos_y": 66.6}]
ass_cases = []
for theme in captions.THEMES:
    for variant in (None, "rock", "glow"):
        for lines, pos, wpc in ((1, "bottom", None), (2, "top", 2), (1, "middle", 4)):
            p = Path("/tmp/_golden.ass")
            captions._write_ass(str(p), words, theme, "bebas" if theme == "minimal" else "poppins", wpc, lines, pos, elements, variant)
            ass_cases.append({"theme": theme, "variant": variant, "lines": lines, "position": pos, "wpc": wpc, "font": "bebas" if theme == "minimal" else "poppins", "ass": p.read_text(encoding="utf-8")})
out["ass"] = {"words": words, "elements": elements, "cases": ass_cases}
out["srt"] = {"words": words, "srt": captions._write_srt(words)}
out["ass_time"] = [{"s": s, "r": captions._ass_time(s)} for s in (0, 0.005, 1.994, 59.999, 61.5, 3599.5, 3661.234)]
out["ass_color"] = [{"h": h, "r": captions._ass_color(h)} for h in ("#FF3B30", "#12", "FFF", "#abcdef01")]

# ---- ffmpeg render args ----
captured = []
class _R: returncode = 0; stderr = ""
captions.subprocess.run = lambda args, **k: (captured.append(list(args)), _R())[1]
render_cases = []
for muted, vol, q, sfx in ((False, 100, "high", []), (True, 150, "draft", []),
                           (False, 80, "high", [{"type": "whoosh", "time": 1.5}, {"type": "pop", "time": 0.123}, {"type": "unknown", "time": 2}])):
    captured.clear()
    captions._render("/tmp/w/source.mp4", "/tmp/w/captions.ass", "/tmp/w/out.mp4", "/opt/fonts", vol, muted, -10, 120, 90, q, sfx_markers=sfx)
    render_cases.append({"muted": muted, "volume": vol, "quality": q, "sfx": sfx, "args": captured[0]})
out["render"] = render_cases
out["sfx_filter"] = [{"type": t, "filter": captions._sfx_source_filter(t, "L")} for t in captions.SFX_TONE_PARAMS]

(out_dir / "golden.json").write_text(json.dumps(out, ensure_ascii=False, indent=1, default=list), encoding="utf-8")
print("wrote", out_dir / "golden.json")
