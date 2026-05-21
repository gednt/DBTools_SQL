using DBTools.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace DBTools.Migrations
{
    /// <summary>
    /// Abstract base class for migrations that provides common functionality
    /// including checksum computation.
    /// </summary>
    public abstract class MigrationBase : IMigration
    {
        /// <summary>
        /// Unique identifier for the migration (timestamp-based).
        /// </summary>
        public abstract string MigrationId { get; }

        /// <summary>
        /// Human-readable description of what this migration does.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// SQL script to apply the migration (upgrade).
        /// </summary>
        public abstract string UpSql { get; }

        /// <summary>
        /// SQL script to revert the migration (downgrade).
        /// </summary>
        public abstract string DownSql { get; }

        /// <summary>
        /// Computes a SHA256 checksum of the given SQL string.
        /// Used to detect if a migration script was modified after being applied.
        /// </summary>
        /// <param name="sql">The SQL string to hash.</param>
        /// <returns>A lowercase hex-encoded SHA256 hash string.</returns>
        public static string ComputeChecksum(string sql)
        {
            if (string.IsNullOrEmpty(sql))
                sql = string.Empty;

            using var sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(sql);
            byte[] hash = sha256.ComputeHash(bytes);

            var sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
