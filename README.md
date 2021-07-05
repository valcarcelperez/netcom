# netcom
New place for "TCP/IP Communication Framework" from https://cftcpip.codeplex.com/
Moved from CodePlex.

Legacy library for TCP/IP. Last updated in 2013. https://archive.codeplex.com/?p=cftcpip

## Original Readme
---

### cftcpip
TCP/IP Communication Framework

TCP/IP Communication Framework (TCP/IP CF) is a library that wraps the .NET Socket class and defines several classes for developing communication applications..

**Project Description**

TCP/IP Communication Framework (TCP/IP CF) is a library that wraps the .NET Socket class and defines several classes for developing communication applications that use TCP/IP. TCP/IP CF defines asynchronous operations and is designed to be used in small applications that communicate with a few devices or server applications that maintain communication with a large number of devices.

**Features**

- Multi-threaded/Asynchronous
- Event-based
- Ready-to-use clients and servers
- Multicast
- Unit test.
- Extensible

**Main classes**

`TcpServer`
> A server that listens on a TCP port and accepts connections from clients.
> Raises an event per each connected client. The event arguments contains the client socket.
> The listening process executes on a separated thread.

`UdpServer`
> A server that listens on a UDP port and receives packages from clients.
> Raises an event per each received package. The event contains the package and the remote end point that sent the package.
> The listening process executes on a separated thread.

`UdpMessageServer<T>`
> A server that listens on a UDP port and receives messages of type T from clients.
> Raises an event per each received message. The event contains the message and the remote end point that sent the message.
> Uses a custom message encoder that is passes in the constructor for decoding the received package.

`TcpMessageProcessor<T>`
> A client used to connect to a TCP server and send/receive messages of type T.
> Uses a custom message encoder and a custom message framer.
> Supports asynchronous operations.

`UdpMessageProcessor<T>`
> A client used to send/receive UDP messages of type T.
> Uses a custom message encoder.
> Supports asynchronous operations.

`MulticastMessageReceiver<T>`
> A receiver (client) that listens for UDP messages that are sent to a multicast group.
> Uses a custom message encoder.

`MulticastMessageSender<T>`
> A sender (server) that sends UDP messages to a multicast group.
> Uses a custom message encoder.
> Supports asynchronous operations.

**Support classes**

`CancelableMethodManager`
> Executes a cancelable process in a separated thread.
> Implements a cooperative cancellation of a process without re-implementing the signaling mechanism.
> Includes the method `Start()` and `Stop()`.
> Supports the implementation of servers and long-running processes.

`ProcessExecutor`
> Executes a process and calls a delegate if the process times out.
> Implements a cooperative (without killing a thread) cancellation of a process that may time out.
> Supports the implementation of communication protocols when a client and a server exchange messages.
> Supports asynchronous operations.

**Sample applications**

Pricing Multicast
> Demonstrates how to use the classes `MulticastMessageSender` and `MulticastMessageReceiver`.
> Pricing Source - Application that uses `MulticastMessageSender` to send pricing information.
> Pricing Client - Application that uses `MulticastMessageReceiver` to receive pricing information sent by Pricing Source.

Temperature Client Server
> Demonstrate how to use the classes `TcpServer`, `UdpMessageServer`, `TcpMessageProcessor` and `UdpMessageProcessor`.
> Demonstrates how to use library for implementing a custom protocol.

Temperature Generator - Application that uses `TcpMessageProcessor` and `UdpMessageProcessor` for sending temperature updates to a server.
> Server - Application that uses `TcpServer` and `UdpMessageServer` for receiving temperature updates from clients. Uses `TcpMessageProcessor` when sending confirmation messages to clients.