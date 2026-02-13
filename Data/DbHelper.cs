using System.Data;
using IQA.Models;
using MySqlConnector;

namespace YourApp.Data
{
    public sealed class DbHelper : IDbHelper, IAsyncDisposable
    {
        private readonly string _connectionString;
        private readonly int _defaultCommandTimeoutSeconds;
        public DbHelper(DbOptions options)
        {
            var csb = new MySqlConnectionStringBuilder
            {
                Server = options.Server,
                Port = options.Port,
                UserID = options.UserId,
                Password = options.Password,
                Database = options.Database,
                SslMode = options.Ssl ? MySqlSslMode.Required : MySqlSslMode.None,
                Pooling = true,
                MinimumPoolSize = (uint)options.MinPoolSize,
                MaximumPoolSize = (uint)options.MaxPoolSize,
                ConnectionTimeout = (uint)options.ConnectTimeoutSeconds,
                AllowPublicKeyRetrieval = true
            };
            _connectionString = csb.ConnectionString;

            _defaultCommandTimeoutSeconds = options is not null && options.ConnectTimeoutSeconds > 0 ? options.ConnectTimeoutSeconds : 30;
        }

        private MySqlConnection CreateConnection() => new MySqlConnection(_connectionString);

        public async Task<bool> TryOpenAsync(CancellationToken ct = default)
        {
            try
            {
                await using var conn = CreateConnection();
                await conn.OpenAsync(ct).ConfigureAwait(false);
                await conn.CloseAsync().ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<MySqlResponse> ExecuteStoredProcAsyncResponse(string procName, Action<MySqlParameterCollection>? bind = null, int? commandTimeoutSeconds = null, CancellationToken ct = default)
        {
            return await Task.Run(async () =>
            {
                await using var conn = CreateConnection();
                await conn.OpenAsync(ct).ConfigureAwait(false);

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = procName;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = commandTimeoutSeconds.HasValue ? commandTimeoutSeconds.Value : _defaultCommandTimeoutSeconds;
                bind?.Invoke(cmd.Parameters);

                var ds = new DataSet();
                using var adapter = new MySqlDataAdapter(cmd);
                adapter.Fill(ds);

                var response = new MySqlResponse
                {
                    Data = ds,
                    RowsAffected = 0,
                    OutputParameters = CollectOutputParameters(cmd.Parameters)
                };

                return response;
            }, ct);
        }

        private static List<MySqlParameter?> CollectOutputParameters(MySqlParameterCollection? parameters)
        {
            return parameters?.AsEnumerable()?.Where(p => p.Direction is ParameterDirection.Output or ParameterDirection.InputOutput or ParameterDirection.ReturnValue)?.Select(p => p ?? null)?.ToList();
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}