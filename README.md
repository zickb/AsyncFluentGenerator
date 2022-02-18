# AsyncFluentGenerator

A horrible unoptimized Source Generator that allows you to write the following code.
```csharp
using AsyncFluentGenerator;
using AsyncFluentMethodExtionsions;

var test = new TestClass();
await test.FancyWrite("fancy1").FancyWrite("fancy2").NormalWrite("normal");
//instead of
(await (await test.FancyWrite("fancy1")).FancyWrite("fancy2")).NormalWrite("normal");

public class TestClass
{
    [AsyncFluentMethod]
    public async Task<TestClass> FancyWrite(string test)
    {
        await Task.Run(() => System.Console.WriteLine(test));
        return this;
    }
    
    [AsyncFluentMethod]
    public TestClass NormalWrite(string test)
    {
        System.Console.WriteLine(test);
        return this;
    }
}
```
