// 
//  Copyright 2011 Ekon Benefits
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
using System.Linq;
using Dynamitey.DynamicObjects;

namespace Dynamitey
{


    /// <summary>
    /// Builds Objects with a Fluent Syntax
    /// </summary>
    public static class Builder
    {
        /// <summary>
        /// New Builder
        /// </summary>
        /// <returns></returns>
        [RequiresUnreferencedCode("Constructs the annotated Builder<ChainableDictionary>.")]
        [RequiresDynamicCode("Constructing any BaseObject-derived type instantiates System.Dynamic.DynamicObject, whose default constructor requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public static IBuilder New()
        {
            return new Builder<ChainableDictionary>();
        }

       

        /// <summary>
        /// New Builder
        /// </summary>
        /// <typeparam name="TObjectPrototype">The type of the object prototype.</typeparam>
        /// <returns></returns>
        [RequiresUnreferencedCode("Constructs the annotated Builder<TObjectPrototype>.")]
        [RequiresDynamicCode("Constructing any BaseObject-derived type instantiates System.Dynamic.DynamicObject, whose default constructor requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public static IBuilder New<TObjectPrototype>() where TObjectPrototype : new()
        {
            return new Builder<TObjectPrototype>();
        }

        
    }

    /// <summary>
    /// Syntax for a quick new inline prototype object
    /// </summary>
    public static class Build
    {
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
            "Constructing a Builder<ChainableDictionary> (a BaseObject-derived type) requires the DLR " +
            "regardless of whether NewObject is ever used. This field initializer has no " +
            "caller to warn at; the actionable warning lives on NewObject itself.")]
        private static readonly dynamic _objectBuilder = new Builder<ChainableDictionary>().Object;

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
            "Constructs a Builder<ChainableDictionary>, calls its dynamic ListSetup<List>()/List(), and curries the result - all DLR/reflection-heavy. This field initializer has no caller to warn at; the actionable " +
            "warning lives on the members that use it once resolved (NewList).")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Same reasoning as the IL2026 suppression above.")]
        private static readonly dynamic _listBuilder =
            Dynamic.Curry(new Builder<ChainableDictionary>().ListSetup<List>()).
                List();

        /// <summary>
        /// Gets the new object builder.
        /// </summary>
        /// <value>The new object.</value>
        public static dynamic NewObject => _objectBuilder;

        /// <summary>
        /// Gets the new list builder.
        /// </summary>
        /// <value>The new list.</value>
        public static dynamic NewList => _listBuilder;
    }

    /// <summary>
    /// Syntax for a quick inline object property setup
    /// </summary>
    /// <typeparam name="TObjectPrototype">The type of the object prototype.</typeparam>
    public static class Build<TObjectPrototype> where TObjectPrototype : new()
    {
// ReSharper disable StaticFieldInGenericType
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
            "Constructing a Builder<TObjectPrototype> (a BaseObject-derived type) requires the DLR " +
            "regardless of whether NewObject is ever used. This field initializer has no " +
            "caller to warn at; the actionable warning lives on NewObject itself.")]
        private static readonly dynamic _typedBuilder = new Builder<TObjectPrototype>().Object;
// ReSharper restore StaticFieldInGenericType

// ReSharper disable StaticFieldInGenericType
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
            "Constructs a Builder<TObjectPrototype>, calls its dynamic ListSetup<TObjectPrototype>()/List(), and curries the result - all DLR/reflection-heavy. This field initializer has no caller to warn at; the actionable " +
            "warning lives on the members that use it once resolved (NewList).")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Same reasoning as the IL2026 suppression above.")]
        private static readonly dynamic _typedListBuilder = Dynamic.Curry(new Builder<TObjectPrototype>().ListSetup<TObjectPrototype>()).List();
