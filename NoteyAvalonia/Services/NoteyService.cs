using NoteToolAvalonia.Models;
using NoteToolAvalonia.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace NoteToolAvalonia.Services;

// ============================================================
//  NAVIGATION
//  Decouples views from each other — no view holds a reference
//  to another. MainWindowViewModel registers itself on startup,
//  then anyone with a NoteyService can navigate anywhere.
// ============================================================

public interface INoteyNavigator
{
    void NavigateToWelcome();
    void NavigateToNoteEditor(NoteCard card);
    void NavigateToSettings();
    void NavigateToCredits();
}

// ============================================================
//  NOTEY SERVICE
//  Single entry point for all app-level operations:
//    - Navigation  (INoteyNavigator)
//    - Note files  (markdown + frontmatter on disk)
//    - App data    (settings + note index as JSON)
//    - Edit history (undo / redo / revision snapshots)
// ============================================================

public class NoteyService : INoteyNavigator
{
    // ── Paths ──────────────────────────────────────────────

    private readonly string _dataFolder;
    private readonly string _notesFolder;
    private readonly string _notesIndexFile;
    private readonly string _settingsFile;

    // ── Infrastructure ─────────────────────────────────────

    private readonly JsonSerializerOptions _jsonOptions;
    private readonly object _fileLock = new();
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 100;

    // ── Navigation target ──────────────────────────────────

    private MainWindowViewModel? _mainVm;

    // ── Edit history (one instance per open note session) ──

    private readonly EditHistory _editHistory = new();

    // ══════════════════════════════════════════════════════
    //  CONSTRUCTOR
    // ══════════════════════════════════════════════════════

