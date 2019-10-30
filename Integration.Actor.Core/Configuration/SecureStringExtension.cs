using System;
using System.Runtime.InteropServices;
using System.Security;

namespace ServiceFabric.Integration.Actor.Core.Configuration
{
    public static class SecureStringExtensions
    {
        /// <summary>
        /// Gets a plaintext string value from a SecureString to use in APIs that don't accept SecureString parameters.
        /// </summary>
        /// <param name="secureString"></param>
        /// <returns></returns>
        public static string ToUnsecureString(this SecureString secureString)
        {
            if (secureString == null)
            {
                throw new ArgumentNullException();
            }

            IntPtr unmanagedString = IntPtr.Zero;

            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }
    }
}
