using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Sending;
using Vostok.Logging.Context;
using Vostok.Logging.Tracing;
using Vostok.Tracing.Abstractions;
using Vostok.Tracing.Extensions.Http;

namespace Vostok.Clusterclient.Tracing
{
    [PublicAPI]
    public class TracingRequestSender : IRequestSender
    {
        private readonly IRequestSender requestSender;
        private readonly TracingConfiguration configuration;

        public TracingRequestSender([NotNull] IRequestSender requestSender, [NotNull] TracingConfiguration configuration)
        {
            this.requestSender = requestSender;
            this.configuration = configuration;
        }

        [CanBeNull]
        public Func<string> TargetServiceProvider { get; set; }

        [CanBeNull]
        public Func<string> TargetEnvironmentProvider { get; set; }

        public async Task<ReplicaResult> SendToReplicaAsync(Uri replica, Request request, TimeSpan? connectionTimeout, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using (var spanBuilder = configuration.Tracer.BeginHttpClientSpan())
            using (new OperationContextToken(TracingLogPropertiesFormatter.FormatPrefix(spanBuilder.CurrentSpan.SpanId) ?? string.Empty))
            {
                var traceContext = configuration.Tracer.CurrentContext;
                if (traceContext != null)
                {
                    spanBuilder.SetTargetDetails(TargetServiceProvider?.Invoke(), TargetEnvironmentProvider?.Invoke());
                    spanBuilder.SetRequestDetails(request);
                    spanBuilder.SetAnnotation(WellKnownAnnotations.Common.Component, Constants.Component);

                    if (configuration.AdditionalRequestTransformation != null)
                        request = configuration.AdditionalRequestTransformation(request, traceContext);
                }

                var response = await requestSender
                    .SendToReplicaAsync(replica, request, connectionTimeout, timeout, cancellationToken)
                    .ConfigureAwait(false);

                if (traceContext != null)
                {
                    spanBuilder.SetResponseDetails(response.Response);
                }

                return response;
            }
        }
    }
}