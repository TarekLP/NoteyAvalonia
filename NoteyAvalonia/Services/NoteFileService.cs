// Services/NoteFileService.cs
using System;
using System.IO;
using System.Threading;

namespace NoteToolAvalonia.Services;

/// <summary>
/// Handles reading and writing individual Markdown (.md) files for note content.
/// Each note is stored as its own file named by the note's unique ID.
/// </summary>
public class NoteFileService
{
	private readonly string _notesFolder;
	private readonly object _fileLock = new();
	private const int MaxRetries = 3;
	private const int RetryDelayMs = 100;

	public NoteFileService(string baseDataFolder)
	{
		_notesFolder = Path.Combine(baseDataFolder, "notes");
		Directory.CreateDirectory(_notesFolder);
	}

	public string LoadNote(string noteId)
	{
		var path = GetNotePath(noteId);
		lock (_fileLock)
		{
			if (!File.Exists(path)) return string.Empty;
			try { return File.ReadAllText(path); }
			catch { return string.Empty; }
		}
	}

	public void SaveNote(string noteId, string content)
	{
		lock (_fileLock)
		{
			WriteWithRetry(GetNotePath(noteId), content ?? string.Empty);
		}
	}

	public void DeleteNote(string noteId)
	{
		var path = GetNotePath(noteId);
		lock (_fileLock)
		{
			if (File.Exists(path)) File.Delete(path);
		}
	}

	private string GetNotePath(string noteId) =>
		Path.Combine(_notesFolder, $"{noteId}.md");

	private void WriteWithRetry(string path, string content)
	{
		for (int attempt = 0; attempt < MaxRetries; attempt++)
		{
			try { File.WriteAllText(path, content); return; }
			catch (IOException) when (attempt < MaxRetries - 1) { Thread.Sleep(RetryDelayMs); }
			catch { throw; }
		}
		throw new IOException($"Could not write to {path} after {MaxRetries} attempts");
	}

	public string NotesFolder => _notesFolder;
}