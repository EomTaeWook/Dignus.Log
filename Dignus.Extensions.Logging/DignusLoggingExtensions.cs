// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log;
using Dignus.Log.Formatting;
using Dignus.Log.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Dignus.Extensions.Logging
{
    public static class DignusLoggingExtensions
    {
        public static IServiceCollection AddDignusLog(this IServiceCollection services, LogConfiguration logConfiguration)
        {
            services.TryAddSingleton((s) =>
            {
                return LogManager.GetCurrentLogger();
            });

            services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DignusLoggerProvider>());

            var builder = LogBuilder.Configuration(logConfiguration);

            var callingAssembly = Assembly.GetCallingAssembly();
            FormatElements.RegisterFromAssembly(callingAssembly);
            builder.RegisterLogTargets(callingAssembly);

            var executingAssembly = Assembly.GetExecutingAssembly();
            FormatElements.RegisterFromAssembly(executingAssembly);
            builder.RegisterLogTargets(executingAssembly);

            builder.Build();

            return services;
        }
    }
}
