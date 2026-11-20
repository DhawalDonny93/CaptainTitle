using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Services.Captions;
using Xunit;

namespace AppBackendBasic.Captions.Tests;

/// <summary>
/// The 35 templates converted from tscaps (migration 28): SQL and C# seed stay identical, every font they name is bundled for both the
/// export (libass resolves it to a bundled file, never a system fallback) and the preview, and their optional render flags work without
/// changing anything for the older templates.
/// </summary>
public class TscapsTemplateTests
{
    private static readonly Regex SqlRow = new(@"\('([a-z_]+)','((?:[^']|'')*)',NULL,'(#[0-9A-F]{6})',NULL,'((?:[^']|'')*)'::jsonb,'((?:[^']|'')*)'::jsonb,TRUE,(\d+)\)");

    private static string Unq(string s) => s.Replace("''", "'");

    [Fact]
    public void Sql_script_28_and_csharp_seed_define_the_same_templates()
    {
        var sql = File.ReadAllText(SchemaTests.RepoFile("Database_Migrations/28_tscaps_templates.sql"));
        var rows = SqlRow.Matches(sql).Select(m => (Key: m.Groups[1].Value, Name: Unq(m.Groups[2].Value), Accent: m.Groups[3].Value,
            Tiers: Unq(m.Groups[4].Value), Card: Unq(m.Groups[5].Value), Sort: int.Parse(m.Groups[6].Value))).ToList();
        var seed = TscapsTemplates.Themes();

        Assert.Equal(35, seed.Count);
        Assert.Equal(seed.Select(t => t.Key), rows.Select(r => r.Key));
        foreach (var (t, r) in seed.Zip(rows))
        {
            Assert.Equal(t.Name, r.Name);
            Assert.Equal(t.AccentColor, r.Accent);
            Assert.Equal(t.TiersJson, r.Tiers);
            Assert.Equal(t.CardJson, r.Card);
            Assert.Equal(t.SortOrder, r.Sort);
        }
        // No clash with the core catalog keys
        var core = CaptionSeedData.Themes().Select(t => t.Key).Except(seed.Select(t => t.Key)).ToList();
        Assert.Equal(11, core.Count);
        Assert.Empty(core.Intersect(seed.Select(t => t.Key)));
    }

