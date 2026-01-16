using System;
using System.Collections.Generic;
using System.Security.RightsManagement;
using System.Text;
using Microsoft.Data.Sqlite;

namespace DoneTetris
{
    public sealed class DoneRepository
    {
        public List<Done> GetAllDonesOrdered()
        {
            var list = new List<Done>();

            using var conn = new SqliteConnection(Db.ConnectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT Id, BatchId, DoneDate, Text, CreatedAt, GrantedLengthN
FROM Done
ORDER BY Id ASC;";

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Done
                {
                    Id = r.GetInt64(0),
                    BatchId = r.GetInt32(1),
                    DoneDate = r.GetString(2),
                    Text = r.GetString(3),
                    CreatedAt = r.GetString(4),
                    GrantedLengthN = r.GetInt32(5)
                });
            }
            return list;
        }

        public Done? GetOldestUnplacedDone()
        {
            // 次に配置するテトリスを特定するためのメソッド
            using var conn = new SqliteConnection(Db.ConnectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT d.Id, d.BatchId, d.DoneId, d.Text, d.CreatedAt, d.GrantedLengthN,
FROM Done d
LEFT JOIN Move m ON m.DoneId = d.Id
WHERE m.Id IS NULL
ORDER BY d.Id ASC LIMIT 1;";

            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
                return new Done
                {
                    Id = r.GetInt64(0),
                    BatchId = r.GetInt32(1),
                    DoneDate = r.GetString(2),
                    Text = r.GetString(3),
                    CreatedAt = r.GetString(4),
                    GrantedLengthN = r.GetInt32(5)
                };
        }

        public sealed class MoveRepository
        {
            public List<Move> GetAllMovesOrdered() 
            {
                // DB操作をUIから見えないように隠し、パッケージ化するためのメソッド
                var list = new List<Move>();

                using var conn = new SqliteConnection(Db.ConnectionString);
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
SELECT Id, DoneId, PlacedAt, Column, StartRow, LengthN, IsVertical, ClearedLines
FROM Move
ORDER BY Id ASC;";

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new Move
                    {
                        Id = r.GetInt64(0),
                        DoneId = r.GetInt64(1),
                        PlacedAt = r.GetString(2),
                        Column = r.GetInt32(3),
                        StartRow = r.GetInt32(4),
                        LengthN = r.GetInt32(5),
                        IsVertical = r.GetInt32(6) == 1,
                        ClearedLines = r.GetInt32(7)
                    });
                }
                return list;
            }
        }

    } 
}
