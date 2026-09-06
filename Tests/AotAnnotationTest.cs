using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Dynamitey;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    /// <summary>
    /// Guards the trim/AOT annotations added for issue #4.
    ///
    /// This library is built on the DLR and can never be trim-safe or AOT-safe. The
    /// annotations exist so a consumer finds that out at build time, naming the API they
    /// called, instead of at runtime from a RuntimeBinderException claiming a member they
    /// can plainly see does not exist.
    ///
    /// The failure mode being guarded is a quiet one: someone adds a public method that
    /// dispatches dynamically, forgets the attributes, and nothing complains. Their build
    /// is green, the suite is green, and the gap only shows up in a consumer's AOT publish
    /// — as an anonymous warning inside Dynamitey internals that names nothing useful.
    ///
    /// An AOT publish cannot catch that either: it only warns about APIs a test happens to
    /// call. These tests check the surface itself, so they catch an unannotated API whether
    /// or not anything calls it, and they run everywhere without the NativeAOT toolchain.
    ///
    /// Adding an entry to an exemption list below is a deliberate act and needs a reason.
    /// </summary>
    [TestFixture]
    public class AotAnnotationTest
    {
        /// <summary>
        /// Public statics on <see cref="Dynamic"/> that genuinely do not generate code at
        /// runtime. Each is exempt for a stated reason, not because it was overlooked.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, string> DynamicCodeExempt =
            new Dictionary<string, string>
            {
                ["ClearCaches"] =
                    "Clears dictionaries. Touches no binder and emits nothing.",
                ["GenericDelegateType"] =
                    "Indexes InvokeHelper's precomputed Action<>/Func<> array. No MakeGenericType, "
                    + "no emit - it returns a Type that already exists.",
                ["AwaitResult"] =
                    "Awaits a Task and reads its Result by reflection. Reflection is a trimming "
                    + "hazard, not a dynamic-code one, so it carries RequiresUnreferencedCode only.",
            };

        /// <summary>
        /// Public statics on <see cref="Dynamic"/> that resolve nothing by name and so
        /// survive trimming.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, string> UnreferencedCodeExempt =
            new Dictionary<string, string>
            {
                ["ClearCaches"] =
                    "Clears dictionaries. Resolves no member by name.",
                ["GenericDelegateType"] =
                    "Returns a precomputed Type from an array. Resolves no member by name.",
            };

        private static IEnumerable<MethodInfo> PublicStaticsOnDynamic() =>
            typeof(Dynamic)
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(it => !it.IsSpecialName);

        private static bool HasAttribute(MethodInfo method, string attributeTypeName) =>
            method.GetCustomAttributes(inherit: false)
                  .Any(it => it.GetType().Name == attributeTypeName);

        [Test]
        public void EveryPublicDynamicMethodRequiresDynamicCodeUnlessExempt()
        {
            var tMissing = PublicStaticsOnDynamic()
                .Where(it => !DynamicCodeExempt.ContainsKey(it.Name))
                .Where(it => !HasAttribute(it, nameof(RequiresDynamicCodeAttribute)))
                .Select(it => it.Name)
                .Distinct()
                .OrderBy(it => it)
                .ToList();

            Assert.That(tMissing, Is.Empty,
                "These public members of Dynamic dispatch through the DLR but carry no "
                + "[RequiresDynamicCode]. A consumer AOT-compiling against them gets no warning "
                + "and a misleading runtime failure. Annotate them, or add them to "
                + "DynamicCodeExempt with the reason they are safe.");
        }

        [Test]
        public void EveryPublicDynamicMethodRequiresUnreferencedCodeUnlessExempt()
        {
            var tMissing = PublicStaticsOnDynamic()
                .Where(it => !UnreferencedCodeExempt.ContainsKey(it.Name))
                .Where(it => !HasAttribute(it, nameof(RequiresUnreferencedCodeAttribute)))
                .Select(it => it.Name)
                .Distinct()
                .OrderBy(it => it)
                .ToList();

            Assert.That(tMissing, Is.Empty,
                "These public members of Dynamic resolve members by name but carry no "
                + "[RequiresUnreferencedCode]. Trimming can remove what they resolve, and the "
                + "failure reports the member as absent rather than trimmed. Annotate them, or "
                + "add them to UnreferencedCodeExempt with the reason they are safe.");
        }

        /// <summary>
        /// An exemption list that names a method which no longer exists is worse than no list:
        /// it silently stops guarding whatever replaced it.
        /// </summary>
        [Test]
        public void ExemptionListsNameOnlyMethodsThatExist()
        {
            var tActual = PublicStaticsOnDynamic().Select(it => it.Name).ToHashSet();

            var tStale = DynamicCodeExempt.Keys
                .Concat(UnreferencedCodeExempt.Keys)
                .Distinct()
                .Where(it => !tActual.Contains(it))
                .OrderBy(it => it)
                .ToList();

            Assert.That(tStale, Is.Empty,
                "These names are exempted but no longer exist on Dynamic. Remove them, so the "
                + "lists cannot quietly excuse a method that took the same name later.");
        }

        /// <summary>
        /// The annotations are only useful if their messages say something. A consumer reads
        /// the message at their own build, having never seen this source.
        /// </summary>
        [Test]
        public void EveryAnnotationCarriesAnExplanation()
        {
            var tUseless = new List<string>();

            foreach (var tMethod in PublicStaticsOnDynamic())
            {
                foreach (var tAttribute in tMethod.GetCustomAttributes(inherit: false))
                {
                    var tName = tAttribute.GetType().Name;
                    if (tName != nameof(RequiresDynamicCodeAttribute)
                        && tName != nameof(RequiresUnreferencedCodeAttribute))
                    {
                        continue;
                    }

                    var tMessage = tAttribute.GetType()
                        .GetProperty("Message")?.GetValue(tAttribute) as string;

                    if (string.IsNullOrWhiteSpace(tMessage) || tMessage.Length < 30)
                    {
                        tUseless.Add($"{tMethod.Name} [{tName}]");
                    }
                }
            }

            Assert.That(tUseless, Is.Empty,
                "These annotations have no message, or one too short to act on. The message is "
                + "the entire benefit - it is what a consumer sees at their build.");
        }
    }
}