    public NoteyService()
    {
        _dataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NoteToolAvalonia");

        _notesFolder = Path.Combine(_dataFolder, "notes");
        _notesIndexFile = Path.Combine(_dataFolder, "notes-index.json");
        _settingsFile = Path.Combine(_dataFolder, "settings.json");

        Directory.CreateDirectory(_dataFolder);
        Directory.CreateDirectory(_notesFolder);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.Never
        };
    }

    // ══════════════════════════════════════════════════════
    //  REGION: NAVIGATION
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Call this once from App startup so the service knows
    /// which window to drive navigation on.
    /// </summary>
    public void SetMainViewModel(MainWindowViewModel vm) => _mainVm = vm;

    public void NavigateToWelcome()     => _mainVm?.NavigateToWelcome();
    public void NavigateToSettings()    => _mainVm?.NavigateToSettings();
    public void NavigateToCredits()     => _mainVm?.NavigateToCredits();
    public void NavigateToNoteEditor(NoteCard card) =>
        _mainVm?.NavigateToNoteEditor(card);

    // ══════════════════════════════════════════════════════
    //  REGION: NOTE FILE I/O  (markdown + YAML frontmatter)
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Loads the raw markdown body of a note (everything after
    /// the frontmatter block). Returns empty string if not found.
    /// </summary>
    public string LoadNoteContent(string noteId)
    {
        var path = GetNotePath(noteId);
        lock (_fileLock)
        {
            if (!File.Exists(path)) return string.Empty;
            try
            {
                var raw = File.ReadAllText(path);
                return StripFrontmatter(raw);
            }
            catch { return string.Empty; }
        }
    }

    /// <summary>
    /// Saves markdown body + serialised metadata as YAML frontmatter.
    /// The resulting file is valid, portable markdown you can open
    /// in Obsidian, VS Code, or any other editor.
    /// </summary>
    public void SaveNote(NoteCard card, string markdownBody)
    {
        card.LastModified = DateTime.Now;
        var fullFile = BuildFileWithFrontmatter(card, markdownBody);
        lock (_fileLock)
        {
            WriteWithRetry(GetNotePath(card.Id), fullFile);
        }
        // Keep the index in sync
        UpsertNoteInIndex(card);
    }

    /// <summary>
    /// Reads a note's metadata back from its frontmatter without
    /// loading the full body. Useful for building the library list.
    /// </summary>
    public NoteCard? LoadNoteMetadata(string noteId)
    {
        var path = GetNotePath(noteId);
        lock (_fileLock)
        {
            if (!File.Exists(path)) return null;
            try
            {
                var raw = File.ReadAllText(path);
                return ParseFrontmatter(noteId, raw);
            }
            catch { return null; }
        }
    }

    /// <summary>
    /// Deletes the .md file for a note and removes it from the index.
    /// </summary>
    public void DeleteNote(string noteId)
    {
        var path = GetNotePath(noteId);
        lock (_fileLock)
        {
            if (File.Exists(path)) File.Delete(path);
        }
        RemoveNoteFromIndex(noteId);
    }

    /// <summary>
    /// Exports a note's full content (frontmatter + body) to an
    /// arbitrary file path chosen by the user.
    /// </summary>
    public void ExportNote(string noteId, string targetPath)
    {
        var path = GetNotePath(noteId);
        lock (_fileLock)
        {
            if (!File.Exists(path)) return;
            var content = File.ReadAllText(path);
            WriteWithRetry(targetPath, content);
        }
    }

    // ── Frontmatter helpers ────────────────────────────────

    /// <summary>
    /// Builds the full file text: a YAML frontmatter block
    /// followed by the markdown body.
    ///
    /// Example output:
    ///   ---
    ///   title: My Note
    ///   category: Work
    ///   tags: [design, ux]
    ///   deadline: 2026-07-01
    ///   references: [abc-123]
    ///   priority: High
    ///   created: 2026-06-01T10:00:00
    ///   modified: 2026-06-15T14:30:00
    ///   completed: false
    ///   ---
    ///
    ///   Your markdown here...
    /// </summary>
    private static string BuildFileWithFrontmatter(NoteCard card, string body)
    {
        var tags = card.Tags != null && card.Tags.Count > 0
            ? $"[{string.Join(", ", card.Tags)}]"
            : "[]";

        var refs = card.References != null && card.References.Count > 0
            ? $"[{string.Join(", ", card.References)}]"
            : "[]";

        var deadline = card.Deadline.HasValue
            ? card.Deadline.Value.ToString("yyyy-MM-dd")
            : "";

        var fm = $"""
            ---
            title: {card.Title}
            category: {card.Category}
            tags: {tags}
            deadline: {deadline}
            references: {refs}
            priority: {card.Priority}
            created: {card.CreatedAt:O}
            modified: {card.LastModified:O}
            completed: {card.IsCompleted.ToString().ToLower()}
            ---

            """;

        return fm + body;
    }

    /// <summary>
    /// Parses a YAML frontmatter block at the top of a file back
    /// into a NoteCard. Handles missing or malformed fields gracefully.
    /// </summary>
    private static NoteCard ParseFrontmatter(string noteId, string raw)
    {
        var card = new NoteCard { Id = noteId };

        if (!raw.StartsWith("---")) return card;

        var end = raw.IndexOf("---", 3);
        if (end < 0) return card;

        var block = raw[3..end];

        foreach (var line in block.Split('\n'))
        {
            var colon = line.IndexOf(':');
            if (colon < 0) continue;

            var key   = line[..colon].Trim();
            var value = line[(colon + 1)..].Trim();

            switch (key)
            {
                case "title":      card.Title    = value; break;
                case "category":   card.Category = value; break;
                case "completed":  card.IsCompleted = value == "true"; break;
                case "priority":
                    if (Enum.TryParse<NotePriority>(value, out var p))
                        card.Priority = p;
                    break;
                case "deadline":
                    if (DateTime.TryParse(value, out var d))
                        card.Deadline = d;
                    break;
                case "created":
                    if (DateTime.TryParse(value, out var c))
                        card.CreatedAt = c;
                    break;
                case "modified":
                    if (DateTime.TryParse(value, out var m))
                        card.LastModified = m;
                    break;
                case "tags":
                    card.Tags = ParseYamlList(value);
                    break;
                case "references":
                    card.References = ParseYamlList(value);
                    break;
            }
        }

        return card;
    }

    /// <summary>
    /// Returns everything after the closing --- of a frontmatter block.
    /// If there's no frontmatter, returns the whole string unchanged.
    /// </summary>
    private static string StripFrontmatter(string raw)
    {
        if (!raw.StartsWith("---")) return raw;
        var end = raw.IndexOf("---", 3);
        if (end < 0) return raw;
        var bodyStart = raw.IndexOf('\n', end);
        return bodyStart >= 0 ? raw[(bodyStart + 1)..].TrimStart('\n') : string.Empty;
    }

    /// <summary>
    /// Parses a simple YAML inline list: [item1, item2] → List of strings.
    /// </summary>
    private static List<string> ParseYamlList(string value)
    {
        value = value.Trim('[', ']');
        if (string.IsNullOrWhiteSpace(value)) return new();
        return value.Split(',')
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .ToList();
    }

    private string GetNotePath(string noteId) =>
        Path.Combine(_notesFolder, $"{noteId}.md");

    // ══════════════════════════════════════════════════════
    //  REGION: NOTES INDEX  (fast library listing)
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Loads all note metadata from the index file.
    /// The index stores only lightweight info (id, title, category,
    /// tags, deadline) so the library can load without reading every
    /// individual .md file.
    /// </summary>
    public List<NoteCard> LoadNotesIndex()
    {
        lock (_fileLock)
        {
            if (!File.Exists(_notesIndexFile)) return new();
            try
            {
                var json = File.ReadAllText(_notesIndexFile);
                return JsonSerializer.Deserialize<List<NoteCard>>(json, _jsonOptions)
                       ?? new();
            }
            catch { return new(); }
        }
    }

    private void UpsertNoteInIndex(NoteCard card)
    {
        var index = LoadNotesIndex();
        var i = index.FindIndex(n => n.Id == card.Id);
        if (i >= 0) index[i] = card;
        else        index.Add(card);
        SaveNotesIndex(index);
    }

    private void RemoveNoteFromIndex(string noteId)
    {
        var index = LoadNotesIndex();
        index.RemoveAll(n => n.Id == noteId);
        SaveNotesIndex(index);
    }

    private void SaveNotesIndex(List<NoteCard> index)
    {
        lock (_fileLock)
        {
            var json = JsonSerializer.Serialize(index, _jsonOptions);
            WriteWithRetry(_notesIndexFile, json);
        }
    }

    // ══════════════════════════════════════════════════════
    //  REGION: SETTINGS
    // ══════════════════════════════════════════════════════

    public AppSettings LoadSettings()
    {
        lock (_fileLock)
        {
            if (!File.Exists(_settingsFile)) return new AppSettings();
            try
            {
                var json = File.ReadAllText(_settingsFile);
                return JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions)
                       ?? new AppSettings();
            }
            catch { return new AppSettings(); }
        }
    }

    public void SaveSettings(AppSettings settings)
    {
        lock (_fileLock)
        {
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            WriteWithRetry(_settingsFile, json);
        }
    }

    // ══════════════════════════════════════════════════════
    //  REGION: EDIT HISTORY  (undo / redo / revisions)
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Direct access to the edit history for the note currently
    /// open in the editor. The editor ViewModel uses this to push
    /// states and request undo/redo.
    /// </summary>
    public EditHistory History => _editHistory;

    /// <summary>The root folder where all Notey data lives.</summary>
    public string DataFolder => _dataFolder;

    /// <summary>Folder containing individual .md note files.</summary>
    public string NotesFolder => _notesFolder;

    // ══════════════════════════════════════════════════════
    //  REGION: FILE I/O HELPERS
    // ══════════════════════════════════════════════════════

    private void WriteWithRetry(string path, string content)
    {
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                File.WriteAllText(path, content);
                return;
            }
            catch (IOException) when (attempt < MaxRetries - 1)
            {
                Thread.Sleep(RetryDelayMs);
            }
        }
        throw new IOException(
            $"Could not write to {path} after {MaxRetries} attempts.");
    }
}

