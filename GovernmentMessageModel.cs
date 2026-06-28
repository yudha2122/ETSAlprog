using SQLite;
using System;

namespace DaerahRawanBanjir;

public class GovernmentMessageModel
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int SenderUserId { get; set; }

    public string SenderUsername { get; set; } = string.Empty;

    public string MessageText { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.Now;
}