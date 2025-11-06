using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using PACOM.WebApp.Model;
using PACOM.WebApp.Models;
using System.Net.Http;
using static PACOM.WebApp.Models.PacomModels;

namespace PACOM.WebApp.Service
{
    public class DatasourcesService
    {
        private static string? _connectionString;

        public static void Initialize(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public static async Task SaveWebhookToDatabase(string raw)
        {
            // Ensure database and table exist
            await DatasourcesHelper.EnsureDatabaseAndTableAsync();

            using var connection = new SqlConnection(_connectionString);

            string query = @"
                INSERT INTO dbo.TblReceiver (data)
                VALUES (@data)";

            await connection.ExecuteAsync(query, new { data = raw });
        }


        public static PacomResponse<EventLogModel> GetLatestEvent(string OrganizationName)
        {
            var result = new PacomResponse<EventLogModel>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string query = @"WITH TransactionLog AS (SELECT CAST(l.Id AS varchar(36)) AS Id, l.Scope, s.Name AS ScopeName, 
                                CAST(EventId AS varchar(36)) AS EventId, 
                                e.FullName AS EventName,
                                CAST(l.UserId AS varchar(36)) AS UserId, 
                                u.FirstName AS UserName, u.FirstName, u.LastName, 
                                CAST(CredentialId AS varchar(36)) AS CredentialId, 
                                c.CardNumber AS CredentialNumber, 
                                [Value], CAST(AreaFromId AS varchar(36)) AS AreaFromId, 
                                CAST(AreaToId AS varchar(36)) AS AreaToId, 
                                CustomDataString, u.CustomData AS CustomDataUDF,
                                CAST([time] AT TIME ZONE 'UTC' AT TIME ZONE 'Singapore Standard Time' AS DATETIME2) AS MalaysiaTime 
                                FROM [ArcoDbStatusView].[dbo].[ActivityLog] l
                                LEFT JOIN [ArcoDbView].[dbo].[Sites] s ON l.Scope = s.Scope AND s.IsDeleted = 0
                                LEFT JOIN [ArcoDbView].[dbo].[SchemaEvents] e ON l.EventId = e.Id
                                LEFT JOIN [ArcoDbView].[dbo].[User] u ON l.UserId = u.Id
                                LEFT JOIN [ArcoDbView].[dbo].[Credentials] c ON l.CredentialId = c.Id)
                                SELECT TOP(1) Id, Scope, ScopeName, EventId, EventName, UserId, UserName, FirstName, LastName, CredentialId, CredentialNumber, Value, AreaFromId, AreaToId, CustomDataString, CustomDataUDF, MalaysiaTime
                                FROM TransactionLog
                                WHERE AreaFromId IS NOT NULL AND ScopeName = @Organization
                                ORDER BY MalaysiaTime DESC";

                var data = conn.Query<EventLogModel>(query, new { Organization = OrganizationName }).FirstOrDefault();

                result.Error = 0;
                result.Message = "Event logs retrieved successfully.";
                result.Data = data;

                conn.Close();

            }

            return result;

        }