// ============================================================
//  EDIT HISTORY
//  Standalone class — easy to lift out into another project.
//  Manages undo/redo stacks and periodic revision snapshots.
// ============================================================

public class EditHistory
{
    private record HistoryState(string Content, int CursorPosition, DateTime Timestamp);

    private readonly List<HistoryState> _undo      = new();
    private readonly List<HistoryState> _redo      = new();
    private readonly List<HistoryState> _revisions = new();

    private DateTime _lastRevisionTime = DateTime.Now;

    private readonly int      _maxUndo;
    private readonly int      _maxRevisions;
    private readonly TimeSpan _revisionInterval;

    public int UndoCount     => _undo.Count;
    public int RedoCount     => _redo.Count;
    public int RevisionCount => _revisions.Count;

    public EditHistory(
        int maxUndo = 50,
        int maxRevisions = 100,
        int revisionIntervalMinutes = 5)
    {
        _maxUndo           = maxUndo;
        _maxRevisions      = maxRevisions;
        _revisionInterval  = TimeSpan.FromMinutes(revisionIntervalMinutes);
    }

    /// <summary>
    /// Call this every time the user makes an edit.
    /// Clears the redo stack (a new edit invalidates redo history).
    /// Auto-snapshots a revision every few minutes.
    /// </summary>
    public void Push(string content, int cursorPosition = 0)
    {
        _undo.Add(new(content, cursorPosition, DateTime.Now));
        _redo.Clear();

        if (_undo.Count > _maxUndo)
            _undo.RemoveAt(0);

        if (DateTime.Now - _lastRevisionTime >= _revisionInterval)
        {
            Snapshot(content);
            _lastRevisionTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Restores the previous state. Returns (content, cursor) or null
    /// if there's nothing left to undo.
    /// </summary>
    public (string Content, int CursorPosition)? Undo()
    {
        if (_undo.Count == 0) return null;

        var current = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        _redo.Add(current);

        if (_undo.Count == 0) return null;

        var prev = _undo[^1];
        return (prev.Content, prev.CursorPosition);
    }

    /// <summary>
    /// Re-applies the last undone state. Returns (content, cursor) or null.
    /// </summary>
    public (string Content, int CursorPosition)? Redo()
    {
        if (_redo.Count == 0) return null;

        var state = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        _undo.Add(state);

        return (state.Content, state.CursorPosition);
    }

    /// <summary>
    /// Manually saves a named snapshot (e.g. on open, on explicit save).
    /// </summary>
    public void Snapshot(string content) =>
        AddRevision(new(content, 0, DateTime.Now));

    public List<(DateTime Timestamp, string Content)> GetRevisions() =>
        _revisions.Select(r => (r.Timestamp, r.Content)).ToList();

    public string? GetRevision(int index) =>
        index >= 0 && index < _revisions.Count
            ? _revisions[index].Content
            : null;

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
        _revisions.Clear();
        _lastRevisionTime = DateTime.Now;
    }

    private void AddRevision(HistoryState state)
    {
        _revisions.Add(state);
        if (_revisions.Count > _maxRevisions)
            _revisions.RemoveAt(0);
    }
}
