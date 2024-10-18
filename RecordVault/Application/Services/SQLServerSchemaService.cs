using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

using RecordVault.Application.Factories;
using RecordVault.Domain.Persistence;
using RecordVault.Domain.Services;
using RecordVault.Domain.ValueObjects;
using System.Data;
using System.Text;

namespace RecordVault.Application.Services
{
    public class SQLServerSchemaService : ISQLSchemaManagementService
    {
        private readonly ILogger<SQLServerSchemaService> _logger;
        private readonly ISQLPersistence _sqlPersistence;

        public SQLServerSchemaService(ILogger<SQLServerSchemaService> logger, SQLPersistenceFactory sqlPersistenceFactory)
        {
            _logger = logger;

            // SQL Server is the only supported SQL type for this implementation
            var sqlPersistence = sqlPersistenceFactory.GetSQLPersistence();
            _sqlPersistence = sqlPersistence;
        }

        public void CreateOrUpdateTable(SqlCdmTable table)
        {
            var tableName = table.TableName;

            var sqlConn = _sqlPersistence.GetSQLConnection();
            var tableExists = _sqlPersistence.CheckTableExists(tableName, sqlConn);

            var schemaChangeQuery = "";
            var changeRequired = false;

            if (tableExists)
            {
                var currentTableSchema = _sqlPersistence.GetSQLTableSchema(tableName, sqlConn);

                // Compare current table schema with new table schema
                // Update table schema if necessary
                (schemaChangeQuery, changeRequired) = GenerateAlterTableScript(tableName, table.Columns, currentTableSchema);
            }
            else
            {
                // Create table
                schemaChangeQuery = GenerateCreateTableScript(tableName, table.Columns);
                changeRequired = true;
            }

            if (changeRequired)
            {
                _sqlPersistence.ExecuteNonQuery(schemaChangeQuery, sqlConn);
            }
        }

        public void CreateOrUpdateMergeCode(SqlCdmTable originalTable, SqlCdmTable stagingTable)
        {
            throw new NotImplementedException();
        }

        #region Private Methods

        private string GenerateCreateTableScript(string tableName, IEnumerable<SQLCdmColumn> columns)
        {
            var script = new StringBuilder();

            script.Append($"CREATE TABLE {tableName} (");

            foreach (var column in columns)
            {
                var columnSQLDataType = CdmDataTypeToSQLDataType(column.DataType, column.MaxLength, column.Precision, column.Scale);
                var columnScript = $"{column.ColumnName} {columnSQLDataType}";

                if (column.IsNullable == false)
                {
                    columnScript += " NOT NULL";
                }

                columnScript += ", ";

                script.Append(columnScript);
            }

            script.Append(");");

            var ret = script.ToString();
            return ret;
        }

        private (string, bool) GenerateAlterTableScript(string tableName, IEnumerable<SQLCdmColumn> newColumns, DataTable currentTableSchema)
        {
            var currentSQLColumns = SchemaDataTableToSQLColumns(currentTableSchema);

            var alterTableScriptBuilder = new StringBuilder();
            foreach (var column in newColumns)
            {
                var columnSQLDataType = CdmDataTypeToSQLDataType(column.DataType, column.MaxLength, column.Precision, column.Scale);
                var columnBaseSQLDataType = CdmDataTypeToBaseSQLDataType(column.DataType);

                if (currentSQLColumns.TryGetValue(column.ColumnName, out SQLColumn? value))
                {
                    // Column exists in current table schema
                    var currentColumn = value;

                    var updateColumn = false;

                    if (!currentColumn.DataType.Equals(columnBaseSQLDataType, StringComparison.CurrentCultureIgnoreCase))
                    {
                        updateColumn = true;
                    }
                    else
                    {
                        if (IsMaxLengthUsedForDataType(columnBaseSQLDataType))
                        {
                            if (currentColumn.MaxLength != column.MaxLength)
                            {
                                updateColumn = true;
                            }
                        }
                        else if (IsMaxLengthUsedForDataType(columnBaseSQLDataType))
                        {
                            if (currentColumn.Precision != column.Precision || currentColumn.Scale != column.Scale)
                            {
                                updateColumn = true;
                            }
                        }
                    }

                    if (updateColumn)
                    {
                        // Column data type has changed
                        var alterColumnScript = $"ALTER TABLE {tableName} ALTER COLUMN {column.ColumnName} {columnSQLDataType};";
                        alterTableScriptBuilder.AppendLine(alterColumnScript);
                    }
                }
                else
                {
                    // Column does not exist in current table schema
                    var addColumnScript = $"ALTER TABLE {tableName} ADD {column.ColumnName} {columnSQLDataType};";
                    alterTableScriptBuilder.AppendLine(addColumnScript);
                }
            }

            var alterTableScript = alterTableScriptBuilder.ToString();
            return (alterTableScript, !alterTableScript.IsNullOrEmpty());
        }

