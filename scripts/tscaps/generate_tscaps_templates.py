#!/usr/bin/env python3
"""
Converts the tscaps caption templates (the MIT-licensed `templates/` folder of the tscaps repository, tscaps-main.zip) into Captain Title templates.

tscaps templates are CSS stylesheets animated by tscaps' own browser engine; Captain Title renders captions with its own data-driven
"tiered" templates (CaptionThemes rows) and burns them in with ffmpeg/libass. So this is a conversion, not a file copy: each template's
font, size, colours, case, outline/shadow/glow/box and spoken-word colour are mapped onto Captain Title's model. Entrance animations
(glitch, bounce, typewriter...) are not converted - Captain Title keeps its own word pop-in for every template.

Usage:   python3 -I scripts/tscaps/generate_tscaps_templates.py [<path-to-extracted>/tscaps-main/templates]
Writes:  Database_Migrations/28_tscaps_templates.sql  (run by hand - see RUN_ORDER.md)
         AppBackendBasic.Services/Captions/TscapsTemplates.cs  (same rows for the C# seed / tests)
The script is self-contained. The optional source folder is only used to check that every mapped template still exists upstream.
Descriptions are Captain Title's own (DESCRIPTIONS below, describing what Captain Title draws). No tscaps text is copied, and real
people's names must never appear in them.
"""
import json
import os
import sys

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
CANVAS_HEIGHT = 1920  # Captain Title's ASS PlayResY; tscaps sizes are in cqh (1% of the video height)

# Fonts: ASS family (what libass resolves - full names of the bundled static .ttf files) -> CSS font stack (the preview loads the
# SAME .ttf under the same family name, see caption-fonts.css). Existing bundled fonts keep their existing names.
FONTS = {
    "Anton": "'Anton', sans-serif",
    "Bebas Neue": "'Bebas Neue', sans-serif",
    "Caveat": "'Caveat', cursive",
    "Kalam": "'Kalam', cursive",
    "Poppins": "'Poppins', sans-serif",
    # The bundled Playfair file is the SemiBold Italic face; libass only resolves it by this name. An upright Playfair Bold file is
    # deliberately NOT bundled: libass would then pick it even for italic Playfair requests (checked with ffmpeg -loglevel verbose).
    "Playfair Display SemiBold": "'Playfair Display', serif",
    "Inter Regular": "'Inter Regular', sans-serif",
    "Inter SemiBold": "'Inter SemiBold', sans-serif",
    "Inter Bold": "'Inter Bold', sans-serif",
    "Inter Black": "'Inter Black', sans-serif",
    "Lora Medium": "'Lora Medium', serif",
    "Gabarito SemiBold": "'Gabarito SemiBold', sans-serif",
    "Gabarito Black": "'Gabarito Black', sans-serif",
    "Righteous": "'Righteous', sans-serif",
    # Bungee (tscaps' choice for juno/tito) is NOT used: libass sizes it from its very tall vertical metrics, so it renders at about half the
    # size the browser preview shows. Titan One is the closest open chunky display face with normal metrics.
    "Titan One": "'Titan One', sans-serif",
    "Bricolage Grotesque ExtraBold": "'Bricolage Grotesque ExtraBold', sans-serif",
    "JetBrains Mono": "'JetBrains Mono', monospace",
    "Bangers": "'Bangers', sans-serif",
    "Lobster": "'Lobster', cursive",
    "Montserrat Light": "'Montserrat Light', sans-serif",
    "Montserrat SemiBold": "'Montserrat SemiBold', sans-serif",
    "Montserrat Bold": "'Montserrat Bold', sans-serif",
    "IM FELL English": "'IM FELL English', serif",
}

# Templates left out on purpose: their look depends on something Captain Title's renderer cannot draw.
SKIPPED = {
    "lena": "iOS chat bubbles with tails",
    "luna": "difference blend that shows a negative of the video through the letters",
    "lyra": "animated multi-colour gradient inside the letters",
    "nyx": "floating terminal window chrome with a typewriter cursor",
    "pastor": "giant text placed behind the person on screen (needs subject segmentation)",
    "pico": "floating code-editor window chrome with line numbers",
}

OFF = dict(outline_enabled=False, outline_width=2, outline_color="#000000",
           shadow_enabled=False, shadow_angle=0, shadow_distance=2, shadow_blur=2, shadow_opacity=0.5, shadow_color="#000000",
           glow_enabled=False, glow_color=None, glow_blur=8, glow_opacity=0.6,
           background_enabled=False, background_color="#000000", background_opacity=0.6,
           bold=False, italic=False)


