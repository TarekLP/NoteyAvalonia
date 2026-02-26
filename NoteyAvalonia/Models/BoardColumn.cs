using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NoteToolAvalonia.Models;

public class BoardColumn
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "New Column";
    public string Color { get; set; } = "#3498db";
    public int Order { get; set; }
	public ObservableCollection<NoteCard> Cards { get; set; } = new();
}