    [Fact]
    public void Every_template_is_tiered_flowing_and_parses()
    {
        foreach (var t in TscapsTemplates.Themes())
        {
            var style = CaptionStyleFactory.ToStyle(t);
            Assert.NotNull(style.Tiers); // untiered themes would be drawn like Street Glow
            Assert.True(style.Tiers!.ContainsKey("support") && style.Tiers.ContainsKey("feature") && style.Tiers.ContainsKey("key"), t.Key);
            Assert.True(style.Flow, t.Key);
            Assert.InRange(style.Tiers.Values.Max(x => x.Size), 40, 160);
            using var card = JsonDocument.Parse(t.CardJson!);
            Assert.Equal("tscaps", card.RootElement.GetProperty("source").GetString());
            Assert.Equal(JsonValueKind.Object, card.RootElement.GetProperty("text_style").ValueKind);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[1,2]")]
    [InlineData("{\"stroke\":null,\"shadow\":\"0 3px 0 #000\",\"glow\":null,\"highlight\":true}")] // an older template's card
    [InlineData("{\"uppercase\":\"yes\",\"flow\":1,\"active_color\":\"red\"}")]                  // wrong types are ignored, not fatal
    public void Card_flags_default_to_off(string? card)
    {
        Assert.Equal((false, false, (string?)null), CaptionStyleFactory.ParseCardFlags(card));
    }

    [Fact]
    public void Older_templates_keep_all_flags_off()
    {
        foreach (var t in CaptionSeedData.Themes().Where(t => !TscapsTemplates.Themes().Any(x => x.Key == t.Key)))
        {
            var s = CaptionStyleFactory.ToStyle(t);
            Assert.False(s.Uppercase || s.Flow || s.ActiveColor != null, t.Key);
        }
    }

    private static List<CaptionWordDto> Words() => new()
    {
        new() { Index = 0, Text = "yeh", Original = "yeh", Start = 0.0, End = 0.4, Emphasis = "support", Color = "#FFFFFF", FontSize = 90 },
        new() { Index = 1, Text = "sach", Original = "sach", Start = 0.4, End = 0.8, Emphasis = "key", Color = "#FFFF00", FontSize = 90 },
        new() { Index = 2, Text = "hai", Original = "hai", Start = 0.8, End = 1.2, Emphasis = "support", Color = "#FFFFFF", FontSize = 90 },
    };

    [Fact]
    public void Writer_applies_uppercase_flow_and_spoken_word_colour()
    {
        var scripts = AssSubtitleWriter.Build(Words(), "naya", "poppins", 3, 1, "bottom", null, new AssStyleOptions(), CaptionStyleFactory.FromSeed(),
            new TrueTypeTextMeasurer(Fonts()));
        var dialogue = scripts.Main.Split('\n').Single(l => l.StartsWith("Dialogue:"));
        Assert.Contains("}YEH", dialogue);
        Assert.Contains("}SACH", dialogue);
        Assert.DoesNotContain("\\N", dialogue);                                  // flow: the key word stays on the same line
        Assert.Contains("\\t(400,401,\\c&H0000FFFF&)", dialogue);                   // spoken: yellow from 0.4s ...
        Assert.Contains("\\t(800,801,\\c&H0000FFFF&)", dialogue);                   // ... and back to its own (yellow) colour
        Assert.Contains("\\fnAnton", dialogue);
        Assert.Contains("WrapStyle: 2", scripts.ShadowAlpha);              // the shadow copy never auto-wraps away from the text
    }

    [Fact]
    public void Writer_output_for_an_older_tiered_template_has_no_new_tags()
    {
        var scripts = AssSubtitleWriter.Build(Words(), "editorial_pop", "poppins", 3, 1, "bottom", null, new AssStyleOptions(), CaptionStyleFactory.FromSeed(),
            new TrueTypeTextMeasurer(Fonts()));
        var dialogue = scripts.Main.Split('\n').Single(l => l.StartsWith("Dialogue:"));
        Assert.Contains("}yeh", dialogue);                  // natural case
        Assert.Contains("\\N", dialogue);                   // still breaks at the tier change
        Assert.DoesNotContain(",401,\\c", dialogue);        // no karaoke colour switch
        Assert.DoesNotContain("WrapStyle", scripts.ShadowAlpha);           // mask header unchanged (golden parity with the old app)
    }

    private static string Fonts() => SchemaTests.RepoFile("AppBackendBasic.API/Assets/captions/fonts");

    private static IEnumerable<(string Family, string Css)> TemplateFonts() =>
        TscapsTemplates.Themes().SelectMany(t => CaptionStyleFactory.ParseTiers(t.TiersJson)!.Values).Select(v => (v.Font, v.CssFont)).Distinct();

    [Fact]
    public void Every_template_font_is_available_to_the_preview()
    {
        var local = File.ReadAllText(SchemaTests.RepoFile("AppBackendBasic.WebUI/src/features/captions/caption-fonts.css"));
        var webFonts = File.ReadAllText(SchemaTests.RepoFile("AppBackendBasic.WebUI/src/fonts.ts"));
        foreach (var (_, css) in TemplateFonts())
        {
            var family = Regex.Match(css, "^'([^']+)'").Groups[1].Value;
            bool selfHosted = local.Contains($"font-family: '{family}'");
            bool fontsource = webFonts.Contains("'@fontsource/" + family.ToLowerInvariant().Replace(' ', '-') + "/");
            Assert.True(selfHosted || fontsource, $"preview font '{family}' is neither in caption-fonts.css nor imported in src/fonts.ts");
            if (selfHosted)
                foreach (Match m in Regex.Matches(local, $"font-family: '{Regex.Escape(family)}'; src: url\\('/caption-fonts/([^']+)'\\)"))
                    Assert.True(File.Exists(SchemaTests.RepoFile("AppBackendBasic.WebUI/public/caption-fonts/" + m.Groups[1].Value)), m.Groups[1].Value);
        }
    }

    /// <summary>Fonts are self-hosted: no web page may load them from a third-party CDN (that would send visitors' IP addresses to it).</summary>
    [Fact]
    public void Web_ui_loads_no_fonts_from_third_party_hosts()
    {
        var web = SchemaTests.RepoFile("AppBackendBasic.WebUI");
        var files = new[] { Path.Combine(web, "index.html") }
            .Concat(Directory.EnumerateFiles(Path.Combine(web, "src"), "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".css") || f.EndsWith(".tsx") || f.EndsWith(".ts")));
        foreach (var f in files.Where(f => !f.EndsWith("fonts.ts")))  // fonts.ts mentions the old host in a comment
        {
            var text = File.ReadAllText(f);
            Assert.False(Regex.IsMatch(text, @"fonts\.(googleapis|gstatic)\.com|use\.typekit\.net|fonts\.bunny\.net"), $"{f} loads fonts from a third-party host");
        }
    }

    /// <summary>The licence texts must ship next to the fonts and in the public licence folder linked from the Credits page.</summary>
    [Fact]
    public void Font_and_template_licences_ship_with_the_app()
    {
        foreach (var f in new[] {
            "AppBackendBasic.API/Assets/captions/fonts/OFL.txt", "AppBackendBasic.API/Assets/captions/fonts/Apache-2.0.txt",
            "AppBackendBasic.WebUI/public/caption-fonts/OFL.txt", "AppBackendBasic.WebUI/public/licenses/OFL-1.1.txt",
            "AppBackendBasic.WebUI/public/licenses/Apache-2.0.txt", "AppBackendBasic.WebUI/public/licenses/tscaps-MIT.txt" })
            Assert.True(File.Exists(SchemaTests.RepoFile(f)), f);
        var previewOfl = File.ReadAllText(SchemaTests.RepoFile("AppBackendBasic.WebUI/public/caption-fonts/OFL.txt"));
        foreach (var ttf in Directory.EnumerateFiles(SchemaTests.RepoFile("AppBackendBasic.WebUI/public/caption-fonts"), "*.ttf"))
            Assert.Contains(Path.GetFileName(ttf), previewOfl);  // every served font has its copyright notice listed
    }

    /// <summary>libass must resolve every template family to a BUNDLED file - a silent fallback to a system font would change the look on the server.</summary>
    [Fact]
    public void Libass_resolves_every_template_font_to_a_bundled_file()
    {
        var families = TemplateFonts().Select(f => f.Family).Distinct().ToList();
        var work = Path.Combine(Path.GetTempPath(), "ct-fontsel-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            var events = string.Join("\n", families.Select((f, i) => $"Dialogue: 0,0:00:00.00,0:00:01.00,Default,,0,0,0,,{{\\an5\\pos(540,{60 + i * 60})\\fn{f}}}Abc"));
            File.WriteAllText(Path.Combine(work, "t.ass"), "[Script Info]\nScriptType: v4.00+\nPlayResX: 1080\nPlayResY: 1920\n[V4+ Styles]\n"
                + "Format: Name,Fontname,Fontsize,PrimaryColour,SecondaryColour,OutlineColour,BackColour,Bold,Italic,Underline,StrikeOut,ScaleX,ScaleY,Spacing,Angle,BorderStyle,Outline,Shadow,Alignment,MarginL,MarginR,MarginV,Encoding\n"
                + "Style: Default,Poppins,52,&H00FFFFFF,&H00FFFFFF,&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,0,0,2,10,10,10,1\n"
                + "[Events]\nFormat: Layer,Start,End,Style,Name,MarginL,MarginR,MarginV,Effect,Text\n" + events + "\n");
            var fonts = Fonts();
            var psi = new ProcessStartInfo("ffmpeg") { RedirectStandardError = true, RedirectStandardOutput = true, WorkingDirectory = work };
            foreach (var a in new[] { "-hide_banner", "-loglevel", "verbose", "-f", "lavfi", "-i", "color=c=black:s=1080x1920:d=1",
                         "-vf", $"subtitles=t.ass:fontsdir={FfmpegRenderArgs.EscapeFilterPath(fonts)}", "-ss", "0.5", "-frames:v", "1", "-f", "null", "-" })
                psi.ArgumentList.Add(a);
            Process p;
            try { p = Process.Start(psi)!; }
            catch (System.ComponentModel.Win32Exception)
            {
                Assert.True(Environment.GetEnvironmentVariable("CT_REQUIRE_FFMPEG") != "1", "ffmpeg is required but not available");
                return;
            }
            var stdout = p.StandardOutput.ReadToEndAsync();
            var err = p.StandardError.ReadToEnd();
            p.WaitForExit(); _ = stdout.Result;
            if (!err.Contains("fontselect")) { Assert.True(Environment.GetEnvironmentVariable("CT_REQUIRE_FFMPEG") != "1", "ffmpeg has no libass"); return; }

            var picks = Regex.Matches(err, @"fontselect: \(([^,]+), \d+, \d+\) -> (\S+?), ").Select(m => (Family: m.Groups[1].Value, File: m.Groups[2].Value)).ToList();
            foreach (var family in families)
            {
                var pick = picks.FirstOrDefault(x => x.Family == family);
                Assert.True(pick.File != null, $"libass never looked up '{family}'");
                Assert.True(pick.File.StartsWith(fonts) || !Path.IsPathRooted(pick.File), $"'{family}' fell back to a system font: {pick.File}");
            }
        }
        finally { try { Directory.Delete(work, true); } catch { } }
    }
}
