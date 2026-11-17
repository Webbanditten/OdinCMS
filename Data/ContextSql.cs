using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KeplerCMS.Data
{
    /// <summary>
    /// Plain SQL on the DataContext's connection, inside its current transaction when there is one. For tables EF can't
    /// track well (vouchers_items has no key) and for locked claims (SELECT ... FOR UPDATE) shared with Odin.
    /// </summary>
    public sealed class ContextSql
    {
        private readonly DataContext _context;

        public ContextSql(DataContext context)
        {
            _context = context;
        }

        public DbCommand Command(string sql, params (string Name, object Value)[] parameters)
        {
            var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            command.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction();
            foreach (var (name, value) in parameters)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = value ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
            return command;
        }

        public async Task<int> Execute(string sql, params (string, object)[] parameters)
        {
            await using var command = Command(sql, parameters);
            return await command.ExecuteNonQueryAsync();
        }

        public async Task<List<T>> Query<T>(string sql, Func<DbDataReader, T> read, params (string, object)[] parameters)
        {
            var rows = new List<T>();
            await using var command = Command(sql, parameters);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) rows.Add(read(reader));
            return rows;
        }

        public async Task<long> LastInsertId()
        {
            await using var command = Command("SELECT LAST_INSERT_ID()");
            return Convert.ToInt64(await command.ExecuteScalarAsync());
        }

        /// <summary>Runs <paramref name="work"/> with the connection open (transactions need one connection throughout).</summary>
        public async Task<T> InConnection<T>(Func<Task<T>> work)
        {
            await _context.Database.OpenConnectionAsync();
            try { return await work(); }
            finally { await _context.Database.CloseConnectionAsync(); }
        }
    }
}
