using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using NoteToolAvalonia.Models;

namespace NoteToolAvalonia.Services;

public class DataService
{
    private readonly string _dataFolder;
    private readonly string _boardsFile;
    private readonly string _settingsFile;
    private readonly JsonSerializerOptions _jsonOptions;

    public DataService()
    {
        _dataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NoteToolAvalonia");
        Directory.CreateDirectory(_dataFolder);
        _boardsFile = Path.Combine(_dataFolder, "boards.json");
        _settingsFile = Path.Combine(_dataFolder, "settings.json");
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    public List<Board> LoadBoards()
    {
        if (!File.Exists(_boardsFile)) return new List<Board>();
        try
        {
            var json = File.ReadAllText(_boardsFile);
            return JsonSerializer.Deserialize<List<Board>>(json, _jsonOptions) ?? new List<Board>();
        }
        catch { return new List<Board>(); }
    }

    public void SaveBoards(List<Board> boards)
    {
        var json = JsonSerializer.Serialize(boards, _jsonOptions);
        File.WriteAllText(_boardsFile, json);
    }

    public void SaveBoard(Board board)
    {
        var boards = LoadBoards();
        var idx = boards.FindIndex(b => b.Id == board.Id);
        if (idx >= 0) boards[idx] = board;
        else boards.Add(board);
        SaveBoards(boards);
    }

    public void DeleteBoard(string boardId)
    {
        var boards = LoadBoards();
        boards.RemoveAll(b => b.Id == boardId);
        SaveBoards(boards);
    }

    public AppSettings LoadSettings()
    {
        if (!File.Exists(_settingsFile)) return new AppSettings();
        try
        {
            var json = File.ReadAllText(_settingsFile);
            return JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
        }
        catch { return new AppSettings(); }
    }

    public void SaveSettings(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, _jsonOptions);
        File.WriteAllText(_settingsFile, json);
    }

    public string DataFolder => _dataFolder;
}