def outline(width, color="#000000"): return dict(outline_enabled=True, outline_width=width, outline_color=color)
def shadow(distance, blur, opacity, color="#000000", angle=0): return dict(shadow_enabled=True, shadow_angle=angle, shadow_distance=distance, shadow_blur=blur, shadow_opacity=opacity, shadow_color=color)
def glow(blur, opacity, color=None): return dict(glow_enabled=True, glow_color=color, glow_blur=blur, glow_opacity=opacity)
def box(color, opacity): return dict(background_enabled=True, background_color=color, background_opacity=opacity)
SOFT = shadow(2, 4, 0.55)

# key, font, size (cqh), text colour, emphasis colour, spoken-word colour, case, text style, key-word font, key-word size factor
# Emphasis colour = the AI "feature"/"key" tiers; spoken-word colour = karaoke colour while the word is being said (None = off).
T = [
    dict(key="ada", font="Poppins", cqh=2.8, color="#1C1E1D", accent="#1C1E1D", active=None, style={**box("#E7E5E7", 1.0), "bold": True}),
    dict(key="anya", font="Playfair Display SemiBold", cqh=2.97, color="#FFF8E7", accent="#F5C982", active="#F5C982", style={**SOFT, "italic": True}),
    dict(key="cleo", font="Lora Medium", cqh=4.3, color="#FFFFFF", accent="#E6D647", active=None, style=SOFT),
    dict(key="elio", font="Bebas Neue", cqh=6.0, color="#FFFFFF", accent="#FFFFFF", active=None, upper=True, style=SOFT, key_scale=1.2),
    dict(key="enzo", font="Inter Black", cqh=5.2, color="#FFFFFF", accent="#FFFFFF", active=None, style=shadow(3, 0, 1.0, "#333333", 180),
         key_font="Playfair Display SemiBold", key_scale=1.4),
    dict(key="ezra", font="Inter Regular", cqh=3.6, color="#F5F0D0", accent="#F5F0D0", active=None, style=shadow(2, 6, 0.25),
         key_font="Playfair Display SemiBold", key_scale=1.6),
    dict(key="freya", font="Anton", cqh=5.5, color="#FFFFFF", accent="#FFFFFF", active=None, upper=True,
         style={**outline(5, "#0A0A0A"), **shadow(4, 0, 1.0, "#FF0044", 45), **glow(6, 0.6, "#00F4FF")}),
    dict(key="hugo", font="Gabarito SemiBold", cqh=3.5, color="#FFFFFF", accent="#FFFFFF", active=None, upper=True, style=glow(5, 0.5, "#FFFFFF"), key_scale=1.15),
    dict(key="iris", font="Righteous", cqh=4.5, color="#FFFFFF", accent="#FFE066", active=None, style=outline(5), key_color="#FFB3B3"),
    dict(key="ivo", font="Anton", cqh=5.08, color="#FFFFFF", accent="#ADFF2F", active=None, upper=True, style=box("#000000", 1.0)),
    dict(key="juno", font="Titan One", cqh=3.91, color="#FFFFFF", accent="#FFDD00", active="#FFDD00", upper=True,
         style={**outline(1), **shadow(6, 0, 1.0, "#000000", 45)}, note="Bungee replaced by Titan One (Bungee renders at half size in libass)."),
    dict(key="kai", font="Bricolage Grotesque ExtraBold", cqh=5.0, color="#E8E8EF", accent="#FFFFFF", active="#FFFFFF", upper=True,
         style={**outline(1), **shadow(4, 0, 1.0, "#FF2E63", 45), **glow(4, 0.5, "#1FFFD0")}),
    dict(key="kel", font="JetBrains Mono", cqh=2.6, color="#D4D4D4", accent="#569CD6", active="#569CD6", style=box("#1E1E1E", 1.0)),
    dict(key="levi", font="Anton", cqh=6.0, color="#FFFFFF", accent="#FFFFFF", active=None, upper=True, style=shadow(3, 6, 0.5)),
    dict(key="lewis", font="Gabarito Black", cqh=4.92, color="#FFFFFF", accent="#52F73C", active=None, style={**outline(5), **shadow(3, 2, 0.8)},
         key_color="#3BE3F7"),
    dict(key="loki", font="Bangers", cqh=4.5, color="#FFFFFF", accent="#FFEA00", active="#FFEA00", upper=True,
         style={**outline(5), **shadow(5, 2, 1.0)}, note="Uses Bangers (OFL) because the original template's comic font is not openly licensed."),
    dict(key="luca", font="Caveat", cqh=4.0, color="#FFFFFF", accent="#FFFFFF", active=None, style={**SOFT, "bold": True}, key_font="Anton", key_scale=1.5),
    dict(key="milo", font="Lobster", cqh=3.5, color="#FFFFFF", accent="#FFFFFF", active=None, style=SOFT, key_font="Anton", key_scale=1.6),
    dict(key="mira", font="Montserrat SemiBold", cqh=3.9, color="#FFFFFF", accent="#E6D647", active=None, style={**outline(2), **SOFT}),
    dict(key="naya", font="Anton", cqh=4.69, color="#FFFFFF", accent="#FFFF00", active="#FFFF00", upper=True, style=outline(6)),
    dict(key="noor", font="Anton", cqh=6.2, color="#FFD84D", accent="#9CE874", active=None, upper=True, style={**outline(1), **glow(10, 0.6, "#FFD84D")},
         key_color="#FFF3C4"),
    dict(key="nova", font="Poppins", cqh=3.2, color="#FFFFFF", accent="#FFEA00", active=None, style={**SOFT, "bold": True}, key_scale=1.5),
    dict(key="otto", font="Inter SemiBold", cqh=2.7, color="#FFFFFF", accent="#FFD700", active="#FFD700", style=box("#000000", 1.0)),
    dict(key="pepper", font="Anton", cqh=4.0, color="#FFFFFF", accent="#E2553F", active="#E2553F", upper=True, style={**outline(4.5), **shadow(3, 2, 0.8)}),
    dict(key="remi", font="Caveat", cqh=6.0, color="#FFFFFF", accent="#FFD966", active="#FFD966", style={**shadow(2, 3, 0.7), "bold": True}),
    dict(key="rhea", font="Montserrat Bold", cqh=3.6, color="#FFFFFF", accent="#FFFFFF", active=None, style=SOFT),
    dict(key="sara", font="IM FELL English", cqh=4.92, color="#FFFFFF", accent="#FFFFFF", active=None, style=glow(8, 0.4, "#FFFFFF"), key_scale=1.4),
    dict(key="selene", font="Montserrat SemiBold", cqh=3.6, color="#FFFFFF", accent="#FFFFFF", active=None, style=shadow(2, 8, 0.6),
         note="The frosted-glass capsule cannot be drawn (no backdrop blur); a soft shadow keeps it legible instead."),
    dict(key="sol", font="Montserrat Light", cqh=4.0, color="#FFFFFF", accent="#F9D700", active=None, style=glow(8, 0.45, "#FFD500"),
         key_font="Kalam", key_scale=1.3),
    dict(key="tala", font="Anton", cqh=4.0, color="#FFFFFF", accent="#FFD700", active="#FFD700", upper=True, style=box("#1A1A1A", 1.0)),
    # theo/vera: tscaps uses a 55% backplate; an opaque box is used because Captain Title draws one box per word, and translucent boxes
    # darken where neighbouring words' boxes overlap during the pop-in.
    dict(key="theo", font="Inter Black", cqh=3.0, color="#FFFFFF", accent="#FFD700", active="#FFD700", style=box("#000000", 1.0)),
    dict(key="tito", font="Titan One", cqh=3.5, color="#FFFFFF", accent="#00FFB3", active="#00FFB3", style=shadow(5, 0, 1.0, "#000000", 45),
         note="Bungee replaced by Titan One (Bungee renders at half size in libass)."),
    dict(key="vera", font="Inter Black", cqh=3.0, color="#FFFFFF", accent="#3B82F6", active="#3B82F6", style=box("#000000", 1.0)),
    dict(key="yuki", font="Inter Bold", cqh=2.7, color="#FFFFFF", accent="#FFFFFF", active=None, style=shadow(2, 3, 0.6)),
    dict(key="zara", font="Anton", cqh=4.69, color="#FFFFFF", accent="#FFF200", active="#FFF200", upper=True,
         style={**shadow(3, 0, 1.0, "#FF00AA", 45), **glow(5, 0.6, "#00E5FF")}),
]

