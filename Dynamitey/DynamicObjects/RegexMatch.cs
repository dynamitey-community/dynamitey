using System;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;

using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using Dynamitey.Internal.Compat;

namespace Dynamitey.DynamicObjects
{

    /// <summary>
    /// A Regex Match Interface
    /// </summary>
    public interface IRegexMatch
    {
        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        string Value { get;}
    }



    /// <summary>
    /// A Dynamic Regex Match
    /// </summary>
    public class RegexMatch : BaseObject, IRegexMatch
    {
       
        private readonly Match _match;
       
        private readonly Regex _regex;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegexMatch" /> class.
        /// </summary>
        /// <param name="match">The match.</param>
        /// <param name="regex">The regex.</param>
        [RequiresDynamicCode("Constructing any BaseObject-derived type instantiates System.Dynamic.DynamicObject, whose default constructor requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public RegexMatch(Match match, Regex regex = null)
        {
            _match = match;
            _regex = regex;
        }


        /// <summary>
        /// Gets the dynamic member names.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<string> GetDynamicMemberNames()
        {
            if (_regex == null)
                return Enumerable.Empty<string>();
            return _regex.GetGroupNames();
        }

        /// <summary>
        /// Tries the get member.
        /// </summary>
        /// <param name="binder">The binder.</param>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
            "Calls the annotated TryTypeForName/Dynamic.InvokeConstructor/Dynamic.CoerceConvert. " +
            "This is a DynamicObject.TryGetMember override: it can't carry " +
            "[RequiresUnreferencedCode] itself without mismatching the unannotated base member, " +
            "and the DLR invokes it only after the consumer's own dynamic member access already " +
            "triggered the framework's warning.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Same calls as above; see the IL2026 suppression on this member.")]
       public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var tGroup = _match.Groups[binder.Name];
            if (!TryTypeForName(binder.Name, out var outType))
                outType = typeof (string);

            if (!tGroup.Success)
            {
                result = null;
                if (outType.GetTypeInfo().IsValueType)
                    result = Dynamic.InvokeConstructor(outType);
                return true;
            }

            result = Dynamic.CoerceConvert(tGroup.Value, outType);
            return true;
        }

       /// <summary>
       /// Gets the <see cref="System.String" /> with the specified value.
       /// </summary>
       /// <value>
       /// The <see cref="System.String" />.
       /// </value>
       /// <param name="value">The value.</param>
       /// <returns></returns>
        public string this[int value]
        {
            get
            {
                var tGroup = _match.Groups[value];

                if (!tGroup.Success)
                {
                    return null;
                }
                return tGroup.Value;
            }
        }

        /// <summary>
        /// Gets the <see cref="System.String" /> with the specified value.
        /// </summary>
        /// <value>
        /// The <see cref="System.String" />.
        /// </value>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public string this[string value]
        {
            get
            {
                var tGroup = _match.Groups[value];

                if (!tGroup.Success)
                {
                    return null;
                }
                return tGroup.Value;
            }
        }

        string IRegexMatch.Value => _match.Value;

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return _match.ToString();
        }
    }
}
