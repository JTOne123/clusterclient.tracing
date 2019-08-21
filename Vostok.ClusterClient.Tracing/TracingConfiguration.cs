using System;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;
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
        /// If set to a non-null value, an additional request transformation will be called with current <see cref="TraceContext"/>.
        /// </summary>
        [CanBeNull]
        public Func<Request, TraceContext, Request> AdditionalRequestTransformation { get; set; }
    }
}