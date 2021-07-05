using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemperatureClientServerCommon
{
    public abstract class TemperatureSenderProtocolBase
    {
        private ProtocolStep protocolStep = ProtocolStep.None;
        protected int timeout;

        protected object sync = new object();

        public ProtocolStep ProtocolStep
        {
            get
            {
                lock (sync)
                {
                    return this.protocolStep;
                }
            }

            set
            {
                lock (sync)
                {
                    this.protocolStep = value;
                }
            }
        }
    }
}
