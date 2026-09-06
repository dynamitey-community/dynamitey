



namespace Dynamitey.Internal.Compat
{

    using System.Globalization;

    public static class Net40
    {
        public static CultureInfo? GetDefaultThreadCurrentCulture() {

            return CultureInfo.DefaultThreadCurrentCulture;

        }



    }

}

