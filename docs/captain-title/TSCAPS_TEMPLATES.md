# Templates converted from tscaps

35 caption templates come from the MIT-licensed `templates/` folder of the tscaps repository (licence: `third-party/tscaps-templates-LICENSE.txt`).
tscaps templates are CSS stylesheets animated by tscaps' own browser engine, so they were **converted**, not copied: each one became a tiered
Captain Title template (font, size, colours, case, outline/shadow/glow/box preset, spoken-word colour). The tscaps preview videos/images live in
its AGPL `apps/studio` folder and were not copied.

- Source of truth for the mapping: `scripts/tscaps/generate_tscaps_templates.py`. It is self-contained (run it with no argument; the
  extracted `tscaps-main/templates` path is an optional upstream check). Template descriptions are Captain Title's own wording
  (`DESCRIPTIONS`), not tscaps text, and must never name real people.
  It writes `Database_Migrations/28_tscaps_templates.sql` and `AppBackendBasic.Services/Captions/TscapsTemplates.cs`; `TscapsTemplateTests`
  keeps them identical and checks every font resolves to a bundled file in libass.
- New optional template fields (all in `CardJson`, off for older templates): `uppercase`, `flow` (no line break at an emphasis-tier change),
  `active_color` (karaoke colour while a word is spoken), `text_style` (outline/shadow/glow/background/bold/italic preset the studio applies when
  the template is picked - the user can still change it).
- Not converted, because the look needs effects Captain Title cannot draw: lena (chat bubbles), luna (difference blend), lyra (animated gradient
  fill), nyx and pico (terminal/editor window chrome), pastor (text behind the person).
- Approximations: entrance animations (glitch, bounce, typewriter...) are replaced by Captain Title's own word pop-in; "upcoming word" dimming,
  per-word highlight pills and RGB-split ghosting are mapped to emphasis colours, shadow and glow. Font swaps: Komika Axis -> Bangers (licence),
  Bungee -> Titan One (libass size), EB Garamond / Playfair Bold -> the bundled Playfair Display SemiBold Italic. Selene's frosted capsule and
  Theo/Vera's translucent backplate became a soft shadow / an opaque box, because Captain Title draws one box per word and translucent boxes
  darken where neighbouring words overlap.
- Known follow-up (pre-existing, not changed to keep golden parity): for the older templates the shadow/glow mask scripts have no `WrapStyle: 2`,
  so a very long single line could wrap in the mask but not in the text. The converted templates set it.

## Licence compliance (commercial use)

- tscaps is MIT: commercial use is allowed; keep the notice. It ships in `docs/captain-title/third-party/`, `WebUI/public/licenses/tscaps-MIT.txt`
  and on the public `/credits` page.
- Fonts: SIL OFL 1.1, except Permanent Marker (Apache 2.0). Licence texts and per-file copyright notices sit next to the font files
  (`OFL.txt`, `Apache-2.0.txt`, `LICENSE.md`) and on `/credits`. Never sell font files on their own or rename a modified font back to a Reserved Font Name.
- All web fonts are self-hosted (`WebUI/src/fonts.ts`, `@fontsource/*`), so visitors' browsers never contact Google Fonts.
  `TscapsTemplateTests` fails if a third-party font host or a licence file comes back or goes missing.
