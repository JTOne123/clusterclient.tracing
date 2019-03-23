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
        private readonly TracingConfiguration config;

        public TracingModule([NotNull] TracingConfiguration config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }

        [CanBeNull]
        public Func<string> TargetServiceProvider { get; set; }

        [CanBeNull]
        public Func<string> TargetEnvironmentProvider { get; set; }

        public async Task<ClusterResult> ExecuteAsync(IRequestContext context, Func<IRequestContext, Task<ClusterResult>> next)
        {
            using (var spanBuilder = config.Tracer.BeginHttpClusterSpan())
            {
                var traceContext = config.Tracer.CurrentContext;
                if (traceContext != null)
                {
                    spanBuilder.SetTargetDetails(TargetServiceProvider?.Invoke(), TargetEnvironmentProvider?.Invoke());
                    spanBuilder.SetRequestDetails(context.Request);
                    spanBuilder.SetAnnotation(WellKnownAnnotations.Common.Component, Constants.Component);

                    var strategy = context.Parameters.Strategy;
                    if (strategy != null)
                        spanBuilder.SetClusterStrategy(strategy.ToString());
                }

                var result = await next(context).ConfigureAwait(false);

                if (traceContext != null)
                {
                    spanBuilder.SetResponseDetails(result.Response);
                    spanBuilder.SetClusterStatus(result.Status.ToString());
                }

                return result;
            }
        }
    }
}