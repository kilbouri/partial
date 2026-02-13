using System.Linq.Expressions;
using System.Reflection;

namespace Partial.Core;

/// <summary>
/// Defines a model which internally tracks which properties have been assigned during deserialization.
/// <see cref="IsDefined" /> and <see cref="IsUndefined" /> can be used to determine whether
/// or not a given member was bound during deserialization.
/// </summary>
/// <typeparam name="TSelf">The type of the model, which should extend <see cref="Partial{TSelf}" />.</typeparam>
public abstract class Partial<TSelf> where TSelf : Partial<TSelf>
{
    private readonly HashSet<string> definedProperties = [];

    /// <summary>
    /// Indicates whether the selected property is defined or not.
    /// </summary>
    /// <typeparam name="TReturn">The type of the selected property.</typeparam>
    /// <param name="selector">An expression tree. Body must be a <see cref="MemberExpression" />.</param>
    /// <returns><c>true</c> if the selected field is defined, otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentException">If <paramref name="selector" /> is not a <see cref="MemberExpression" /> representing a simple property access.</exception>
    public bool IsDefined<TReturn>(Expression<Func<TSelf, TReturn>> selector)
    {
        if (selector.Body is not MemberExpression memberExpression || memberExpression.Member.MemberType != MemberTypes.Property)
        {
            throw new ArgumentException(
                $"The expression '{selector}' is not a valid property access expression. " +
                $"The expression should represent a simple property: 't => t.MyProperty'.",
                nameof(selector)
            );
        }

        return definedProperties.Contains(memberExpression.Member.Name);
    }

    /// <summary>
    /// Indicates whether the selected property is undefined or not.
    /// </summary>
    /// <typeparam name="TReturn">The type of the selected property.</typeparam>
    /// <param name="selector">An expression tree. Body must be a <see cref="MemberExpression" />.</param>
    /// <returns><c>true</c> if the selected field is undefined, otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentException">If <paramref name="selector" /> is not a <see cref="MemberExpression" /> representing a simple property access.</exception>
    public bool IsUndefined<TReturn>(Expression<Func<TSelf, TReturn>> selector) => !IsDefined(selector);

    /// <summary>
    /// Sets the property value and marks it as defined.
    /// </summary>
    /// <param name="member">The property to set.</param>
    /// <param name="value">The value to set.</param>
    /// <returns><c>true</c> if this is the first time the member is being defined.</returns>
    internal bool DefineProperty(PropertyInfo member, object? value)
    {
        member.SetValue(this, value);
        return definedProperties.Add(member.Name);
    }

    /// <summary>
    /// Provides an enumerable over the defined properties of the instance.
    /// </summary>
    /// <returns></returns>
    internal IEnumerable<PropertyInfo> EnumerateDefinedProperties()
    {
        return typeof(TSelf).GetProperties().Where(member => definedProperties.Contains(member.Name));
    }
}
