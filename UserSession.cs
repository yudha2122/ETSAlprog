using SkiaSharp;

namespace DaerahRawanBanjir;

public static class UserSession
{
    public static int CurrentUserId { get; set; }

    public static string RegisteredUsername { get; set; } = string.Empty;
    public static string RegisteredPassword { get; set; } = string.Empty;
    public static string Role { get; set; } = UserRoles.Masyarakat;

    public static bool IsPemerintah => Role == UserRoles.Pemerintah;
    public static bool IsMasyarakat => Role == UserRoles.Masyarakat;

    public static SKBitmap? RegisteredFace { get; set; }
    public static string RegisteredFaceSource { get; set; } = string.Empty;

    public static void LoadUser(UserModel user)
    {
        CurrentUserId = user.Id;
        RegisteredUsername = user.Username;
        RegisteredPassword = user.Password;
        Role = string.IsNullOrWhiteSpace(user.Role)
            ? UserRoles.Masyarakat
            : user.Role;

        RegisteredFaceSource = user.FaceSource;

        RegisteredFace?.Dispose();
        RegisteredFace = SKBitmap.Decode(user.FaceTemplate);
    }

    public static void ClearSession()
    {
        CurrentUserId = 0;
        RegisteredUsername = string.Empty;
        RegisteredPassword = string.Empty;
        Role = UserRoles.Masyarakat;
        RegisteredFaceSource = string.Empty;

        RegisteredFace?.Dispose();
        RegisteredFace = null;
    }
}