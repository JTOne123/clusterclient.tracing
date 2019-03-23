using JetBrains.Annotations;
using Vostok.Clusterclient.Context;
using Vostok.Clusterclient.Core;
using Vostok.Tracing.Abstractions;

namespace Vostok.Clusterclient.Tracing
{
    [PublicAPI]
    public static class IClusterClientConfigurationExtensions
    {
        /// <summary>
        /// Sets up distributed tracing of HTTP requests using given <paramref name="tracer"/>.
        /// </summary>
        public static void SetupDistributedTracing([NotNull] this IClusterClientConfiguration config, [NotNull] ITracer tracer)
            => SetupDistributedTracing(config, new TracingConfiguration(tracer));

        /// <summary>
        /// Sets up distributed tracing of HTTP requests using given <paramref name="configuration"/>.
        /// </summary>
        public static void SetupDistributedTracing([NotNull] this IClusterClientConfiguration config, [NotNull] TracingConfiguration configuration)
        {
            config.SetupDistributedContext();

            var tracingTransport = new TracingTransport(config.Transport, configuration)
            {
                TargetServiceProvider = () => config.TargetServiceName,
                TargetEnvironmentProvider = () => config.TargetEnvironment
            };

            var tracingModule = new TracingModule(configuration)
            {
                TargetServiceProvider = () => config.TargetServiceName,
                TargetEnvironmentProvider = () => config.TargetEnvironment
            };

            config.Transport = tracingTransport;
            config.AddRequestModule(tracingModule, typeof(DistributedContextModule));
        }
    }
}
