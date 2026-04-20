using System.Data;
using MySqlConnector;
using IQA_SOURCE.Models.Admin;
using YourApp.Data;

namespace IQA_SOURCE.Data
{
    public class SystemCheckParamRepository : ISystemCheckParamRepository
    {
        private readonly IDbHelper _dbHelper;

        public SystemCheckParamRepository(IDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public async Task<SystemCheckParamResponse> GetAllParams(string userId)
        {
            try
            {
                var query = @"
                    SELECT scp_id, scp_param_code, scp_param_name, scp_param_value,
                           scp_data_type, scp_description, scp_active,
                           scp_created_user, scp_created_date, scp_modified_user, scp_modified_date
                    FROM tbl_system_check_params
                    ORDER BY scp_id";

                var table = await Task.Run(() => _dbHelper.ExecuteQuery(query));
                return new SystemCheckParamResponse
                {
                    OutputCode = 1,
                    OutputMsg  = "Params retrieved successfully",
                    Data       = table.Rows.Cast<DataRow>().Select(Map).ToList()
                };
            }
            catch (Exception ex)
            {
                return new SystemCheckParamResponse { OutputCode = 0, OutputMsg = ex.Message };
            }
        }

        public async Task<SystemCheckParamResponse> GetParamByCode(string paramCode, string userId)
        {
            try
            {
                var query = @"
                    SELECT scp_id, scp_param_code, scp_param_name, scp_param_value,
                           scp_data_type, scp_description, scp_active,
                           scp_created_user, scp_created_date, scp_modified_user, scp_modified_date
                    FROM tbl_system_check_params
                    WHERE scp_param_code = @code
                    LIMIT 1";

                var table = await Task.Run(() => _dbHelper.ExecuteQuery(query,
                    [new MySqlParameter("@code", paramCode)]));

                return new SystemCheckParamResponse
                {
                    OutputCode = 1,
                    OutputMsg  = table.Rows.Count > 0 ? "Found" : "Not found",
                    Data       = table.Rows.Cast<DataRow>().Select(Map).ToList()
                };
            }
            catch (Exception ex)
            {
                return new SystemCheckParamResponse { OutputCode = 0, OutputMsg = ex.Message };
            }
        }

        public async Task<SystemCheckParamResponse> UpsertParam(SystemCheckParam param, string userId)
        {
            try
            {
                var query = @"
                    INSERT INTO tbl_system_check_params
                        (scp_param_code, scp_param_name, scp_param_value, scp_data_type,
                         scp_description, scp_active, scp_created_user, scp_created_date)
                    VALUES
                        (@code, @name, @value, @dataType,
                         @desc, @active, @user, UTC_TIMESTAMP())
                    ON DUPLICATE KEY UPDATE
                        scp_param_name    = VALUES(scp_param_name),
                        scp_param_value   = VALUES(scp_param_value),
                        scp_data_type     = VALUES(scp_data_type),
                        scp_description   = VALUES(scp_description),
                        scp_active        = VALUES(scp_active),
                        scp_modified_user = @user,
                        scp_modified_date = UTC_TIMESTAMP()";

                var rows = await Task.Run(() => _dbHelper.ExecuteNonQuery(query, [
                    new MySqlParameter("@code",     param.ScpParamCode),
                    new MySqlParameter("@name",     param.ScpParamName),
                    new MySqlParameter("@value",    param.ScpParamValue),
                    new MySqlParameter("@dataType", param.ScpDataType),
                    new MySqlParameter("@desc",     (object?)param.ScpDescription ?? DBNull.Value),
                    new MySqlParameter("@active",   param.ScpActive),
                    new MySqlParameter("@user",     userId)
                ]));

                return new SystemCheckParamResponse
                {
                    OutputCode = rows > 0 ? 1 : 0,
                    OutputMsg  = rows > 0 ? "Saved successfully" : "No changes made"
                };
            }
            catch (Exception ex)
            {
                return new SystemCheckParamResponse { OutputCode = 0, OutputMsg = ex.Message };
            }
        }

        public async Task<SystemCheckSettings> GetResolvedSettings()
        {
            var response = await GetAllParams("system");
            var lookup   = response.Data
                .Where(p => p.ScpActive == "Y")
                .ToDictionary(p => p.ScpParamCode, p => p.ScpParamValue, StringComparer.OrdinalIgnoreCase);

            return new SystemCheckSettings
            {
                MinScreenWidth    = lookup.TryGetValue("MIN_SCREEN_WIDTH",  out var w) && int.TryParse(w, out var wi)  ? wi   : 1024,
                MinScreenHeight   = lookup.TryGetValue("MIN_SCREEN_HEIGHT", out var h) && int.TryParse(h, out var hi)  ? hi   : 768,
                AllowedDevices    = lookup.TryGetValue("ALLOWED_DEVICES",   out var d) && !string.IsNullOrWhiteSpace(d)
                                        ? d.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                        : ["Desktop", "Laptop"],
                MinDownloadMbps   = lookup.TryGetValue("MIN_DOWNLOAD_MBPS",  out var s) && decimal.TryParse(s, out var si) ? si : 2m,
                IncognitoRequired = !lookup.TryGetValue("INCOGNITO_REQUIRED", out var i) || i.Equals("Y", StringComparison.OrdinalIgnoreCase)
            };
        }

        private static SystemCheckParam Map(DataRow row) => new()
        {
            ScpId           = Convert.ToInt32(row["scp_id"]),
            ScpParamCode    = row["scp_param_code"]?.ToString()    ?? string.Empty,
            ScpParamName    = row["scp_param_name"]?.ToString()    ?? string.Empty,
            ScpParamValue   = row["scp_param_value"]?.ToString()   ?? string.Empty,
            ScpDataType     = row["scp_data_type"]?.ToString()     ?? "string",
            ScpDescription  = row["scp_description"]?.ToString(),
            ScpActive       = row["scp_active"]?.ToString()        ?? "Y",
            ScpCreatedUser  = row["scp_created_user"]?.ToString(),
            ScpCreatedDate  = row["scp_created_date"]  != DBNull.Value ? Convert.ToDateTime(row["scp_created_date"])  : null,
            ScpModifiedUser = row["scp_modified_user"]?.ToString(),
            ScpModifiedDate = row["scp_modified_date"] != DBNull.Value ? Convert.ToDateTime(row["scp_modified_date"]) : null
        };
    }
}