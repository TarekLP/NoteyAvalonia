namespace NoteToolAvalonia.Models;

public class AppSettings
{
	public string Theme { get; set; } = "Dark";
	public string FontFamily { get; set; } = "Inter";
	public int FontSize { get; set; } = 14;
	public bool AutoSave { get; set; } = true;
	public int AutoSaveInterval { get; set; } = 2;
	public bool ConfirmBeforeDelete { get; set; } = true;
	public bool ShowCompletedNotes { get; set; } = true;
}
