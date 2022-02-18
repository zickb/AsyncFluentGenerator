namespace test23;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AsyncFluentGenerator;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal class test2 
{
    internal partial interface u
    {
        public void u();
    }

    private partial interface t<X>
    {
        MyAwaitableObject<t<X>> T();
    }

    partial interface t2<X> : t<X>
    {
    }

    public partial struct nested1<Y> : t<Y>
    {
        async MyAwaitableObject<t<Y>> u<T, String>(Task<t<Y>> te, [NotNullWhen(returnValue: true)] CancellationToken c, String test)
        {
            var x = await te;
            var y = await x.T();
            return (t<Y>)y;
        }

        async Task T()
        {
            var t = JsonSerializer.Deserialize("{}", typeof(string));
        }

        MyAwaitableObject<t<Y>> t<Y>.T()
        {
            throw new NotImplementedException();
        }
    }

    protected internal void T2(test1 test)
    {
        test.T3();
        var t = JsonSerializer.Deserialize("{}", typeof(string));
    }
}

[AsyncMethodBuilder(typeof(MyAwaiter<>))]
public class MyAwaitableObject<T>
{
   public MyAwaiter<T> GetAwaiter()
   {
      return new MyAwaiter<T>();
   }
}
 
public struct MyAwaiter<T> : INotifyCompletion, ICriticalNotifyCompletion
{
    public MyAwaitableObject<T> Task { get; }
    public static MyAwaiter<T> Create() => default;

    public void OnCompleted(Action continuation)
    {
        continuation();
    }
    
    public bool IsCompleted { get; private set; }
    
    public T GetResult()
    {      
        return default(T);      
    }

    public void UnsafeOnCompleted(Action continuation)
    {
        throw new NotImplementedException();
    }

    public void SetException(Exception e) {}
        
    public void SetResult(T result) {}

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        awaiter.OnCompleted(stateMachine.MoveNext);
    }
    
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
    }

    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
    {
        Action move = stateMachine.MoveNext;
        ThreadPool.QueueUserWorkItem(_ =>
        {
            move();
        });
    }

    public void SetStateMachine(IAsyncStateMachine stateMachine)
    {
        // nothing to do
    }
}

// class MyTaskMethodBuilder<T>
// {
//     public static MyTaskMethodBuilder<T> Create() => default;

//     public void SetException(Exception e) {}
        
//     public void SetResult(T result) {}

//     public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
//         where TAwaiter : INotifyCompletion
//         where TStateMachine : IAsyncStateMachine
//     {
//         awaiter.OnCompleted(stateMachine.MoveNext);
//     }
    
//     public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
//         where TAwaiter : ICriticalNotifyCompletion
//         where TStateMachine : IAsyncStateMachine
//     {
//         awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
//     }

//     public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
//     {
//         Action move = stateMachine.MoveNext;
//         ThreadPool.QueueUserWorkItem(_ =>
//         {
//             move();
//         });
//     }

//     public void SetStateMachine(IAsyncStateMachine stateMachine)
//     {
//         // nothing to do
//     }

//     public MyAwaitableObject<T> Task { get; }
// }