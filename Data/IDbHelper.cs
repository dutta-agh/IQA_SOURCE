using IQA.Models;
using MySqlConnector;

namespace YourApp.Data
{
    public interface IDbHelper
    {
        Task<bool> TryOpenAsync(CancellationToken ct = default);
        Task<MySqlResponse> ExecuteStoredProcAsyncResponse(string procName, Action<MySqlParameterCollection>? bind = null, int? commandTimeoutSeconds = null, CancellationToken ct = default);
    }
}