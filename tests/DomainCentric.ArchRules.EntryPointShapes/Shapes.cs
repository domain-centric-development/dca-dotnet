using System;
using System.Threading.Tasks;

namespace Acme.Shapes;

/// <summary>The class <c>dotnet new console --use-program-main</c> writes.</summary>
internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
}

/// <summary>An entry point whose Main holds a lambda and awaits: what the compiler writes for the body belongs to it.</summary>
public static class AsyncWithLambda
{
    public static async Task<int> Main(string[] args)
    {
        Func<int> count = () => args.Length;
        await Task.Yield();
        return count();
    }
}

/// <summary>A field: production code.</summary>
public class WithField
{
    private static int started;

    public static void Main() => started++;
}

/// <summary>A second method: production code.</summary>
public class WithSecondMethod
{
    public static void Main() => Greet();

    private static void Greet()
    {
    }
}

/// <summary>A nested type: production code.</summary>
public class WithNestedType
{
    public static void Main()
    {
    }

    private sealed class Inner
    {
    }
}

/// <summary>A property: production code.</summary>
public class WithProperty
{
    public static string Name => "x";

    public static void Main()
    {
    }
}

/// <summary>No Main at all: production code.</summary>
public class WithoutMain
{
}
