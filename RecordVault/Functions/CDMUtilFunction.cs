using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using RecordVault.Domain.Services;

namespace RecordVault.Functions
{
    public class CDMUtilFunction
    {
        private readonly ILogger<CDMUtilFunction> _logger;
        private readonly ICDMService _cdmService;

        public CDMUtilFunction(ILogger<CDMUtilFunction> logger, ICDMService cdmService)
        {
            _logger = logger;
            _cdmService = cdmService;
        }

        [Function("CDMUtilFunction")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            // TODO This is a test function, remove this later

            await _cdmService.GetCDMEntityMetadataList();

            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
