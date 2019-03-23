using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Modules;
using Vostok.Clusterclient.Core.Strategies;
using Vostok.Tracing.Abstractions;

namespace Vostok.Clusterclient.Tracing.Tests
{
    [TestFixture]
    internal class TracingModule_Tests
    {
        private ITracer tracer;
        private ISpanBuilder spanBuilder;
        private IRequestContext context;

        private string targetService;
        private string targetEnvironment;
        private Request request;
        private Response response;

        private TracingModule module;

        [SetUp]
        public void TestSetup()
        {
            tracer = Substitute.For<ITracer>();
            tracer.CurrentContext.Returns(new TraceContext(Guid.NewGuid(), Guid.NewGuid()));
            tracer.BeginSpan().Returns(spanBuilder = Substitute.For<ISpanBuilder>());

            module = new TracingModule(new TracingConfiguration(tracer))
            {
                TargetServiceProvider = () => targetService,
                TargetEnvironmentProvider = () => targetEnvironment
            };

            targetService = Guid.NewGuid().ToString();
            targetEnvironment = Guid.NewGuid().ToString();

            request = Request.Get("foo/bar");
            response = Responses.Ok;

            context = Substitute.For<IRequestContext>();
            context.Request.Returns(_ => request);
            context.Parameters.Returns(RequestParameters.Empty.WithStrategy(new ParallelRequestStrategy(2)));
        }

        [Test]
        public void Should_not_record_any_specific_annotations_when_trace_context_is_null_after_beginning_a_span()
        {
            tracer.CurrentContext.ReturnsNull();

            Run();

            spanBuilder.ReceivedCalls().Should().HaveCount(2);
        }

        [Test]
        public void Should_record_request_annotations()
        {
            Run();

            spanBuilder.Received(1).SetAnnotation(WellKnownAnnotations.Http.Request.Method, request.Method);
            spanBuilder.Received(1).SetAnnotation(WellKnownAnnotations.Http.Request.Url, request.Url.ToString());
            spanBuilder.Received(1).SetAnnotation(WellKnownAnnotations.Http.Request.TargetService, targetService);
            spanBuilder.Received(1).SetAnnotation(WellKnownAnnotations.Http.Request.TargetEnvironment, targetEnvironment);
        }

        [Test]
        public void Should_record_response_annotations()
        {
            Run();

            spanBuilder.Received(1).SetAnnotation(WellKnownAnnotations.Http.Response.Code, 200);
        }

        [Test]
        public void Should_record_cluster_annotations()
        {
            Run();

            spanBuilder.Received(1).SetAnnotation(WellKnownAnnotations.Http.Cluster.Status, "Success");
            spanBuilder.Received(1).SetAnnotation(WellKnownAnnotations.Http.Cluster.Strategy, "Parallel-2");
        }

        [Test]
        public void Should_record_common_annotations()
        {
            Run();

            spanBuilder.Received(1).SetAnnotation(WellKnownAnnotations.Common.Kind, WellKnownSpanKinds.HttpRequest.Cluster);
            spanBuilder.Received(1).SetAnnotation(WellKnownAnnotations.Common.Component, Constants.Component);
            spanBuilder.Received(1).SetAnnotation(WellKnownAnnotations.Common.Operation, "GET: foo/bar");
        }

        private void Run() => module
            .ExecuteAsync(context, _ => Task.FromResult(new ClusterResult(ClusterResultStatus.Success, new List<ReplicaResult>(), response, request)))
            .GetAwaiter()
            .GetResult();
    }
}