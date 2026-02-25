namespace NoteToolAvalonia.Models;

public class AppSettings
{
    public string Theme { get; set; } = "Dark";
    public string AccentColor { get; set; } = "#3498db";
    public double FontSize { get; set; } = 14;
    public string FontFamily { get; set; } = "Inter";
    public bool AutoSave { get; set; } = true;
    public int AutoSaveIntervalSeconds { get; set; } = 30;
    public string DataFolderPath { get; set; } = string.Empty;
    public bool ConfirmBeforeDelete { get; set; } = true;
    public bool ShowCompletedNotes { get; set; } = true;
}
