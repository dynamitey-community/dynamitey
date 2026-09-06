//
//  Copyright 2026 Ekon Benefits
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

// Polyfills so the same [RequiresUnreferencedCode]/[RequiresDynamicCode]-annotated source
// compiles under both of this library's targets (netstandard2.0;net10.0).
//
// RequiresUnreferencedCodeAttribute shipped in .NET 5; RequiresDynamicCodeAttribute shipped
// in .NET 7. Neither exists on netstandard2.0's own surface, and netstandard2.0 has no
// trim/AOT analyzer to read them anyway - the analyzers that turn these attributes into
// build warnings only run for modern .NET (see Dynamitey.csproj's EnableTrimAnalyzer /
// EnableAotAnalyzer, set for net10.0 only). These stand-ins exist purely so annotating a
// member doesn't force a second, netstandard2.0-only copy of every signature; on
// netstandard2.0 they compile to inert metadata that nothing reads.
//
// Shaped to match the real framework attributes' constructor and properties so the same
// attribute usage compiles unchanged under both `#if` branches.
#if !NET5_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis
{
    using System;

    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Method,
        Inherited = false)]
    internal sealed class RequiresUnreferencedCodeAttribute : Attribute
    {
        public RequiresUnreferencedCodeAttribute(string message)
        {
            Message = message;
        }

        public string Message { get; }

        public string? Url { get; set; }
    }
}
#endif

#if !NET7_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis
{
    using System;

    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Method,
        Inherited = false)]
    internal sealed class RequiresDynamicCodeAttribute : Attribute
    {
        public RequiresDynamicCodeAttribute(string message)
        {
            Message = message;
        }

        public string Message { get; }

        public string? Url { get; set; }
    }
}
#endif

// UnconditionalSuppressMessageAttribute also shipped in .NET 5. Dynamitey uses it to suppress
// analyzer warnings at DynamicObject overrides that call an annotated member internally - see
// the usages in Dynamitey/DynamicObjects for why those specific spots can't carry
// [RequiresUnreferencedCode]/[RequiresDynamicCode] themselves (they would mismatch the
// unannotated virtual member they override). On netstandard2.0 there is no analyzer to invoke
// it, so this polyfill just lets the same attribute usage compile.
#if !NET5_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis
{
    using System;

    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    internal sealed class UnconditionalSuppressMessageAttribute : Attribute
    {
        public UnconditionalSuppressMessageAttribute(string category, string checkId)
        {
            Category = category;
            CheckId = checkId;
        }

        public string Category { get; }

        public string CheckId { get; }

        public string? Scope { get; set; }

        public string? Target { get; set; }

        public string? MessageId { get; set; }

        public string? Justification { get; set; }
    }
}
#endif
