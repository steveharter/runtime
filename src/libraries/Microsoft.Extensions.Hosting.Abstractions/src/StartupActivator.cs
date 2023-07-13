// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting
{
    internal sealed class StartupActivator<TService> : IHostedLifecycleService
        where TService : class
    {
        private readonly TService? _service;
        private readonly IServiceProvider? _provider;
        private readonly Func<TService, IServiceProvider, CancellationToken, Task>? _activator;
        private bool _activatorCalled;

        public StartupActivator(IServiceProvider provider)
        {
            provider.GetService(typeof(TService));
        }

        public StartupActivator(IServiceProvider provider, Func<TService, IServiceProvider, CancellationToken, Task> activator)
        {
            _service = provider.GetRequiredService<TService>();
            _provider = provider;
            _activator = activator;
        }

        Task IHostedLifecycleService.StartingAsync(CancellationToken cancellationToken) => CallActivator(cancellationToken);

        // For backwards compat with hosts that don't support IHostedLifecycleService, use StartAsync().
        Task IHostedService.StartAsync(CancellationToken cancellationToken) => CallActivator(cancellationToken);

        Task IHostedLifecycleService.StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        Task IHostedService.StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        Task IHostedLifecycleService.StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        Task IHostedLifecycleService.StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private Task CallActivator(CancellationToken cancellationToken)
        {
            if (_activator is not null && !_activatorCalled)
            {
                Debug.Assert(_service is not null);
                Debug.Assert(_provider is not null);

                _activatorCalled = true;
                return _activator(_service, _provider, cancellationToken);
            }

            return Task.CompletedTask;
        }
    }
}
