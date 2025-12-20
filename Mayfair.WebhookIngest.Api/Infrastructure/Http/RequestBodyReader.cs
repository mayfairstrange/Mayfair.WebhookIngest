using System.Text;

namespace Mayfair.WebhookIngest.Api.Infrastructure.Http
{
    public static class RequestBodyReader
    {
        public static async Task<string> ReadRawBodyAsync(HttpRequest request, CancellationToken cancellationToken)
        {
            request.EnableBuffering();
            request.Body.Position = 0;

            using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var body = await reader.ReadToEndAsync(cancellationToken);

            request.Body.Position = 0;
            return body;
        }
    }
}
