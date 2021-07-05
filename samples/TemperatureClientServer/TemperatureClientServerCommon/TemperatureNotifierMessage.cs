using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemperatureClientServerCommon
{
    public class TemperatureNotifierMessage
    {
        public TemperatureNotifierMessageType MessageType { get; set; }
        public DateTime MessageDateTime { get; set; }
        public string ClientId { get; set; }
        public long MessageId { get; set; }
        public decimal Temperature { get; set; }
    }
}
