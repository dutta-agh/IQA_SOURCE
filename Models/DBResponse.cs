using MySqlConnector;
using System.Data;

namespace IQA.Models
{
    public sealed class MySqlResponse
    {
        public DataSet? Data { get; init; }
        public int? RowsAffected { get; init; }
        public List<MySqlParameter?> OutputParameters { get; init; } = new();
    }

}
