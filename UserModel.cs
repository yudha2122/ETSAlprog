using SQLite;
using System;

namespace DaerahRawanBanjir;

public class UserModel
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Unique = true)]
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = UserRoles.Masyarakat;

    public byte[] FaceTemplate { get; set; } = Array.Empty<byte>();

    public string FaceSource { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}