# Captain Title's own one-line descriptions of each converted template, describing what Captain Title actually renders (font, colours,
# effects). Written from the mapping above, not copied from tscaps. Keep real people's names out (no "<celebrity>-style").
DESCRIPTIONS = {
    "ada": "Dark Poppins Bold words on solid light-grey boxes. Clean and corporate; works well for presentations and LinkedIn-style clips.",
    "anya": "Cream italic Playfair Display with a soft shadow; emphasised and spoken words turn gold. Suits beauty, fashion and story-led videos.",
    "cleo": "Large Lora Medium serif in white with a soft shadow; emphasised words turn pale yellow. A calm, bookish look for narration and essays.",
    "elio": "Tall Bebas Neue capitals in white with a soft shadow; key words grow 20%. Poster-like, for hooks and big statements.",
    "enzo": "Heavy white Inter Black with a hard grey offset shadow; key words switch to a larger italic Playfair Display. Creator energy with an editorial touch.",
    "ezra": "Light cream Inter Regular with a faint shadow; key words jump to a much larger italic Playfair Display. Magazine-style layout for quotable lines.",
    "freya": "White Anton capitals with a dark outline, a hard red offset shadow and a cyan glow for a split-colour glitch feel. Built for music, trends and hype edits.",
    "hugo": "Gabarito SemiBold capitals in white with a soft white glow; key words are 15% larger. Confident and modern for business and motivation clips.",
    "iris": "Rounded retro Righteous with a thick black outline; emphasised words turn yellow and key words pastel pink. Playful for crafts, kids and creative content.",
    "ivo": "White Anton capitals on solid black boxes; emphasised words turn lime green. A sticker-label look for reviews, lists and announcements.",
    "juno": "Chunky Titan One capitals with a hard diagonal black shadow for a 3D feel; emphasised and spoken words turn yellow. Loud and retro for entertainment.",
    "kai": "Bricolage Grotesque ExtraBold capitals in soft grey with a pink offset shadow and a mint glow; the spoken word lights up white. Raw and fast for commentary.",
    "kel": "JetBrains Mono on dark code-editor boxes; emphasised and spoken words turn syntax blue. Made for coding and tech tutorials.",
    "levi": "Large white Anton capitals with a soft drop shadow. Dramatic and poster-like for quotes and key statements.",
    "lewis": "Gabarito Black in white with a thick black outline; emphasised words turn neon green and key words cyan. High-energy for shorts and reels.",
    "loki": "Comic-book Bangers capitals with a thick black outline and a deep shadow; emphasised and spoken words turn bright yellow. Big energy for gaming and challenge videos.",
    "luca": "Handwritten Caveat in white with a soft shadow; key words switch to a large Anton. A scrapbook feel for personal moments.",
    "milo": "Retro Lobster script with a soft shadow; key words switch to a large Anton. Stylish for intros and highlights.",
    "mira": "Montserrat SemiBold in white with a thin outline and a soft shadow; emphasised words turn pale yellow. A versatile everyday style for talking heads and vlogs.",
    "naya": "White Anton capitals with a heavy black outline; emphasised and spoken words pop in bright yellow. Punchy for podcasts, coaching and business clips.",
    "noor": "Huge gold Anton capitals with a warm golden glow; emphasised words turn green and key words pale gold. A premium feel for money and luxury content.",
    "nova": "Bold Poppins in white with a soft shadow; emphasised words turn yellow and key words are 50% larger. A headline-and-subline look for tips and hooks.",
    "otto": "Inter SemiBold in white on solid black boxes; emphasised and spoken words turn gold. A clear broadcast-caption look for accessibility-first videos.",
    "pepper": "White Anton capitals with a black outline and shadow; emphasised and spoken words turn terracotta orange. A bold word-by-word look for talking-head clips.",
    "remi": "Large bold Caveat handwriting with a soft shadow; emphasised and spoken words turn warm gold. Friendly for food, lifestyle and vlogs.",
    "rhea": "Montserrat Bold in white with a soft shadow. Calm and modern for interviews and podcasts.",
    "sara": "Antique IM FELL English serif in white with a gentle glow; key words grow 40%. An old-book mood for history, poetry and storytelling.",
    "selene": "Montserrat SemiBold in white with a wide, soft shadow that keeps it readable over busy footage. Soft and premium.",
    "sol": "Thin Montserrat Light with a golden glow; emphasised words turn yellow and key words switch to a larger Kalam handwriting. Warm for travel, wellness and lifestyle.",
    "tala": "White Anton capitals on near-black boxes; emphasised and spoken words turn gold. Punchy for hot takes and reactions.",
    "theo": "Inter Black in white on solid black boxes; emphasised and spoken words turn gold. A familiar karaoke-subtitle look for interviews and long talks.",
    "tito": "Chunky Titan One with a hard diagonal shadow; emphasised and spoken words turn neon mint. Arcade-style for gaming and fun content.",
    "vera": "Inter Black in white on solid black boxes; emphasised and spoken words turn bright blue. Clean and corporate for product and education videos.",
    "yuki": "Plain white Inter Bold with a subtle shadow. Minimal subtitles for when the video should do the talking.",
    "zara": "White Anton capitals with a magenta offset shadow and a cyan glow for an RGB-split feel; emphasised and spoken words turn electric yellow. Cyberpunk for tech, gaming and music.",
}


