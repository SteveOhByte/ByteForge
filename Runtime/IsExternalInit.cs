namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Reserved to be used by the compiler for tracking metadata.
    /// This class should not be used by developers in source code.
    /// </summary>
    /// <remarks>
    /// This class is necessary for init-only properties and C# 9.0 records
    /// when targeting older .NET Framework versions or Unity's .NET Standard.
    /// The compiler specifically looks for this class in the System.Runtime.CompilerServices
    /// namespace to enable these language features.
    /// 
    /// Without this class, you would get compiler errors when trying to use
    /// init accessors or record types in projects that don't include the latest
    /// .NET runtime libraries.
    /// </remarks>
    public class IsExternalInit { }
}