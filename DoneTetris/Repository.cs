using System;
using System.Collections.Generic;
using System.Security.RightsManagement;
using System.Text;
using Microsoft.Data.Sqlite;

namespace DoneTetris
{
    public sealed partial class DoneRepository
    {
        public List<Done> GetAllDonesOrdered()
        {
            var list = new List<Done>();

            using var conn = new SqliteConnection(Db.ConnectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT Id, BatchId, DoneDate, DoneText, CreatedAt, GrantedLengthN
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
                    DoneText = r.GetString(3),
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
SELECT
    d.Id,
    d.BatchId,
    d.DoneText,
    d.CreatedAt,
    d.GrantedLengthN
FROM Done d
LEFT JOIN Move m ON m.DoneId = d.Id
WHERE m.Id IS NULL
ORDER BY d.Id ASC
LIMIT 1;
";

            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            return new Done
            {
                Id = r.GetInt64(0),
                BatchId = r.GetInt32(1),
                DoneDate = r.GetString(2),
                DoneText = r.GetString(3),
                CreatedAt = r.GetString(4),
                GrantedLengthN = r.GetInt32(5)
            };
        }

        public int GetNextBatchId()
        {
            using var conn = new SqliteConnection(Db.ConnectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COALECE(MAX(BatchId), 0) + 1 FROM Done;";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void AddDones(int batchId, string doneDate, List<string> texts, Func<int> grantNFactory)
        {
            // textがリストなのは複数追加を可能にするため。

            using var conn = new SqliteConnection(Db.ConnectionString);
            conn.Open();

            using var tx = conn.BeginTransaction();

            foreach (var text in texts)
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
INSERT INTO Done(BatchId, DoneDate, DoneText, CreatedAt, GrantedLengthN)
VALUES ($batchId, $doneDate, $doneText, $createdAt, $n);";

                cmd.Parameters.AddWithValue("$batchId",batchId);
                cmd.Parameters.AddWithValue("$doneDate", doneDate);
                cmd.Parameters.AddWithValue("$doneText",text);
                cmd.Parameters.AddWithValue("$createdAt",DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
                cmd.Parameters.AddWithValue("$n",grantNFactory());

                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }

        public List<Done> GetDonesByDate(string doneDate)
        {
            // 登録順にDoneを整理して画面に表示するためのメソッド。
            var list = new List<Done>();

            using var conn = new SqliteConnection(Db.ConnectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT Id, BatchId, DoneDate, DoneText, CreatedAt, GrantedLengthN
FROM Done
WHERE DoneDate = $doneDate
ORDER BY Id ASC;";
            cmd.Parameters.AddWithValue("$doneDate", doneDate);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Done
                {
                    Id = r.GetInt64(0),
                    BatchId = r.GetInt32(1),
                    DoneDate = r.GetString(2),
                    DoneText = r.GetString(3),
                    CreatedAt = r.GetString(4),
                    GrantedLengthN = r.GetInt32(5)
                });
            }
            
            return list;
        }

        public void DeleteDoneByIds(IEnumerable<long> ids)
        {
            // チェックが付けられたDoneを削除するためのメソッド
            var idList = new List<long>(ids);
            if (idList.Count == 0) return;
            
            using var conn = new SqliteConnection(Db.ConnectionString);
            conn.Open();

            using var tx = conn.BeginTransaction();

            var placeholders = new List<string>();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;

            for (int i = 0; i < idList.Count; i++)
            {
                var p = $"id{i}";
                placeholders.Add(p);
                cmd.Parameters.AddWithValue(p, idList[i]);
            }

            cmd.CommandText = $"DELETE FROM Done WHERE Id IN ({string.Join(",", placeholders)});";
            cmd.ExecuteNonQuery();

            tx.Commit();

        }
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

