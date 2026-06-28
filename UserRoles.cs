namespace DaerahRawanBanjir;

public static class UserRoles
{
    public const string Pemerintah = "User Pemerintah";
    public const string Masyarakat = "User Masyarakat";

    public static bool IsValid(string role)
    {
        return role == Pemerintah || role == Masyarakat;
    }
}