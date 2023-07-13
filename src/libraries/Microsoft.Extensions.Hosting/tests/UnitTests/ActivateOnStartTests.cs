// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public class ActivateOnStartTests
    {
        private static IHostBuilder CreateHostBuilder(Action<IServiceCollection> configure) =>
            new HostBuilder().ConfigureServices(configure);

        [Fact]
        public async void ActivateByType()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services
                    .AddSingleton<ActivateByType_Impl>()
                    .AddStartupActivation<ActivateByType_Impl>();
            });

            using (IHost host = hostBuilder.Build())
            {
                await host.StartAsync();
                Assert.True(ActivateByType_Impl.Activated);
            }
        }

        public class ActivateByType_Impl
        {
            public static bool Activated = false;
            public ActivateByType_Impl()
            {
                Activated = true;
            }
        }

        [Fact]
        public async void ActivateByTypeWithCallback()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services
                    .AddSingleton<ActivateByTypeWithCallback_Impl>()
                    .AddStartupActivation<ActivateByTypeWithCallback_Impl>(async (service, sp, cancellationToken) =>
                {
                    await service.DoSomethingAsync(cancellationToken);
                });
            });

            using (IHost host = hostBuilder.Build())
            {
                await host.StartAsync();
                Assert.True(ActivateByTypeWithCallback_Impl.Activated);
                Assert.True(ActivateByTypeWithCallback_Impl.DidSomethingAsync_Before);
                Assert.True(ActivateByTypeWithCallback_Impl.DidSomethingAsync_After);
            }
        }

        public class ActivateByTypeWithCallback_Impl
        {
            public static bool Activated = false;
            public static bool DidSomethingAsync_Before = false;
            public static bool DidSomethingAsync_After = false;

            public ActivateByTypeWithCallback_Impl()
            {
                Assert.False(Activated);
                Activated = true;
            }

            public async Task DoSomethingAsync(CancellationToken cancellationToken)
            {
                Assert.False(DidSomethingAsync_Before);
                DidSomethingAsync_Before = true;

                await Task.Delay(10, cancellationToken);

                Assert.False(DidSomethingAsync_After);
                DidSomethingAsync_After = true;
            }
        }
    }
}
