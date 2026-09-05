



namespace Dynamitey.Internal.Compat
{

    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using System.Globalization;
    using System.Threading;

    public static class Net40
    {
        public static CultureInfo GetDefaultThreadCurrentCulture() {

            return CultureInfo.DefaultThreadCurrentCulture;

        }



    }

}

