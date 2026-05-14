using NoteToolAvalonia.Models;
using NoteToolAvalonia.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace NoteToolAvalonia.Services;

public class DataService
{
	private readonly string _dataFolder;
	private readonly string _boardsFile;
	private readonly string _settingsFile;
	private readonly NoteFileService _noteFileService;
	private readonly JsonSerializerOptions _jsonOptions;
	private readonly object _fileLock = new object();
	private const int MaxRetries = 3;
	private const int RetryDelayMs = 100;

	public DataService()
	{
		_dataFolder = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			"NoteToolAvalonia");
		Directory.CreateDirectory(_dataFolder);
		_boardsFile = Path.Combine(_dataFolder, "boards.json");
		_settingsFile = Path.Combine(_dataFolder, "settings.json");
		_noteFileService = new NoteFileService(_dataFolder);
		_jsonOptions = new JsonSerializerOptions
		{
			WriteIndented = true,
			PropertyNameCaseInsensitive = true,
			DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
		};
	}

	public List<Board> LoadBoards()
	{
		lock (_fileLock)
		{
			if (!File.Exists(_boardsFile)) return new List<Board>();
			try
			{
				var json = File.ReadAllText(_boardsFile);
				return JsonSerializer.Deserialize<List<Board>>(json, _jsonOptions) ?? new List<Board>();
			}
			catch { return new List<Board>(); }
		}
	}

	public void SaveBoards(List<Board> boards)
	{
		lock (_fileLock)
		{
			var json = JsonSerializer.Serialize(boards, _jsonOptions);
			WriteFileWithRetry(_boardsFile, json);
		}
	}

	public void SaveBoard(Board board)
	{
		lock (_fileLock)
		{
			var boards = LoadBoards();
			var idx = boards.FindIndex(b => b.Id == board.Id);
			if (idx >= 0) boards[idx] = board;
			else boards.Add(board);
			SaveBoards(boards);
		}
	}

	public void DeleteBoard(string boardId)
	{
		lock (_fileLock)
		{
			var boards = LoadBoards();
			boards.RemoveAll(b => b.Id == boardId);
			SaveBoards(boards);
		}
	}

	public AppSettings LoadSettings()
	{
		lock (_fileLock)
		{
			if (!File.Exists(_settingsFile)) return new AppSettings();
			try
			{
				var json = File.ReadAllText(_settingsFile);
				return JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
			}
			catch { return new AppSettings(); }
		}
	}

	public void SaveSettings(AppSettings settings)
	{
		lock (_fileLock)
		{
			var json = JsonSerializer.Serialize(settings, _jsonOptions);
			WriteFileWithRetry(_settingsFile, json);
		}
	}

	/// <summary>
	/// Write file with retry logic to handle file locking from other processes.
	/// Attempts up to MaxRetries times with RetryDelayMs between attempts.
	/// </summary>
	private void WriteFileWithRetry(string filePath, string content)
	{
		for (int attempt = 0; attempt < MaxRetries; attempt++)
		{
			try
			{
				File.WriteAllText(filePath, content);
				return; // Success
			}
			catch (IOException) when (attempt < MaxRetries - 1)
			{
				// File is locked, wait and retry
				Thread.Sleep(RetryDelayMs);
			}
			catch
			{
				throw; // Not a locking error, throw immediately
			}
		}

		// If we got here, all retries failed
		throw new IOException($"Could not write to {filePath} after {MaxRetries} attempts");
	}

	public string DataFolder => _dataFolder;
	public NoteFileService NoteFiles => _noteFileService;


	public void DeleteNoteContent(string noteId) =>
	_noteFileService.DeleteNote(noteId);
}
