using System.Data;

namespace DBTools.StoredProcedures
{
    /// <summary>
    /// Represents a parameter for a stored procedure call.
    /// Supports input, output, input/output, and return value directions.
    /// </summary>
    public class StoredProcedureParameter
    {
        /// <summary>
        /// The parameter name (without prefix).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The parameter value. For output-only parameters, this may be null.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// The parameter direction (Input, Output, InputOutput, ReturnValue).
        /// </summary>
        public ParameterDirection Direction { get; set; }

        /// <summary>
        /// The database type of the parameter. Optional; if null, the provider infers the type.
        /// </summary>
        public DbType? DbType { get; set; }

        /// <summary>
        /// The size of the parameter. Used primarily for output parameters to specify buffer size.
        /// </summary>
        public int? Size { get; set; }

        /// <summary>
        /// Creates an input parameter with the specified name and value.
        /// </summary>
        /// <param name="name">The parameter name</param>
        /// <param name="value">The parameter value</param>
        /// <returns>A configured StoredProcedureParameter</returns>
        public static StoredProcedureParameter Input(string name, object value)
        {
            return new StoredProcedureParameter
            {
                Name = name,
                Value = value,
                Direction = ParameterDirection.Input
            };
        }

        /// <summary>
        /// Creates an output parameter with the specified name, type, and optional size.
        /// </summary>
        /// <param name="name">The parameter name</param>
        /// <param name="dbType">The database type for the output value</param>
        /// <param name="size">Optional size for the output buffer</param>
        /// <returns>A configured StoredProcedureParameter</returns>
        public static StoredProcedureParameter Output(string name, DbType dbType, int? size = null)
        {
            return new StoredProcedureParameter
            {
                Name = name,
                Value = null,
                Direction = ParameterDirection.Output,
                DbType = dbType,
                Size = size
            };
        }

        /// <summary>
        /// Creates an input/output parameter with the specified name, value, type, and optional size.
        /// </summary>
        /// <param name="name">The parameter name</param>
        /// <param name="value">The initial input value</param>
        /// <param name="dbType">The database type</param>
        /// <param name="size">Optional size for the output buffer</param>
        /// <returns>A configured StoredProcedureParameter</returns>
        public static StoredProcedureParameter InputOutput(string name, object value, DbType dbType, int? size = null)
        {
            return new StoredProcedureParameter
            {
                Name = name,
                Value = value,
                Direction = ParameterDirection.InputOutput,
                DbType = dbType,
                Size = size
            };
        }

        /// <summary>
        /// Creates a return value parameter. The name defaults to "@RETURN_VALUE".
        /// </summary>
        /// <returns>A configured StoredProcedureParameter for capturing the return value</returns>
        public static StoredProcedureParameter ReturnValue()
        {
            return new StoredProcedureParameter
            {
                Name = "@RETURN_VALUE",
                Value = null,
                Direction = ParameterDirection.ReturnValue,
                DbType = System.Data.DbType.Int32
            };
        }
    }
}
