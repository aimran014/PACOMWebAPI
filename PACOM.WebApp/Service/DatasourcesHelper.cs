using Microsoft.Data.SqlClient;

namespace PACOM.WebApp.Service
{
    public class DatasourcesHelper
    {

        private static string? _masterConnectionString;

        public static void Initialize(IConfiguration config)
        {
            _masterConnectionString = config.GetConnectionString("DefaultConnection");
        }

        public static async Task EnsureDatabaseAndTableAsync()
        {
            await EnsureDatabaseExistsAsync();
            await EnsureTableExistsAsync();
        }

        private static async Task EnsureDatabaseExistsAsync()
        {
            using var connection = new SqlConnection(_masterConnectionString);
            await connection.OpenAsync();

            string checkDbQuery = "IF DB_ID('Webhook_db') IS NULL CREATE DATABASE Webhook_db;";
            using var command = new SqlCommand(checkDbQuery, connection);
            await command.ExecuteNonQueryAsync();

            Console.WriteLine("✅ Checked/created database: Webhook_db");
        }

        private static async Task EnsureTableExistsAsync()
        {
            using var connection = new SqlConnection(_masterConnectionString);
            await connection.OpenAsync();

            string checkTableQuery = @"IF OBJECT_ID('Webhook_db.dbo.TblReceiver', 'U') IS NULL
                                        BEGIN
                                            CREATE TABLE Webhook_db.dbo.TblReceiver (
                                                Id INT IDENTITY(1,1) PRIMARY KEY,
                                                data NVARCHAR(MAX) NULL,
                                                CreatedAt DATETIME DEFAULT(GETDATE())
                                            );
                                        END";

            using var command = new SqlCommand(checkTableQuery, connection);
            await command.ExecuteNonQueryAsync();

            Console.WriteLine("✅ Checked/created table: TblReceiver");
        }
    }
}
