using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PricingMulticastCommon
{
    public class Logger
    {
        public static void Log(string format, params object[] arg)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}-[{1}]\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), Thread.CurrentThread.ManagedThreadId);
            sb.AppendFormat(format, arg);
            Console.WriteLine(sb.ToString());
        }
    }
}
