// Shamelessly copied from https://stackoverflow.com/a/43498938
// Modified slightly.

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ClassicUO.Common;

public class Accessor<T>
{
    private readonly Action<T> _setter;
    private readonly Func<T> _getter;

    public Accessor(Func<T> getter, Action<T> setter)
    {
        _getter = getter;
        _setter = setter;
    }

    public Accessor(Expression<Func<T>> expr, [CallerArgumentExpression(nameof(expr))] string exprName = null)
    {
        ArgumentNullException.ThrowIfNull(expr);

        if (expr.Body is not MemberExpression memberExpression)
            throw new ArgumentException($"Expression '{exprName}' is not a member expression.");

        Expression instanceExpression = memberExpression.Expression;
        ParameterExpression parameter = Expression.Parameter(typeof(T));

        switch (memberExpression.Member)
        {
            case PropertyInfo propertyInfo:
                MethodInfo setMethod = propertyInfo.GetSetMethod() ?? throw new InvalidOperationException($"Property '{propertyInfo.Name}' does not have a setter.");
                MethodInfo getMethod = propertyInfo.GetGetMethod() ?? throw new InvalidOperationException($"Property '{propertyInfo.Name}' does not have a setter.");

                _setter = Expression.Lambda<Action<T>>(Expression.Call(instanceExpression, setMethod, parameter), parameter).Compile();
                _getter = Expression.Lambda<Func<T>>(Expression.Call(instanceExpression, getMethod)).Compile();
                break;
            case FieldInfo fieldInfo:
                _setter = Expression.Lambda<Action<T>>(Expression.Assign(memberExpression, parameter), parameter).Compile();
                _getter = Expression.Lambda<Func<T>>(Expression.Field(instanceExpression,fieldInfo)).Compile();
                break;
        }

    }

    public void Set(T value) => _setter(value);

    public T Get() => _getter();
}
