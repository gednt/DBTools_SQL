using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace DBTools.Caching
{
    /// <summary>
    /// Generates deterministic cache keys from SQL queries and parameters.
    /// Uses SHA256 hashing to produce consistent, fixed-length keys.
    /// </summary>
    public static class CacheKeyGenerator
    {
        /// <summary>
        /// Generates a deterministic cache key from an SQL query and its parameters.
        /// </summary>
        /// <param name="sql">The SQL query string.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <returns>A deterministic hash-based cache key.</returns>
        public static string GenerateKey(string sql, List<object> parameters)
        {
            var sb = new StringBuilder();
            sb.Append(sql ?? string.Empty);

            if (parameters != null)
            {
                sb.Append("|params:");
                for (int i = 0; i < parameters.Count; i++)
                {
                    if (i > 0) sb.Append(",");
                    sb.Append(parameters[i]?.ToString() ?? "null");
                }
            }

            return ComputeHash(sb.ToString());
        }

        /// <summary>
        /// Generates a deterministic cache key from an SQL query, parameters, and table name.
        /// The table name is included to support table-based invalidation tracking.
        /// </summary>
        /// <param name="sql">The SQL query string.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="tableName">The table name associated with the query.</param>
        /// <returns>A deterministic hash-based cache key.</returns>
        public static string GenerateKey(string sql, List<object> parameters, string tableName)
        {
            var sb = new StringBuilder();
            sb.Append(sql ?? string.Empty);

            if (parameters != null)
            {
                sb.Append("|params:");
                for (int i = 0; i < parameters.Count; i++)
                {
                    if (i > 0) sb.Append(",");
                    sb.Append(parameters[i]?.ToString() ?? "null");
                }
            }

            if (!string.IsNullOrEmpty(tableName))
            {
                sb.Append("|table:");
                sb.Append(tableName);
            }

            return ComputeHash(sb.ToString());
        }

        private static string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(input);
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
