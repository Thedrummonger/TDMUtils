#if NETFRAMEWORK || NETSTANDARD2_0

// ------------------------------------------------------------
// Compiler support types required for modern C# features
// when targeting .NET Framework.
// DO NOT MODIFY unless upgrading language features.
// ------------------------------------------------------------

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class CallerArgumentExpressionAttribute : Attribute
    {
        public CallerArgumentExpressionAttribute(string parameterName) { }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    internal sealed class RequiredMemberAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Constructor)]
    internal sealed class SetsRequiredMembersAttribute : Attribute { }
}

#endif