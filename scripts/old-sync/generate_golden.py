#!/usr/bin/env python3
"""Generate golden fixtures from the ORIGINAL Python Captain Title backend.

Usage:  python3 scripts/old-sync/generate_golden.py <path-to-old>/backend <out-dir>
Needs Pillow (real glyph measurement for the Doodle Marker decorations) and indic_transliteration==2.3.82
(pip install --target <dir> indic_transliteration==2.3.82, then set CT_PYLIB=<dir>; no -I flag so the env var is honoured).

Stubs the third-party modules (fastapi, openai, sarvamai, imageio_ffmpeg, auth) so the pure functions in
caption_ai.py / captions.py can be imported without the old dependency stack, then dumps their outputs
for a fixed set of inputs. The C# test-suite (AppBackendBasic.Captions.Tests) asserts the port produces
byte-identical output. Re-run this whenever the old project's caption logic changes.
"""
import json, os, sys, types, random, tempfile
sys.dont_write_bytecode = True
from pathlib import Path
if os.environ.get("CT_PYLIB"): sys.path.insert(0, os.environ["CT_PYLIB"])

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

# The old app measures Doodle Marker words with PIL/FreeType, whose hinted advances can't be reproduced outside FreeType. The C# port sums per-glyph
# advances straight from the font file (cmap + hmtx), rounded to whole pixels. To test everything ELSE exactly, the ASS fixtures below are generated
# with that same unhinted measurer patched in; the real PIL widths are exported separately ("measure") and compared with a tolerance in C#.
import struct, math
def _unhinted_width(text, family, size, _cache={}):
    path = captions.FONTS_DIR / f"{family}-Bold.ttf"
    if path not in _cache:
        d = path.read_bytes()
        n = struct.unpack(">H", d[4:6])[0]
        T = {d[12 + i * 16:16 + i * 16].decode(): struct.unpack(">II", d[20 + i * 16:28 + i * 16]) for i in range(n)}
        upem = struct.unpack(">H", d[T["head"][0] + 18:T["head"][0] + 20])[0]
        nh = struct.unpack(">H", d[T["hhea"][0] + 34:T["hhea"][0] + 36])[0]
        adv = [struct.unpack(">H", d[T["hmtx"][0] + i * 4:T["hmtx"][0] + i * 4 + 2])[0] for i in range(nh)]
        c = T["cmap"][0]
        subs = [(struct.unpack(">HH", d[c + 4 + i * 8:c + 8 + i * 8]), struct.unpack(">I", d[c + 8 + i * 8:c + 12 + i * 8])[0]) for i in range(struct.unpack(">H", d[c + 2:c + 4])[0])]
        off = next(o for (pl, en), o in subs if (pl, en) == (3, 1))
        o = c + off; segx2 = struct.unpack(">H", d[o + 6:o + 8])[0]; seg = segx2 // 2
        ends = struct.unpack(">%dH" % seg, d[o + 14:o + 14 + segx2]); st = o + 14 + segx2 + 2
        starts = struct.unpack(">%dH" % seg, d[st:st + segx2]); dl = struct.unpack(">%dh" % seg, d[st + segx2:st + 2 * segx2]); ro = struct.unpack(">%dH" % seg, d[st + 2 * segx2:st + 3 * segx2])
        def gid(cp):
            for k in range(seg):
                if cp <= ends[k]:
                    if cp < starts[k]: return 0
                    if ro[k] == 0: return (cp + dl[k]) & 0xFFFF
                    a = st + 2 * segx2 + k * 2 + ro[k] + (cp - starts[k]) * 2
                    g = struct.unpack(">H", d[a:a + 2])[0]; return (g + dl[k]) & 0xFFFF if g else 0
            return 0
        _cache[path] = (upem, nh, adv, gid)
    upem, nh, adv, gid = _cache[path]
    return float(sum(math.floor(adv[min(gid(ord(ch)), nh - 1)] * size / upem + 0.5) for ch in text))
_pil_measure = captions._measure_text_width

