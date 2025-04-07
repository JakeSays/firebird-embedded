// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.IO;
using System.Threading;
using Std.CommandLine.Binding;
using Std.CommandLine.Parsing;

namespace Std.CommandLine.Invocation
{
    internal sealed class InvocationContext : IDisposable
    {
        private CancellationTokenSource? _cts;
        private Action<CancellationTokenSource>? _cancellationHandlingAddedEvent;

        public BindingContext BindingContext { get; }

        public InvocationContext(
            ParseResult parseResult)
        {
            BindingContext = new BindingContext(parseResult);
            BindingContext.ServiceProvider.AddService(_ => GetCancellationToken());
            BindingContext.ServiceProvider.AddService(_ => this);
        }

        public InvocationContext(BindingContext context)
        {
            BindingContext = context;
            BindingContext.ServiceProvider.AddService(_ => GetCancellationToken());
            BindingContext.ServiceProvider.AddService(_ => this);
        }

        public TextReader ConsoleIn => System.Console.In;

        public SystemConsole Console => DefaultConsoles.StdOut;

        public SystemConsole ErrorConsole => DefaultConsoles.StdErr;

        internal CommandLineParser Parser => BindingContext.ParseResult.Parser;

        public ParseResult ParseResult
        {
            get => BindingContext.ParseResult;
            set => BindingContext.ParseResult = value;
        }

        public int ResultCode { get; set; }

        public IInvocationResult? InvocationResult { get; set; }

        internal event Action<CancellationTokenSource> CancellationHandlingAdded
        {
            add
            {
                if (_cts != null)
                {
                    throw new InvalidOperationException("Handlers must be added before adding cancellation handling.");
                }

                _cancellationHandlingAddedEvent += value;
            }
            remove => _cancellationHandlingAddedEvent -= value;
        }

        /// <summary>
        /// Gets token to implement cancellation handling.
        /// </summary>
        /// <returns>Token used by the caller to implement cancellation handling.</returns>
        public CancellationToken GetCancellationToken()
        {
            if (!(_cts is null))
            {
                return _cts.Token;
            }

            _cts = new CancellationTokenSource();
            _cancellationHandlingAddedEvent?.Invoke(_cts);

            return _cts.Token;
        }

        public void Dispose()
        {
        }
    }
}
