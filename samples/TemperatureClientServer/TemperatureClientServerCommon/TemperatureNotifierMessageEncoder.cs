using System;
using System.Collections.Generic;
using System.CommunicationFramework.Interfaces;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TemperatureClientServerCommon
{
    public class TemperatureNotifierMessageEncoder : IMessageEncoder<TemperatureNotifierMessage>
    {
        public const string FrameBegin = "ZCZC";
        public static byte[] FrameBeginBytes = Encoding.ASCII.GetBytes(FrameBegin);

        public int EncodeMessage(TemperatureNotifierMessage message, byte[] buffer, int index)
        {
            int initialIndex = index;
            XElement xmlMsg = new XElement("msg");
            xmlMsg.Add(new XAttribute("type", message.MessageType));
            xmlMsg.Add(new XAttribute("dateTime", message.MessageDateTime.ToString("yyyyMMddHHmmssfff")));
            xmlMsg.Add(new XAttribute("messageId", message.MessageId));
            xmlMsg.Add(new XAttribute("clientId", message.ClientId));
            xmlMsg.Add(new XAttribute("value", message.Temperature));

            byte[] data = Encoding.ASCII.GetBytes(xmlMsg.ToString());
            byte[] dataLength = BitConverter.GetBytes((ushort)data.Length);
            
            Array.Copy(FrameBeginBytes, 0, buffer, index, FrameBeginBytes.Length);
            index += FrameBeginBytes.Length;            
            Array.Copy(dataLength, 0, buffer, index, dataLength.Length);
            index += dataLength.Length;
            Array.Copy(data, 0, buffer, index, data.Length);
            index += data.Length;
            return index - initialIndex;
        }

        public TemperatureNotifierMessage DecodeMessage(byte[] buffer, int index, int size)
        {
            int initialIndex = index;
            if (size < 6)
            {
                throw new FormatException("Invalid format. Frame is too short.");
            }

            byte[] receivedFrameBegin = new byte[FrameBeginBytes.Length];
            Array.Copy(buffer, index, receivedFrameBegin, 0, FrameBeginBytes.Length);
            index += FrameBeginBytes.Length;
            bool expectedFrameBegin = CompareArray(FrameBeginBytes, receivedFrameBegin, FrameBeginBytes.Length);
            if (!expectedFrameBegin)
            {
                throw new FormatException("Invalid format. Invalid Frame Begin");
            }

            ushort dataLength = BitConverter.ToUInt16(buffer, index);
            index += 2;

            if (size != dataLength + 2 + FrameBeginBytes.Length)
            {
                throw new FormatException("Invalid format. Invalid data length.");
            }

            string xmlData = Encoding.ASCII.GetString(buffer, index, dataLength);
            XElement xml = XElement.Parse(xmlData);
            TemperatureNotifierMessage message = new TemperatureNotifierMessage();
            message.ClientId = xml.Attribute("clientId").Value;
            message.MessageId = long.Parse(xml.Attribute("messageId").Value);
            message.MessageType = (TemperatureNotifierMessageType)Enum.Parse(typeof(TemperatureNotifierMessageType), xml.Attribute("type").Value);
            if (message.MessageType == TemperatureNotifierMessageType.Temperature)
            {
                message.Temperature = decimal.Parse(xml.Attribute("value").Value);
            }

            string dateTime = xml.Attribute("dateTime").Value;
            if (dateTime.Length != 17)
            {
                throw new FormatException("Invalid format. Invalid datetime.");
            }

            int year = int.Parse(dateTime.Substring(0, 4));
            int month = int.Parse(dateTime.Substring(4, 2));
            int day = int.Parse(dateTime.Substring(6, 2));
            int hour = int.Parse(dateTime.Substring(8, 2));
            int min = int.Parse(dateTime.Substring(10, 2));
            int second = int.Parse(dateTime.Substring(12, 2));
            int millisecond = int.Parse(dateTime.Substring(14, 3));
            message.MessageDateTime = new DateTime(year, month, day, hour, min, second, millisecond);

            return message;
        }

        private bool CompareArray(byte[] a, byte[] b, int size)
        {
            for (int i = 0; i < size; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
