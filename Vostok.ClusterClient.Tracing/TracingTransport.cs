using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Transport;
using Vostok.Tracing.Abstractions;
using Vostok.Tracing.Extensions.Http;

namespace Vostok.Clusterclient.Tracing
{
    /// <summary>
    /// A decorator that can be applied to arbitrary <see cref="ITransport"/> instance to add HTTP client requests tracing.
    /// </summary>
    [PublicAPI]
    public class TracingTransport : ITransport
    {
        private readonly ITransport transport;
        private readonly TracingConfiguration configuration;

        public TracingTransport([NotNull] ITransport transport, [NotNull] TracingConfiguration configuration)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public TransportCapabilities Capabilities => transport.Capabilities;

        [CanBeNull]
        public Func<string> TargetServiceProvider { get; set; }

        [CanBeNull]
        public Func<string> TargetEnvironmentProvider { get; set; }

        public async Task<Response> SendAsync(Request request, TimeSpan? connectionTimeout, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using (var spanBuilder = configuration.Tracer.BeginHttpClientSpan())
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

                var response = await transport
                    .SendAsync(request, connectionTimeout, timeout, cancellationToken)
                    .ConfigureAwait(false);

                if (traceContext != null)
                {
                    spanBuilder.SetResponseDetails(response);
                }

                return response;
            }
        }
    }
}
