using System;
using JetBrains.Annotations;
using Std.CommandLine.Binding;
using Std.CommandLine.Invocation;


namespace Std.CommandLine.Commands
{
    internal interface HandlerTarget
    {
        void SetHandler(Invokable handler);
    }

    [PublicAPI]
    public interface HandlerProvider<out TBuilder>
    {
        internal HandlerTarget Target { get; }
        internal TBuilder Builder { get; }

        public TBuilder OnExecute(Action exec)
        {
            Target.SetHandler(CommandHandler.Create(exec));
            return Builder;
        }

        public TBuilder OnExecute<T>(Action<T> action)
        {
            Target.SetHandler(HandlerDescriptor.FromDelegate(action).GetCommandHandler());
            return Builder;
        }

        public TBuilder OnExecute<T1, T2>(Action<T1, T2> action)
        {
            Target.SetHandler(HandlerDescriptor.FromDelegate(action).GetCommandHandler());
            return Builder;
        }

        public TBuilder OnExecute<T1, T2, T3>(Action<T1, T2, T3> action)
        {
            Target.SetHandler(HandlerDescriptor.FromDelegate(action).GetCommandHandler());
            return Builder;
        }

        public TBuilder OnExecute<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action)
        {
            Target.SetHandler(HandlerDescriptor.FromDelegate(action).GetCommandHandler());
            return Builder;
        }

        public TBuilder OnExecute<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action)
        {
            Target.SetHandler(HandlerDescriptor.FromDelegate(action).GetCommandHandler());
            return Builder;
        }

        public TBuilder OnExecute<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> action)
        {
            Target.SetHandler(HandlerDescriptor.FromDelegate(action).GetCommandHandler());
            return Builder;
        }

        public TBuilder OnExecute<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> action)
        {
            Target.SetHandler(HandlerDescriptor.FromDelegate(action).GetCommandHandler());
            return Builder;
        }


        public TBuilder OnExecute<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T8> action)
        {
            Target.SetHandler(HandlerDescriptor.FromDelegate(action).GetCommandHandler());
            return Builder;
        }

        public TBuilder OnExecute<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> action)
        {
            Target.SetHandler(HandlerDescriptor.FromDelegate(action).GetCommandHandler());
            return Builder;
        }

        public TBuilder OnExecute<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9,
            T10> action)
        {
            Target.SetHandler(HandlerDescriptor.FromDelegate(action).GetCommandHandler());
            return Builder;
        }

        public TBuilder OnExecute(Func<int> action)
        {
            Target.SetHandler(HandlerDescriptor.FromDelegate(action).GetCommandHandler());
            return Builder;
        }

        public TBuilder OnExecute<T>(Func<T, int> action)
        {
            Target.SetHandler(HandlerDescriptor.FromDelegate(action).GetCommandHandler());
            return Builder;
        }

        public TBuilder OnExecute<T1, T2>(Func<T1, T2, int> action)
        {
            Target.SetHandler(HandlerDescriptor.FromDelegate(action).GetCommandHandler());
            return Builder;
        }

        public TBuilder OnExecute<T1, T2, T3>(Func<T1, T2, T3, int> action)
        {
            Target.SetHandler(HandlerDescriptor.FromDelegate(action).GetCommandHandler());
            return Builder;
        }

        public TBuilder OnExecute<T1, T2, T3, T4>(Func<T1, T2, T3, T4, int> action)
        {
            Target.SetHandler(HandlerDescriptor.FromDelegate(action).GetCommandHandler());
            return Builder;
        }

        public TBuilder OnExecute<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5, int> action)
        {
            Target.SetHandler(HandlerDescriptor.FromDelegate(action).GetCommandHandler());
            return Builder;
        }

        public TBuilder OnExecute<T1, T2, T3, T4, T5, T6>(Func<T1, T2, T3, T4, T5, T6, int> action)
        {
            Target.SetHandler(HandlerDescriptor.FromDelegate(action).GetCommandHandler());
            return Builder;
        }

        public TBuilder OnExecute<T1, T2, T3, T4, T5, T6, T7>(Func<T1, T2, T3, T4, T5, T6, T7, int> action)
        {
            Target.SetHandler(HandlerDescriptor.FromDelegate(action).GetCommandHandler());
            return Builder;
        }

        public TBuilder OnExecute<T1, T2, T3, T4, T5, T6, T7, T8>(Func<T1, T2, T3, T4, T5, T6, T7, T8, int> action)
        {
            Target.SetHandler(HandlerDescriptor.FromDelegate(action).GetCommandHandler());
            return Builder;
        }

        public TBuilder OnExecute<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, int> action)
        {
            Target.SetHandler(HandlerDescriptor.FromDelegate(action).GetCommandHandler());
            return Builder;
        }

        public TBuilder OnExecute<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, int> action)
        {
            Target.SetHandler(HandlerDescriptor.FromDelegate(action).GetCommandHandler());
            return Builder;
        }

        public TBuilder OnExecute<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, int> action)
        {
            Target.SetHandler(HandlerDescriptor.FromDelegate(action).GetCommandHandler());
            return Builder;
        }

        public TBuilder OnExecute<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, int> action)
        {
            Target.SetHandler(HandlerDescriptor.FromDelegate(action).GetCommandHandler());
            return Builder;
        }

        public TBuilder OnExecute<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, int> action)
        {
            Target.SetHandler(HandlerDescriptor.FromDelegate(action).GetCommandHandler());
            return Builder;
        }
    }
}
