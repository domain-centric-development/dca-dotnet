// Library shim: the test project has no dependency on a discriminated-union library. This type carries the full
// name the default FrameworkTypes.ClosedResultBaseTypes refers to, so the fixtures can exercise DCA-ERR-006
// without pulling in the library. Only the shape a caller sees is modelled: the cases as generic arguments, the
// held value, and one function or action per case.
using System;

namespace OneOf;

public abstract class OneOfBase<T0, T1>
{
    private readonly int _index;

    protected OneOfBase(T0 value)
    {
        Value = value;
        _index = 0;
    }

    protected OneOfBase(T1 value)
    {
        Value = value;
        _index = 1;
    }

    public object? Value { get; }

    public TResult Match<TResult>(Func<T0, TResult> f0, Func<T1, TResult> f1) =>
        _index == 0 ? f0((T0)Value!) : f1((T1)Value!);

    public void Switch(Action<T0> f0, Action<T1> f1)
    {
        if (_index == 0)
        {
            f0((T0)Value!);
        }
        else
        {
            f1((T1)Value!);
        }
    }
}
