// Polyfill required for C# 9 'init' accessors when targeting net48.
// The compiler synthesises references to this type for init-only setters;
// .NET 5+ includes it in the BCL, but net48 does not.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