out = {}
# ---- static tables (seed fidelity) ----
out["tables"] = {"themes": captions.THEMES, "fonts": captions.FONTS, "positions": captions.POSITION_DEFAULTS,
                 "quality": captions.RENDER_QUALITY_PRESETS,
                 "constants": {"street_glow_line1_size": captions.STREET_GLOW_LINE1_SIZE, "street_glow_line2_size": captions.STREET_GLOW_LINE2_SIZE,
                               "street_glow_gap_px": captions.STREET_GLOW_GAP_PX, "street_glow_glow_blur": captions.STREET_GLOW_GLOW_BLUR,
                               "street_glow_shadow_blur": captions.STREET_GLOW_SHADOW_BLUR, "street_glow_glow_opacity": captions.STREET_GLOW_GLOW_OPACITY,
                               "street_glow_shadow_opacity": captions.STREET_GLOW_SHADOW_OPACITY, "doodle_font_size": captions.DOODLE_FONT_SIZE,
                               "doodle_native_line_height": captions.DOODLE_NATIVE_LINE_HEIGHT, "doodle_reveal_steps": captions.DOODLE_REVEAL_STEPS,
                               "pop_in_ms": captions.POP_IN_MS, "pop_out_ms": captions.POP_OUT_MS, "pop_scale": captions.POP_SCALE,
                               "default_emphasize_accent": captions.DEFAULT_EMPHASIZE_ACCENT}}

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
                               ({"words": [], "segments": [{"text": "bad", "start": 0, "end": 1, "avg_logprob": -2.0}]}, 0.0),
                               # opening speech at segment index 1 (after a lead-in segment) with an inflated no_speech_prob
                               ({"words": [{"word": "lead", "start": 0.1, "end": 0.3}, {"word": "open", "start": 0.6, "end": 0.9}, {"word": "later", "start": 5.0, "end": 5.4}],
                                 "segments": [{"text": "lead", "start": 0.0, "end": 0.4, "avg_logprob": -0.3, "no_speech_prob": 0.1},
                                              {"text": "open", "start": 0.5, "end": 1.0, "avg_logprob": -0.3, "no_speech_prob": 0.9},
                                              {"text": "later", "start": 4.9, "end": 5.5, "avg_logprob": -0.3, "no_speech_prob": 0.9}]}, 0.5)]]

# ---- layout / ass / srt ----
def cw(i, text, s, e, **kw):
    d = {"index": i, "text": text, "original": kw.pop("original", text), "start": s, "end": e, "important": False, "emphasis": "support",
         "uncertain": False, "color": "#FFFFFF", "font_size": 66, "pos_x": None, "pos_y": None, "new_line": None,
         "box_width": None, "box_height": None, "rotation": None, "new_caption": None, "kinetic": None, "emphasize": False}
    d.update(kw); return d

words = [cw(0, "Yeh", 0.0, 0.2, emphasis="support"), cw(1, "BAHUT", 0.25, 0.6, emphasis="key", important=True, color="#FF3B30", font_size=62),
         cw(2, "acchi", 0.65, 0.9, emphasis="feature", important=True), cw(3, "video", 0.95, 1.3, original="video."),
         cw(4, "hai", 1.35, 1.6, new_line=True, original="है"), cw(5, "{bold}", 2.5, 2.9, emphasize=True), cw(6, "x", 3.0, 3.2, pos_x=40.5, pos_y=70.25, font_size=80, rotation=-12.5),
         cw(7, "y", 3.25, 3.5, new_caption=True), cw(8, "z", 3.55, 3.9), cw(9, "w", 4.0, 4.2, new_caption=False),
         cw(10, "kya", 4.3, 4.6, original="क्या", emphasis="key"), cw(11, "scene", 4.65, 5.0, emphasis="feature", original="सीन")]
out["group"] = [{"words_per_caption": n, "min_display": md, "groups": [[x["index"] for x in g] for g in captions._group_words_for_display(words, min_display=md, max_words=n or 3, strict_count=n is not None)]}
                for n in (None, 1, 2, 4, 6) for md in (0.35, 1.5)]
elements = [{"id": "e1", "kind": "sticker", "char": "\u2605", "font_id": "poppins", "color": "#FF00AA", "font_size": 120, "start": 0, "end": 2, "pos_x": 50, "pos_y": 30, "layer": "front"},
            {"id": "e2", "kind": "text", "char": "Hi {x}", "font_id": "anton", "color": "#00FF00", "font_size": 48, "start": 1, "end": 1, "pos_x": 10, "pos_y": 90, "layer": "front"},
            {"id": "e3", "kind": "text", "char": "Title", "font_id": "bogus", "color": "#12", "font_size": 60, "start": 1.5, "end": 3.25, "pos_x": 33.3, "pos_y": 66.6, "layer": "behind"}]
style_cases = {
    "default": {},
    "full": dict(script_mode="native", bold=True, italic=True, outline_enabled=True, outline_width=2.5, outline_color="#112233",
                 shadow_enabled=True, shadow_angle=45, shadow_distance=4, shadow_blur=3, shadow_opacity=0.4, shadow_color="#FF0000",
                 glow_enabled=True, glow_color=None, glow_blur=10, glow_opacity=0.7),
    "glow_custom_no_shadow": dict(bold=False, shadow_enabled=False, glow_enabled=True, glow_color="#00FFAA", glow_blur=6, glow_opacity=0.5),
    "background": dict(background_enabled=True, background_color="#102030", background_opacity=0.55, glow_enabled=True, outline_enabled=True),
}
captions._measure_text_width = _unhinted_width
ass_cases = []
workdir = Path(tempfile.mkdtemp(prefix="golden_ass_"))
for theme in ("street_glow", "doodle_marker", "viral"):
    for style_name, style_kw in style_cases.items():
        for lines, pos, wpc in ((1, "bottom", None), (2, "top", 2), (1, "middle", 4)):
            font = "bebas" if theme == "viral" else "poppins"
            paths = {k: str(workdir / f"{k}.ass") for k in ("main", "shadow_alpha", "shadow_color", "glow_alpha", "glow_color")}
            for pth in paths.values(): Path(pth).unlink(missing_ok=True)
            captions._write_ass(paths["main"], words, theme, font, wpc, lines, pos, elements, shadow_alpha_path=paths["shadow_alpha"],
                                shadow_color_path=paths["shadow_color"], glow_alpha_path=paths["glow_alpha"], glow_color_path=paths["glow_color"], **style_kw)
            rd = lambda k: Path(paths[k]).read_text(encoding="utf-8") if Path(paths[k]).exists() else None
            ass_cases.append({"theme": theme, "style": style_name, "style_args": style_kw, "lines": lines, "position": pos, "wpc": wpc, "font": font,
                              "ass": rd("main"), "shadow_alpha": rd("shadow_alpha"), "shadow_color": rd("shadow_color"),
                              "glow_alpha": rd("glow_alpha"), "glow_color": rd("glow_color")})
