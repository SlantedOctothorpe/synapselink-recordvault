using Microsoft.IdentityModel.Tokens;

using RecordVault.Domain.Persistence;
using RecordVault.Infrastructure.Persistence;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            var sqlType = sqlTypeParam;

            if (sqlType.IsNullOrEmpty())
            {
                sqlType = Environment.GetEnvironmentVariable("RecodVaultDBType") ?? "";
            }

            if (sqlType.IsNullOrEmpty())
            {
                throw new ArgumentException("SQL Type not provided");
            }

            switch (sqlType)
            {
                case "SQLServer":
                    var sqlPersistance = _serviceProvider.GetService(typeof(SQLServerPersistence)) as ISQLPersistence;
                    if (sqlPersistance == null)
                    {
                        throw new ArgumentException("SQL Server Persistence not found");
                    }

                    return sqlPersistance;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
