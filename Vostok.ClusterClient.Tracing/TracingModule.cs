using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Modules;
using Vostok.Tracing.Abstractions;
using Vostok.Tracing.Extensions.Http;

namespace Vostok.Clusterclient.Tracing
{
    /// <summary>
    /// A module that can be injected into <see cref="ClusterClient"/> pipeline in order to add HTTP cluster requests tracing.
    /// </summary>
    [PublicAPI]
    public class TracingModule : IRequestModule
    {
        private readonly ITracer tracer;

        public TracingModule([NotNull] ITracer tracer)
        {
            this.tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        }

        [CanBeNull]
        public Func<string> TargetServiceProvider { get; set; }

        [CanBeNull]
        public Func<string> TargetEnvironmentProvider { get; set; }

        public async Task<ClusterResult> ExecuteAsync(IRequestContext context, Func<IRequestContext, Task<ClusterResult>> next)
        {
            using (var spanBuilder = tracer.BeginHttpClusterSpan())
            {
                spanBuilder.SetTargetDetails(TargetServiceProvider?.Invoke(), TargetEnvironmentProvider?.Invoke());
                spanBuilder.SetRequestDetails(context.Request);
                spanBuilder.SetAnnotation(WellKnownAnnotations.Common.Component, Constants.Component);

                var strategy = context.Parameters.Strategy;
                if (strategy != null)
                    spanBuilder.SetClusterStrategy(strategy.ToString());

                var result = await next(context).ConfigureAwait(false);

                spanBuilder.SetResponseDetails(result.Response);
                spanBuilder.SetClusterStatus(result.Status.ToString());

                return result;
            }
        }
    }
}