def css_shadow(st):
    """CSS approximation of the text style for the small picker card only (the real preview uses the project's text style)."""
    parts = []
    if st.get("shadow_enabled"):
        parts.append(f"0 2px {int(st['shadow_blur'])}px {st['shadow_color']}")
    return ", ".join(parts) or None


def build(src=None):
    rows = []
    for i, t in enumerate(T):
        if src:  # optional upstream check: the mapped template still exists in the tscaps source
            assert os.path.isfile(os.path.join(src, t["key"], "template.json")), f"{t['key']} missing in {src}"
        assert t["key"] in DESCRIPTIONS, t["key"]
        assert t["font"] in FONTS and t.get("key_font", t["font"]) in FONTS, t["key"]
        size = max(40, min(130, round(t["cqh"] * CANVAS_HEIGHT / 100)))
        key_font = t.get("key_font", t["font"])
        tier = lambda font, color, sz: {"font": font, "css_font": FONTS[font], "color": color, "size": sz}
        tiers = {
            "support": tier(t["font"], t["color"], size),
            "feature": tier(t["font"], t["accent"], size),
            "key": tier(key_font, t.get("key_color", t["accent"]), round(size * t.get("key_scale", 1.0))),
        }
        st = {**OFF, **t["style"]}
        card = {
            "stroke": f"1px {st['outline_color']}" if st["outline_enabled"] else None,
            "shadow": css_shadow(st),
            "glow": f"0 0 10px {st['glow_color']}" if st["glow_enabled"] and st["glow_color"] else None,
            "highlight": bool(st["background_enabled"]),
            "uppercase": bool(t.get("upper")),
            "flow": True,
            "active_color": t["active"],
            "text_style": st,
            "source": "tscaps",
        }
        rows.append(dict(key=t["key"], name=t["key"].capitalize(), description=DESCRIPTIONS[t["key"]], note=t.get("note"),
                         accent=t["accent"], tiers=tiers, card=card, sort=100 + i))
    return rows


