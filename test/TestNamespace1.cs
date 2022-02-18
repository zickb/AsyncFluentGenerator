namespace test23;
using System.Text.Json;
internal abstract class test1 
{ 
    private void T<X>()
    {
        var test = new test2.nested1<X>();
        var t = JsonSerializer.Deserialize("{}", typeof(string));
        var test2 = new test2();
        test2.T2(this);
        var t3 = new Task<test3<int>>(null, null);
        t3.MyMethod("2");
    }

    public abstract void T3();
}

internal class test3<T>
{
    public void MyMethod(string t = null) {
    }
}

public static class Extensionclass 
{
    internal static void MyMethod<U, T>(this Task<test3<U>> t, T test)
    where T:class?
    {}
}

public struct Person
{
    public Person(string firstname)
    {
        
    }
}