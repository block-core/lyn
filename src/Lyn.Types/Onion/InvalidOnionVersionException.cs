using System;

namespace Lyn.Types.Onion
{
    [Serializable]
    public class InvalidOnionVersionException : Exception
    {
        public InvalidOnionVersionException() :
            base("Legacy Onion Format is not supported anymore")
        {

        }
    }
}
