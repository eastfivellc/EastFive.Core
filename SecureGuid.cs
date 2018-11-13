using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EastFive.Security
{
    public class SecureGuid
    {
        public static Guid Generate()
        {
            var guidData = new byte[0x10];
            System.Security.Cryptography.RandomNumberGenerator.Create().GetBytes(guidData);
            return new Guid(guidData);
        }
    }
}
