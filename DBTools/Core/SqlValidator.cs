using DBTools.Abstractions;
using System;
using System.Text.RegularExpressions;

namespace DBTools.Core
{
    public class SqlValidator : ISqlValidator
    {
        private static readonly Regex IdentifierRegex =
            new Regex(@"^[\w\.\[\]\,\s\*\(\)]+$", RegexOptions.Compiled);

        private static readonly Regex DangerousKeywordRegex =
            new Regex(@"\b(DROP|DELETE|TRUNCATE|INSERT|UPDATE|EXEC|EXECUTE|ALTER|CREATE|GRANT|REVOKE|MERGE)\b",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public bool IsValidIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return false;

            if (DangerousKeywordRegex.IsMatch(identifier))
                return false;

            if (identifier.Contains("--") || identifier.Contains(";--") ||
                identifier.Contains("/*") || identifier.Contains("*/"))
                return false;

            return IdentifierRegex.IsMatch(identifier);
        }
    }
}
