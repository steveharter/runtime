// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting
{
    internal sealed class StartupActivatorWithFunction<TService> : IHostedLifecycleService
    {
        public StartupActivatorWithFunction(TService service, IServiceProvider provider, FunctionDerivedActivator<TService> activator)
        {
            Service = service;
            Provider = provider;
            Activator = activator;
        }

        public readonly TService Service;
        public readonly IServiceProvider Provider;
        public readonly FunctionDerivedActivator<TService> Activator;

        async Task IHostedLifecycleService.StartingAsync(CancellationToken cancellationToken) =>
            await Activator.CallActivator(Service, Provider, cancellationToken).ConfigureAwait(false);

        Task IHostedService.StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        Task IHostedLifecycleService.StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        Task IHostedService.StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        Task IHostedLifecycleService.StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        Task IHostedLifecycleService.StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
