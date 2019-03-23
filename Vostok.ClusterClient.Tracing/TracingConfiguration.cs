using System;
using JetBrains.Annotations;
using Vostok.Tracing.Abstractions;

namespace Vostok.Clusterclient.Tracing
{
    [PublicAPI]
    public class TracingConfiguration
    {
        public TracingConfiguration([NotNull] ITracer tracer)
        {
            Tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        }

        [NotNull]
        public ITracer Tracer { get; }

        /// <summary>
        /// If set to a non-null value, an additional header with this name and the value of <see cref="TraceContext"/>'s <see cref="TraceContext.TraceId"/> will be added to requests.
        /// </summary>
        [CanBeNull]
        public string AdditionalTraceIdHeader { get; set; }

        /// <summary>
        /// If set to a non-null value, an additional header with this name and the value of <see cref="TraceContext"/>'s <see cref="TraceContext.SpanId"/> will be added to requests.
        /// </summary>
        [CanBeNull]
        public string AdditionalSpanIdHeader { get; set; }
    }
}