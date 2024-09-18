using Azure.Storage.Files.Shares;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionAzureHW
{
    public class QueueFileProcessor
    {
        private readonly ILogger _logger;
        private const string LocalPath = @"";

        public QueueFileProcessor(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<QueueFileProcessor>();
        }

        [Function(nameof(QueueFileProcessor))]
        public async Task Run([QueueTrigger("myqueue-items")] string myQueueItem, FunctionContext context)
        {
            var logger = context.GetLogger("QueueFileProcessor");
            try
            {
                logger.LogInformation($"Received message: {myQueueItem}");

                var storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                var fileShareName = Environment.GetEnvironmentVariable("FileShareName");
                var shareFileClient = new ShareFileClient(storageConnectionString, fileShareName, myQueueItem);

                if (await shareFileClient.ExistsAsync())
                {
                    logger.LogInformation($"File '{myQueueItem}' found! Downloading");

                    string localFilePath = Path.Combine(LocalPath, myQueueItem);
                    var downloadResponse = await shareFileClient.DownloadAsync();
                    using var fileStream = File.OpenWrite(localFilePath);

                    await downloadResponse.Value.Content.CopyToAsync(fileStream);
                    logger.LogInformation($"File '{myQueueItem}' downloaded to `{localFilePath}`");
                }
                else
                {
                    logger.LogWarning($"File '{myQueueItem}' not found!");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error while processing the message '{myQueueItem}': {ex.Message}");
            }
        }
    }
}
