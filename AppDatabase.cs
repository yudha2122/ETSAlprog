using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DaerahRawanBanjir;

public static class AppDatabase
{
    private static SQLiteAsyncConnection? _database;

    private static async Task InitAsync()
    {
        if (_database != null)
            return;

        SQLitePCL.Batteries_V2.Init();

        string dbPath = Path.Combine(
            Microsoft.Maui.Storage.FileSystem.AppDataDirectory,
            "database_banjir.db3"
        );

        _database = new SQLiteAsyncConnection(
            dbPath,
            SQLiteOpenFlags.ReadWrite |
            SQLiteOpenFlags.Create |
            SQLiteOpenFlags.SharedCache
        );

        await _database.CreateTableAsync<UserModel>();
        await _database.CreateTableAsync<AccessHistoryModel>();
        await _database.CreateTableAsync<GovernmentMessageModel>();

        await EnsureUserRoleColumnAsync();
    }

    private static async Task EnsureUserRoleColumnAsync()
    {
        try
        {
            List<SQLiteConnection.ColumnInfo> columns =
                await _database!.GetTableInfoAsync(nameof(UserModel));

            bool roleColumnExists =
                columns.Any(c => c.Name == nameof(UserModel.Role));

            if (!roleColumnExists)
            {
                await _database.ExecuteAsync(
                    $"ALTER TABLE {nameof(UserModel)} ADD COLUMN {nameof(UserModel.Role)} TEXT DEFAULT '{UserRoles.Masyarakat}'"
                );
            }
        }
        catch
        {
            // Untuk menjaga aplikasi tetap aman jika database masih kosong atau sudah punya kolom Role.
        }
    }

    public static async Task<bool> UsernameSudahAdaAsync(string username)
    {
        await InitAsync();

        UserModel? user = await _database!
            .Table<UserModel>()
            .FirstOrDefaultAsync(u => u.Username == username);

        return user != null;
    }

    public static async Task SimpanUserAsync(UserModel user)
    {
        await InitAsync();

        if (string.IsNullOrWhiteSpace(user.Role) ||
            !UserRoles.IsValid(user.Role))
        {
            user.Role = UserRoles.Masyarakat;
        }

        await _database!.InsertAsync(user);
    }

    public static async Task<UserModel?> LoginAsync(string username, string password)
    {
        await InitAsync();

        UserModel? user = await _database!
            .Table<UserModel>()
            .FirstOrDefaultAsync(u =>
                u.Username == username &&
                u.Password == password
            );

        if (user != null && string.IsNullOrWhiteSpace(user.Role))
        {
            user.Role = UserRoles.Masyarakat;
        }

        return user;
    }

    public static async Task SimpanRiwayatAksesMasyarakatAsync()
    {
        await InitAsync();

        if (!UserSession.IsMasyarakat)
        {
            return;
        }

        AccessHistoryModel history = new AccessHistoryModel
        {
            UserId = UserSession.CurrentUserId,
            Username = UserSession.RegisteredUsername,
            Role = UserSession.Role,
            AccessedAt = DateTime.Now,
            Activity = "Berhasil mengakses sistem setelah verifikasi wajah"
        };

        await _database!.InsertAsync(history);
    }

    public static async Task<List<AccessHistoryModel>> AmbilRiwayatAksesMasyarakatAsync()
    {
        await InitAsync();

        return await _database!
            .Table<AccessHistoryModel>()
            .Where(h => h.Role == UserRoles.Masyarakat)
            .OrderByDescending(h => h.AccessedAt)
            .ToListAsync();
    }

    public static async Task SimpanPesanPemerintahAsync(string messageText)
    {
        await InitAsync();

        if (!UserSession.IsPemerintah)
        {
            return;
        }

        GovernmentMessageModel message = new GovernmentMessageModel
        {
            SenderUserId = UserSession.CurrentUserId,
            SenderUsername = UserSession.RegisteredUsername,
            MessageText = messageText,
            SentAt = DateTime.Now
        };

        await _database!.InsertAsync(message);
    }

    public static async Task<List<GovernmentMessageModel>> AmbilPesanPemerintahAsync()
    {
        await InitAsync();

        return await _database!
            .Table<GovernmentMessageModel>()
            .OrderByDescending(m => m.SentAt)
            .Take(20)
            .ToListAsync();
    }
}