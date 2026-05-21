using DBTools.StoredProcedures;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DBTools.Abstractions
{
    /// <summary>
    /// Provides synchronous stored procedure execution capabilities.
    /// Supports input/output parameters and return values.
    /// </summary>
    public interface IStoredProcedureClient
    {
        /// <summary>
        /// Executes a stored procedure and returns the result including output parameters and return value.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure to execute</param>
        /// <param name="parameters">The parameters to pass to the stored procedure</param>
        /// <returns>A result containing the return value, output parameters, and rows affected</returns>
        StoredProcedureResult ExecuteStoredProcedure(string procedureName, StoredProcedureParameter[] parameters);

        /// <summary>
        /// Executes a stored procedure and returns the result set as a DataTable.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure to execute</param>
        /// <param name="parameters">The parameters to pass to the stored procedure</param>
        /// <returns>A DataTable containing the result set</returns>
        DataTable ExecuteStoredProcedureReader(string procedureName, StoredProcedureParameter[] parameters);
    }

    /// <summary>
    /// Provides asynchronous stored procedure execution capabilities.
    /// Supports input/output parameters and return values with cancellation support.
    /// </summary>
    public interface IAsyncStoredProcedureClient
    {
        /// <summary>
        /// Asynchronously executes a stored procedure and returns the result including output parameters and return value.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure to execute</param>
        /// <param name="parameters">The parameters to pass to the stored procedure</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>A result containing the return value, output parameters, and rows affected</returns>
        Task<StoredProcedureResult> ExecuteStoredProcedureAsync(string procedureName, StoredProcedureParameter[] parameters, CancellationToken ct = default);

        /// <summary>
        /// Asynchronously executes a stored procedure and returns the result set as a DataTable.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure to execute</param>
        /// <param name="parameters">The parameters to pass to the stored procedure</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>A DataTable containing the result set</returns>
        Task<DataTable> ExecuteStoredProcedureReaderAsync(string procedureName, StoredProcedureParameter[] parameters, CancellationToken ct = default);
    }
}
