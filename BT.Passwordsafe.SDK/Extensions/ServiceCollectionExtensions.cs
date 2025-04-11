using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BT.PasswordSafe.SDK.Interfaces;
using BT.PasswordSafe.SDK.Models;

namespace BT.PasswordSafe.SDK.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/> to add Password Safe SDK services
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Password Safe client services to the specified <see cref="IServiceCollection"/>
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to</param>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="configSectionPath">The configuration section path (default: "PasswordSafe")</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained</returns>
        public static IServiceCollection AddPasswordSafeClient(
            this IServiceCollection services,
            IConfiguration configuration,
            string configSectionPath = "PasswordSafe")
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            // Configure options from configuration
            services.Configure<PasswordSafeOptions>(options => configuration.GetSection(configSectionPath).Bind(options));

            // Register HTTP client
            services.AddHttpClient<IPasswordSafeClient, PasswordSafeClient>();

            return services;
        }

        /// <summary>
        /// Adds Password Safe client services to the specified <see cref="IServiceCollection"/>
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to</param>
        /// <param name="configureOptions">The action to configure the <see cref="PasswordSafeOptions"/></param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained</returns>
        public static IServiceCollection AddPasswordSafeClient(
            this IServiceCollection services,
            Action<PasswordSafeOptions> configureOptions)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            // Configure options from action
            services.Configure(configureOptions);

            // Register HTTP client
            services.AddHttpClient<IPasswordSafeClient, PasswordSafeClient>();

            return services;
        }
    }
}
