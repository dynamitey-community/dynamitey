



namespace Dynamitey.Internal.Compat
{

    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;

    public static class Net40
    {
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

