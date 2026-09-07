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

// Polyfills so the same nullable-postcondition-annotated source compiles under both of this
// library's targets (netstandard2.0;net10.0).
//
// The nullable *annotations* themselves (`?`, `!`) are a pure compiler feature and need nothing
// from the framework - they work identically on netstandard2.0. But the postcondition
// *attributes* ([NotNullWhen], [MaybeNullWhen], [DoesNotReturn], etc.) that tell the analyzer
// about a Try-pattern method's or a throw-helper's behavior shipped as real framework types
// starting with .NET Core 3.0 / netstandard2.1; netstandard2.0's own surface has neither the
// types nor an analyzer that would read them if it did. These stand-ins exist purely so
// annotating a member doesn't force a second, netstandard2.0-only copy of every signature; on
// netstandard2.0 they compile to inert metadata that nothing reads - exactly like
// AotAttributesPolyfill.cs alongside this file, which does the same for the trim/AOT attributes.
//
// Shaped to match the real framework attributes' constructors and properties so the same
// attribute usage compiles unchanged whether or not this branch is active.
#if NETSTANDARD2_0
namespace System.Diagnostics.CodeAnalysis
{
    using System;

    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class NotNullWhenAttribute : Attribute
    {
        public NotNullWhenAttribute(bool returnValue)
        {
            ReturnValue = returnValue;
        }

        public bool ReturnValue { get; }
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class MaybeNullWhenAttribute : Attribute
    {
        public MaybeNullWhenAttribute(bool returnValue)
        {
            ReturnValue = returnValue;
        }

        public bool ReturnValue { get; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
    internal sealed class NotNullAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
    internal sealed class MaybeNullAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal sealed class DoesNotReturnAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class DoesNotReturnIfAttribute : Attribute
    {
        public DoesNotReturnIfAttribute(bool parameterValue)
        {
            ParameterValue = parameterValue;
        }

        public bool ParameterValue { get; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
    internal sealed class AllowNullAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
    internal sealed class DisallowNullAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    internal sealed class MemberNotNullAttribute : Attribute
    {
        public MemberNotNullAttribute(string member)
        {
            Members = new[] { member };
        }

        public MemberNotNullAttribute(params string[] members)
        {
            Members = members;
        }

        public string[] Members { get; }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    internal sealed class MemberNotNullWhenAttribute : Attribute
    {
        public MemberNotNullWhenAttribute(bool returnValue, string member)
        {
            ReturnValue = returnValue;
            Members = new[] { member };
        }

        public MemberNotNullWhenAttribute(bool returnValue, params string[] members)
        {
            ReturnValue = returnValue;
            Members = members;
        }

        public bool ReturnValue { get; }

        public string[] Members { get; }
    }
}
#endif

#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices
{
    using System;

    // CallerArgumentExpression arrived in C# 10 / .NET 6. Internal.Guard.NotNull uses it so a
    // call reads Guard.NotNull(target) and still reports "target" as the parameter name, rather
    // than repeating nameof(target) at every one of the twenty call sites. Same arrangement as
    // the attributes above: on netstandard2.0 this compiles to inert metadata, and the compiler
    // recognises it by name and shape regardless of which assembly declares it.
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class CallerArgumentExpressionAttribute : Attribute
    {
        public CallerArgumentExpressionAttribute(string parameterName)
        {
            ParameterName = parameterName;
        }

        public string ParameterName { get; }
    }
}
#endif