def sql_str(s): return "'" + s.replace("'", "''") + "'"


def write_sql(rows):
    keys = ", ".join(sql_str(r["key"]) for r in rows)
    out = [
        "-- ============================================================",
        "-- Migration 28: Captain Title - 35 caption templates converted from tscaps (MIT)",
        "-- GENERATED by scripts/tscaps/generate_tscaps_templates.py - edit the script, not this file.",
        "--",
        "-- WHAT IT DOES",
        "--   Adds 35 new tiered caption templates to \"CaptionThemes\" (font, size, colours, upper case, outline/shadow/glow/box",
        "--   preset and spoken-word colour). Nothing else changes: no columns, no fonts table rows, existing templates untouched.",
        "--   The template fonts are .ttf files shipped with the application build (Assets/captions/fonts + WebUI/public/caption-fonts).",
        "--",
        "-- PREREQUISITES: 21_captain_title_captions.sql and 26_captions_templates_and_text_styling.sql already applied.",
        "-- RUN ORDER:     ... 26 -> 27 -> 28. Deploy the application build that contains this change FIRST or at the same time:",
        "--                an older build would show these templates but draw them like Street Glow.",
        "-- IDEMPOTENT:    yes - ON CONFLICT (\"Key\") DO NOTHING (an admin's later edits to these rows survive re-runs).",
        "-- DESTRUCTIVE:   no.",
        "-- ROLLBACK:      UPDATE \"CaptionThemes\" SET \"IsActive\" = FALSE WHERE \"CardJson\"->>'source' = 'tscaps';   (hide them)",
        "--                or DELETE FROM \"CaptionThemes\" WHERE \"CardJson\"->>'source' = 'tscaps';  (projects using one fall back to the default)",
        "--",
        "-- Licence: the tscaps templates are MIT (Copyright (c) 2026 Franco Zanardi); see docs/captain-title/third-party/tscaps-templates-LICENSE.txt",
        "-- Not converted (their look needs effects Captain Title cannot draw): " + ", ".join(f"{k} ({v})" for k, v in SKIPPED.items()),
        "-- ============================================================",
        "",
        "-- READ-ONLY CHECK (optional, before): which of these keys already exist?",
        f"-- SELECT \"Key\", \"Name\", \"IsActive\" FROM \"CaptionThemes\" WHERE \"Key\" IN ({keys});",
        "",
        "BEGIN;",
        "",
        "INSERT INTO \"CaptionThemes\" (\"Key\",\"Name\",\"BaseColor\",\"AccentColor\",\"AccentFont\",\"TiersJson\",\"CardJson\",\"IsActive\",\"SortOrder\") VALUES",
    ]
    vals = []
    for r in rows:
        comment = f"  -- {r['name']}: {r['description']}" + (f" NOTE: {r['note']}" if r["note"] else "")
        vals.append(comment + "\n  (" + ",".join([
            sql_str(r["key"]), sql_str(r["name"]), "NULL", sql_str(r["accent"]), "NULL",
            sql_str(json.dumps(r["tiers"], separators=(",", ":"))) + "::jsonb",
            sql_str(json.dumps(r["card"], separators=(",", ":"))) + "::jsonb",
            "TRUE", str(r["sort"])]) + ")")
    out.append(",\n".join(vals))
    out += ["ON CONFLICT (\"Key\") DO NOTHING;", "", "COMMIT;", "",
            "-- ============================================================",
            "-- READ-ONLY CHECK (after): expect 35 rows",
            "-- ============================================================",
            "SELECT \"Key\", \"Name\", \"IsActive\", \"SortOrder\" FROM \"CaptionThemes\" WHERE \"CardJson\"->>'source' = 'tscaps' ORDER BY \"SortOrder\";",
            ""]
    path = os.path.join(REPO, "Database_Migrations", "28_tscaps_templates.sql")
    open(path, "w", encoding="utf-8").write("\n".join(out))
    return path


