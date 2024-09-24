using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordVault.Domain.Services
{
    public interface ICSVProcessingService
    {
        public void CSVStreamReaderToSQL(StreamReader streamReader, string tableName, string connectionString = "", string sqlType = "");
    }
}
