using Microsoft.Data.SqlClient;

namespace GVC.Web.Services;

public interface IBackupService
{
    string DirectoryPath { get; }
    long MaxUploadSizeBytes { get; }
    Task<BackupFileInfo> CreateAsync(CancellationToken cancellationToken = default);
    IReadOnlyList<BackupFileInfo> List();
    string GetFilePath(string fileName);
    Task<BackupFileInfo> SaveUploadAsync(IFormFile file, CancellationToken cancellationToken = default);
    void Delete(string fileName);
    Task RestoreAsync(string fileName, CancellationToken cancellationToken = default);
}

public sealed record BackupFileInfo(string FileName, long SizeBytes, DateTime CreatedAt)
{
    public decimal SizeMb => Math.Round(SizeBytes / 1024m / 1024m, 2);
}

public sealed class BackupService : IBackupService
{
    private const string RequiredDatabaseName = "erp_gvc";
    private static readonly SemaphoreSlim OperationLock = new(1, 1);
    private readonly string connectionString;
    private readonly string quotedDatabaseName;

    public BackupService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        connectionString = configuration.GetConnectionString("ErpConnection")
            ?? throw new InvalidOperationException("A conexão 'ErpConnection' não foi configurada.");

        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!string.Equals(builder.InitialCatalog, RequiredDatabaseName, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"O módulo de backup só pode operar no banco '{RequiredDatabaseName}'.");

        quotedDatabaseName = $"[{builder.InitialCatalog.Replace("]", "]]", StringComparison.Ordinal)}]";
        string configuredPath = configuration["BackupSettings:DirectoryPath"] ?? "App_Data\\Backups";
        DirectoryPath = Path.GetFullPath(Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath));
        MaxUploadSizeBytes = configuration.GetValue<long?>("BackupSettings:MaxUploadSizeBytes")
            ?? 5L * 1024 * 1024 * 1024;
        Directory.CreateDirectory(DirectoryPath);
    }

    public string DirectoryPath { get; }
    public long MaxUploadSizeBytes { get; }

    public async Task<BackupFileInfo> CreateAsync(CancellationToken cancellationToken = default)
    {
        await OperationLock.WaitAsync(cancellationToken);
        try
        {
            string fileName = $"erp_gvc_backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
            string path = GetFilePath(fileName);
            if (File.Exists(path))
                throw new IOException("Já existe um backup criado neste segundo. Aguarde e tente novamente.");

            await using var connection = CreateMasterConnection();
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandTimeout = 0;
            command.CommandText = $"""
                BACKUP DATABASE {quotedDatabaseName}
                TO DISK = @BackupPath
                WITH FORMAT, INIT, NAME = N'ERP GVC Full Database Backup',
                SKIP, NOREWIND, NOUNLOAD, CHECKSUM, STATS = 10;
                """;
            command.Parameters.AddWithValue("@BackupPath", path);

            try
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (SqlException exception)
            {
                throw new InvalidOperationException(
                    $"O SQL Server não conseguiu gravar o backup em '{DirectoryPath}'. " +
                    "Conceda permissão de leitura/gravação nessa pasta à conta do serviço SQL Server Express.",
                    exception);
            }

            return ToInfo(new FileInfo(path));
        }
        finally
        {
            OperationLock.Release();
        }
    }

    public IReadOnlyList<BackupFileInfo> List()
    {
        Directory.CreateDirectory(DirectoryPath);
        return new DirectoryInfo(DirectoryPath)
            .EnumerateFiles("*.bak", SearchOption.TopDirectoryOnly)
            .OrderByDescending(x => x.CreationTime)
            .Select(ToInfo)
            .ToList();
    }

    public string GetFilePath(string fileName)
    {
        string safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName) ||
            !safeName.Equals(fileName, StringComparison.Ordinal) ||
            !safeName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Nome de arquivo de backup inválido.", nameof(fileName));
        }

        string path = Path.GetFullPath(Path.Combine(DirectoryPath, safeName));
        string root = DirectoryPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Caminho de backup inválido.", nameof(fileName));
        return path;
    }

    public async Task<BackupFileInfo> SaveUploadAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (file.Length <= 0 || file.Length > MaxUploadSizeBytes)
            throw new InvalidOperationException($"O arquivo deve ter até {MaxUploadSizeBytes / 1024 / 1024} MB.");
        if (!string.Equals(Path.GetExtension(file.FileName), ".bak", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Envie um arquivo com extensão .bak.");

        string originalName = Path.GetFileNameWithoutExtension(file.FileName);
        string safeBaseName = string.Concat(originalName.Select(x => char.IsLetterOrDigit(x) || x is '-' or '_' ? x : '_'));
        safeBaseName = string.IsNullOrWhiteSpace(safeBaseName) ? "backup" : safeBaseName[..Math.Min(80, safeBaseName.Length)];
        string fileName = $"upload_{DateTime.Now:yyyyMMdd_HHmmss}_{safeBaseName}.bak";
        string path = GetFilePath(fileName);

        await using var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await file.CopyToAsync(target, cancellationToken);
        await target.FlushAsync(cancellationToken);
        return ToInfo(new FileInfo(path));
    }

    public void Delete(string fileName)
    {
        string path = GetFilePath(fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException("Backup não encontrado.", fileName);
        File.Delete(path);
    }

    public async Task RestoreAsync(string fileName, CancellationToken cancellationToken = default)
    {
        string path = GetFilePath(fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException("Backup não encontrado.", fileName);

        await OperationLock.WaitAsync(cancellationToken);
        try
        {
            SqlConnection.ClearAllPools();
            await using var connection = CreateMasterConnection();
            await connection.OpenAsync(cancellationToken);

            await ExecuteAsync(connection,
                "RESTORE VERIFYONLY FROM DISK = @BackupPath WITH CHECKSUM;",
                path,
                cancellationToken);

            bool singleUserSet = false;
            try
            {
                await ExecuteAsync(connection,
                    $"ALTER DATABASE {quotedDatabaseName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;",
                    null,
                    cancellationToken);
                singleUserSet = true;

                await ExecuteAsync(connection,
                    $"RESTORE DATABASE {quotedDatabaseName} FROM DISK = @BackupPath WITH REPLACE, RECOVERY, STATS = 10;",
                    path,
                    cancellationToken);

                await ExecuteAsync(connection,
                    $"ALTER DATABASE {quotedDatabaseName} SET MULTI_USER;",
                    null,
                    cancellationToken);
                singleUserSet = false;
            }
            finally
            {
                if (singleUserSet)
                {
                    try
                    {
                        await ExecuteAsync(connection,
                            $"ALTER DATABASE {quotedDatabaseName} SET MULTI_USER WITH ROLLBACK IMMEDIATE;",
                            null,
                            CancellationToken.None);
                    }
                    catch (SqlException)
                    {
                        // A exceção original é preservada; a recuperação manual pode ser necessária.
                    }
                }
            }
        }
        finally
        {
            SqlConnection.ClearAllPools();
            OperationLock.Release();
        }
    }

    private SqlConnection CreateMasterConnection()
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master",
            Pooling = false,
            MultipleActiveResultSets = false
        };
        return new SqlConnection(builder.ConnectionString);
    }

    private static async Task ExecuteAsync(
        SqlConnection connection,
        string sql,
        string? backupPath,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 0;
        command.CommandText = sql;
        if (backupPath is not null)
            command.Parameters.AddWithValue("@BackupPath", backupPath);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static BackupFileInfo ToInfo(FileInfo file)
    {
        file.Refresh();
        return new BackupFileInfo(file.Name, file.Length, file.CreationTime);
    }
}
