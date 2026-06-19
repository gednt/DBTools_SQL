using DBTools.Abstractions;
using System;

namespace DBTools.Models
{
    /// <summary>
    /// Generic data container with optional <see cref="ISqlClient"/> injection for Insert/Update operations.
    /// </summary>
    /// <remarks>
    /// Constructed via the parameterless constructor, this object acts as a pure data carrier.
    /// <see cref="Insert"/> and <see cref="Update(string)"/> require an explicit client to be supplied
    /// either through the <see cref="GenericObject(ISqlClient)"/> constructor or by setting
    /// <see cref="DbTools"/> before invoking those operations.
    /// </remarks>
    public class GenericObject
    {
        private ISqlClient _dbTools;

        /// <summary>
        /// Gets or sets the database client used by <see cref="Insert"/> and <see cref="Update(string)"/>.
        /// Setting <c>null</c> removes the current client and restores the data-only behavior.
        /// </summary>
        public ISqlClient DbTools
        {
            get => _dbTools;
            set => _dbTools = value;
        }

        /// <summary>
        /// Creates a data-only instance. <see cref="Insert"/> and <see cref="Update(string)"/> will
        /// throw <see cref="InvalidOperationException"/> until an <see cref="ISqlClient"/> is provided.
        /// </summary>
        public GenericObject()
        {
        }

        /// <summary>
        /// Creates an instance bound to the supplied <paramref name="dbTools"/> client.
        /// </summary>
        /// <param name="dbTools">Client used by <see cref="Insert"/> and <see cref="Update(string)"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dbTools"/> is <c>null</c>.</exception>
        public GenericObject(ISqlClient dbTools)
        {
            _dbTools = dbTools ?? throw new ArgumentNullException(nameof(dbTools));
        }

        public String[] columns { get; set; }
        public Object[] values { get; set; }
        public String[] valuesString { get; set; }
        public String[] types { get; set; }
        public String table { get; set; }

        /// <summary>
        /// Inserts the configured row using the injected <see cref="DbTools"/> client.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no <see cref="ISqlClient"/> has been supplied.
        /// </exception>
        public bool Insert()
        {
            EnsureClient();
            return _dbTools.Insert(columns, table, valuesString);
        }

        /// <summary>
        /// Updates the configured row using the injected <see cref="DbTools"/> client.
        /// </summary>
        /// <param name="conditions">WHERE clause required by the underlying UPDATE operation.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no <see cref="ISqlClient"/> has been supplied.
        /// </exception>
        public bool Update(string conditions)
        {
            EnsureClient();
            return _dbTools.Update(columns, table, valuesString, conditions);
        }

        private void EnsureClient()
        {
            if (_dbTools == null)
            {
                throw new InvalidOperationException(
                    "GenericObject has no ISqlClient configured. Use the GenericObject(ISqlClient) " +
                    "constructor or assign the DbTools property before calling Insert/Update.");
            }
        }
    }
}