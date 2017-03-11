using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hong.Common.Extendsion
{
    public class Cookie
    {
        public static void Set(string name, string value, int expire = 0)
        {
            throw new NotSupportedException();
        }

        public static void Set(string name, string value, string domain = null, int expire = 0)
        {
            throw new NotSupportedException();
        }

        public static string Get(string name)
        {
            throw new NotSupportedException();
        }
    }
}
