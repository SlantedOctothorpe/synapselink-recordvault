using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordVault.Domain.ValueObjects
{
    public class EntitySyncPackage
    {
        public string EntityName { get; set; }

        public List<string> EntityFileURLs { get; set; }
    }
}
