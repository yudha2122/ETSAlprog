using SQLite;

namespace DaerahRawanBanjir;

public class AccessHistoryModel
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Role { get; set; } = UserRoles.Masyarakat;

    public DateTime AccessedAt { get; set; } = DateTime.Now;

    public string Activity { get; set; } = string.Empty;
}