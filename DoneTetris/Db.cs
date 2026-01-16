using DoneListWithTetris;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.Sqlite;
namespace DoneTetris
{
    public static class Db
    {
        public static readonly string DbPath = DbInitializer.GetDbPath();

        public static string ConnectionString =>
            new SqliteConnectionStringBuilder { DataSource = DbPath }.ToString();
    }
}
