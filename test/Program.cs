// See https://aka.ms/new-console-template for more information
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using AsyncFluentGenerator;

Compilation inputCompilation = CreateCompilation(@"
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.CompilerServices;
using System;
using System.Collections;
using System.Collections.Generic;
using AsyncFluentGenerator;

System.Console.WriteLine(""Test"");

namespace TestProgram
{
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

    public partial class Nested<X>
    {
        public struct Person
        {
        }

        public enum Education
        {
            None,
            MidSchool,
            HighSchool,
            University
        }

        internal interface A 
        {
            internal MyAwaitableObject<Name<Z>> AddEventListener<T, Z>(string eventName, Action<T?, Z> eventHandler) where T:class where Z:class;
        }

        public class Name<U> : A
        where U:class
        {
            [AsyncFluentMethod(true)]
            [return: MaybeNull] 
            async MyAwaitableObject<Name<Z>> A.AddEventListener<T, Z>(string eventName, Action<T?, Z> eventHandler) where T:class where Z:class
            {
                await Task.Run(() => System.Console.WriteLine(""Test""));
                return new Name<Z>();
            }

            public async Task<Name<Z>> AddEventListener2<T, Z>(string eventName, Action<T?> eventHandler) where Z:class
            {
                await Task.Run(() => System.Console.WriteLine(""Test""));
                return new Name<Z>();
            }

            [AsyncFluentMethod(includeAttributes: true, methodName: ""test2"")]
            public IEnumerable<Name<U>> AddEventListener3<T, U>(string eventName, [NotNullWhen(true)] Action<B?> eventHandler, string t111 = null) where T:class where U:class?, IEnumerable
            {
                Task.Run(() => System.Console.WriteLine(""Test""));
                foreach(var c in ""1234"")
                {
                    yield return new Name<U>();
                }
                // return new Name<U>();
            }

            public interface B 
            {
                
            }
        }
    }

    public partial class Name<U>
    {
        [AsyncFluentMethod(""testName"", true)]
        public partial IEnumerable<string> AddEventListener3<T, U>(string eventName, [NotNullWhen(returnValue: true)] Action<Nested<T>.Name<U>.B?> eventHandler, string t111 = null) where T:class where U:class?, IEnumerable
        {
            Task.Run(() => System.Console.WriteLine(""Test""));
            foreach(var c in ""1234"")
            {
                yield return """";
            }
        }
    }

    [AsyncFluentClass(""Test"")]
    public partial class Name<U>
    {
        public partial IEnumerable<string> AddEventListener3<T, U>(string eventName, Action<Nested<T>.Name<U>.B?> eventHandler, string t111 = null) where T:class where U:class?, IEnumerable;

        [AsyncFluentMethod(""testName2"", true)]
        public IEnumerable<string> AddEventListener4<T, U>(string eventName, [NotNullWhen(returnValue: true)] Action<Nested<T>.Name<U>.B?> eventHandler, string t111 = null) where T:class where U:class?, IEnumerable
        {
            Task.Run(() => System.Console.WriteLine(""Test""));
            foreach(var c in ""1234"")
            {
                yield return """";
            }
        }
    }
}
");

IIncrementalGenerator generator = new AsyncFluentGenerator.AsyncFluentGenerator();

GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

System.Console.WriteLine("");

static Compilation CreateCompilation([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string source)
    => CSharpCompilation.Create("compilation",
        new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp10)) },
        new[] { 
            MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location), 
            MetadataReference.CreateFromFile(typeof(AsyncFluentMethod).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "System.Console.dll")),  
            MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "System.Runtime.Handles.dll")),  
            MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "System.Threading.ThreadPool.dll"))  
        },
        new CSharpCompilationOptions(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Enable));