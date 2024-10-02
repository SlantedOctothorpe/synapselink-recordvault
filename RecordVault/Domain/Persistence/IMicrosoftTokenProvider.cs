using Microsoft.CommonDataModel.ObjectModel.Utilities.Network;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordVault.Domain.Persistence
{
    public interface IMicrosoftTokenProvider : TokenProvider, TokenProviderAsync
    {
    }
}
