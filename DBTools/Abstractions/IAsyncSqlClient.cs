using DBTools.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DBTools.Abstractions
{
    /// <summary>
    /// Async counterpart of ISqlClient for modern .NET async/await patterns.
    /// All operations support CancellationToken for cooperative cancellation.
    /// </summary>
    public interface IAsyncSqlClient : IAsyncDisposable, IDisposable
    {
        Task<DataTable> SelectAsync(string fields, string table, string whereClause, object[] parameters, CancellationToken ct = default);
        Task<DataTable> SelectAsync(string queryWithoutSelect, object[] parameters, CancellationToken ct = default);
        Task<bool> InsertAsync(string[] fields, string table, object[] values, string primaryKeyName = null, bool autoIncrement = true, CancellationToken ct = default);
        Task<bool> UpdateAsync(string[] fields, string table, string[] values, string whereClause, object[] whereParameters, CancellationToken ct = default);
        Task<bool> DeleteAsync(string table, string whereClause, object[] parameters, CancellationToken ct = default);
        Task ExecuteQueryAsync(string query, CancellationToken ct = default);
        Task<DataTable> SelectRawAsync(string fullSql, object[] parameters, CancellationToken ct = default);
        Task<object> ExecuteScalarAsync(string query, object[] parameters, CancellationToken ct = default);

        ISqlQueryBuilder QueryBuilderInstance { get; }
        IDbConfiguration Configuration { get; }
        string ConnectionString { get; }
        string Error { get; }
    }
}
