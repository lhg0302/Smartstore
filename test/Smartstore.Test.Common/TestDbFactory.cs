using System;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Data;
using Smartstore.Data.Providers;
using SqlSugar;

namespace Smartstore.Test.Common
{
    public class TestDbFactory : DbFactory
    {
        private readonly SqliteConnection _connection;

        public TestDbFactory()
        {
            // Single shared in‑memory connection so that EF and SqlSugar can operate
            // on the same database during a test run.
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            SugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = _connection.ConnectionString,
                DbType = DbType.Sqlite,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });

            // Create the database schema once for all tests
            SugarClient.DbMaintenance.CreateDatabase();
        }

        public ISqlSugarClient SugarClient { get; }

        public override DbSystemType DbSystem { get; } = DbSystemType.Unknown;

        public override DbConnectionStringBuilder CreateConnectionStringBuilder(string connectionString)
            => new SqliteConnectionStringBuilder(connectionString);

        public override DbConnectionStringBuilder CreateConnectionStringBuilder(
            string server,
            string database,
            string userName,
            string password)
            => new SqliteConnectionStringBuilder
            {
                DataSource = database
            };

        public override DataProvider CreateDataProvider(DatabaseFacade database)
            => new TestDataProvider(database);

        public override TContext CreateDbContext<TContext>(string connectionString, int? commandTimeout = null)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TContext>()
                .UseSqlite(_connection);

            return (TContext)Activator.CreateInstance(typeof(TContext), optionsBuilder.Options);
        }

        public override DbContextOptionsBuilder ConfigureDbContext(DbContextOptionsBuilder builder, string connectionString)
        {
            return builder.UseSqlite(_connection);
        }
    }
}
