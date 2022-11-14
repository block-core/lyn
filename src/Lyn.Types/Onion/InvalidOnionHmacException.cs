using System;

namespace Lyn.Types.Onion
{
    [Serializable]
    public class InvalidOnionHmacException : Exception
    {
        public InvalidOnionHmacException() : 
            base("Onion has an invalid HMAC") 
        { 

        }
    }
}
