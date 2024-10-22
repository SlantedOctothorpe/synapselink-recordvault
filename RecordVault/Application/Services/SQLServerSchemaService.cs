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

        public string GenerateMergeCode(SqlCdmTable baseTable, SqlCdmTable stagingTable)
        {
            var mergeCode = new StringBuilder();

            var baseTableName = baseTable.TableName;
            var stagingTableName = stagingTable.TableName;

            var containsIsDeleteColumn = baseTable.Columns.Any(c => string.Equals(c.ColumnName, "isdelete", StringComparison.OrdinalIgnoreCase));

            var rowNumPartitionByColumnList = GetRowNumPartitionByColumnList(baseTable.Columns);
            var rowNumOrderByColumnList = GetRowNumOrderByColumnList(baseTable.Columns);

            var sourceJoinColumnList = GetSourceJoinColumnList(baseTable.Columns);
            var matchedConditionColumnList = GetMatchedConditionColumnList(baseTable.Columns);
            
            if (rowNumPartitionByColumnList.IsNullOrEmpty()
                || rowNumOrderByColumnList.IsNullOrEmpty()
                || sourceJoinColumnList.IsNullOrEmpty()
                || matchedConditionColumnList.IsNullOrEmpty())
            {
                // No valid column found log error
                // TODO: Log error
                return "";
            }

            var updateColumnList = GetUpdateColumnList(baseTable.Columns);
            var insertColumnList = GetInsertColumnList(baseTable.Columns);
            var insertValuesList = GetInsertValuesList(baseTable.Columns);

            var mergeDataWhereClause = containsIsDeleteColumn ? " WHERE IsDelete = 0" : "";

            mergeCode.AppendLine("WITH mergeData AS (");
            mergeCode.AppendLine($"SELECT *, ROW_NUMBER() OVER (PARTITION BY {rowNumPartitionByColumnList} ORDER BY {rowNumOrderByColumnList}) rowNum FROM {stagingTableName}{mergeDataWhereClause}");
            //mergeCode.AppendLine($"SELECT *, ROW_NUMBER() OVER (PARTITION BY Id ORDER BY versionnumber DESC, SinkModifiedOn DESC) rowNum FROM {stagingTableName} WHERE IsDelete = 0");
            mergeCode.AppendLine(")");
            mergeCode.AppendLine($"MERGE INTO {baseTableName} AS tgt");
            mergeCode.AppendLine($"USING mergeData AS src ON {sourceJoinColumnList} AND src.rowNum = 1");
            //mergeCode.AppendLine($"USING mergeData AS src ON tgt.Id = src.Id AND src.rowNum = 1");
            mergeCode.AppendLine($"WHEN MATCHED AND {matchedConditionColumnList} THEN");
            //mergeCode.AppendLine("WHEN MATCHED AND tgt.SinkModifiedOn <> src.SinkModifiedOn THEN");
            mergeCode.AppendLine("UPDATE SET");
            mergeCode.AppendLine(updateColumnList);
            mergeCode.AppendLine("WHEN NOT MATCHED BY TARGET THEN");
            mergeCode.AppendLine($"INSERT ({insertColumnList})");
            mergeCode.AppendLine($"VALUES ({insertValuesList});");

            if (containsIsDeleteColumn)
            {
                mergeCode.AppendLine("");

                // Only update IsDelete based on the latest change (should be deleted)
                mergeCode.AppendLine("WITH deleteData AS (");
                mergeCode.AppendLine($"SELECT *, ROW_NUMBER() OVER (PARTITION BY {rowNumPartitionByColumnList} ORDER BY {rowNumOrderByColumnList}) rowNum FROM {stagingTableName}");
                mergeCode.AppendLine(")");
                mergeCode.AppendLine("UPDATE tgt SET IsDelete = src.IsDelete");
                mergeCode.AppendLine($"FROM {baseTableName} tgt");
                mergeCode.AppendLine($"JOIN deleteData src ON {sourceJoinColumnList} AND src.rowNum = 1");
                mergeCode.AppendLine("WHERE src.IsDelete = 1");
            }

            string mergeCodeOutput = mergeCode.ToString();
            return mergeCodeOutput;
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
                var maxLength = (int)columnSizeField;

                var numericPrecisionField = row["NumericPrecision"];
                var precision = Convert.ToInt32(numericPrecisionField);

                var scaleField = row["NumericScale"];
                var scale = Convert.ToInt32(scaleField);

                var isNullableField = row["AllowDBNull"];
                var isNullable = (bool)isNullableField;

                var sqlColumn = new SQLColumn(columnName, dataType, isNullable, maxLength, precision, scale);
                columns.Add(columnName, sqlColumn);
            }

            return columns;
        }

        private string GetRowNumPartitionByColumnList(IEnumerable<SQLCdmColumn> columns)
        {
            var identityColumns = GetMergeIdentityColumns(columns);
            var partitionByColumns = identityColumns.Select(c => c.ColumnName);

            if (!partitionByColumns.Any())
            {
                // No valid column found log error
                // TODO: Log error
                return "";
            }

            var partitionByColumnList = string.Join(", ", partitionByColumns);
            return partitionByColumnList;
        }

        private string GetRowNumOrderByColumnList(IEnumerable<SQLCdmColumn> columns)
        {
            // Current ORDER BY approach uses one version column and one modified date column
            // This can be extended to support different version columns and modified date columns

            var versionColumn = GetMergeVersionColumn(columns);
            var modifiedDateColumn = GetMergeModifiedOnColumn(columns);

            if (versionColumn.IsNullOrEmpty() || modifiedDateColumn.IsNullOrEmpty())
            {
                // No valid column found log error
                // TODO: Log error
                return "";
            }

            var orderByColumnList = $"{versionColumn} DESC, {modifiedDateColumn} DESC";
            return orderByColumnList;
        }

        private string GetSourceJoinColumnList(IEnumerable<SQLCdmColumn> columns, string targetAlias = "tgt", string sourceAlias = "src")
        {
            var identityColumns = GetMergeIdentityColumns(columns);
            if (!identityColumns.Any())
            {
                // No valid column found log error
                // TODO: Log error
                return "";
            }

            var joinColumnsScript = string.Join(" AND ", identityColumns.Select(c => $"{targetAlias}.{c.ColumnName} = {sourceAlias}.{c.ColumnName}"));

            return joinColumnsScript;
        }

        private string GetMatchedConditionColumnList(IEnumerable<SQLCdmColumn> columns, string targetAlias = "tgt", string sourceAlias = "src")
        {
            var versionColumn = GetMergeVersionColumn(columns);
            var modifiedDateColumn = GetMergeModifiedOnColumn(columns);

            if (versionColumn.IsNullOrEmpty() || modifiedDateColumn.IsNullOrEmpty())
            {
                // No valid column found log error
                // TODO: Log error
                return "";
            }

            var matchedConditionColumnsScript = $"{targetAlias}.{versionColumn} <> {sourceAlias}.{versionColumn} OR {targetAlias}.{modifiedDateColumn} <> {sourceAlias}.{modifiedDateColumn}";
            //var matchedConditionColumnsScript = string.Join(" OR ", columns.Select(c => $"{targetAlias}.{c.ColumnName} <> {sourceAlias}.{c.ColumnName}"));

            return matchedConditionColumnsScript;
        }

        private List<SQLCdmColumn> GetMergeIdentityColumns(IEnumerable<SQLCdmColumn> columns)
        {
            var identityColumn = columns.FirstOrDefault(c =>
                string.Equals(c.ColumnName, "id", StringComparison.OrdinalIgnoreCase)
                || string.Equals(c.ColumnName, "recid", StringComparison.OrdinalIgnoreCase)
            );

            if (identityColumn == null)
            {
                // No valid column found log error
                // TODO: Log error
                return [];
            }

            var identityColumns = new List<SQLCdmColumn>
            {
                identityColumn
            };

            return identityColumns;
        }

        private string GetMergeVersionColumn(IEnumerable<SQLCdmColumn> columns)
        {
            var versionColumn = columns
                .Select(c => c.ColumnName)
                .FirstOrDefault(name =>
                    string.Equals(name, "versionnumber", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name, "sysrowversion", StringComparison.OrdinalIgnoreCase)
                );

            if (versionColumn.IsNullOrEmpty())
            {
                // No valid column found log error
                // TODO: Log error
                return "";
            }

            return versionColumn ?? "";
        }

        private string GetMergeModifiedOnColumn(IEnumerable<SQLCdmColumn> columns)
        {
            var modifiedOnColumn = columns
                .Select(c => c.ColumnName)
                .FirstOrDefault(name =>
                    string.Equals(name, "sinkmodifiedon", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name, "modifiedon", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name, "modifieddatetime", StringComparison.OrdinalIgnoreCase)
                );

            if (modifiedOnColumn.IsNullOrEmpty())
            {
                // No valid column found log error
                // TODO: Log error
                return "";
            }

            return modifiedOnColumn ?? "";
        }

        private string GetUpdateColumnList(IEnumerable<SQLCdmColumn> columns, string targetAlias = "tgt", string sourceAlias = "src")
        {
            var updateColumnsScript = string.Join(", ", columns.Select(c => $"{targetAlias}.{c.ColumnName} = {sourceAlias}.{c.ColumnName}"));

            return updateColumnsScript;
        }

        private string GetInsertColumnList(IEnumerable<SQLCdmColumn> columns)
        {
            var insertColumnsScript = string.Join(", ", columns.Select(c => c.ColumnName));

            return insertColumnsScript;
        }

        private string GetInsertValuesList(IEnumerable<SQLCdmColumn> columns, string sourceAlias = "src")
        {
            var insertValuesScript = string.Join(", ", columns.Select(c => $"{sourceAlias}.{c.ColumnName}"));

            return insertValuesScript;
        }

        #endregion
    }
}
