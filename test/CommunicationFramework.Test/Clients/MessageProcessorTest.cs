namespace CommunicationFramework.Clients.UnitTest
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Communication.UnitTest;
    using System.CommunicationFramework.Clients;
    using System.Net.Sockets;

    public class MessageProcessorDesc : MessageProcessor<MockMessage>
    {
        public MessageProcessorDesc(MockEncoder encoder, Socket client)
            :base(encoder, client)
        {
        }
    }

    [TestClass]
    public class MessageProcessorTest
    {
        [TestMethod]
        public void MessageProcessor_Constructor()
        {
            MockEncoder encoder = new MockEncoder();
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            MessageProcessorDesc target = new MessageProcessorDesc(encoder, client);

            Assert.AreEqual(client, target.Client);
            Assert.AreEqual(SocketFlags.None, target.SocketFlags);
        }

        [TestMethod]
        public void MessageProcessor_Constructor_Client_And_Framer_Can_Be_Null()
        {
            MockEncoder encoder = new MockEncoder();
            Socket client = null;
            MessageProcessorDesc target = new MessageProcessorDesc(encoder, client);
            Assert.AreEqual(SocketFlags.None, target.SocketFlags);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void MessageProcessor_Constructor_Encoder_Cannot_Be_Null()
        {
            MockEncoder encoder = null;
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            MessageProcessorDesc target = new MessageProcessorDesc(encoder, client);
        }

        [TestMethod]
        public void MessageProcessor_Dispose_Client_Must_Be_Disposed()
        {
            MockEncoder encoder = new MockEncoder();
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            MessageProcessorDesc target = new MessageProcessorDesc(encoder, client);
            target.Dispose();

            try
            {
                int size = client.ReceiveBufferSize;
            }
            catch (Exception ex)
            {
                Assert.AreEqual(typeof(ObjectDisposedException), ex.GetType());
                return;
            }

            Assert.Fail("The client was not disposed.");
        }
    }
}
