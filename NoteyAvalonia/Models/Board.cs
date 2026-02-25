using System;
using System.Collections.Generic;

namespace NoteToolAvalonia.Models;

public class Board
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Untitled Board";
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastModified { get; set; } = DateTime.Now;
    public List<BoardColumn> Columns { get; set; } = new();
}
