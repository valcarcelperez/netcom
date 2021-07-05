using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemperatureClientServerCommon
{
    public enum ProtocolStep
    {
        None,
        Connecting,
        Disconnecting,
        ReceivingFirstMessage,
        ReceivingAckMessage,
        SendingAckMessage,
        SendingTemperatureMessage,
        Completed
    }
}