def cs_str(s): return '"' + s.replace("\\", "\\\\").replace('"', '\\"') + '"'


def write_cs(rows):
    lines = [
        "// <auto-generated> by scripts/tscaps/generate_tscaps_templates.py - edit the script, not this file. </auto-generated>",
        "using AppBackendBasic.Core.Entities;",
        "",
        "namespace AppBackendBasic.Services.Captions;",
        "",
        "/// <summary>",
        "/// The 35 caption templates converted from tscaps (MIT, Copyright (c) 2026 Franco Zanardi). Same rows as",
        "/// Database_Migrations/28_tscaps_templates.sql (a test keeps the two in sync); the database stays the source of truth at runtime.",
        "/// </summary>",
        "public static class TscapsTemplates",
        "{",
        "    public static List<CaptionTheme> Themes() => new()",
        "    {",
    ]
    for r in rows:
        lines.append(f"        new() {{ Key = {cs_str(r['key'])}, Name = {cs_str(r['name'])}, AccentColor = {cs_str(r['accent'])}, SortOrder = {r['sort']},")
        lines.append(f"            TiersJson = {cs_str(json.dumps(r['tiers'], separators=(',', ':')))},")
        lines.append(f"            CardJson = {cs_str(json.dumps(r['card'], separators=(',', ':')))} }},")
    lines += ["    };", "}", ""]
    path = os.path.join(REPO, "AppBackendBasic.Services", "Captions", "TscapsTemplates.cs")
    open(path, "w", encoding="utf-8").write("\n".join(lines))
    return path


if __name__ == "__main__":
    if len(sys.argv) > 2 or (len(sys.argv) == 2 and not os.path.isdir(sys.argv[1])):
        sys.exit(__doc__)
    rows = build(sys.argv[1] if len(sys.argv) == 2 else None)
    assert len({r["key"] for r in rows}) == len(rows) == 35
    print(write_sql(rows)); print(write_cs(rows))
