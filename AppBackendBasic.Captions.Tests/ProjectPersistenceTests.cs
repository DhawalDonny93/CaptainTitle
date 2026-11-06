using System.Text.RegularExpressions;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;
using AppBackendBasic.Infrastructure.Repositories;
using AppBackendBasic.Services.Admin;
using AppBackendBasic.Services.Captions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AppBackendBasic.Captions.Tests;

/// <summary>
/// Runs only with CAPTIONS_TEST_PG set to a THROW-AWAY database that already has scripts 22, 21, 25, 26 and 27 applied, and whose name starts with
/// "captain_test" (anything else is refused, so these can never write to a real database). Otherwise the tests are skipped.
/// </summary>
public sealed class PgFactAttribute : FactAttribute
{
    public static readonly string? ConnectionString = Environment.GetEnvironmentVariable("CAPTIONS_TEST_PG");

    public PgFactAttribute()
    {
        if (string.IsNullOrEmpty(ConnectionString)) Skip = "CAPTIONS_TEST_PG is not set";
        else if (!Regex.IsMatch(ConnectionString, @"Database=captain_test\w*", RegexOptions.IgnoreCase)) Skip = "CAPTIONS_TEST_PG must point at a captain_test* database";
    }
}

public class ProjectPersistenceTests : IDisposable
{
    private readonly string _storageRoot = Path.Combine(Path.GetTempPath(), "captain-test-" + Guid.NewGuid().ToString("N"));
    private readonly List<ApplicationDbContext> _contexts = new();
    private readonly RecordingQueue _queue = new();
    private readonly IOptions<CaptionOptions> _options;
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    public ProjectPersistenceTests()
    {
        Directory.CreateDirectory(_storageRoot);
        _options = Options.Create(new CaptionOptions { StorageRoot = _storageRoot, MaxVersionsPerProject = 3, VersionIntervalMinutes = 5 });
    }

    public void Dispose()
    {
        foreach (var c in _contexts) c.Dispose();
        _cache.Dispose();
        try { Directory.Delete(_storageRoot, true); } catch { /* best effort */ }
    }

