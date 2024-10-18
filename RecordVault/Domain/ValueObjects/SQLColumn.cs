namespace RecordVault.Domain.ValueObjects
{
    // TODO defaults by config
    public class SQLColumn (string columnName, string dataType, bool? isNullable = true, int maxLength = -1, int precision = 0, int scale = 0)
    {
        public string ColumnName { get; private set; } = columnName;
        public string DataType { get; private set; } = dataType;
        public bool? IsNullable { get; private set; } = isNullable;
        public int MaxLength { get; private set; } = maxLength;
        public int Precision { get; private set; } = precision;
        public int Scale { get; private set; } = scale;
    }
}
