using Vostok.Clusterclient.Core.Model;
using Vostok.Tracing.Extensions.Http;

namespace Vostok.Clusterclient.Tracing
{
    internal static class TracingExtensions
    {
        public static void SetRequestDetails(this IHttpRequestSpanBuilder builder, Request request)
        {
            builder.SetRequestDetails(
                request.Url,
                request.Method,
                request.Content?.Length ??
                request.CompositeContent?.Length ??
                request.StreamContent?.Length);
        }

        public static void SetResponseDetails(this IHttpRequestSpanBuilder builder, Response response)
            => builder.SetResponseDetails((int)response.Code, ContentLength(response));

        private static long? ContentLength(Response response)
        {
            if (response.HasContent)
                return response.Content.Length;

            if (long.TryParse(response.Headers[HeaderNames.ContentLength], out var length))
                return length;

            return null;
        }
    }
}