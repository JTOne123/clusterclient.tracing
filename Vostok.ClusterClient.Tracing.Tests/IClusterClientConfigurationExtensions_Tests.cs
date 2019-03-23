using System;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Clusterclient.Core;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Topology;
using Vostok.Clusterclient.Core.Transport;
using Vostok.Context;
using Vostok.Logging.Abstractions;
using Vostok.Tracing;
using Vostok.Tracing.Abstractions;

namespace Vostok.Clusterclient.Tracing.Tests
{
    [TestFixture]
    internal class IClusterClientConfigurationExtensions_Tests
    {
        private ITracer tracer;
        private ITransport transport;
        private ClusterClient client;

        private Request observedRequest;
        private TraceContext observedContext;

        [SetUp]
        public void TestSetup()
        {
            tracer = new Tracer(new TracerSettings(new DevNullSpanSender()));
            transport = Substitute.For<ITransport>();
            transport
                .WhenForAnyArgs(t => t.SendAsync(default, default, default, default))
                .Do(info =>
                {
                    observedRequest = info.Arg<Request>();
                    observedContext = tracer.CurrentContext;
                });

            observedRequest = null;
            observedContext = null;

            client = new ClusterClient(new SilentLog(),
                config =>
                {
                    config.ClusterProvider = new FixedClusterProvider(new Uri("http://localhost:123/"));
                    config.Transport = transport;

                    config.SetupDistributedTracing(new TracingConfiguration(tracer)
                    {
                        AdditionalTraceIdHeader = "traceId",
                        AdditionalSpanIdHeader = "spanId"
                    });
                });
        }

        [Test]
        public void Should_send_trace_context_information_in_request_headers()
        {
            client.Send(Request.Get("foo/bar"));

            observedRequest.Should().NotBeNull();
            observedContext.Should().NotBeNull();

            observedRequest.Headers?["traceId"].Should().Be(observedContext.TraceId.ToString());
            observedRequest.Headers?["spanId"].Should().Be(observedContext.SpanId.ToString());

            FlowingContext.RestoreDistributedGlobals(observedRequest.Headers?[HeaderNames.ContextGlobals] ?? string.Empty);

            tracer.CurrentContext.Should().BeEquivalentTo(observedContext);
        }
    }
}