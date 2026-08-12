using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.Services;

namespace NoteyAvalonia.Tests;

public static class Program
{
    private static int _passed;
    private static int _failed;

    public static int Main()
    {
        Console.WriteLine("=== Notey service-layer verification ===");

        string tempRoot = Path.Combine(Path.GetTempPath(), "notey-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            Run("constructor creates data + notes folders", () =>
            {
                var svc = new NoteyService(FolderFor(tempRoot, "ctor"));
                Expect(Directory.Exists(tempRoot), "data folder created");
                Expect(Directory.Exists(svc.NotesFolder), "notes folder created");
            });

            Run("frontmatter write + body strip round-trip", () =>
            {
                var svc = new NoteyService(FolderFor(tempRoot, "frontmatter"));
                var card = new NoteCard
                {
                    Title = "My Note",
                    Category = "Work",
                    Tags = new List<string> { "design", "ux" },
                    References = new List<string> { "other-id" },
                    Priority = NotePriority.High,
                    Deadline = new DateTime(2026, 7, 1),
                    IsCompleted = true,
                    IsPinned = true
                };
                svc.SaveNote(card, "# Heading\n\nSome *body* text.");

                var done = Directory.GetFiles(svc.NotesFolder, "*.md").Length == 1;
                Expect(done, "one .md file written");

                var body = svc.LoadNoteContent(card.Id);
                Expect(body.Contains("# Heading"), "body excludes frontmatter");
                Expect(!body.StartsWith("---"), "body does not start with frontmatter marker");
            });

            Run("frontmatter metadata round-trips incl. pinned/completed", () =>
            {
                var svc = new NoteyService(FolderFor(tempRoot, "meta"));
                var card = new NoteCard
                {
                    Title = "Pinned one",
                    Category = "Ideas",
                    Tags = new List<string> { "a", "b" },
                    References = new List<string> { "ref-1" },
                    Priority = NotePriority.Critical,
                    Deadline = new DateTime(2026, 8, 15),
                    CreatedAt = new DateTime(2026, 1, 2, 3, 4, 5),
                    IsCompleted = false,
                    IsPinned = true
                };
                svc.SaveNote(card, "content");
                var reloaded = svc.LoadNoteMetadata(card.Id);
                Expect(reloaded != null, "metadata loadable");
                if (reloaded != null)
                {
                    Expect(reloaded.Title == "Pinned one", "title round-trips");
                    Expect(reloaded.Category == "Ideas", "category round-trips");
                    Expect(reloaded.Tags.SequenceEqual(new[] { "a", "b" }), "tags round-trip");
                    Expect(reloaded.References.SequenceEqual(new[] { "ref-1" }), "references round-trip");
                    Expect(reloaded.Priority == NotePriority.Critical, "priority round-trips");
                    Expect(reloaded.Deadline?.Date == new DateTime(2026, 8, 15), "deadline round-trips");
                    Expect(reloaded.CreatedAt == card.CreatedAt, "created-at round-trips");
                    Expect(reloaded.IsCompleted == false, "completed round-trips");
                    Expect(reloaded.IsPinned == true, "pinned round-trips (was previously lost)");
                }
            });

            Run("index keeps IsCompleted/IsPinned in sync", () =>
            {
                var svc = new NoteyService(FolderFor(tempRoot, "index"));
                var card = new NoteCard { Title = "Indexed", IsCompleted = true, IsPinned = true };
                svc.SaveNote(card, "x");
                var index = svc.LoadNotesIndex();
                var found = index.FirstOrDefault(n => n.Id == card.Id);
                Expect(found?.IsCompleted == true, "index stores completed");
                Expect(found?.IsPinned == true, "index stores pinned");
            });

            Run("HTML export converts markdown into an HTML document", () =>
            {
                var svc = new NoteyService(FolderFor(tempRoot, "export"));
                var card = new NoteCard { Title = "Export me" };
                svc.SaveNote(card, "# Big Title\n\nHello **world**.");
                var outFile = Path.Combine(FolderFor(tempRoot, "export"), "export.html");
                svc.ExportNote(card.Id, outFile);
                Expect(File.Exists(outFile), "html file written");
                var html = File.ReadAllText(outFile);
                Expect(html.Contains("<!DOCTYPE html>"), "full html document");
                Expect(html.Contains("<h1>"), "markdown heading converted");
                Expect(html.Contains("<strong>"), "markdown bold converted");
                Expect(html.Contains("Big Title"), "title embedded in document");
            });

            Run("settings save/load round-trip incl. ShowPreview + fonts", () =>
            {
                var svc = new NoteyService(FolderFor(tempRoot, "settings"));
                var saved = new AppSettings
                {
                    Theme = "Light",
                    FontFamily = "Segoe UI",
                    FontSize = 18,
                    AutoSave = false,
                    AutoSaveInterval = 7,
                    ConfirmBeforeDelete = false,
                    ShowCompletedNotes = false,
                    ShowPreview = false
                };
                svc.SaveSettings(saved);
                var loaded = svc.LoadSettings();
                Expect(loaded.Theme == "Light", "theme round-trips");
                Expect(loaded.FontFamily == "Segoe UI", "font family round-trips");
                Expect(loaded.FontSize == 18, "font size round-trips");
                Expect(!loaded.AutoSave, "autosave round-trips");
                Expect(loaded.AutoSaveInterval == 7, "interval round-trips");
                Expect(!loaded.ConfirmBeforeDelete, "confirm round-trips");
                Expect(!loaded.ShowCompletedNotes, "show-completed round-trips");
                Expect(!loaded.ShowPreview, "show-preview round-trips");
            });

            Run("MoveNotesFolder re-points writes to new location", () =>
            {
                var svc = new NoteyService(FolderFor(tempRoot, "move"));
                var oldFolder = svc.NotesFolder;
                var newFolder = Path.Combine(FolderFor(tempRoot, "move"), "custom-notes");
                svc.MoveNotesFolder(newFolder);
                Expect(svc.NotesFolder == newFolder, "NotesFolder updated");
                var card = new NoteCard { Title = "Moved" };
                svc.SaveNote(card, "body");
                Expect(File.Exists(Path.Combine(newFolder, card.Id + ".md")), "note lands in new folder");
                Expect(!File.Exists(Path.Combine(oldFolder, card.Id + ".md")), "old folder untouched");
            });

            Run("DeleteNote removes file and index entry", () =>
            {
                var svc = new NoteyService(FolderFor(tempRoot, "delete"));
                var card = new NoteCard { Title = "Doomed" };
                svc.SaveNote(card, "body");
                svc.DeleteNote(card.Id);
                Expect(!File.Exists(Path.Combine(svc.NotesFolder, card.Id + ".md")), "file removed");
                Expect(svc.LoadNotesIndex().All(n => n.Id != card.Id), "index entry removed");
            });

            Run("EditHistory: 50 max undo, redo cleared on new push", () =>
            {
                var history = new EditHistory(maxUndo: 50, maxRevisions: 100, revisionIntervalMinutes: 5);
                for (int i = 0; i < 60; i++) history.Push($"state-{i}");
                Expect(history.UndoCount <= 50, $"undo capped at 50 (got {history.UndoCount})");

                history = new EditHistory(50, 100, 5);
                history.Push("a"); history.Push("b");
                var undone = history.Undo();
                Expect(undone?.Content == "a", "undo returns previous state");
                var redone = history.Redo();
                Expect(redone?.Content == "b", "redo re-applies state");
                history.Push("c");
                Expect(history.RedoCount == 0, "new edit clears redo stack");
            });

            Run("EditHistory: periodic revision snapshots capped at max", () =>
            {
                var history = new EditHistory(maxUndo: 50, maxRevisions: 3, revisionIntervalMinutes: 0);
                for (int i = 0; i < 10; i++) history.Push($"revision-{i}");
                Expect(history.RevisionCount <= 3, $"revisions capped at 3 (got {history.RevisionCount})");
                Expect(history.RevisionCount == 3, "revisions actually captured");
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  FAIL — unhandled exception: {ex}");
            _failed++;
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { /* ignore */ }
        }

        Console.WriteLine();
        Console.WriteLine($"Result: {_passed} passed, {_failed} failed");
        return _failed == 0 ? 0 : 1;
    }

    private static string FolderFor(string root, string name)
    {
        var dir = Path.Combine(root, name);
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
            Console.WriteLine($"  PASS — {name}");
            _passed++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  FAIL — {name}: {ex.Message}");
            _failed++;
        }
    }

    private static void Expect(bool condition, string what)
    {
        if (!condition) throw new Exception($"assertion failed: {what}");
    }
}