// ReSharper restore StaticFieldInGenericType

        /// <summary>
        /// Gets the new object builder.
        /// </summary>
        /// <value>The new.</value>
        public static dynamic NewObject => _typedBuilder;

        /// <summary>
        /// Gets the new list builder.
        /// </summary>
        /// <value>The new list.</value>
        public static dynamic NewList => _typedListBuilder;
    }

    /// <summary>
    /// Encapsulates an Activator
    /// </summary>
    public class Activate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Activate"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="args">The args.</param>
        public Activate(Type type, params object[] args)
        {
            Type = type;

            var tArg = args.OfType<Func<object[]>>().SingleOrDefault();
            Arguments = tArg != null ? tArg : () => args;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="Activate"/> class. With Factory Function
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="args">The args.</param>
        public Activate(Type type, Func<object[]> args)
        {
            Type = type;
            Arguments = args;
        }
        /// <summary>
        /// Gets or sets the constructor type.
        /// </summary>
        /// <value>The type.</value>
        public virtual Type Type { get; private set; }

        /// <summary>
        /// Gets or sets the constructor arguments.
        /// </summary>
        /// <value>The arguments.</value>
        public virtual Func<object[]> Arguments
        {
            get; private set;
        }

        /// <summary>
        /// Creates this instance.
        /// </summary>
        /// <returns></returns>
        [RequiresUnreferencedCode("Calls the annotated Dynamic.InvokeConstructor.")]
        [RequiresDynamicCode("Dynamic.InvokeConstructor requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public virtual dynamic Create()
        {
            object[] tArgs = Arguments();
            // A constructor call never actually returns null; InvokeConstructor's return is
            // nullable only because it's declared for the general "whatever was invoked" case.
            return Dynamic.InvokeConstructor(Type, tArgs)!;
        }
    }

    /// <summary>
    /// Encapsulates an Activator
    /// </summary>
    /// <typeparam name="TObjectPrototype">The type of the object prototype.</typeparam>
    public class Activate<TObjectPrototype> : Activate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Activate&lt;TObjectPrototype&gt;"/> class.
        /// </summary>
        /// <param name="args">The args.</param>
        public Activate(params object[] args) : base(typeof(TObjectPrototype), args)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Activate&lt;TObjectPrototype&gt;"/> class. With Factory Function
        /// </summary>
        /// <param name="args">The args.</param>
        public Activate(Func<object[]> args)
            : base(typeof(TObjectPrototype), args)
        {
        }

        /// <summary>
        /// Creates this instance.
        /// </summary>
        /// <returns></returns>
        [RequiresUnreferencedCode("Calls Activator.CreateInstance<TObjectPrototype>(), which requires TObjectPrototype to have a public parameterless constructor for trim analysis, and falls back to the annotated Dynamic.InvokeConstructor.")]
        [RequiresDynamicCode("The Dynamic.InvokeConstructor fallback requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public override dynamic Create()
        {
            var tArgs = Arguments();

            if(tArgs.Any())
                return base.Create();


            TObjectPrototype tObjectPrototype;
            try
            {
                tObjectPrototype = Activator.CreateInstance<TObjectPrototype>();//Try first because it's really fast, but won't work with optional parameters
            }
            catch (MissingMemberException)
            {
                // The one documented failure mode of Activator.CreateInstance<T>() is exactly the
                // "no parameterless constructor" case this method exists to work around (e.g. a
                // constructor with only optional parameters); Dynamitey's own binder can bind that.
                // Catching Exception here would also swallow a genuine failure from inside a real
                // parameterless constructor and silently invoke it a second time via the DLR path
                // (cs/catch-of-all-exceptions).
                //
                // MissingMemberException, not its MissingMethodException subclass: the documented
                // exception is MissingMethodException, but the same docs direct reduced-surface
                // targets to catch the base class instead, and netstandard2.0 is one. The base is
                // still narrow - it can only mean "the member is absent", never "the constructor
                // threw", which arrives as TargetInvocationException and must keep propagating.
                tObjectPrototype = Dynamic.InvokeConstructor(typeof(TObjectPrototype))!;
            }
            return tObjectPrototype!;
        }
    }


}
