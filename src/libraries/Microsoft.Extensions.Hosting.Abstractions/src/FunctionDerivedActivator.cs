// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting
{
    internal sealed class FunctionDerivedActivator<TService>
    {
        private readonly Func<TService, IServiceProvider, CancellationToken, Task> _action;

        public FunctionDerivedActivator(Func<TService, IServiceProvider, CancellationToken, Task> action)
        {
            _action = action;
        }

        public Task CallActivator(TService service, IServiceProvider provider, CancellationToken token)  => _action(service, provider, token);
    }
}
