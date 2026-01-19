using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Windows;

namespace DoneListWithTetris
{
    public static class DbInitializer
    {
        public static string GetDbPath()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DoneTetris"
                );
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "donetetris.db");
        }

        public static void Initialize(string dbPath)
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath
            }.ToString();

            using var conn = new SqliteConnection(connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Done (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    BatchId         INTEGER NOT NULL,
    DoneDate        TEXT    NOT NULL,
    DoneText        TEXT    NOT NULL,
    CreatedAt       TEXT    NOT NULL,
    GrantedLengthN  INTEGER NOT NULL CHECK (GrantedLengthN BETWEEN 1 AND 5)
);

CREATE TABLE IF NOT EXISTS Move (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    DoneId       INTEGER NOT NULL UNIQUE,
    PlacedAt     TEXT    NOT NULL,
    Column       INTEGER NOT NULL,
    StartRow     INTEGER NOT NULL,
    LengthN      INTEGER NOT NULL CHECK (LengthN BETWEEN 1 AND 5),
    IsVertical   INTEGER NOT NULL CHECK (IsVertical IN (0, 1)),
    ClearedLines INTEGER NOT NULL CHECK (ClearedLines >= 0),

    FOREIGN KEY (DoneId) REFERENCES Done(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS Meta (
    Key     TEXT PRIMARY KEY,
    Value   TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_Done_DoneDate ON Done(DoneDate);
CREATE INDEX IF NOT EXISTS IX_Done_BatchId ON Done(BatchId);
CREATE INDEX IF NOT EXISTS IX_Move_PlacedAt ON Move(PlacedAt);

INSERT OR IGNORE INTO Meta(Key, Value) VALUES ('CurrentStreak', '0');
INSERT OR IGNORE INTO Meta(Key, Value) VALUES ('LastActiveDate', '');
";
                
            cmd.ExecuteNonQuery();


        }


    }
}
