using System.Text.RegularExpressions;
using AppBackendBasic.Infrastructure.Data;
using AppBackendBasic.Services.Captions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AppBackendBasic.Captions.Tests;

/// <summary>
/// Guards the EF model against the hand-written Postgres scripts in Database_Migrations (22 generic schema + 21 Captain Title).
/// No database connection is needed. The application NEVER creates or changes the schema itself: SQL is run manually.
/// </summary>
public class SchemaTests
{
    private static readonly string[] RemovedSeoTables =
    {
        "AiTrackingResults", "BrandMentions", "BrandProfiles", "CitationAggregations", "CitationMetrics", "Citations", "CommunityOpportunities",
        "CompetitiveIntelligence", "CompetitorAnalyses", "Competitors", "ContentArticles", "ContentGapOpportunities", "JobExecutionLogs", "Keywords",
        "OpportunityImpacts", "OpportunityScans", "Projects", "PromptEditHistory", "PromptPlatformTrackings", "PromptResearchDatas", "PromptTemplates",
        "PromptTrackings", "PromptVisibilityMetrics", "SeoOpportunities", "SiteAuditDetails", "SiteAuditHistories", "SiteAuditIssues", "SiteAuditRuns",
        "SiteAudits", "UserTasks", "VisibilityAlerts", "VisibilityDashboardMetrics", "VisibilityGoals", "VisibilityOpportunities", "VisibilityTrends",
    };

    private static ApplicationDbContext Ctx() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql("Host=localhost;Database=x;Username=x;Password=x").Options);

    internal static string RepoFile(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "AppBackendBasic.sln"))) dir = dir.Parent;
        return Path.Combine(dir!.FullName, relative);
    }

    private static Dictionary<string, List<string>> SqlTables(string sql) =>
        Regex.Matches(sql, "CREATE TABLE IF NOT EXISTS \"(\\w+)\" \\((.*?)\\n\\);", RegexOptions.Singleline).Cast<Match>()
            .ToDictionary(m => m.Groups[1].Value,
                m => Regex.Matches(m.Groups[2].Value, "^\\s+\"(\\w+)\" ", RegexOptions.Multiline).Select(x => x.Groups[1].Value).ToList());

    [Fact]
    public void Model_has_no_ai_seo_tables_and_has_all_caption_tables()
    {
        using var ctx = Ctx();
        var tables = ctx.Model.GetEntityTypes().Select(e => e.GetTableName()!).ToHashSet();
        foreach (var t in RemovedSeoTables) Assert.DoesNotContain(t, tables);
        foreach (var t in new[] { "CaptionProjects", "CaptionUsages", "CaptionPaymentTransactions", "CaptionThemes", "CaptionFonts",
                                  "CaptionVariants", "CaptionSfxTypes", "CaptionStickers", "CaptionPlans", "CaptionFillerWords" })
            Assert.Contains(t, tables);
    }

    [Fact]
    public void Sql_scripts_define_exactly_the_tables_and_columns_of_the_ef_model()
    {
        using var ctx = Ctx();
        var generated = ctx.Database.GenerateCreateScript();
        var expected = Regex.Matches(generated, "CREATE TABLE \"(\\w+)\" \\((.*?)\\n\\);", RegexOptions.Singleline).Cast<Match>()
            .ToDictionary(m => m.Groups[1].Value,
                m => Regex.Matches(m.Groups[2].Value, "^\\s+\"(\\w+)\" ", RegexOptions.Multiline).Select(x => x.Groups[1].Value).ToList());

        var actual = SqlTables(File.ReadAllText(RepoFile("Database_Migrations/22_generic_template_schema.sql")));
        foreach (var kv in SqlTables(File.ReadAllText(RepoFile("Database_Migrations/21_captain_title_captions.sql")))) actual[kv.Key] = kv.Value;
        // columns added by later scripts (ALTER TABLE ... ADD COLUMN IF NOT EXISTS)
        foreach (var m in Regex.Matches(File.ReadAllText(RepoFile("Database_Migrations/25_razorpay_payments_and_render_settings.sql")),
                     "ALTER TABLE \"(\\w+)\"\\s+ADD COLUMN IF NOT EXISTS \"(\\w+)\"").Cast<Match>())
            actual[m.Groups[1].Value].Add(m.Groups[2].Value);
        // script 26 adds many columns in ONE multi-column ALTER TABLE
        foreach (Match stmt in Regex.Matches(File.ReadAllText(RepoFile("Database_Migrations/26_captions_templates_and_text_styling.sql")),
                     "ALTER TABLE \"(\\w+)\"(.*?);", RegexOptions.Singleline))
            foreach (Match col in Regex.Matches(stmt.Groups[2].Value, "ADD COLUMN IF NOT EXISTS \"(\\w+)\""))
                actual[stmt.Groups[1].Value].Add(col.Groups[1].Value);

        Assert.Equal(expected.Keys.OrderBy(x => x), actual.Keys.OrderBy(x => x));
        foreach (var (table, cols) in expected)
            Assert.Equal(cols.OrderBy(x => x), actual[table].OrderBy(x => x));
    }

    [Fact]
    public void Generic_schema_script_contains_no_ai_seo_tables_or_references()
    {
        // comments may mention the removed tables; only executable SQL counts
        var sql = string.Join("\n", File.ReadAllLines(RepoFile("Database_Migrations/22_generic_template_schema.sql")).Where(l => !l.TrimStart().StartsWith("--")));
        foreach (var t in RemovedSeoTables)
            Assert.DoesNotContain($"\"{t}\"", sql);
    }

    [Fact]
    public void Drop_script_drops_every_removed_table_and_nothing_else()
    {
        var sql = File.ReadAllText(RepoFile("Database_Migrations/24_remove_ai_seo_tables.sql"));
        var dropped = Regex.Matches(sql, "DROP TABLE IF EXISTS \"(\\w+)\" CASCADE;").Select(m => m.Groups[1].Value).ToList();
        Assert.Equal(RemovedSeoTables.OrderBy(x => x), dropped.OrderBy(x => x));
    }
}

