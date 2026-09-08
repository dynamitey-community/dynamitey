



namespace Dynamitey.Internal.Compat
{

    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;

    /// <summary>
    /// Framework-compatibility shims. The name is a leftover from when this library targeted
    /// <c>net40</c>; that target is gone, but the type survives because the code below is still
    /// the only supplier of <see cref="GetDefaultThreadCurrentCulture"/> on some targets.
    /// </summary>
    public static class Net40
    {
        /// <summary>
        /// Gets the default culture for threads in the current application domain.
        /// </summary>
        /// <returns>
        /// The default thread culture, or <c>null</c> when none has been set - which is the
        /// normal state unless the application assigned one.
        /// </returns>
        [SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification =
            "GetDefaultThreadCurrentCulture is declared public API (PublicAPI.Unshipped.txt) despite " +
            "living under the Internal namespace: turning it into a property is a breaking signature " +
            "change, out of scope for an analyzer-driven cleanup - same public-API-freeze reasoning as " +
            "the CA1819 sites (see Invocation.Args, Invocation.cs).")]
        public static CultureInfo? GetDefaultThreadCurrentCulture() {

            return CultureInfo.DefaultThreadCurrentCulture;

        }



    }

}

