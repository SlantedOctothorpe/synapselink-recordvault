using RecordVault.Domain.Enums;
using RecordVault.Domain.Persistence;
using RecordVault.Infrastructure.Persistence;

namespace RecordVault.Application.Factories
{
    public class SQLPersistenceFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public SQLPersistenceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ISQLPersistence GetSQLPersistence(string sqlTypeParam = "")
        {
            var sqlTypeStr = sqlTypeParam;

            if (string.IsNullOrEmpty(sqlTypeStr))
            {
                sqlTypeStr = Environment.GetEnvironmentVariable("RecodVaultDBType") ?? "";
            }

            if (string.IsNullOrEmpty(sqlTypeStr))
            {
                throw new ArgumentNullException(nameof(sqlTypeParam));
            }

            var sqlType = Enum.TryParse<SQLImplementationEnum>(sqlTypeStr, out var sqlTypeResult) ? sqlTypeResult : SQLImplementationEnum.SQLServer;

            switch (sqlType)
            {
                case SQLImplementationEnum.SQLServer:
                    var sqlPersistance = _serviceProvider.GetService(typeof(SQLServerPersistence)) as ISQLPersistence;
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
