using Azure;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using PACOM.WebhookApp.Data;
using PACOM.WebhookApp.Model;
using System.Data;
using System.Net.Http;
using System.Security.AccessControl;
using static PACOM.WebhookApp.Model.PacomModels;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PACOM.WebhookApp.Service
{
    public class DatasourcesService
    {
        private static string? _pacomConnectionString;
        private static string? _connectionString;
        private readonly ApplicationDbContext _contextFactory;

        //private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public DatasourcesService(ApplicationDbContext contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public static void Initialize(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
            _pacomConnectionString = config.GetConnectionString("PacomConnection");
        }

        public static async Task SaveWebhookInTAMS(AcstrxModel TAMS)
        {
            using var connection = new SqlConnection(_connectionString);

            string query = @"INSERT INTO [TAMS_ALPHA_JPM].[dbo].[ACSTRX] (DateTrx, BadgeNo, ReaderNo, flag, Trx_Type) VALUES(@DateTrx, @BadgeNo, @ReaderNo, @flag, @Trx_Type)";

            await connection.ExecuteAsync(query, new { DateTrx = TAMS.DateTrx, BadgeNo = TAMS.BadgeNo, ReaderNo = TAMS.ReaderNo, flag = TAMS.flag, Trx_Type = TAMS.Trx_Type });
        }



        public static PacomResponse<EventLogModel> GetFirstPacomEvent()
        {
            var result = new PacomResponse<EventLogModel>();

            var TodayUtcTime = DateTime.UtcNow;

            using (var conn = new SqlConnection(_pacomConnectionString))
            {
                conn.Open();

                string query = @"WITH TransactionLog AS (SELECT l.Version, CAST(l.Id AS varchar(36)) AS Id, l.Scope, s.Name AS ScopeName, o.name as OrganizationName,
                                CAST(EventId AS varchar(36)) AS EventId, 
                                e.FullName AS EventName,
                                CAST(l.UserId AS varchar(36)) AS UserId, 
                                u.FirstName AS UserName, u.FirstName, u.LastName, 
                                CAST(CredentialId AS varchar(36)) AS CredentialId, 
                                c.CardNumber AS CredentialNumber, 
                                [Value], CAST(AreaFromId AS varchar(36)) AS AreaFromId, 
                                CAST(AreaToId AS varchar(36)) AS AreaToId, 
                                CustomDataString, u.CustomData AS CustomDataUDF,
                                [time] AS UtcTime 
                                FROM [ArcoDbStatusView].[dbo].[ActivityLog] l
                                LEFT JOIN [ArcoDbView].[dbo].[Organisations] o on SUBSTRING(l.Scope, 1, CHARINDEX('/', l.Scope, 2)) = o.Scope
                                LEFT JOIN [ArcoDbView].[dbo].[Sites] s ON l.Scope = s.Scope AND s.IsDeleted = 0
                                LEFT JOIN [ArcoDbView].[dbo].[SchemaEvents] e ON l.EventId = e.Id
                                LEFT JOIN [ArcoDbView].[dbo].[User] u ON l.UserId = u.Id
                                LEFT JOIN [ArcoDbView].[dbo].[Credentials] c ON l.CredentialId = c.Id)
                                SELECT TOP(1) Version, Id, Scope, ScopeName, OrganizationName, EventId, EventName, UserId, UserName, FirstName, LastName, CredentialId, CredentialNumber, Value, AreaFromId, AreaToId, CustomDataString, CustomDataUDF, UtcTime
                                FROM TransactionLog
                                WHERE AreaFromId IS NOT NULL AND CONVERT(date, UtcTime) = @GetTodayUtcTime
                                ORDER BY Version ASC";

                //string query = @"WITH TransactionLog AS (SELECT l.Version, CAST(l.Id AS varchar(36)) AS Id, l.Scope, s.Name AS ScopeName, o.name as OrganizationName,
                //                CAST(EventId AS varchar(36)) AS EventId, 
                //                e.FullName AS EventName,
                //                CAST(l.UserId AS varchar(36)) AS UserId, 
                //                u.FirstName AS UserName, u.FirstName, u.LastName, 
                //                CAST(CredentialId AS varchar(36)) AS CredentialId, 
                //                c.CardNumber AS CredentialNumber, 
                //                [Value], CAST(AreaFromId AS varchar(36)) AS AreaFromId, 
                //                CAST(AreaToId AS varchar(36)) AS AreaToId, 
                //                CustomDataString, u.CustomData AS CustomDataUDF,
                //                [time] AS UtcTime 
                //                FROM [ArcoDbStatusView].[dbo].[ActivityLog] l
                //                LEFT JOIN [ArcoDbView].[dbo].[Organisations] o on SUBSTRING(l.Scope, 1, CHARINDEX('/', l.Scope, 2)) = o.Scope
                //                LEFT JOIN [ArcoDbView].[dbo].[Sites] s ON l.Scope = s.Scope AND s.IsDeleted = 0
                //                LEFT JOIN [ArcoDbView].[dbo].[SchemaEvents] e ON l.EventId = e.Id
                //                LEFT JOIN [ArcoDbView].[dbo].[User] u ON l.UserId = u.Id
                //                LEFT JOIN [ArcoDbView].[dbo].[Credentials] c ON l.CredentialId = c.Id)
                //                SELECT TOP(1) Version, Id, Scope, ScopeName, OrganizationName, EventId, EventName, UserId, UserName, FirstName, LastName, CredentialId, CredentialNumber, Value, AreaFromId, AreaToId, CustomDataString, CustomDataUDF, UtcTime
                //                FROM TransactionLog
                //                WHERE AreaFromId IS NOT NULL AND Version = '2561039984480210432'
                //                ORDER BY Version ASC";

                var data = conn.Query<EventLogModel>(query, new { GetTodayUtcTime  = TodayUtcTime }).FirstOrDefault();

                result.Error = 0;
                result.Message = "Event logs retrieved successfully.";
                result.Data = data;

                conn.Close();

            }

            return result;

        }

        public static PacomResponse<EventLogModel> GetLatestPacomEvent()
        {
            var result = new PacomResponse<EventLogModel>();

            using (var conn = new SqlConnection(_pacomConnectionString))
            {
                conn.Open();

                string query = @"WITH TransactionLog AS (SELECT l.Version, CAST(l.Id AS varchar(36)) AS Id, l.Scope, s.Name AS ScopeName, o.name as OrganizationName,
                                CAST(EventId AS varchar(36)) AS EventId, 
                                e.FullName AS EventName,
                                CAST(l.UserId AS varchar(36)) AS UserId, 
                                u.FirstName AS UserName, u.FirstName, u.LastName, 
                                CAST(CredentialId AS varchar(36)) AS CredentialId, 
                                c.CardNumber AS CredentialNumber, 
                                [Value], CAST(AreaFromId AS varchar(36)) AS AreaFromId, 
                                CAST(AreaToId AS varchar(36)) AS AreaToId, 
                                CustomDataString, u.CustomData AS CustomDataUDF,
                                [time] AS UtcTime 
                                FROM [ArcoDbStatusView].[dbo].[ActivityLog] l
                                LEFT JOIN [ArcoDbView].[dbo].[Organisations] o on SUBSTRING(l.Scope, 1, CHARINDEX('/', l.Scope, 2)) = o.Scope
                                LEFT JOIN [ArcoDbView].[dbo].[Sites] s ON l.Scope = s.Scope AND s.IsDeleted = 0
                                LEFT JOIN [ArcoDbView].[dbo].[SchemaEvents] e ON l.EventId = e.Id
                                LEFT JOIN [ArcoDbView].[dbo].[User] u ON l.UserId = u.Id
                                LEFT JOIN [ArcoDbView].[dbo].[Credentials] c ON l.CredentialId = c.Id)
                                SELECT TOP(1) Version, Id, Scope, ScopeName, OrganizationName, EventId, EventName, UserId, UserName, FirstName, LastName, CredentialId, CredentialNumber, Value, AreaFromId, AreaToId, CustomDataString, CustomDataUDF, UtcTime
                                FROM TransactionLog
                                WHERE AreaFromId IS NOT NULL
                                ORDER BY Version DESC";

                var data = conn.Query<EventLogModel>(query).FirstOrDefault();

                result.Error = 0;
                result.Message = "Event logs retrieved successfully.";
                result.Data = data;

                conn.Close();

            }

            return result;

        }

        public static PacomResponse<List<EventLogModel>> GetEvent(DateTime UtcStartDate, DateTime UtcEndDate, string? OrganizationName = null)
        {
            var result = new PacomResponse<List<EventLogModel>>();
            try
            {
                using (var conn = new SqlConnection(_pacomConnectionString))
                {
                    conn.Open();

                    string query = @"WITH TransactionLog AS (SELECT l.Version, CAST(l.Id AS varchar(36)) AS Id, l.Scope, s.Name AS ScopeName, o.name as OrganizationName, 
                                                            CAST(EventId AS varchar(36)) AS EventId, 
                                                            e.FullName AS EventName,
                                                            CAST(l.UserId AS varchar(36)) AS UserId, 
                                                            u.FirstName AS UserName, u.FirstName, u.LastName, 
                                                            CAST(CredentialId AS varchar(36)) AS CredentialId, 
                                                            c.CardNumber AS CredentialNumber, 
                                                            [Value], CAST(AreaFromId AS varchar(36)) AS AreaFromId, 
                                                            CAST(AreaToId AS varchar(36)) AS AreaToId, 
                                                            CustomDataString, u.CustomData AS CustomDataUDF,
                                                            [Time] AS UtcTime 
                                                            FROM [ArcoDbStatusView].[dbo].[ActivityLog] l
                                                            LEFT JOIN [ArcoDbView].[dbo].[Organisations] o on SUBSTRING(l.Scope, 1, CHARINDEX('/', l.Scope, 2)) = o.Scope
                                                            LEFT JOIN [ArcoDbView].[dbo].[Sites] s ON l.Scope = s.Scope AND s.IsDeleted = 0
                                                            LEFT JOIN [ArcoDbView].[dbo].[SchemaEvents] e ON l.EventId = e.Id
                                                            LEFT JOIN [ArcoDbView].[dbo].[User] u ON l.UserId = u.Id
                                                            LEFT JOIN [ArcoDbView].[dbo].[Credentials] c ON l.CredentialId = c.Id)
                                    SELECT l.Version,Id, Scope, ScopeName, OrganizationName, EventId, EventName, UserId, UserName, FirstName, LastName, CredentialId, CredentialNumber, Value, AreaFromId, AreaToId, CustomDataString, CustomDataUDF, UtcTime
                                    FROM TransactionLog
                                    WHERE AreaFromId IS NOT NULL 
                                    AND (CONVERT(VARCHAR(19), UtcTime, 120) between @StartDateTime AND @EndDateTime) 
                                    AND (@Organization IS NULL OR OrganizationName = @Organization)
                                    ORDER BY UtcTime DESC";

                    var data = conn.Query<EventLogModel>(query, new { Organization = OrganizationName, StartDateTime = UtcStartDate, EndDateTime = UtcEndDate }).AsList();

                    result.Error = 0;
                    result.Message = "Event logs retrieved successfully.";
                    result.Data = data;

                    conn.Close();

                }

                

            }
            catch (Exception ex)
            {
                result.Error = 1;
                result.Message = $"Error managing organization: {ex.Message}";
                result.Data = null;

            }
            return result;
        }

        public static PacomResponse<List<EventLogModel>> GetEventByVersion(string LastVersionProcess, string LatestVersion)
        {
            var result = new PacomResponse<List<EventLogModel>>();
            try
            {
                using (var conn = new SqlConnection(_pacomConnectionString))
                {
                    conn.Open();


                    string query = @"WITH TransactionLog AS (SELECT l.Version, CAST(l.Id AS varchar(36)) AS Id, l.Scope, s.Name AS ScopeName, o.name as OrganizationName, 
                                                            CAST(EventId AS varchar(36)) AS EventId, 
                                                            e.FullName AS EventName,
                                                            CAST(l.UserId AS varchar(36)) AS UserId, 
                                                            u.FirstName AS UserName, u.FirstName, u.LastName, 
                                                            CAST(CredentialId AS varchar(36)) AS CredentialId, 
                                                            c.CardNumber AS CredentialNumber, 
                                                            [Value], CAST(AreaFromId AS varchar(36)) AS AreaFromId, 
                                                            CAST(AreaToId AS varchar(36)) AS AreaToId, 
                                                            CustomDataString, u.CustomData AS CustomDataUDF,
                                                            [Time] AS UtcTime 
                                                            FROM [ArcoDbStatusView].[dbo].[ActivityLog] l
                                                            LEFT JOIN [ArcoDbView].[dbo].[Organisations] o on SUBSTRING(l.Scope, 1, CHARINDEX('/', l.Scope, 2)) = o.Scope
                                                            LEFT JOIN [ArcoDbView].[dbo].[Sites] s ON l.Scope = s.Scope AND s.IsDeleted = 0
                                                            LEFT JOIN [ArcoDbView].[dbo].[SchemaEvents] e ON l.EventId = e.Id
                                                            LEFT JOIN [ArcoDbView].[dbo].[User] u ON l.UserId = u.Id
                                                            LEFT JOIN [ArcoDbView].[dbo].[Credentials] c ON l.CredentialId = c.Id)
                                    SELECT TOP(100) Version,Id, Scope, ScopeName, OrganizationName, EventId, EventName, UserId, UserName, FirstName, LastName, CredentialId, CredentialNumber, Value, AreaFromId, AreaToId, CustomDataString, CustomDataUDF, UtcTime
                                    FROM TransactionLog
                                    WHERE AreaFromId IS NOT NULL 
                                    AND Version BETWEEN @LastVersionProcess AND @LatestVersion
                                    ORDER BY UtcTime ASC";

                    var data = conn.Query<EventLogModel>(query, new { LastVersionProcess = LastVersionProcess, LatestVersion = LatestVersion }).AsList();

                    result.Error = 0;
                    result.Message = "Event logs retrieved successfully.";
                    result.Data = data;

                    conn.Close();

                }

                

            }
            catch (Exception ex)
            {
                result.Error = 1;
                result.Message = $"Error managing organization: {ex.Message}";
                result.Data = null;

            }
            return result;
        }

        public static string GetColumnNameById(string ColumnId, string ScopeOrganization)
        {
            using (var conn = new SqlConnection(_pacomConnectionString))
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

            using (var conn = new SqlConnection(_pacomConnectionString))
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

            using (var conn = new SqlConnection(_pacomConnectionString))
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

        public static PacomResponse<List<string>> ListPacomOrganization()
        {
            var result = new PacomResponse<List<string>>();

            try
            {
                using (var conn = new SqlConnection(_pacomConnectionString))
                {
                    conn.Open();

                    string query = @"SELECT Name FROM [ArcoDbView].[dbo].[Organisations] WHERE IsDeleted = 0";
                    var data = conn.Query<string>(query).AsList();

                    result.Error = 0;
                    result.Message = "Organizations retrieved successfully.";
                    result.Data = data;
                }
            }
            catch (Exception ex)
            {
                result.Error = 1;
                result.Message = $"Failed to retrieve organizations: {ex.Message}";
                result.Data = new List<string>();
                // Optional: log ex.StackTrace or ex to an exception logger
            }

            return result;
        }



        public async Task<PacomResponse<Organization>> ManageOrganizationAsync(Organization organization)
        {
            var response = new PacomResponse<Organization>();

            try
            {
                var existing = await _contextFactory.Organizations
                    .FirstOrDefaultAsync(o => o.Code == organization.Code);

                if (existing != null)
                {
                    // ✅ Update existing organization
                    existing.Name = organization.Name;
                    existing.Description = organization.Description;
                    existing.IsActive = organization.IsActive;
                    existing.url = organization.url;
                    existing.UpdatedAt = DateTime.UtcNow;

                    _contextFactory.Organizations.Update(existing);
                    await _contextFactory.SaveChangesAsync();

                    response.Message = "Organization updated successfully.";
                    response.Data = existing;
                    response.Error = 0;
                }
                else
                {
                    // ✅ Add new organization
                    organization.url = organization.url;
                    organization.CreatedAt = DateTime.UtcNow;
                    organization.UpdatedAt = DateTime.UtcNow;

                    await _contextFactory.Organizations.AddAsync(organization);
                    await _contextFactory.SaveChangesAsync();

                    response.Message = "Organization added successfully.";
                    response.Data = organization;
                    response.Error = 0;
                }
            }
            catch (Exception ex)
            {
                response.Error = 1;
                response.Message = $"Error managing organization: {ex.Message}";
                response.Data = null;
            }

            return response;
        }

        public async Task<PacomResponse<Organization>> RemoveOrganization(int orgId)
        {
            var response = new PacomResponse<Organization>();

            try
            {
                var organization = await _contextFactory.Organizations
                    .FirstOrDefaultAsync(o => o.Id == orgId);

                if (organization == null)
                {
                    response.Error = 1;
                    response.Message = "Organization not found.";
                    response.Data = null;
                    return response;
                }

                _contextFactory.Organizations.Remove(organization);
                await _contextFactory.SaveChangesAsync();

                response.Error = 0;
                response.Message = "Organization removed successfully.";
                response.Data = organization;
            }
            catch (Exception ex)
            {
                response.Error = 1;
                response.Message = $"Error removing organization: {ex.Message}";
                response.Data = null;
            }

            return response;
        }

        public async Task<PacomResponse<List<Organization>>> ListOrganizationAsync()
        {
            var response = new PacomResponse<List<Organization>>();
            try
            {
                var organizations = await _contextFactory.Organizations.ToListAsync();

                response.Error = 0;
                response.Message = organizations.Any()
                    ? $"Found {organizations.Count} organization(s)."
                    : "No organizations found.";
                response.Data = organizations;
            }
            catch (Exception ex)
            {
                response.Error = 1;
                response.Message = $"Error retrieving organizations: {ex.Message}";
                response.Data = null;
            }

            return response;
        }

        public async Task<PacomResponse<string>> StoreActivityEventAsync(ActivityEvent log)
        { 
            var response = new PacomResponse<string>();

            try
            {
                var existing = await _contextFactory.ActivityEvents.FirstOrDefaultAsync(o => o.Version == log.Version);

                if (existing == null)
                {
                    await _contextFactory.ActivityEvents.AddAsync(log);
                    await _contextFactory.SaveChangesAsync();

                    response.Message = "Activity event stored successfully.";
                    response.Data = log.Version;
                    response.Error = 0;
                }
                else
                {
                    existing.IsProcessed = log.IsProcessed;

                    _contextFactory.ActivityEvents.Update(existing);
                    await _contextFactory.SaveChangesAsync();

                    response.Message = "Activity event update.";
                    response.Data = existing.Version;
                    response.Error = 0;
                }

            }
            catch (Exception ex)
            {
                response.Error = 1;
                response.Message = $"Error managing activity event: {ex.Message}";
                response.Data = null;
            }

            return response;
        }

        public async Task<PacomResponse<ActivityEvent>> LastActivityEventAsync()
        {
            var response = new PacomResponse<ActivityEvent>();
            try
            {
                var latestEvent = await _contextFactory.ActivityEvents
                    .OrderByDescending(e => e.Version)
                    .FirstOrDefaultAsync();
                response.Error = 0;
                response.Message = latestEvent != null
                    ? "Latest activity event retrieved successfully."
                    : "No activity events found.";
                response.Data = latestEvent;
            }
            catch (Exception ex)
            {
                response.Error = 1;
                response.Message = $"Error retrieving latest activity event: {ex.Message}";
                response.Data = null;
            }
            return response;
        }

        public async Task<PacomResponse<List<ActivityEvent>>> GetUnprocessEventAsync()
        {
            var response = new PacomResponse<List<ActivityEvent>>();
            try
            {
                var unprocessedEvents = await _contextFactory.ActivityEvents
                    .Where(e => e.IsProcessed != true )
                    .ToListAsync();
                response.Error = 0;
                response.Message = unprocessedEvents.Any()
                    ? $"Found {unprocessedEvents.Count} unprocessed activity event(s)."
                    : "No unprocessed activity events found.";
                response.Data = unprocessedEvents;
            }
            catch (Exception ex)
            {
                response.Error = 1;
                response.Message = $"Error retrieving unprocessed activity events: {ex.Message}";
                response.Data = null;
            }
            return response;
        }

        public async Task<PacomResponse<List<ActivityEvent>>> ListActivityEventAsync(DateTime UtcStartDate, DateTime UtcEndDate, string? OrganizationName = null)
        {
            var response = new PacomResponse<List<ActivityEvent>>();
            try
            {
                var ListEvents = await _contextFactory.ActivityEvents.ToListAsync();
                response.Error = 0;
                response.Message = ListEvents.Any()
                    ? $"Found {ListEvents.Count} unprocessed activity event(s)."
                    : "No unprocessed activity events found.";
                response.Data = ListEvents;
            }
            catch (Exception ex)
            {
                response.Error = 1;
                response.Message = $"Error retrieving unprocessed activity events: {ex.Message}";
                response.Data = null;
            }
            return response;
        }

    }
}
