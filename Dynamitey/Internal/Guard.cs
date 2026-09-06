//
//  Copyright 2026 The dynamitey-community contributors
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

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Dynamitey.Internal
{
    /// <summary>
    /// Argument validation for the library's public entry points.
    /// </summary>
    /// <remarks>
    /// This exists to hold one piece of conditional compilation instead of sixteen.
    /// <see cref="ArgumentNullException.ThrowIfNull(object?, string?)"/> does not exist on
    /// netstandard2.0, while writing the plain <c>if (x is null) throw</c> form inline trips
    /// CA1510 on the net10.0 leg, which wants the helper. Satisfying both at every call site
    /// meant a five-line <c>#if</c> block per guard.
    /// <para>
    /// CA1062 knows about this pattern: <c>dotnet_code_quality.CA1062.null_check_validation_methods</c>
    /// in .editorconfig registers <see cref="NotNull{T}"/> as a validator, so the analyzer still
    /// fails the build for a public method that dereferences an unvalidated argument - it simply
    /// accepts this call as the validation. The rule stays live; only the boilerplate goes.
    /// </para>
    /// </remarks>
    internal static class Guard
    {
        /// <summary>
        /// Throws <see cref="ArgumentNullException"/> when <paramref name="value"/> is null.
        /// </summary>
        /// <param name="value">The argument to check.</param>
        /// <param name="paramName">
        /// Supplied automatically by the compiler from the caller's expression, so a call reads
        /// <c>Guard.NotNull(target)</c> and still reports "target" as the parameter name.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotNull<T>(
            [NotNull] T? value,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : class
        {
#if NET
            ArgumentNullException.ThrowIfNull(value, paramName);
#else
            if (value is null)
            {
                throw new ArgumentNullException(paramName);
            }
#endif
        }
    }
}
