using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if WindowsCE
    using OpenNETCF.Security.Cryptography;
#endif

using System.Security.Cryptography;

namespace OpenNETCF.WindowsAzure.StorageClient
{
    public abstract class StorageCredentials
    {
        public string AccountName { get; set; }

        public abstract bool CanComputeHmac { get; }
        public abstract string ComputeHmac(string value);

    }

    public class StorageCredentialsAccountAndKey : StorageCredentials
    {
        private byte[] AccountKey { get; set; }

        public StorageCredentialsAccountAndKey(string accountName, string key)
            : this(accountName, Convert.FromBase64String(key))
        {
        }

        public StorageCredentialsAccountAndKey(string accountName, byte[] key)
        {
            AccountName = accountName;
            AccountKey = key;
        }

        public override bool CanComputeHmac
        {
            get { return true; }
        }

        public override string ComputeHmac(string value)
        {            
            using (var hmacSha256 = new HMACSHA256(AccountKey))
            {
                var dataToHash = System.Text.Encoding.UTF8.GetBytes(value);
                return Convert.ToBase64String(hmacSha256.ComputeHash(dataToHash));
            }
        }
    }
}

