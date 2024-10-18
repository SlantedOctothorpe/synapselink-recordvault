using RecordVault.Application.Services;
using RecordVault.Domain.Enums;
using RecordVault.Domain.Services;

namespace RecordVault.Application.Factories
{
    public class SQLSchemaManagementFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public SQLSchemaManagementFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ISQLSchemaManagementService GetSQLSchemaManagementService(string sqlTypeParam = "")
        {
            var sqlTypeStr = sqlTypeParam;

            if (string.IsNullOrEmpty(sqlTypeStr))
            {
                sqlTypeStr = Environment.GetEnvironmentVariable("RecodVaultDBType") ?? "";
            }

            if (string.IsNullOrEmpty(sqlTypeStr))
            {
                throw new ArgumentNullException("SQL Type not provided");
            }

            var sqlType = Enum.TryParse<SQLImplementationEnum>(sqlTypeStr, out var sqlTypeResult) ? sqlTypeResult : SQLImplementationEnum.SQLServer;

            switch (sqlType)
            {
                case SQLImplementationEnum.SQLServer:
                    var sqlPersistance = _serviceProvider.GetService(typeof(SQLServerSchemaService)) as ISQLSchemaManagementService;
                    if (sqlPersistance == null)
                    {
                        throw new ArgumentException("SQL Server Persistence not found");
                    }

                    return sqlPersistance;
                default:
                    throw new ArgumentException($"SQL Implementation {sqlType} not supported");
            }
        }
    }
}