        public static PacomResponse<List<EventLogModel>> GetEvent(string OrganizationName, DateTime StartDate, DateTime EndDate)
        {
            var result = new PacomResponse<List<EventLogModel>>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string query = @"WITH TransactionLog AS (SELECT CAST(l.Id AS varchar(36)) AS Id, l.Scope, s.Name AS ScopeName, 
                                CAST(EventId AS varchar(36)) AS EventId, 
                                e.FullName AS EventName,
                                CAST(l.UserId AS varchar(36)) AS UserId, 
                                u.FirstName AS UserName, u.FirstName, u.LastName, 
                                CAST(CredentialId AS varchar(36)) AS CredentialId, 
                                c.CardNumber AS CredentialNumber, 
                                [Value], CAST(AreaFromId AS varchar(36)) AS AreaFromId, 
                                CAST(AreaToId AS varchar(36)) AS AreaToId, 
                                CustomDataString, u.CustomData AS CustomDataUDF,
                                CAST([time] AT TIME ZONE 'UTC' AT TIME ZONE 'Singapore Standard Time' AS DATETIME2) AS MalaysiaTime 
                                FROM [ArcoDbStatusView].[dbo].[ActivityLog] l
                                LEFT JOIN [ArcoDbView].[dbo].[Sites] s ON l.Scope = s.Scope AND s.IsDeleted = 0
                                LEFT JOIN [ArcoDbView].[dbo].[SchemaEvents] e ON l.EventId = e.Id
                                LEFT JOIN [ArcoDbView].[dbo].[User] u ON l.UserId = u.Id
                                LEFT JOIN [ArcoDbView].[dbo].[Credentials] c ON l.CredentialId = c.Id 
                                WHERE CAST([time] AT TIME ZONE 'UTC' AT TIME ZONE 'Singapore Standard Time' AS DATETIME2) between @StartDateTime AND @EndDateTime)
                                SELECT Id, Scope, ScopeName, EventId, EventName, UserId, UserName, FirstName, LastName, CredentialId, CredentialNumber, Value, AreaFromId, AreaToId, CustomDataString, CustomDataUDF, MalaysiaTime
                                FROM TransactionLog
                                WHERE AreaFromId IS NOT NULL AND ScopeName = @Organization
                                ORDER BY MalaysiaTime DESC";

                var data = conn.Query<EventLogModel>(query, new { Organization = OrganizationName, StartDateTime = StartDate, EndDateTime = EndDate }).AsList();

                result.Error = 0;
                result.Message = "Event logs retrieved successfully.";
                result.Data = data;

                conn.Close();

            }

            return result;

        }

        public static string GetColumnNameById(string ColumnId, string ScopeOrganization)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string query = @"SELECT TOP 1 Name 
                                FROM [ArcoDbView].[dbo].[ObjectMetadata] 
                                WHERE CAST(Id AS varchar(36)) = @ReaderId";

                var readerName = conn.QueryFirstOrDefault<string>(query, new { ReaderId = ColumnId });

                conn.Close();

                return readerName ?? string.Empty;
            }
        }

        public static PacomResponse<List<ObjectMetaData>> GetObjectMetaData()
        {
            var result = new PacomResponse<List<ObjectMetaData>>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string query = "SELECT CAST(Id AS varchar(36)) AS Id, Scope, Name, CAST(ObjectTypeId AS varchar(36)) AS ObjectTypeId, CAST(AliasTypeId AS varchar(36)) AS AliasTypeId " +
               "FROM [ArcoDbView].[dbo].[ObjectMetadata]";

                var data = conn.Query<ObjectMetaData>(query).AsList();

                result.Error = 0;
                result.Message = "Event logs retrieved successfully.";
                result.Data = data;

                conn.Close();
            }

            return result;
        }

        public static PacomResponse<List<UsersModel>> ListAllUsers(string ScopeOrganization)
        {
            var result = new PacomResponse<List<UsersModel>>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string query = @"SELECT CAST(u.Id AS varchar(36)) AS Id, 
                               u.Scope, o.Name AS ScopeName, u.Username, u.UserType, u.Email, u.FirstName, u.LastName, CAST(u.ImageId AS varchar(36)) AS ImageId, u.isDeleted, u.CustomData
                               FROM [ArcoDbView].[dbo].[User] u 
                               LEFT JOIN [ArcoDbView].[dbo].[Organisations] o ON u.Scope = o.Scope WHERE o.Name = @Organization";

                var data = conn.Query<UsersModel>(query, new { Organization = ScopeOrganization }).AsList();

                result.Error = 0;
                result.Message = "Users retrieved successfully.";
                result.Data = data;

                conn.Close();
            }

            return result;
        }

    }
}