    private ApplicationDbContext Db()
    {
        var ctx = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(PgFactAttribute.ConnectionString).Options);
        _contexts.Add(ctx);
        return ctx;
    }

    private CaptionCatalogService Catalog(ApplicationDbContext db) =>
        new(db, new SystemSettingsService(db, NullLogger<SystemSettingsService>.Instance), _cache, _options);

    private CaptionEntitlementService Entitlement(ApplicationDbContext db) => new(db, Catalog(db));

    /// <summary>A fresh service on its own DbContext, like one HTTP request.</summary>
    private CaptionProjectService Service()
    {
        var db = Db();
        return new CaptionProjectService(new CaptionProjectRepository(db), new LocalCaptionStorage(_options), new StubFfmpeg(), Catalog(db), Entitlement(db), _queue,
            new StubHinglish(), new CaptionMediaTokenService(new EphemeralDataProtectionProvider(), _options), _options, NullLogger<CaptionProjectService>.Instance);
    }

    private CaptionJobRunner Runner(ICaptionTranscriptionService? transcription = null, IFfmpegService? ffmpeg = null)
    {
        var db = Db();
        return new CaptionJobRunner(db, new LocalCaptionStorage(_options), ffmpeg ?? new StubFfmpeg(), transcription ?? new StubTranscription(), new StubHinglish(), Catalog(db), Entitlement(db), NullLogger<CaptionJobRunner>.Instance);
    }

    private async Task<int> NewUserAsync(int free = 1)
    {
        var db = Db();
        var user = new User { Email = $"{Guid.NewGuid():N}@test.local" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        db.CaptionUsages.Add(new CaptionUsage { UserId = user.Id, FreeRemaining = free });
        await db.SaveChangesAsync();
        return user.Id;
    }

    private async Task<int> NewProjectAsync(int userId, string status = CaptionProjectStatus.Ready, Action<CaptionProject>? configure = null)
    {
        var path = $"captain-title/uploads/{userId}/{Guid.NewGuid():N}.mp4";
        var full = Path.Combine(_storageRoot, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await File.WriteAllBytesAsync(full, new byte[] { 1, 2, 3 });
        var p = new CaptionProject
        {
            UserId = userId, Name = "clip.mp4", Duration = 10, SourcePath = path, Status = status, CreditType = CaptionCreditType.Free,
            CreditConsumedAt = status == CaptionProjectStatus.Ready ? DateTime.UtcNow : null, TranscriptionMode = status == CaptionProjectStatus.Ready ? "m" : null,
            WordsJson = status == CaptionProjectStatus.Ready ? CaptionJson.Serialize(Words("original")) : "[]",
        };
        configure?.Invoke(p);
        var db = Db();
        db.CaptionProjects.Add(p);
        await db.SaveChangesAsync();
        return p.Id;
    }

    private static List<CaptionWordDto> Words(string text) => new() { new CaptionWordDto { Index = 0, Text = text, Original = text, Start = 0, End = 1 } };

    private static CaptionProjectEditDto Edit(string text, int? baseRevision, string theme = FixedTemplates.StreetGlow) =>
        new() { BaseRevision = baseRevision, Theme = theme, FontId = "poppins", Words = Words(text) };

    private async Task AgeVersionsAsync(int projectId) =>
        await Db().CaptionProjectVersions.Where(v => v.ProjectId == projectId).ExecuteUpdateAsync(s => s.SetProperty(v => v.CreatedAt, v => v.CreatedAt.AddHours(-1)));

    private Task<int> CountVersionsAsync(int projectId) => Db().CaptionProjectVersions.CountAsync(v => v.ProjectId == projectId);

    [PgFact]
    public async Task List_is_scoped_to_the_user_sorted_by_last_edit_and_pages_with_a_cursor()
    {
        var user = await NewUserAsync();
        var other = await NewUserAsync();
        await NewProjectAsync(other);
        var ids = new List<int>();
        for (int i = 0; i < 5; i++) ids.Add(await NewProjectAsync(user, configure: p => p.LastEditedAt = DateTime.UtcNow.AddMinutes(-10 + i)));
        await Service().SaveAsync(user, ids[0], Edit("edited", 1)); // the oldest becomes the newest

        var first = await Service().ListAsync(user, null, 2);
        Assert.Equal(new[] { ids[0], ids[4] }, first.Items.Select(x => x.ProjectId));
        Assert.NotNull(first.NextCursor);
        var second = await Service().ListAsync(user, first.NextCursor, 2);
        Assert.Equal(new[] { ids[3], ids[2] }, second.Items.Select(x => x.ProjectId));
        var third = await Service().ListAsync(user, second.NextCursor, 2);
        Assert.Equal(new[] { ids[1] }, third.Items.Select(x => x.ProjectId));
        Assert.Null(third.NextCursor);

        var bad = await Assert.ThrowsAsync<CaptionServiceException>(() => Service().ListAsync(user, "not-a-cursor", 2));
        Assert.Equal(400, bad.StatusCode);
    }

    [PgFact]
    public async Task Another_users_project_is_a_404_everywhere()
    {
        var owner = await NewUserAsync();
        var stranger = await NewUserAsync();
        var id = await NewProjectAsync(owner);
        foreach (var call in new Func<Task>[]
        {
            () => Service().GetAsync(stranger, id),
            () => Service().EnsureOwnedAsync(stranger, id),
            () => Service().SaveAsync(stranger, id, Edit("x", null)),
            () => Service().SaveEditorStateAsync(stranger, id, new CaptionEditorStateDto()),
            () => Service().DeleteAsync(stranger, id),
            () => Service().ListVersionsAsync(stranger, id),
            () => Service().OpenRenderAsync(stranger, id),
            () => Service().OpenThumbnailAsync(stranger, id),
        })
        {
            var ex = await Assert.ThrowsAsync<CaptionServiceException>(call);
            Assert.Equal(404, ex.StatusCode);
        }
        Assert.Null(await Db().CaptionProjectEditorStates.AsNoTracking().FirstOrDefaultAsync(s => s.ProjectId == id));
    }

    [PgFact]
    public async Task Stale_base_revision_is_rejected_and_keeps_the_newer_content()
    {
        var user = await NewUserAsync();
        var id = await NewProjectAsync(user);
        var saved = await Service().SaveAsync(user, id, Edit("tab one", 1));
        Assert.Equal(2, saved.Revision);

        var ex = await Assert.ThrowsAsync<CaptionServiceException>(() => Service().SaveAsync(user, id, Edit("tab two", 1)));
        Assert.Equal(409, ex.StatusCode);
        Assert.Equal(CaptionServiceException.RevisionConflict, ex.Code);
        Assert.Equal("tab one", (await Service().GetAsync(user, id)).Words[0].Text);
    }

    [PgFact]
    public async Task Racing_saves_with_the_same_base_revision_have_exactly_one_winner()
    {
        var user = await NewUserAsync();
        var id = await NewProjectAsync(user);
        var results = await Task.WhenAll(Enumerable.Range(0, 6).Select(async i =>
        {
            try { await Service().SaveAsync(user, id, Edit($"writer {i}", 1)); return true; }
            catch (CaptionServiceException ex) when (ex.StatusCode == 409) { return false; }
        }));
        Assert.Equal(1, results.Count(ok => ok));
        Assert.Equal(2, (await Service().GetAsync(user, id)).Revision);
    }

    [PgFact]
    public async Task A_retired_template_still_saves_on_a_project_using_it_but_cannot_be_newly_chosen()
    {
        var user = await NewUserAsync();
        var id = await NewProjectAsync(user, configure: p => p.Theme = "viral"); // deactivated by script 26
        Assert.Equal(2, (await Service().SaveAsync(user, id, Edit("kept", 1, theme: "viral"))).Revision);

        var other = await NewProjectAsync(user);
        var ex = await Assert.ThrowsAsync<CaptionServiceException>(() => Service().SaveAsync(user, other, Edit("x", 1, theme: "viral")));
        Assert.Equal(400, ex.StatusCode);
    }

    [PgFact]
    public async Task Saving_is_refused_while_captions_are_generating()
    {
        var user = await NewUserAsync();
        foreach (var status in new[] { CaptionProjectStatus.Queued, CaptionProjectStatus.Processing })
        {
            var id = await NewProjectAsync(user, status);
            var ex = await Assert.ThrowsAsync<CaptionServiceException>(() => Service().SaveAsync(user, id, Edit("x", null)));
            Assert.Equal(409, ex.StatusCode);
        }
    }

    [PgFact]
    public async Task Autosave_keeps_a_throttled_and_bounded_set_of_restore_points()
    {
        var user = await NewUserAsync();
        var id = await NewProjectAsync(user);
        await Service().SaveAsync(user, id, Edit("a", 1));
        await Service().SaveAsync(user, id, Edit("b", 2));
        await Service().SaveAsync(user, id, Edit("c", 3));
        Assert.Equal(1, await CountVersionsAsync(id)); // within the interval: only the pre-edit original

        int revision = 4;
        for (int i = 0; i < 5; i++)
        {
            await AgeVersionsAsync(id);
            await Service().SaveAsync(user, id, Edit($"later {i}", revision++));
        }
        Assert.Equal(3, await CountVersionsAsync(id)); // MaxVersionsPerProject
    }

    [PgFact]
    public async Task Restore_brings_the_document_back_and_keeps_what_it_replaced()
    {
        var user = await NewUserAsync();
        var id = await NewProjectAsync(user);
        await Service().SaveAsync(user, id, Edit("edited", 1));
        var version = (await Service().ListVersionsAsync(user, id)).Single();
        Assert.Equal(CaptionVersionReason.Autosave, version.Reason);

        var restored = await Service().RestoreVersionAsync(user, id, version.VersionId);
        Assert.Equal("original", restored.Words[0].Text);
        Assert.Equal(3, restored.Revision);
        var versions = await Service().ListVersionsAsync(user, id);
        Assert.Equal(CaptionVersionReason.BeforeRestore, versions[0].Reason);
        Assert.Equal("edited", (await Service().RestoreVersionAsync(user, id, versions[0].VersionId)).Words[0].Text);

        var missing = await Assert.ThrowsAsync<CaptionServiceException>(() => Service().RestoreVersionAsync(user, id, int.MaxValue));
        Assert.Equal(404, missing.StatusCode);
    }

    [PgFact]
    public async Task A_version_of_another_project_cannot_be_restored()
    {
        var user = await NewUserAsync();
        var a = await NewProjectAsync(user);
        var b = await NewProjectAsync(user);
        await Service().SaveAsync(user, a, Edit("edited", 1));
        var version = (await Service().ListVersionsAsync(user, a)).Single();
        var ex = await Assert.ThrowsAsync<CaptionServiceException>(() => Service().RestoreVersionAsync(user, b, version.VersionId));
        Assert.Equal(404, ex.StatusCode);
    }

    [PgFact]
    public async Task Relisten_keeps_a_restore_point_of_the_edits_it_wipes_and_a_failed_relisten_can_be_undone()
    {
        var user = await NewUserAsync();
        var id = await NewProjectAsync(user);
        await Service().SaveAsync(user, id, Edit("hand edited", 1));
        var queued = await Service().RetranscribeAsync(user, id, "auto");
        Assert.Empty(queued.Words);
        Assert.Equal(CaptionProjectStatus.Queued, queued.Status);

        await Db().CaptionProjects.Where(p => p.Id == id).ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, CaptionProjectStatus.Failed));
        var version = (await Service().ListVersionsAsync(user, id)).First(v => v.Reason == CaptionVersionReason.BeforeRelisten);
        var restored = await Service().RestoreVersionAsync(user, id, version.VersionId);
        Assert.Equal("hand edited", restored.Words[0].Text);
        Assert.Equal(CaptionProjectStatus.Ready, restored.Status);
        Assert.Null(restored.ErrorMessage);
    }

    [PgFact]
    public async Task Credit_is_charged_once_for_the_first_success_however_many_reruns_happen()
    {
        var user = await NewUserAsync(free: 3);
        var id = await NewProjectAsync(user, CaptionProjectStatus.Queued);

        await Runner().RunAsync(id, consumeCredit: true);
        Assert.Equal(2, (await Entitlement(Db()).GetOrCreateUsageAsync(user)).FreeRemaining);

        // Re-listen, then the worker re-queues it again after a restart: neither may charge again.
        await Service().RetranscribeAsync(user, id, "auto");
        await Runner().RunAsync(id, consumeCredit: true);
        Assert.Equal(2, (await Entitlement(Db()).GetOrCreateUsageAsync(user)).FreeRemaining);
        var after = await Service().GetAsync(user, id);
        Assert.Equal(CaptionProjectStatus.Ready, after.Status);
        Assert.NotEmpty(after.Words);
    }

    [PgFact]
    public async Task A_relisten_after_a_failed_first_run_pays_for_itself_and_needs_a_credit()
    {
        var broke = await NewUserAsync(free: 0);
        var failed = await NewProjectAsync(broke, CaptionProjectStatus.Failed);
        var denied = await Assert.ThrowsAsync<CaptionServiceException>(() => Service().RetranscribeAsync(broke, failed, "auto"));
        Assert.Equal(402, denied.StatusCode);

        var user = await NewUserAsync(free: 1);
        var id = await NewProjectAsync(user, CaptionProjectStatus.Failed);
        await Service().RetranscribeAsync(user, id, "auto");
        await Runner().RunAsync(id, consumeCredit: true);
        Assert.Equal(0, (await Entitlement(Db()).GetOrCreateUsageAsync(user)).FreeRemaining);
    }

    [PgFact]
    public async Task An_admin_rerun_settles_the_credit_without_charging()
    {
        var user = await NewUserAsync(free: 1);
        var id = await NewProjectAsync(user, CaptionProjectStatus.Queued);
        await Runner().RunAsync(id, consumeCredit: false);
        Assert.Equal(1, (await Entitlement(Db()).GetOrCreateUsageAsync(user)).FreeRemaining);
        Assert.NotNull(await Db().CaptionProjects.Where(p => p.Id == id).Select(p => p.CreditConsumedAt).FirstAsync());
    }

    [PgFact]
    public async Task Finished_transcription_moves_the_revision_and_stores_a_thumbnail()
    {
        var user = await NewUserAsync();
        var id = await NewProjectAsync(user, CaptionProjectStatus.Queued);
        await Runner().RunAsync(id, consumeCredit: true);
        var project = await Service().GetAsync(user, id);
        Assert.Equal(2, project.Revision);
        var card = (await Service().ListAsync(user, null, 10)).Items.Single();
        Assert.StartsWith($"/api/captions/thumbnail/{id}?t=", card.ThumbnailUrl);
        await using var thumbnail = await Service().OpenThumbnailAsync(user, id);
        Assert.True(thumbnail.Length > 0);
    }

    [PgFact]
    public async Task Editing_after_an_export_makes_it_out_of_date()
    {
        var user = await NewUserAsync();
        var id = await NewProjectAsync(user, configure: p => { p.Status = CaptionProjectStatus.Rendered; p.RenderPath = "r.mp4"; p.RenderedRevision = 1; });
        Assert.True((await Service().ListAsync(user, null, 1)).Items[0].ExportCurrent);
        await Service().SaveAsync(user, id, Edit("changed", 1));
        Assert.False((await Service().ListAsync(user, null, 1)).Items[0].ExportCurrent);
        var project = await Service().GetAsync(user, id);
        Assert.Equal(1, project.RenderedRevision);
        Assert.Equal(2, project.Revision);
    }

    [PgFact]
    public async Task Editor_state_round_trips_upserts_and_never_touches_the_revision()
    {
        var user = await NewUserAsync();
        var id = await NewProjectAsync(user);
        Assert.Null((await Service().GetAsync(user, id)).EditorState);
        await Service().SaveEditorStateAsync(user, id, new CaptionEditorStateDto { Playhead = 4.5, LeftTab = "audio", ThemeTab = "text", SelectedIndex = 7, SafeZone = true });
        await Service().SaveEditorStateAsync(user, id, new CaptionEditorStateDto { Playhead = 8.25, LeftTab = "ai", ThemeTab = "text", SelectedIndex = 9, SafeZone = true });
        var project = await Service().GetAsync(user, id);
        Assert.Equal(8.25, project.EditorState!.Playhead);
        Assert.Equal("ai", project.EditorState.LeftTab);
        Assert.Equal(9, project.EditorState.SelectedIndex);
        Assert.Equal(1, project.Revision);
    }

    [PgFact]
    public async Task Delete_removes_the_row_children_and_files_but_not_while_a_job_runs()
    {
        var user = await NewUserAsync();
        var busy = await NewProjectAsync(user, CaptionProjectStatus.Processing);
        var refused = await Assert.ThrowsAsync<CaptionServiceException>(() => Service().DeleteAsync(user, busy));
        Assert.Equal(409, refused.StatusCode);
        await Service().EnsureOwnedAsync(user, busy);

        var id = await NewProjectAsync(user);
        await Service().SaveAsync(user, id, Edit("x", 1));
        await Service().SaveEditorStateAsync(user, id, new CaptionEditorStateDto { Playhead = 1 });
        var source = await Db().CaptionProjects.Where(p => p.Id == id).Select(p => p.SourcePath).FirstAsync();
        Assert.True(File.Exists(Path.Combine(_storageRoot, source)));

        await Service().DeleteAsync(user, id);

        Assert.False(File.Exists(Path.Combine(_storageRoot, source)));
        Assert.Equal(0, await CountVersionsAsync(id));
        Assert.False(await Db().CaptionProjectEditorStates.AnyAsync(s => s.ProjectId == id));
        var gone = await Assert.ThrowsAsync<CaptionServiceException>(() => Service().GetAsync(user, id));
        Assert.Equal(404, gone.StatusCode);
    }

    [PgFact]
    public async Task A_video_missing_from_storage_is_a_clear_410()
    {
        var user = await NewUserAsync();
        var id = await NewProjectAsync(user);
        var source = await Db().CaptionProjects.Where(p => p.Id == id).Select(p => p.SourcePath).FirstAsync();
        File.Delete(Path.Combine(_storageRoot, source));
        var ex = await Assert.ThrowsAsync<CaptionServiceException>(() => Service().OpenMediaAsync(user, id));
        Assert.Equal(410, ex.StatusCode);
    }

    [PgFact]
    public async Task A_provider_failure_is_reported_as_a_server_issue_and_is_not_charged()
    {
        var user = await NewUserAsync();
        var id = await NewProjectAsync(user, CaptionProjectStatus.Queued);
        await Runner(new ThrowingTranscription(new HttpRequestException("Whisper request failed (401)"))).RunAsync(id, consumeCredit: true);
        var p = await Db().CaptionProjects.AsNoTracking().FirstAsync(x => x.Id == id);
        Assert.Equal(CaptionProjectStatus.Failed, p.Status);
        Assert.Equal(CaptionErrors.ServerIssue, p.ErrorMessage);
        Assert.Null(p.CreditConsumedAt);
    }

    [PgFact]
    public async Task Unclear_audio_is_reported_as_a_problem_with_the_video()
    {
        var user = await NewUserAsync();
        var id = await NewProjectAsync(user, CaptionProjectStatus.Queued);
        await Runner(new ThrowingTranscription(new NoTrustworthyTranscriptException())).RunAsync(id, consumeCredit: true);
        var p = await Db().CaptionProjects.AsNoTracking().FirstAsync(x => x.Id == id);
        Assert.Equal(CaptionProjectStatus.Failed, p.Status);
        Assert.Contains("No trustworthy transcript", p.ErrorMessage);
    }

    [PgFact]
    public async Task A_video_without_audio_says_so()
    {
        var user = await NewUserAsync();
        var id = await NewProjectAsync(user, CaptionProjectStatus.Queued);
        await Runner(ffmpeg: new StubFfmpeg(new VideoProbe(10, 1080, 1920, "h264"))).RunAsync(id, consumeCredit: true);
        var p = await Db().CaptionProjects.AsNoTracking().FirstAsync(x => x.Id == id);
        Assert.Equal(CaptionProjectStatus.Failed, p.Status);
        Assert.Contains("no audio track", p.ErrorMessage);
    }

    private sealed class ThrowingTranscription : ICaptionTranscriptionService
    {
        private readonly Exception _error;
        public ThrowingTranscription(Exception error) { _error = error; }
        public Task<(List<CaptionWordDto> Words, TranscriptionDiagnostics Diagnostics)> TranscribeWordsAsync(string audioPath, string speechLanguage, CancellationToken ct = default) =>
            Task.FromException<(List<CaptionWordDto>, TranscriptionDiagnostics)>(_error);
    }

    private sealed class RecordingQueue : ICaptionJobQueue
    {
        public bool Enqueue(int projectId, bool consumeCredit) => true;
        public bool EnqueueRender(int projectId) => true;
    }

    private sealed class StubHinglish : IHinglishService
    {
        public Task<List<CaptionWordDto>> ConvertToHinglishAsync(List<CaptionWordDto> words, string context, CancellationToken ct = default) => Task.FromResult(words);
        public Task<List<CaptionWordDto>> ReassignEmphasisAsync(List<CaptionWordDto> words, string context, CancellationToken ct = default) => Task.FromResult(words);
    }

    private sealed class StubTranscription : ICaptionTranscriptionService
    {
        public Task<(List<CaptionWordDto> Words, TranscriptionDiagnostics Diagnostics)> TranscribeWordsAsync(string audioPath, string speechLanguage, CancellationToken ct = default) =>
            Task.FromResult((Words("heard"), new TranscriptionDiagnostics("stub", 1, 1, 1, 0)));
    }

    private sealed class StubFfmpeg : IFfmpegService
    {
        private readonly VideoProbe _probe;
        public StubFfmpeg(VideoProbe? probe = null) { _probe = probe ?? new VideoProbe(10, 1080, 1920, "h264", "aac"); }
        public Task<FfmpegResult> RunAsync(IEnumerable<string> args, string? workingDirectory = null, CancellationToken ct = default) => Task.FromResult(new FfmpegResult(0, ""));
        public Task<VideoProbe> ProbeAsync(string path, CancellationToken ct = default) => Task.FromResult(_probe);
        public Task ExtractAudioAsync(string source, string wavOut, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> CreateBrowserPreviewAsync(string source, string output, CancellationToken ct = default) => Task.FromResult(false);
        public async Task<bool> CreateThumbnailAsync(string source, string output, double atSeconds, CancellationToken ct = default)
        {
            await File.WriteAllBytesAsync(output, new byte[] { 0xFF, 0xD8 }, ct);
            return true;
        }
        public Task<FfmpegCapability> CheckAsync(CancellationToken ct = default) => Task.FromResult(new FfmpegCapability(true, "", null, true, true, null));
        public Task<List<double>> SilenceMidpointsAsync(string audioPath, CancellationToken ct = default) => Task.FromResult(new List<double>());
    }
}
