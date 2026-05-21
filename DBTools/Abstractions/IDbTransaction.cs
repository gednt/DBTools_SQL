using System;
using System.Threading;
using System.Threading.Tasks;

namespace DBTools.Abstractions
{
    /// <summary>
    /// Represents a database transaction that can span multiple operations.
    /// Implements both sync and async disposal patterns.
    /// </summary>
    public interface IDbTransaction : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Commits the transaction, persisting all changes.
        /// </summary>
        void Commit();

        /// <summary>
        /// Rolls back the transaction, discarding all changes.
        /// </summary>
        void Rollback();

        /// <summary>
        /// Asynchronously commits the transaction.
        /// </summary>
        Task CommitAsync(CancellationToken ct = default);

        /// <summary>
        /// Asynchronously rolls back the transaction.
        /// </summary>
        Task RollbackAsync(CancellationToken ct = default);

        /// <summary>
        /// Gets whether the transaction is still active (not committed or rolled back).
        /// </summary>
        bool IsActive { get; }
    }
}
