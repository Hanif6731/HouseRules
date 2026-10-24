// netstandard2.0 lacks the type the compiler needs for records. The compiler looks it up by this exact name, so the
// namespace cannot follow the folder.
#pragma warning disable IDE0130
namespace System.Runtime.CompilerServices;
#pragma warning restore IDE0130

internal static class IsExternalInit
{
}
