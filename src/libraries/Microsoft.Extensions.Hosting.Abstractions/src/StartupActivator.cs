// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting
{
    internal sealed class StartupActivator<TService> : IHostedLifecycleService
    {
        public StartupActivator(TService service)
        {
            _ = service;
        }

        Task IHostedLifecycleService.StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        Task IHostedService.StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        Task IHostedLifecycleService.StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        Task IHostedService.StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        Task IHostedLifecycleService.StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        Task IHostedLifecycleService.StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
