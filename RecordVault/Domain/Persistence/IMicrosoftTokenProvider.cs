using Microsoft.CommonDataModel.ObjectModel.Utilities.Network;

namespace RecordVault.Domain.Persistence
{
    public interface IMicrosoftTokenProvider : TokenProvider, TokenProviderAsync
    {
    }
}
