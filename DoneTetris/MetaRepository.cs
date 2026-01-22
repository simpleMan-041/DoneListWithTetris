using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;

namespace DoneTetris
{
    public sealed class MetaRepository
    {
        public string? Get(string key)
        {
            using var conn = new SqliteConnection(Db.ConnectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Value FROM Meta WHERE Key = $key LIMIT 1;";
            cmd.Parameters.AddWithValue("$key", key);

            return cmd.ExecuteScalar() as string;
        }

        public void Set(string key, string value)
        {
            using var conn = new SqliteConnection(Db.ConnectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
INSERT INTO Meta(""Key"", ""Value"") VALUES ($key, $value)
ON CONFLICT(""Key"") DO UPDATE SET ""Value"" = excluded.Value;
";
            cmd.Parameters.AddWithValue("$key", key);
            cmd.Parameters.AddWithValue("$value",value);

            cmd.ExecuteNonQuery();
        }

        public int GetInt(string key, int defaultValue)
        {
            var s = Get(key);
            return int.TryParse(s, out var v) ? v : defaultValue;
        }

        public void SetInt(string key, int value) => Set(key, value.ToString());
    }
}
