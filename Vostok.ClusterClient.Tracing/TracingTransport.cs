﻿using System;
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
        private readonly ITracer tracer;

        public TracingTransport([NotNull] ITransport transport, [NotNull] ITracer tracer)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        }

        public TransportCapabilities Capabilities => transport.Capabilities;

        [CanBeNull]
        public Func<string> TargetServiceProvider { get; set; }

        [CanBeNull]
        public Func<string> TargetEnvironmentProvider { get; set; }

        public async Task<Response> SendAsync(Request request, TimeSpan? connectionTimeout, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using (var spanBuilder = tracer.BeginHttpClientSpan())
            {
                spanBuilder.SetTargetDetails(TargetServiceProvider?.Invoke(), TargetEnvironmentProvider?.Invoke());
                spanBuilder.SetRequestDetails(request);
                spanBuilder.SetAnnotation(WellKnownAnnotations.Common.Component, Constants.Component);

                var response = await transport
                    .SendAsync(request, connectionTimeout, timeout, cancellationToken)
                    .ConfigureAwait(false);

                spanBuilder.SetResponseDetails(response);

                return response;
            }
        }
    }
}