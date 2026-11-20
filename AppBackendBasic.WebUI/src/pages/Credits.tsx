import Header from '../components/Header'
import Footer from '../components/Footer'

// Open-source credits and licence notices. Keep this in sync when a font, template source or runtime library is added:
// fonts -> AppBackendBasic.API/Assets/captions/fonts/LICENSE.md, templates -> docs/captain-title/TSCAPS_TEMPLATES.md, libraries -> package.json.

const TSCAPS_MIT = `MIT License

Copyright (c) 2026 Franco Zanardi

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.`

// [family, copyright notice] - notices as written in each font file (SIL Open Font License 1.1 unless stated).
const FONTS: [string, string][] = [
  ['Anton', 'Copyright 2020 The Anton Project Authors'],
  ['Archivo Black', 'Copyright 2017 The Archivo Black Project Authors'],
  ['Baloo 2', 'Copyright 2019 The Baloo 2 Project Authors'],
  ['Bangers', 'Copyright 2010 The Bangers Project Authors'],
  ['Bebas Neue', 'Copyright 2019 The Bebas Neue Project Authors'],
  ['Bricolage Grotesque', 'Copyright 2022 The Bricolage Grotesque Project Authors'],
  ['Caveat', 'Copyright 2014 The Caveat Project Authors'],
  ['Gabarito', 'Copyright 2023 The Gabarito Project Authors'],
  ['Hind', 'Copyright (c) 2014 Indian Type Foundry'],
  ['IBM Plex Sans', 'Copyright 2019 IBM Corp., with Reserved Font Name "Plex"'],
  ['IM FELL English', '© 2007 Igino Marini, with Reserved Font Name "IM FELL English Roman"'],
  ['Inter', 'Copyright 2016 The Inter Project Authors'],
  ['JetBrains Mono', 'Copyright 2020 The JetBrains Mono Project Authors'],
  ['Kalam', 'Copyright (c) 2014 Indian Type Foundry'],
  ['Lobster', 'Copyright 2010 The Lobster Project Authors, with Reserved Font Name "Lobster"'],
  ['Lora', 'Copyright 2011 The Lora Project Authors, with Reserved Font Name "Lora"'],
  ['Montserrat', 'Copyright 2011 The Montserrat Project Authors'],
  ['Mukta', 'Copyright (c) 2014, Girish Dalvi, Ek Type'],
  ['Noto Sans Devanagari', 'Copyright 2022 The Noto Project Authors'],
  ['Outfit', 'Copyright 2021 The Outfit Project Authors'],
  ['Playfair Display', 'Copyright 2017 The Playfair Display Project Authors, with Reserved Font Name "Playfair Display"'],
  ['Poppins', 'Copyright 2020 The Poppins Project Authors'],
  ['Righteous', 'Copyright (c) 2011 Brian J. Bonislawsky DBA Astigmatic (AOETI), with Reserved Font Name "Righteous"'],
  ['Rozha One', 'Copyright (c) 2014 Indian Type Foundry'],
  ['Space Grotesk', 'Copyright 2020 The Space Grotesk Project Authors'],
  ['Tiro Devanagari Hindi', 'Copyright 2020 The Indigo Project Authors'],
  ['Titan One', 'Copyright (c) 2011 Rodrigo Fuenzalida, with Reserved Font Name "Titan One"'],
  ['Yatra One', 'Copyright 2014 The Yatra Project Authors'],
]

const LIBRARIES: [string, string][] = [
  ['React, React DOM', 'MIT'],
  ['React Router', 'MIT'],
  ['Framer Motion', 'MIT'],
  ['Axios', 'MIT'],
  ['Tailwind CSS, tailwind-merge, clsx', 'MIT'],
  ['Phosphor Icons', 'MIT'],
  ['Lucide icons', 'ISC'],
  ['Fontsource (font packaging)', 'MIT'],
]

const linkClass = 'font-semibold underline decoration-2 underline-offset-2 hover:text-cherry'

export default function Credits() {
  return (
    <div className="min-h-screen bg-paper text-ink" style={{ fontFamily: "'IBM Plex Sans', system-ui, sans-serif" }}>
      <Header />
      <main className="mx-auto max-w-4xl px-4 py-16 sm:px-6">
        <h1 className="font-display text-5xl font-extrabold tracking-tight">Credits &amp; licences</h1>
        <p className="mt-4 max-w-2xl text-lg font-medium">
          Captain Title is built with open-source work. Thank you to everyone listed here. These notices are provided as their licences require.
        </p>

        <section className="mt-12">
          <h2 className="font-display text-2xl font-extrabold">Caption templates</h2>
          <p className="mt-3">
            Several caption templates (Ada to Zara) are adapted from the templates of <strong>tscaps</strong> by Franco Zanardi, used under the MIT
            License. Captain Title converted them to its own rendering engine, so fonts, effects and animations differ from the originals.
          </p>
          <pre className="mt-4 overflow-x-auto whitespace-pre-wrap rounded-lg border-2 border-ink bg-white p-4 text-xs leading-relaxed">{TSCAPS_MIT}</pre>
        </section>

        <section className="mt-12">
          <h2 className="font-display text-2xl font-extrabold">Fonts</h2>
          <p className="mt-3">
            Fonts are hosted by Captain Title itself; your browser does not contact any font service. All fonts below are licensed under
            the <a className={linkClass} href="/licenses/OFL-1.1.txt" target="_blank" rel="noopener">SIL Open Font License 1.1</a>, except
            Permanent Marker (Copyright (c) 2010 Font Diner, Inc.), which is licensed under
            the <a className={linkClass} href="/licenses/Apache-2.0.txt" target="_blank" rel="noopener">Apache License 2.0</a>.
          </p>
          <ul className="mt-4 grid gap-1 text-sm sm:grid-cols-2">
            {FONTS.map(([family, notice]) => (
              <li key={family}><strong>{family}</strong>: {notice}</li>
            ))}
          </ul>
        </section>

        <section className="mt-12">
          <h2 className="font-display text-2xl font-extrabold">Software</h2>
          <ul className="mt-4 grid gap-1 text-sm">
            {LIBRARIES.map(([name, licence]) => (
              <li key={name}><strong>{name}</strong>: {licence} License</li>
            ))}
          </ul>
        </section>
      </main>
      <Footer />
    </div>
  )
}
