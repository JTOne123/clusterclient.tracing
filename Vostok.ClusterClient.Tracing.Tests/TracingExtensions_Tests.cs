using System.IO;
using NSubstitute;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Model;
using Vostok.Tracing.Extensions.Http;

namespace Vostok.Clusterclient.Tracing.Tests
{
    [TestFixture]
    internal class TracingExtensions_Tests
    {
        private IHttpRequestSpanBuilder builder;
        private Request request;
        private Response response;

        [SetUp]
        public void TestSetup()
        {
            builder = Substitute.For<IHttpRequestSpanBuilder>();
            request = Request.Get("foo/bar");
            response = Responses.Ok;
        }

        [Test]
        public void SetRequestDetails_should_record_request_url_and_method()
        {
            builder.SetRequestDetails(request);

            builder.Received(1).SetRequestDetails(request.Url, request.Method, null);
        }

        [Test]
        public void SetRequestDetails_should_respect_buffer_content_length()
        {
            request = request.WithContent(new byte[10]);

            builder.SetRequestDetails(request);

            builder.Received(1).SetRequestDetails(request.Url, request.Method, 10L);
        }

        [Test]
        public void SetRequestDetails_should_respect_composite_content_length()
        {
            request = request.WithContent(new [] {new byte[10], new byte[11]});

            builder.SetRequestDetails(request);

            builder.Received(1).SetRequestDetails(request.Url, request.Method, 21L);
        }

        [Test]
        public void SetRequestDetails_should_respect_stream_content_length()
        {
            request = request.WithContent(new StreamContent(Stream.Null, 31));

            builder.SetRequestDetails(request);

            builder.Received(1).SetRequestDetails(request.Url, request.Method, 31L);
        }

        [Test]
        public void SetResponseDetails_should_record_response_code()
        {
            builder.SetResponseDetails(response);

            builder.Received(1).SetResponseDetails(200, null);
        }

        [Test]
        public void SetResponseDetails_should_respect_response_content_length()
        {
            response = response.WithContent(new byte[123]);

            builder.SetResponseDetails(response);

            builder.Received(1).SetResponseDetails(200, 123L);
        }
    }
}