/// <summary>The seed rows in 21_captain_title_captions.sql must equal CaptionSeedData (which the parity tests tie to the old Python tables).</summary>
public class CaptionSeedSqlTests
{
    // seed rows live in 21 (catalog), 25 (render settings) and 26 (new templates/fonts)
    private static readonly string Sql = File.ReadAllText(SchemaTests.RepoFile("Database_Migrations/21_captain_title_captions.sql"))
        + "\n" + File.ReadAllText(SchemaTests.RepoFile("Database_Migrations/25_razorpay_payments_and_render_settings.sql"))
        + "\n" + File.ReadAllText(SchemaTests.RepoFile("Database_Migrations/26_captions_templates_and_text_styling.sql"));

    private static string Block(string table)
    {
        var blocks = Regex.Matches(Sql, $"INSERT INTO \"{table}\" .*?ON CONFLICT", RegexOptions.Singleline).Cast<Match>().Select(x => x.Value).ToList();
        Assert.NotEmpty(blocks);
        return string.Join("\n", blocks);
    }

    [Fact]
    public void Seed_rows_match_seed_data()
    {
        foreach (var t in CaptionSeedData.Themes())
        {
            Assert.Contains($"('{t.Key}','{t.Name}'", Block("CaptionThemes"));
            Assert.Contains($"'{t.AccentColor}'", Block("CaptionThemes"));
        }
        foreach (var f in CaptionSeedData.Fonts()) Assert.Contains($"('{f.Key}','{f.Label}','{f.Family}'", Block("CaptionFonts"));
        foreach (var v in CaptionSeedData.Variants()) Assert.Contains($"('{v.Key}','{v.Label}','{v.FontFamily}',{v.Border}", Block("CaptionVariants"));
        foreach (var s in CaptionSeedData.SfxTypes()) Assert.Contains($"('{s.Key}','{s.Label}','{s.Icon}','{s.Color}','{s.PreviewKind}',{s.Frequency},{s.SweepTo},{s.DurationSeconds}", Block("CaptionSfxTypes"));
        foreach (var g in CaptionSeedData.Stickers()) Assert.Contains($"('{g.Glyph}',TRUE,{g.SortOrder})", Block("CaptionStickers"));
        foreach (var w in CaptionSeedData.FillerWords()) Assert.Contains($"('{w.Word}',TRUE)", Block("CaptionFillerWords"));
        foreach (var p in CaptionSeedData.Plans()) Assert.Contains($"('{p.Key}','{p.Name}','{p.Kind}',{p.Price}", Block("CaptionPlans"));
        foreach (var (key, value, _, _) in CaptionSeedData.Settings()) Assert.Contains($"('{key}','{value}'", Block("SystemSetting"));
        foreach (var j in CaptionSeedData.LlmJobs()) Assert.Contains($"('{j.JobKey}'", Block("LlmJobConfigs"));
    }

    [Fact]
    public void Tiered_theme_json_in_sql_parses_to_the_seed_tiers()
    {
        foreach (var t in CaptionSeedData.Themes().Where(x => x.TiersJson != null))
        {
            var m = Regex.Match(Sql, $"\\('{t.Key}','[^']*',NULL,'[^']*',NULL,'(\\{{.*?\\}}\\}})'::jsonb", RegexOptions.Singleline);
            Assert.True(m.Success, t.Key);
            var fromSql = CaptionStyleFactory.ParseTiers(m.Groups[1].Value.Replace("''", "'"))!;
            var fromSeed = CaptionStyleFactory.ParseTiers(t.TiersJson)!;
            Assert.Equal(fromSeed.Keys.OrderBy(x => x), fromSql.Keys.OrderBy(x => x));
            foreach (var k in fromSeed.Keys) Assert.Equal(fromSeed[k], fromSql[k]);
        }
    }
}
