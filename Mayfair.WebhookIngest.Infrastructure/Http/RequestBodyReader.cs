using System.Text;
using Microsoft.AspNetCore.Http;


namespace Mayfair.WebhookIngest.Infrastructure.Http
{
    public static class RequestBodyReader
    {
        public static async Task<string> ReadRawBodyAsync(
            HttpRequest request,
            CancellationToken cancellationToken)
        {
            request.EnableBuffering();

            request.Body.Position = 0;

            using var reader = new StreamReader(
                request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true);

            var body = await reader.ReadToEndAsync(cancellationToken);

            request.Body.Position = 0;

            return body;
        }
    }
}