        private string CdmDataTypeToSQLDataType(string cdmDataType, int maxLength, int numericPrecision, int numericScale)
        {
            var colDataType = CdmDataTypeToBaseSQLDataType(cdmDataType);

            if (IsMaxLengthUsedForDataType(colDataType))
            {
                if (maxLength > 0)
                {
                    colDataType = $"{colDataType}({maxLength})";
                }
                else
                {
                    colDataType = $"{colDataType}(MAX)";
                }
            }
            else if (IsPrecisionAndScaleUsedForDataType(colDataType))
            {
                colDataType = $"{colDataType}({numericPrecision}, {numericScale})";
            }

            return colDataType;
        }

        private string CdmDataTypeToBaseSQLDataType(string cdmDataType)
        {
            var baseDataType = cdmDataType.ToLower() switch
            {
                "string" => "NVARCHAR",
                "int" => "INT",
                "int64" => "BIGINT",
                "decimal" => "DECIMAL",
                "numeric" => "NUMERIC",
                "datetime" => "DATETIME",
                "datetime2" => "DATETIME2",
                "datetimeoffset" => "DATETIMEOFFSET",
                "boolean" => "BIT",
                "guid" => "UNIQUEIDENTIFIER",
                _ => "NVARCHAR"
            };

            return baseDataType;
        }

        private bool IsMaxLengthUsedForDataType(string dataType)
        {
            return dataType.ToLower() switch
            {
                "nvarchar" => true,
                "varchar" => true,
                "varbinary" => true,
                _ => false
            };
        }

        private bool IsPrecisionAndScaleUsedForDataType(string dataType)
        {
            return dataType.ToLower() switch
            {
                "decimal" => true,
                "numeric" => true,
                _ => false
            };
        }

        private Dictionary<string, SQLColumn> SchemaDataTableToSQLColumns(DataTable tableSchema)
        {
            var columns = new Dictionary<string, SQLColumn>();

            foreach (DataRow row in tableSchema.Rows)
            {
                var columnNameField = row["ColumnName"];
                var columnName = (string)columnNameField;
                var dataTypeField = row["DataTypeName"];
                var dataType = (string)dataTypeField;

                var columnSizeField = row["ColumnSize"];
                //var maxLengthResult = int.TryParse((string)columnSizeField, out var maxLenOut);
                //var maxLength = maxLengthResult ? maxLenOut : -1;
                var maxLength = (int)columnSizeField;

                var numericPrecisionField = row["NumericPrecision"];
                //var precisionResult = int.TryParse((string)numericPrecisionField, out var precisionOut);
                //var precision = precisionResult ? precisionOut : 0;
                var precision = Convert.ToInt32(numericPrecisionField);

                var scaleField = row["NumericScale"];
                //var scaleResult = int.TryParse((string)scaleField, out var scaleOut);
                //var scale = scaleResult ? scaleOut : 0;
                var scale = Convert.ToInt32(scaleField);

                var isNullableField = row["AllowDBNull"];
                //var isNullableResult = bool.TryParse((string)isNullableField, out var isNullableOut);
                //var isNullable = isNullableResult ? isNullableOut : true;
                var isNullable = (bool)isNullableField;

                var sqlColumn = new SQLColumn(columnName, dataType, isNullable, maxLength, precision, scale);
                columns.Add(columnName, sqlColumn);
            }

            return columns;
        }

        #endregion
    }
}
