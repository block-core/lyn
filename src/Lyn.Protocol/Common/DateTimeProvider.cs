using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Lyn.Protocol.Common
{
    public class DateTimeProvider : IDateTimeProvider
    {
        /// <inheritdoc />
        public virtual DateTime GetUtcNow()
        {
            return DateTime.UtcNow;
        }
    }
}