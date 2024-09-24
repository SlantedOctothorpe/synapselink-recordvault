using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using RecordVault.Domain.Services;
using RecordVault.DTOs;

using System.Text.Json;

namespace RecordVault.Functions
{
    public class SyncCSVByURLFunction
    {
        private readonly ILogger<SyncCSVByURLFunction> _logger;
        private readonly IDataSyncService _dataSyncService;

        public SyncCSVByURLFunction(ILogger<SyncCSVByURLFunction> logger, IDataSyncService dataSyncService)
        {
            _logger = logger;
            _dataSyncService = dataSyncService;
        }

        [Function("SyncCSVByURL")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("Received request to process csv file");

                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogInformation(requestBody);

                var requestData = JsonSerializer.Deserialize<SyncCSVByURLDTO>(requestBody);
                if (requestData == null)
                {
                    return new BadRequestObjectResult("Invalid request body");
                }

                var fileURL = requestData.FileURL;

                await _dataSyncService.SyncStorageAccountFile(fileURL);

                _logger.LogInformation($"Processing file {fileURL}");

                return new OkObjectResult("Successfully processed request");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.ToString());

                return new BadRequestObjectResult(ex.ToString());
            }
        }
    }
}
