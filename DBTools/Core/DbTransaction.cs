using DBTools.Abstractions;
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace DBTools.Core
{
    /// <summary>
    /// Wraps a database transaction with both sync and async commit/rollback support.
    /// Used by SqlClient.BeginTransaction() to provide cross-operation transactional support.
    /// </summary>
    public class DbToolsTransaction : IDbTransaction
    {
        private readonly DbConnection _connection;
        private readonly System.Data.Common.DbTransaction _transaction;
        private bool _isActive = true;
        private bool _disposed = false;

        internal DbToolsTransaction(DbConnection connection, System.Data.Common.DbTransaction transaction)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        }

        /// <summary>
        /// Gets the underlying DbTransaction for use by SqlClient operations within this transaction scope.
        /// </summary>
        internal System.Data.Common.DbTransaction UnderlyingTransaction => _transaction;

        /// <summary>
        /// Gets the connection associated with this transaction.
        /// </summary>
        internal DbConnection Connection => _connection;

        /// <inheritdoc/>
        public bool IsActive => _isActive;

        /// <inheritdoc/>
        public void Commit()
        {
            ThrowIfNotActive();
            _transaction.Commit();
            _isActive = false;
        }

        /// <inheritdoc/>
        public void Rollback()
        {
            ThrowIfNotActive();
            _transaction.Rollback();
            _isActive = false;
        }

        /// <inheritdoc/>
        public async Task CommitAsync(CancellationToken ct = default)
        {
            ThrowIfNotActive();
            await _transaction.CommitAsync(ct).ConfigureAwait(false);
            _isActive = false;
        }

        /// <inheritdoc/>
        public async Task RollbackAsync(CancellationToken ct = default)
        {
            ThrowIfNotActive();
            await _transaction.RollbackAsync(ct).ConfigureAwait(false);
            _isActive = false;
        }

        private void ThrowIfNotActive()
        {
            if (!_isActive)
                throw new InvalidOperationException("Transaction is no longer active. It has already been committed or rolled back.");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_isActive)
            {
                try { _transaction.Rollback(); } catch { /* Swallow on dispose */ }
                _isActive = false;
            }

            _transaction.Dispose();
            _connection.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            if (_isActive)
            {
                try { await _transaction.RollbackAsync().ConfigureAwait(false); } catch { /* Swallow on dispose */ }
                _isActive = false;
            }

            await _transaction.DisposeAsync().ConfigureAwait(false);
            await _connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}
