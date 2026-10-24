// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log;
using Dignus.Log.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Dignus.Extensions.Logging
{
    public static class DignusLoggingExtensions
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static IServiceCollection AddDignusLog(this IServiceCollection services, LogConfiguration logConfiguration)
        {
            services.TryAddSingleton<Log.ILogger>(serviceProvider => LogManager.GetDefaultLogger());
            return AddDignusLog(services, logConfiguration, Assembly.GetCallingAssembly());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static IServiceCollection AddDignusLog(this IServiceCollection services, LogConfiguration logConfiguration, string loggerName)
        {
            services.TryAddSingleton<Log.ILogger>(serviceProvider => LogManager.GetLogger(loggerName));
            return AddDignusLog(services, logConfiguration, Assembly.GetCallingAssembly());
        }

        private static IServiceCollection AddDignusLog(IServiceCollection services, LogConfiguration logConfiguration, Assembly callingAssembly)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DignusLoggerProvider>());

            var logBuilder = LogBuilder.Configuration(logConfiguration);

            logBuilder.RegisterAssembly(callingAssembly);

            var executingAssembly = Assembly.GetExecutingAssembly();
            logBuilder.RegisterAssembly(executingAssembly);

            logBuilder.Build();

            return services;
        }
    }
}
