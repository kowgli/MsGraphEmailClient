using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace MsGraphEmailClient
{
    public record MailFolderItem(string Id, string DisplayName);

    public record MessageSummary(
        string Id,
        string Subject,
        string From,
        DateTimeOffset? ReceivedDateTime,
        bool IsRead);

    public record MessagesPage(List<MessageSummary> Messages, string? NextLink);

    public class GraphService
    {
        private readonly GraphServiceClient _graphClient;
        private readonly string _mailboxEmail;

        public GraphService(string tenantId, string clientId, string clientSecret, string mailboxEmail)
        {
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            _graphClient = new GraphServiceClient(credential, ["https://graph.microsoft.com/.default"]);
            _mailboxEmail = mailboxEmail;
        }

        public async Task<List<MailFolderItem>> GetFoldersAsync()
        {
            var result = await _graphClient
                .Users[_mailboxEmail]
                .MailFolders
                .GetAsync(c =>
                {
                    c.QueryParameters.Top = 100;
                    c.QueryParameters.Select = ["id", "displayName"];
                });

            var folders = new List<MailFolderItem>();
            var pageIterator = PageIterator<MailFolder, MailFolderCollectionResponse>
                .CreatePageIterator(_graphClient, result!, folder =>
                {
                    folders.Add(new MailFolderItem(folder.Id!, folder.DisplayName!));
                    return true;
                });
            await pageIterator.IterateAsync();
            return folders;
        }

        public async Task<MessagesPage> GetMessagesAsync(string folderId, int top = 50)
        {
            var result = await _graphClient
                .Users[_mailboxEmail]
                .MailFolders[folderId]
                .Messages
                .GetAsync(c =>
                {
                    c.QueryParameters.Top = top;
                    c.QueryParameters.Select = ["id", "subject", "from", "receivedDateTime", "isRead"];
                    c.QueryParameters.Orderby = ["receivedDateTime desc"];
                });

            return new MessagesPage(Map(result?.Value), result?.OdataNextLink);
        }

        // Follows the @odata.nextLink from a previous GetMessagesAsync / GetMoreMessagesAsync call.
        // WithUrl replaces the URL entirely so the _mailboxEmail prefix is irrelevant here.
        public async Task<MessagesPage> GetMoreMessagesAsync(string nextLink)
        {
            var result = await _graphClient
                .Users[_mailboxEmail]
                .Messages
                .WithUrl(nextLink)
                .GetAsync();

            return new MessagesPage(Map(result?.Value), result?.OdataNextLink);
        }

        // Deletes in chunks of 4 with a 1 s pause between chunks.
        // Exchange Online counts each sub-request toward its concurrent-operation limit
        // regardless of batching, so bursting 20 at once reliably triggers 429s.
        // Within each chunk, any per-item 429s are collected and retried once after 5 s.
        // Returns the set of message IDs confirmed deleted (HTTP 204).
        public async Task<HashSet<string>> DeleteMessagesAsync(
            IList<string> messageIds, IProgress<int>? progress = null)
        {
            const int ChunkSize = 4;
            const int InterChunkDelayMs = 1000;

            var deleted = new HashSet<string>();
            var queue = new Queue<string>(messageIds);
            int done = 0;

            while (queue.Count > 0)
            {
                var chunk = new List<string>(ChunkSize);
                while (chunk.Count < ChunkSize && queue.Count > 0)
                    chunk.Add(queue.Dequeue());

                var throttled = await PostDeleteBatchAsync(chunk, deleted);

                if (throttled.Count > 0)
                {
                    await Task.Delay(5000);
                    await PostDeleteBatchAsync(throttled, deleted);
                }

                done += chunk.Count;
                progress?.Report(done);

                if (queue.Count > 0)
                    await Task.Delay(InterChunkDelayMs);
            }

            return deleted;
        }

        private async Task<List<string>> PostDeleteBatchAsync(
            IList<string> ids, HashSet<string> deleted)
        {
            // One BatchRequestContentCollection per chunk: with ≤4 items it creates exactly
            // one internal batch of 4, avoiding the obsolete BatchRequestContent API while
            // still sending a single HTTP request per chunk.
            var batch = new BatchRequestContentCollection(_graphClient);
            var stepToId = new Dictionary<string, string>();

            foreach (var id in ids)
            {
                var req = _graphClient.Users[_mailboxEmail].Messages[id]
                    .ToDeleteRequestInformation();
                var stepId = await batch.AddBatchRequestStepAsync(req);
                stepToId[stepId] = id;
            }

            var response = await _graphClient.Batch.PostAsync(batch);
            var statuses = await response!.GetResponsesStatusCodesAsync();

            var throttled = new List<string>();
            foreach (var (stepId, status) in statuses)
            {
                var id = stepToId[stepId];
                if ((int)status == 204)      deleted.Add(id);
                else if ((int)status == 429) throttled.Add(id);
                // other errors (4xx/5xx): skip, don't retry
            }
            return throttled;
        }

        public async Task<string> GetMessageBodyAsync(string messageId)
        {
            var msg = await _graphClient
                .Users[_mailboxEmail]
                .Messages[messageId]
                .GetAsync(c => c.QueryParameters.Select = ["body"]);

            var body = msg?.Body;
            if (body is null) return "<p>(no content)</p>";

            if (body.ContentType == BodyType.Html)
                return body.Content ?? "<p>(empty)</p>";

            var escaped = System.Net.WebUtility.HtmlEncode(body.Content ?? string.Empty);
            return $"<pre style='font-family:Calibri,sans-serif'>{escaped}</pre>";
        }

        private static List<MessageSummary> Map(IList<Microsoft.Graph.Models.Message>? value)
        {
            if (value is null) return [];
            return value.Select(m => new MessageSummary(
                Id: m.Id!,
                Subject: m.Subject ?? "(no subject)",
                From: m.From?.EmailAddress?.Address ?? "(unknown)",
                ReceivedDateTime: m.ReceivedDateTime,
                IsRead: m.IsRead ?? true)).ToList();
        }
    }
}
