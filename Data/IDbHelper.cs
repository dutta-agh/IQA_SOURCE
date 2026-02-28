using System.Data;
using IQA.Models;
using MySqlConnector;

namespace YourApp.Data
{
    public interface IDbHelper
    {
        Task<bool> TryOpenAsync(CancellationToken ct = default);
        Task<MySqlResponse> ExecuteStoredProcAsyncResponse(string procName, Action<MySqlParameterCollection>? bind = null, int? commandTimeoutSeconds = null, CancellationToken ct = default);
        DataTable ExecuteQuery(string query, MySqlParameter[]? parameters = null, int? commandTimeoutSeconds = null);
        int ExecuteNonQuery(string query, MySqlParameter[]? parameters = null, int? commandTimeoutSeconds = null);
    }
}