using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;

namespace Microsoft.Maui.Controls.PerformanceTracker
{
    /// <summary>
    /// Extension methods to register and configure performance monitoring services for a MAUI application.
    /// </summary>
    public static class PerformanceMonitoringExtensions
    {
        /// <summary>
        /// Adds .NET MAUI performance monitoring services to the application's dependency injection container.
        /// </summary>
        /// <param name="builder">The <see cref="MauiAppBuilder"/> to which performance monitoring is being added.</param>
        /// <returns>The same <see cref="MauiAppBuilder"/> instance.</returns>
        public static MauiAppBuilder AddPerformanceMonitoring(
            this MauiAppBuilder builder)
        {
            // Register the Meter
            var meter = new Meter("Microsoft.Maui");
            builder.Services.AddSingleton(meter);
            
            // Register core services
            builder.Services.AddSingleton<IPerformanceProfiler, PerformanceProfiler>();
            builder.Services.AddSingleton<ILayoutPerformanceTracker, LayoutPerformanceTracker>();
            
            return builder;
        }
    }
}