out["ass"] = {"words": words, "elements": elements, "cases": ass_cases}
out["srt"] = [{"mode": m, "words": words, "srt": captions._write_srt(words, m)} for m in ("roman", "native")]
out["ass_time"] = [{"s": s, "r": captions._ass_time(s)} for s in (0, 0.005, 1.994, 59.999, 61.5, 3599.5, 3661.234)]
out["ass_color"] = [{"h": h, "r": captions._ass_color(h)} for h in ("#FF3B30", "#12", "FFF", "#abcdef01")]
out["ass_color_alpha"] = [{"h": h, "o": o, "r": captions._ass_color_alpha(h, o)} for h, o in (("#102030", 0.55), ("#FFFFFF", 1), ("#000", 0), ("#abcdef", 0.3333))]
out["shadow_offset"] = [{"a": a, "d": d, "r": list(captions._shadow_offset(a, d))} for a, d in ((0, 2), (45, 4), (90, 3), (180, 5.5), (270, 1), (360, 7))]
out["measure"] = [{"text": t, "w": _pil_measure(t, "Caveat", 56)} for t in ("this", "changes", " ", "lagatar consistent", "a", "BAHUT", "x y z")]

# ---- Hinglish fallback romanization (needs indic_transliteration) ----
roman_inputs = ["है", "हैं", "आप", "क्या।", "नमस्ते", "भारत", "विद्यालय", "कृष्ण", "ज़िंदगी", "फ़ोन", "क्षमा", "ज्ञान", "सपना,", "मैं", "ऑफ़िस", "बच्चे", "छोटा!", "hello", "", "अंग्रेज़ी", "पढ़ाई", "ड़", "संस्कृत", "दुनिया", "कंपनी", "चाँद"]
out["romanize"] = [{"text": t, "fallback": caption_ai._romanize_fallback(t), "common": caption_ai._common_hindi_spelling(t)} for t in roman_inputs]

# ---- ffmpeg render args ----
captured = []
class _R: returncode = 0; stderr = ""
captions.subprocess.run = lambda args, **k: (captured.append(list(args)), _R())[1]
render_cases = []
rwork = Path(tempfile.mkdtemp(prefix="golden_render_"))
(rwork / "fonts").mkdir()
for name in ("shadow_alpha", "shadow_color", "glow_alpha", "glow_color"): (rwork / f"{name}.ass").write_text("x")
def norm(args): return [a.replace(str(rwork), "/tmp/w") for a in args]
for muted, vol, q, sfx, masks, size in ((False, 100, "high", [], False, None), (True, 150, "draft", [], False, None),
                                        (False, 80, "high", [{"type": "whoosh", "time": 1.5}, {"type": "pop", "time": 0.123}, {"type": "unknown", "time": 2},
                                                             {"type": "cash", "time": 3}, {"type": "shutter", "time": 4}, {"type": "bass_drop", "time": 5}], True, (720, 1280))):
    captured.clear()
    kw = dict(shadow_alpha_path=str(rwork / "shadow_alpha.ass"), shadow_color_path=str(rwork / "shadow_color.ass"), shadow_blur=3,
              glow_alpha_path=str(rwork / "glow_alpha.ass"), glow_color_path=str(rwork / "glow_color.ass"), glow_blur=10,
              video_width=size[0], video_height=size[1]) if masks else {}
    captions._render(str(rwork / "source.mp4"), str(rwork / "captions.ass"), str(rwork / "out.mp4"), str(rwork / "fonts"), vol, muted, -10, 120, 90, q, sfx_markers=sfx, **kw)
    render_cases.append({"muted": muted, "volume": vol, "quality": q, "sfx": sfx, "masks": masks, "size": size, "args": norm(captured[0])})
out["render"] = render_cases
out["sfx_filter"] = [{"type": t, "filter": captions._sfx_source_filter(t, "L")} for t in ("whoosh", "bass_drop", "pop", "cash", "shutter", "unknown")]

(out_dir / "golden.json").write_text(json.dumps(out, ensure_ascii=False, indent=1, default=list), encoding="utf-8")
print("wrote", out_dir / "golden.json")
