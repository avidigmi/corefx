// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualMode
    {
        // Ports 8 and 8887 are unassigned as per https://www.iana.org/assignments/service-names-port-numbers/service-names-port-numbers.txt
        private const int UnusedPort = 8;
        private const int UnusedBindablePort = 8887;

        private readonly ITestOutputHelper _log;

        private static IPAddress[] ValidIPv6Loopbacks = new IPAddress[] {
            new IPAddress(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 127, 0, 0, 1 }, 0),  // ::127.0.0.1
            IPAddress.Loopback.MapToIPv6(),                                                     // ::ffff:127.0.0.1
            IPAddress.IPv6Loopback                                                              // ::1
        };

        public DualMode(ITestOutputHelper output)
        {
            _log = TestLogging.GetInstance();
            Assert.True(Capability.IPv4Support() && Capability.IPv6Support());
        }

        private static void AssertDualModeEnabled(Socket socket, IPAddress listenOn)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.True(socket.DualMode);
            }
            else
            {
                Assert.True((listenOn != IPAddress.IPv6Any && !listenOn.IsIPv4MappedToIPv6) || socket.DualMode);
            }
        }

        #region Constructor and Property

        [Fact]
        public void DualModeConstructor_InterNetworkV6Default()
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            Assert.Equal(AddressFamily.InterNetworkV6, socket.AddressFamily);
            Assert.True(socket.DualMode);
        }

        [Fact]
        public void DualModeUdpConstructor_DualModeConfgiured()
        {
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            Assert.Equal(AddressFamily.InterNetworkV6, socket.AddressFamily);
            Assert.True(socket.DualMode);
        }

        [Fact]
        public void NormalConstructor_DualModeConfgiureable()
        {
            Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            Assert.False(socket.DualMode);

            socket.DualMode = true;
            Assert.True(socket.DualMode);

            socket.DualMode = false;
            Assert.False(socket.DualMode);
        }

        [Fact]
        public void IPv4Constructor_DualModeThrows()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Assert.Throws<NotSupportedException>(() =>
            {
                Assert.False(socket.DualMode);
            });

            Assert.Throws<NotSupportedException>(() =>
            {
                socket.DualMode = true;
            });
        }

        #endregion Constructor and Property
		
        #region Connect to IPAddress

        [Fact] // Base case
        public void Socket_ConnectV4IPAddressToV4Host_Throws()
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.DualMode = false;

            Assert.Throws<NotSupportedException>(() =>
            {
                socket.Connect(IPAddress.Loopback, UnusedPort);
            });
        }

        [Fact] // Base Case
        public void ConnectV4MappedIPAddressToV4Host_Success()
        {
            DualModeConnect_IPAddressToHost_Helper(IPAddress.Loopback.MapToIPv6(), IPAddress.Loopback, false);
        }

        [Fact] // Base Case
        public void ConnectV4MappedIPAddressToDualHost_Success()
        {
            DualModeConnect_IPAddressToHost_Helper(IPAddress.Loopback.MapToIPv6(), IPAddress.IPv6Any, true);
        }

        [Fact]
        public void ConnectV4IPAddressToV4Host_Success()
        {
            DualModeConnect_IPAddressToHost_Helper(IPAddress.Loopback, IPAddress.Loopback, false);
        }

        [Fact]
        public void ConnectV6IPAddressToV6Host_Success()
        {
            DualModeConnect_IPAddressToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false);
        }

        [Fact]
        public void ConnectV4IPAddressToV6Host_Fails()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_IPAddressToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, false);
            });
        }

        [Fact]
        public void ConnectV6IPAddressToV4Host_Fails()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_IPAddressToHost_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, false);
            });
        }

        [Fact]
        public void ConnectV4IPAddressToDualHost_Success()
        {
            DualModeConnect_IPAddressToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true);
        }

        [Fact]
        public void ConnectV6IPAddressToDualHost_Success()
        {
            DualModeConnect_IPAddressToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true);
        }

        private void DualModeConnect_IPAddressToHost_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer)
        {
            int port;
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out port))
            {
                socket.Connect(connectTo, port);
                Assert.True(socket.Connected);
            }
        }

        #endregion Connect to IPAddress

        #region Connect to IPEndPoint

        [Fact] // Base case
        public void Socket_ConnectV4IPEndPointToV4Host_Throws()
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.DualMode = false;

            Assert.Throws<SocketException>(() =>
            {
                socket.Connect(new IPEndPoint(IPAddress.Loopback, UnusedPort));
            });
        }

        [Fact] // Base case
        public void ConnectV4MappedIPEndPointToV4Host_Success()
        {
            DualModeConnect_IPEndPointToHost_Helper(IPAddress.Loopback.MapToIPv6(), IPAddress.Loopback, false);
        }

        [Fact] // Base case
        public void ConnectV4MappedIPEndPointToDualHost_Success()
        {
            DualModeConnect_IPEndPointToHost_Helper(IPAddress.Loopback.MapToIPv6(), IPAddress.IPv6Any, true);
        }

        [Fact]
        public void ConnectV4IPEndPointToV4Host_Success()
        {
            DualModeConnect_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.Loopback, false);
        }

        [Fact]
        public void ConnectV6IPEndPointToV6Host_Success()
        {
            DualModeConnect_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false);
        }

        [Fact]
        public void ConnectV4IPEndPointToV6Host_Fails()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, false);
            });
        }

        [Fact]
        public void ConnectV6IPEndPointToV4Host_Fails()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, false);
            });
        }

        [Fact]
        public void ConnectV4IPEndPointToDualHost_Success()
        {
            DualModeConnect_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true);
        }

        [Fact]
        public void ConnectV6IPEndPointToDualHost_Success()
        {
            DualModeConnect_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true);
        }

        private void DualModeConnect_IPEndPointToHost_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer)
        {
            int port;
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out port))
            {
                socket.Connect(new IPEndPoint(connectTo, port));
                Assert.True(socket.Connected);
            }
        }

        #endregion Connect to IPEndPoint

        #region Connect to IPAddress[]

        [Fact] // Base Case
        [PlatformSpecific(PlatformID.Windows)]
        // "None of the discovered or specified addresses match the socket address family."
        public void Socket_ConnectV4IPAddressListToV4Host_Throws()
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.DualMode = false;

            int port;
            using (SocketServer server = new SocketServer(_log, IPAddress.Loopback, false, out port))
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    socket.Connect(new IPAddress[] { IPAddress.Loopback }, port);
                });
            }
        }

        [Theory]
        [MemberData(nameof(DualMode_IPAddresses_ListenOn_DualMode_Throws_Data))]
        [PlatformSpecific(PlatformID.Windows)]
        public void DualModeConnect_IPAddressListToHost_Throws(IPAddress[] connectTo, IPAddress listenOn, bool dualModeServer)
        {
            Assert.Throws<SocketException>(() => DualModeConnect_IPAddressListToHost_Success(connectTo, listenOn, dualModeServer));
        }

        [Theory]
        [MemberData(nameof(DualMode_IPAddresses_ListenOn_DualMode_Success_Data))]
        [PlatformSpecific(PlatformID.Windows)]
        public void DualModeConnect_IPAddressListToHost_Success(IPAddress[] connectTo, IPAddress listenOn, bool dualModeServer)
        {
            int port;
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out port))
            {
                socket.Connect(connectTo, port);
                Assert.True(socket.Connected);
            }
        }

        #endregion Connect to IPAdddress[]

        #region Connect to host string

        [Theory]
        [MemberData(nameof(DualMode_Connect_IPAddress_DualMode_Data))]
        [PlatformSpecific(PlatformID.Windows)]
        public void DualModeConnect_LoopbackDnsToHost_Helper(IPAddress listenOn, bool dualModeServer)
        {
            int port;
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out port))
            {
                socket.Connect("localhost", port);
                Assert.True(socket.Connected);
            }
        }

        #endregion Connect to host string

        #region Connect to DnsEndPoint

        [Theory]
        [MemberData(nameof(DualMode_Connect_IPAddress_DualMode_Data))]
        [PlatformSpecific(PlatformID.Windows)]
        private void DualModeConnect_DnsEndPointToHost_Helper(IPAddress listenOn, bool dualModeServer)
        {
            int port;
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out port))
            {
                socket.Connect(new DnsEndPoint("localhost", port, AddressFamily.Unspecified));
                Assert.True(socket.Connected);
            }
        }

        #endregion Connect to DnsEndPoint

        #region BeginConnect to IPAddress

        [Fact] // Base case
        public void Socket_BeginConnectV4IPAddressToV4Host_Throws()
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.DualMode = false;

            Assert.Throws<NotSupportedException>(() =>
            {
                socket.BeginConnect(IPAddress.Loopback, UnusedPort, null, null);
            });
        }

        [Fact]
        public void BeginConnectV4IPAddressToV4Host_Success()
        {
            DualModeBeginConnect_IPAddressToHost_Helper(IPAddress.Loopback, IPAddress.Loopback, false);
        }

        [Fact]
        public void BeginConnectV6IPAddressToV6Host_Success()
        {
            DualModeBeginConnect_IPAddressToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false);
        }

        [Fact]
        public void BeginConnectV4IPAddressToV6Host_Fails()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeBeginConnect_IPAddressToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, false);
            });
        }

        [Fact]
        public void BeginConnectV6IPAddressToV4Host_Fails()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeBeginConnect_IPAddressToHost_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, false);
            });
        }

        [Fact]
        public void BeginConnectV4IPAddressToDualHost_Success()
        {
            DualModeBeginConnect_IPAddressToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true);
        }

        [Fact]
        public void BeginConnectV6IPAddressToDualHost_Success()
        {
            DualModeBeginConnect_IPAddressToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true);
        }

        private void DualModeBeginConnect_IPAddressToHost_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer)
        {
            int port;
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out port))
            {
                IAsyncResult async = socket.BeginConnect(connectTo, port, null, null);
                socket.EndConnect(async);
                Assert.True(socket.Connected);
            }
        }

        #endregion BeginConnect to IPAddress

        #region BeginConnect to IPEndPoint

        [Fact] // Base case
        // "The system detected an invalid pointer address in attempting to use a pointer argument in a call"
        public void Socket_BeginConnectV4IPEndPointToV4Host_Throws()
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.DualMode = false;
            Assert.Throws<SocketException>(() =>
            {
                socket.BeginConnect(new IPEndPoint(IPAddress.Loopback, UnusedPort), null, null);
            });
        }

        [Fact]
        public void BeginConnectV4IPEndPointToV4Host_Success()
        {
            DualModeBeginConnect_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.Loopback, false);
        }

        [Fact]
        public void BeginConnectV6IPEndPointToV6Host_Success()
        {
            DualModeBeginConnect_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false);
        }

        [Fact]
        public void BeginConnectV4IPEndPointToV6Host_Fails()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeBeginConnect_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, false);
            });
        }

        [Fact]
        public void BeginConnectV6IPEndPointToV4Host_Fails()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeBeginConnect_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, false);
            });
        }

        [Fact]
        public void BeginConnectV4IPEndPointToDualHost_Success()
        {
            DualModeBeginConnect_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true);
        }

        [Fact]
        public void BeginConnectV6IPEndPointToDualHost_Success()
        {
            DualModeBeginConnect_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true);
        }

        private void DualModeBeginConnect_IPEndPointToHost_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer)
        {
            int port;
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out port))
            {
                IAsyncResult async = socket.BeginConnect(new IPEndPoint(connectTo, port), null, null);
                socket.EndConnect(async);
                Assert.True(socket.Connected);
            }
        }

        #endregion BeginConnect to IPEndPoint

        #region BeginConnect to IPAddress[]

        [Theory]
        [MemberData(nameof(DualMode_IPAddresses_ListenOn_DualMode_Data))]
        [PlatformSpecific(PlatformID.Windows)]
        private void DualModeBeginConnect_IPAddressListToHost_Helper(IPAddress[] connectTo, IPAddress listenOn, bool dualModeServer)
        {
            int port;
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out port))
            {
                IAsyncResult async = socket.BeginConnect(connectTo, port, null, null);
                socket.EndConnect(async);
                Assert.True(socket.Connected);
            }
        }

        #endregion BeginConnect to IPAdddress[]

        #region BeginConnect to host string

        [Theory]
        [MemberData(nameof(DualMode_Connect_IPAddress_DualMode_Data))]
        [PlatformSpecific(PlatformID.Windows)]
        public void DualModeBeginConnect_LoopbackDnsToHost_Helper(IPAddress listenOn, bool dualModeServer)
        {
            int port;
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out port))
            {
                IAsyncResult async = socket.BeginConnect("localhost", port, null, null);
                socket.EndConnect(async);
                Assert.True(socket.Connected);
            }
        }

        #endregion BeginConnect to host string

        #region BeginConnect to DnsEndPoint

        [Theory]
        [MemberData(nameof(DualMode_Connect_IPAddress_DualMode_Data))]
        [PlatformSpecific(PlatformID.Windows)]
        public void DualModeBeginConnect_DnsEndPointToHost_Helper(IPAddress listenOn, bool dualModeServer)
        {
            int port;
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out port))
            {
                IAsyncResult async = socket.BeginConnect(new DnsEndPoint("localhost", port), null, null);
                socket.EndConnect(async);
                Assert.True(socket.Connected);
            }
        }

        #endregion BeginConnect to DnsEndPoint

        #region ConnectAsync

        #region ConnectAsync to IPEndPoint

        [Fact] // Base case
        public void Socket_ConnectAsyncV4IPEndPointToV4Host_Throws()
        {
            Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, UnusedPort);
            Assert.Throws<NotSupportedException>(() =>
            {
                socket.ConnectAsync(args);
            });
        }

        [Fact]
        public void ConnectAsyncV4IPEndPointToV4Host_Success()
        {
            DualModeConnectAsync_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.Loopback, false);
        }

        [Fact]
        public void ConnectAsyncV6IPEndPointToV6Host_Success()
        {
            DualModeConnectAsync_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false);
        }

        [Fact]
        public void ConnectAsyncV4IPEndPointToV6Host_Fails()
        {
            Assert.Throws<SocketException>(() =>
           {
               DualModeConnectAsync_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, false);
           });
        }

        [Fact]
        public void ConnectAsyncV6IPEndPointToV4Host_Fails()
        {
            Assert.Throws<SocketException>(() =>
          {
              DualModeConnectAsync_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, false);
          });
        }

        [Fact]
        public void ConnectAsyncV4IPEndPointToDualHost_Success()
        {
            DualModeConnectAsync_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true);
        }

        [Fact]
        public void ConnectAsyncV6IPEndPointToDualHost_Success()
        {
            DualModeConnectAsync_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true);
        }

        private void DualModeConnectAsync_IPEndPointToHost_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer)
        {
            int port;
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out port))
            {
                ManualResetEvent waitHandle = new ManualResetEvent(false);
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(AsyncCompleted);
                args.RemoteEndPoint = new IPEndPoint(connectTo, port);
                args.UserToken = waitHandle;

                socket.ConnectAsync(args);

                Assert.True(waitHandle.WaitOne(Configuration.PassingTestTimeout), "Timed out while waiting for connection");
                if (args.SocketError != SocketError.Success)
                {
                    throw new SocketException((int)args.SocketError);
                }
                Assert.True(socket.Connected);
            }
        }

        #endregion ConnectAsync to IPAddress

        #region ConnectAsync to DnsEndPoint

        [Theory]
        [MemberData(nameof(DualMode_Connect_IPAddress_DualMode_Data))]
        [PlatformSpecific(PlatformID.Windows)]
        public void DualModeConnectAsync_DnsEndPointToHost_Helper(IPAddress listenOn, bool dualModeServer)
        {
            int port;
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out port))
            {
                ManualResetEvent waitHandle = new ManualResetEvent(false);
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(AsyncCompleted);
                args.RemoteEndPoint = new DnsEndPoint("localhost", port);
                args.UserToken = waitHandle;

                socket.ConnectAsync(args);

                Assert.True(waitHandle.WaitOne(Configuration.PassingTestTimeout), "Timed out while waiting for connection");
                if (args.SocketError != SocketError.Success)
                {
                    throw new SocketException((int)args.SocketError);
                }
                Assert.True(socket.Connected);
            }
        }

        [Theory]
        [MemberData(nameof(DualMode_Connect_IPAddress_DualMode_Data))]
        [ActiveIssue(4002, PlatformID.AnyUnix)]
        public void DualModeConnectAsync_Static_DnsEndPointToHost_Helper(IPAddress listenOn, bool dualModeServer)
        {
            int port;
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out port))
            {
                ManualResetEvent waitHandle = new ManualResetEvent(false);
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(AsyncCompleted);
                args.RemoteEndPoint = new DnsEndPoint("localhost", port);
                args.UserToken = waitHandle;

                Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, args);

                Assert.True(waitHandle.WaitOne(Configuration.PassingTestTimeout), "Timed out while waiting for connection");
                if (args.SocketError != SocketError.Success)
                {
                    throw new SocketException((int)args.SocketError);
                }
                Assert.True(args.ConnectSocket.Connected);
                args.ConnectSocket.Dispose();
            }
        }

        #endregion ConnectAsync to DnsEndPoint

        #endregion ConnectAsync

        #region Accept

        #region Bind

        [Fact] // Base case
        // "The system detected an invalid pointer address in attempting to use a pointer argument in a call"
        public void Socket_BindV4IPEndPoint_Throws()
        {
            Assert.Throws<SocketException>(() =>
            {
                using (Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Bind(new IPEndPoint(IPAddress.Loopback, UnusedBindablePort));
                }
            });
        }

        [Fact] // Base Case; BSoD on Win7, Win8 with IPv4 uninstalled
        public void BindMappedV4IPEndPoint_Success()
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                socket.BindToAnonymousPort(IPAddress.Loopback.MapToIPv6());
            }
        }

        [Fact] // BSoD on Win7, Win8 with IPv4 uninstalled
        public void BindV4IPEndPoint_Success()
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                socket.BindToAnonymousPort(IPAddress.Loopback);
            }
        }

        [Fact]
        public void BindV6IPEndPoint_Success()
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                socket.BindToAnonymousPort(IPAddress.IPv6Loopback);
            }
        }

        [Fact]
        public void Socket_BindDnsEndPoint_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Bind(new DnsEndPoint("localhost", UnusedBindablePort));
                }
            });
        }

        [Fact]
        // "An invalid argument was supplied"
        public void Socket_EnableDualModeAfterV4Bind_Throws()
        {
            using (Socket serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                serverSocket.DualMode = false;
                serverSocket.BindToAnonymousPort(IPAddress.IPv6Any);
                Assert.Throws<SocketException>(() =>
                {
                    serverSocket.DualMode = true;
                });
            }
        }

        #endregion Bind

        #region Accept Sync

        [Fact]
        public void AcceptV4BoundToSpecificV4_Success()
        {
            Accept_Helper(IPAddress.Loopback, IPAddress.Loopback);
        }

        [Fact]
        public void AcceptV4BoundToAnyV4_Success()
        {
            Accept_Helper(IPAddress.Any, IPAddress.Loopback);
        }

        [Fact]
        public void AcceptV6BoundToSpecificV6_Success()
        {
            Accept_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        public void AcceptV6BoundToAnyV6_Success()
        {
            Accept_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback);
        }

        [Fact]
        [ActiveIssue(5832, PlatformID.AnyUnix)]
        public void AcceptV6BoundToSpecificV4_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                Accept_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback);
            });
        }

        [Fact]
        [ActiveIssue(5832, PlatformID.AnyUnix)]
        public void AcceptV4BoundToSpecificV6_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                Accept_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback);
            });
        }

        [Fact]
        [ActiveIssue(5832, PlatformID.AnyUnix)]
        public void AcceptV6BoundToAnyV4_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                Accept_Helper(IPAddress.Any, IPAddress.IPv6Loopback);
            });
        }

        [Fact]
        public void AcceptV4BoundToAnyV6_Success()
        {
            Accept_Helper(IPAddress.IPv6Any, IPAddress.Loopback);
        }

        private void Accept_Helper(IPAddress listenOn, IPAddress connectTo)
        {
            using (Socket serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                int port = serverSocket.BindToAnonymousPort(listenOn);
                serverSocket.Listen(1);
                SocketClient client = new SocketClient(_log, serverSocket, connectTo, port);
                Socket clientSocket = serverSocket.Accept();
                Assert.True(clientSocket.Connected);
                AssertDualModeEnabled(clientSocket, listenOn);
                Assert.Equal(AddressFamily.InterNetworkV6, clientSocket.AddressFamily);
            }
        }

        #endregion Accept Sync

        #region Accept Begin/End

        [Fact]
        public void BeginAcceptV4BoundToSpecificV4_Success()
        {
            DualModeConnect_BeginAccept_Helper(IPAddress.Loopback, IPAddress.Loopback);
        }

        [Fact]
        public void BeginAcceptV4BoundToAnyV4_Success()
        {
            DualModeConnect_BeginAccept_Helper(IPAddress.Any, IPAddress.Loopback);
        }

        [Fact]
        public void BeginAcceptV6BoundToSpecificV6_Success()
        {
            DualModeConnect_BeginAccept_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        public void BeginAcceptV6BoundToAnyV6_Success()
        {
            DualModeConnect_BeginAccept_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback);
        }

        [Fact]
        public void BeginAcceptV6BoundToSpecificV4_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_BeginAccept_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback);
            });
        }

        [Fact]
        public void BeginAcceptV4BoundToSpecificV6_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_BeginAccept_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback);
            });
        }

        [Fact]
        public void BeginAcceptV6BoundToAnyV4_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_BeginAccept_Helper(IPAddress.Any, IPAddress.IPv6Loopback);
            });
        }

        [Fact]
        public void BeginAcceptV4BoundToAnyV6_Success()
        {
            DualModeConnect_BeginAccept_Helper(IPAddress.IPv6Any, IPAddress.Loopback);
        }

        private void DualModeConnect_BeginAccept_Helper(IPAddress listenOn, IPAddress connectTo)
        {
            using (Socket serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                int port = serverSocket.BindToAnonymousPort(listenOn);
                serverSocket.Listen(1);
                IAsyncResult async = serverSocket.BeginAccept(null, null);
                SocketClient client = new SocketClient(_log, serverSocket, connectTo, port);

                // Due to the nondeterministic nature of calling dispose on a Socket that is doing
                // an EndAccept operation, we expect two types of exceptions to happen.
                Socket clientSocket;
                try
                {
                    clientSocket = serverSocket.EndAccept(async);
                    Assert.True(clientSocket.Connected);
                    AssertDualModeEnabled(clientSocket, listenOn);
                    Assert.Equal(AddressFamily.InterNetworkV6, clientSocket.AddressFamily);
                    if (connectTo == IPAddress.Loopback)
                    {
                        Assert.Contains(((IPEndPoint)clientSocket.LocalEndPoint).Address, ValidIPv6Loopbacks);
                    }
                    else
                    {
                        Assert.Equal(connectTo.MapToIPv6(), ((IPEndPoint)clientSocket.LocalEndPoint).Address);
                    }
                    Assert.Equal(connectTo.MapToIPv6(), ((IPEndPoint)clientSocket.LocalEndPoint).Address);
                }
                catch (ObjectDisposedException) { }
                catch (SocketException) { }

                Assert.True(
                    client.WaitHandle.WaitOne(Configuration.PassingTestTimeout),
                    "Timed out while waiting for connection");

                if ( client.Error != SocketError.Success)
                {
                    throw new SocketException((int)client.Error);
                }
            }
        }

        #endregion Accept Begin/End

        #region Accept Async/Event

        [Fact]
        public void AcceptAsyncV4BoundToSpecificV4_Success()
        {
            DualModeConnect_AcceptAsync_Helper(IPAddress.Loopback, IPAddress.Loopback);
        }

        [Fact]
        public void AcceptAsyncV4BoundToAnyV4_Success()
        {
            DualModeConnect_AcceptAsync_Helper(IPAddress.Any, IPAddress.Loopback);
        }

        [Fact]
        public void AcceptAsyncV6BoundToSpecificV6_Success()
        {
            DualModeConnect_AcceptAsync_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        public void AcceptAsyncV6BoundToAnyV6_Success()
        {
            DualModeConnect_AcceptAsync_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback);
        }

        [Fact]
        public void AcceptAsyncV6BoundToSpecificV4_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_AcceptAsync_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback);
            });
        }

        [Fact]
        public void AcceptAsyncV4BoundToSpecificV6_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_AcceptAsync_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback);
            });
        }

        [Fact]
        public void AcceptAsyncV6BoundToAnyV4_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_AcceptAsync_Helper(IPAddress.Any, IPAddress.IPv6Loopback);
            });
        }

        [Fact]
        public void AcceptAsyncV4BoundToAnyV6_Success()
        {
            DualModeConnect_AcceptAsync_Helper(IPAddress.IPv6Any, IPAddress.Loopback);
        }

        private void DualModeConnect_AcceptAsync_Helper(IPAddress listenOn, IPAddress connectTo)
        {
            using (Socket serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                int port = serverSocket.BindToAnonymousPort(listenOn);
                serverSocket.Listen(1);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += AsyncCompleted;
                ManualResetEvent waitHandle = new ManualResetEvent(false);
                args.UserToken = waitHandle;
                args.SocketError = SocketError.SocketError;

                _log.WriteLine(args.GetHashCode() + " SocketAsyncEventArgs with manual event " + waitHandle.GetHashCode());
                if (!serverSocket.AcceptAsync(args))
                {
                    throw new SocketException((int)args.SocketError);
                }

                SocketClient client = new SocketClient(_log, serverSocket, connectTo, port);

                var waitHandles = new WaitHandle[2];
                waitHandles[0] = waitHandle;
                waitHandles[1] = client.WaitHandle;

                int completedHandle = WaitHandle.WaitAny(waitHandles, Configuration.PassingTestTimeout);

                if (completedHandle == WaitHandle.WaitTimeout)
                {
                    throw new TimeoutException("Timed out while waiting for either of client and server connections...");
                }

                if (completedHandle == 1)   // Client finished
                {
                    if (client.Error != SocketError.Success)
                    {
                        // Client SocketException
                        throw new SocketException((int)client.Error);
                    }

                    if (!waitHandle.WaitOne(5000))  // Now wait for the server.
                    {
                        throw new TimeoutException("Timed out while waiting for the server accept...");
                    }
                }

                _log.WriteLine(args.SocketError.ToString());


                if (args.SocketError != SocketError.Success)
                {
                    throw new SocketException((int)args.SocketError);
                }

                Socket clientSocket = args.AcceptSocket;
                Assert.NotNull(clientSocket);
                Assert.True(clientSocket.Connected);
                AssertDualModeEnabled(clientSocket, listenOn);
                Assert.Equal(AddressFamily.InterNetworkV6, clientSocket.AddressFamily);
                if (connectTo == IPAddress.Loopback)
                {
                    Assert.Contains(((IPEndPoint)clientSocket.LocalEndPoint).Address, ValidIPv6Loopbacks);
                }
                else
                {
                    Assert.Equal(connectTo.MapToIPv6(), ((IPEndPoint)clientSocket.LocalEndPoint).Address);
                }
                clientSocket.Dispose();
            }
        }

        #endregion Accept Async/Event

        #endregion Accept

        #region Connectionless
        #region SendTo Sync IPEndPoint

        [Fact] // Base case
        // "The system detected an invalid pointer address in attempting to use a pointer argument in a call"
        public void Socket_SendToV4IPEndPointToV4Host_Throws()
        {
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.DualMode = false;
            Assert.Throws<SocketException>(() =>
            {
                socket.SendTo(new byte[1], new IPEndPoint(IPAddress.Loopback, UnusedPort));
            });
        }

        [Fact] // Base case
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public void Socket_SendToDnsEndPoint_Throws()
        {
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            Assert.Throws<ArgumentException>(() =>
            {
                socket.SendTo(new byte[1], new DnsEndPoint("localhost", UnusedPort));
            });
        }

        [Fact]
        public void SendToV4IPEndPointToV4Host_Success()
        {
            DualModeSendTo_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.Loopback, false);
        }

        [Fact]
        public void SendToV6IPEndPointToV6Host_Success()
        {
            DualModeSendTo_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false);
        }

        [Fact]
        public void SendToV4IPEndPointToV6Host_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                DualModeSendTo_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, false, expectedToTimeout: true);
            });
        }

        [Fact]
        public void SendToV6IPEndPointToV4Host_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                DualModeSendTo_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, false, expectedToTimeout: true);
            });
        }

        [Fact]
        public void SendToV4IPEndPointToDualHost_Success()
        {
            DualModeSendTo_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true);
        }

        [Fact]
        public void SendToV6IPEndPointToDualHost_Success()
        {
            DualModeSendTo_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true);
        }

        private void DualModeSendTo_IPEndPointToHost_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer, bool expectedToTimeout = false)
        {
            int port;
            Socket client = new Socket(SocketType.Dgram, ProtocolType.Udp);
            using (SocketUdpServer server = new SocketUdpServer(_log, listenOn, dualModeServer, out port))
            {
                // Send a few packets, in case they aren't delivered reliably.
                for (int i = 0; i < Configuration.UDPRedundancy; i++)
                {
                    int sent = client.SendTo(new byte[1], new IPEndPoint(connectTo, port));
                    Assert.Equal(1, sent);
                }

                bool success = server.WaitHandle.WaitOne(expectedToTimeout ? Configuration.FailingTestTimeout : Configuration.PassingTestTimeout); // Make sure the bytes were received
                if (!success)
                {
                    throw new TimeoutException();
                }
            }
        }

        #endregion SendTo Sync

        #region SendTo Begin/End

        [Fact] // Base case
        // "The system detected an invalid pointer address in attempting to use a pointer argument in a call"
        public void Socket_BeginSendToV4IPEndPointToV4Host_Throws()
        {
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.DualMode = false;

            Assert.Throws<SocketException>(() =>
            {
                socket.BeginSendTo(new byte[1], 0, 1, SocketFlags.None, new IPEndPoint(IPAddress.Loopback, UnusedPort), null, null);
            });
        }

        [Fact] // Base case
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public void Socket_BeginSendToDnsEndPoint_Throws()
        {
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

            Assert.Throws<ArgumentException>(() =>
            {
                socket.BeginSendTo(new byte[1], 0, 1, SocketFlags.None, new DnsEndPoint("localhost", UnusedPort), null, null);
            });
        }

        [Fact]
        public void BeginSendToV4IPEndPointToV4Host_Success()
        {
            DualModeBeginSendTo_EndPointToHost_Helper(IPAddress.Loopback, IPAddress.Loopback, false);
        }

        [Fact]
        public void BeginSendToV6IPEndPointToV6Host_Success()
        {
            DualModeBeginSendTo_EndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false);
        }

        [Fact]
        public void BeginSendToV4IPEndPointToV6Host_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                DualModeBeginSendTo_EndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, false, expectedToTimeout: true);
            });
        }

        [Fact]
        public void BeginSendToV6IPEndPointToV4Host_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                DualModeBeginSendTo_EndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, false, expectedToTimeout: true);
            });
        }

        [Fact]
        public void BeginSendToV4IPEndPointToDualHost_Success()
        {
            DualModeBeginSendTo_EndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true);
        }

        [Fact]
        public void BeginSendToV6IPEndPointToDualHost_Success()
        {
            DualModeBeginSendTo_EndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true);
        }

        private void DualModeBeginSendTo_EndPointToHost_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer, bool expectedToTimeout = false)
        {
            int port;
            Socket client = new Socket(SocketType.Dgram, ProtocolType.Udp);
            using (SocketUdpServer server = new SocketUdpServer(_log, listenOn, dualModeServer, out port))
            {
                // Send a few packets, in case they aren't delivered reliably.
                for (int i = 0; i < Configuration.UDPRedundancy; i++)
                {
                    IAsyncResult async = client.BeginSendTo(new byte[1], 0, 1, SocketFlags.None, new IPEndPoint(connectTo, port), null, null);

                    int sent = client.EndSendTo(async);
                    Assert.Equal(1, sent);
                }

                bool success = server.WaitHandle.WaitOne(expectedToTimeout ? Configuration.FailingTestTimeout : Configuration.PassingTestTimeout); // Make sure the bytes were received
                if (!success)
                {
                    throw new TimeoutException();
                }
            }
        }

        #endregion SendTo Begin/End

        #region SendTo Async/Event

        [Fact] // Base case
        public void Socket_SendToAsyncV4IPEndPointToV4Host_Throws()
        {
            Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, UnusedPort);
            args.SetBuffer(new byte[1], 0, 1);
            bool async = socket.SendToAsync(args);
            Assert.False(async);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Equal(SocketError.Fault, args.SocketError);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // NOTE: on Linux, this API returns ENETUNREACH instead of EFAULT: this platform
                //       checks the family of the provided socket address before checking its size
                //       (as long as the socket address is large enough to store an address family).
                Assert.Equal(SocketError.NetworkUnreachable, args.SocketError);
            }
            else
            {
                // NOTE: on other Unix platforms, this API returns EINVAL instead of EFAULT.
                Assert.Equal(SocketError.InvalidArgument, args.SocketError);
            }
        }

        [Fact] // Base case
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public void Socket_SendToAsyncDnsEndPoint_Throws()
        {
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new DnsEndPoint("localhost", UnusedPort);
            args.SetBuffer(new byte[1], 0, 1);
            Assert.Throws<ArgumentException>(() =>
            {
                socket.SendToAsync(args);
            });
        }

        [Fact]
        public void SendToAsyncV4IPEndPointToV4Host_Success()
        {
            DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.Loopback, false);
        }

        [Fact]
        public void SendToAsyncV6IPEndPointToV6Host_Success()
        {
            DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false);
        }

        [Fact]
        public void SendToAsyncV4IPEndPointToV6Host_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, false, expectedToTimeout: true);
            });
        }

        [Fact]
        public void SendToAsyncV6IPEndPointToV4Host_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, false, expectedToTimeout: true);
            });
        }

        [Fact]
        public void SendToAsyncV4IPEndPointToDualHost_Success()
        {
            DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true);
        }

        [Fact]
        public void SendToAsyncV6IPEndPointToDualHost_Success()
        {
            DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true);
        }

        private void DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer, bool expectedToTimeout = false)
        {
            int port;
            Socket client = new Socket(SocketType.Dgram, ProtocolType.Udp);
            using (SocketUdpServer server = new SocketUdpServer(_log, listenOn, dualModeServer, out port))
            {
                // Send a few packets, in case they aren't delivered reliably.
                for (int i = 0; i < Configuration.UDPRedundancy; i++)
                {
                    using (ManualResetEvent waitHandle = new ManualResetEvent(false))
                    {
                        SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                        args.RemoteEndPoint = new IPEndPoint(connectTo, port);
                        args.SetBuffer(new byte[1], 0, 1);
                        args.UserToken = waitHandle;
                        args.Completed += AsyncCompleted;

                        bool async = client.SendToAsync(args);
                        if (async)
                        {
                            Assert.True(waitHandle.WaitOne(Configuration.PassingTestTimeout), "Timeout while waiting for connection");
                        }

                        Assert.Equal(1, args.BytesTransferred);
                        if (args.SocketError != SocketError.Success)
                        {
                            throw new SocketException((int)args.SocketError);
                        }
                    }
                }

                bool success = server.WaitHandle.WaitOne(expectedToTimeout ? Configuration.FailingTestTimeout : Configuration.PassingTestTimeout); // Make sure the bytes were received
                if (!success)
                {
                    throw new TimeoutException();
                }
            }
        }

        #endregion SendTo Async/Event

        #region ReceiveFrom Sync

        [Fact] // Base case
        public void Socket_ReceiveFromV4IPEndPointFromV4Client_Throws()
        {
            // "The supplied EndPoint of AddressFamily InterNetwork is not valid for this Socket, use InterNetworkV6 instead."
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.DualMode = false;

            EndPoint receivedFrom = new IPEndPoint(IPAddress.Loopback, UnusedPort);
            Assert.Throws<ArgumentException>(() =>
            {
                int received = socket.ReceiveFrom(new byte[1], ref receivedFrom);
            });
        }

        [Fact] // Base case
        [PlatformSpecific(~PlatformID.OSX)]
        public void Socket_ReceiveFromDnsEndPoint_Throws()
        {
            // "The parameter remoteEP must not be of type DnsEndPoint."
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                int port = socket.BindToAnonymousPort(IPAddress.IPv6Loopback);
                EndPoint receivedFrom = new DnsEndPoint("localhost", port, AddressFamily.InterNetworkV6);
                Assert.Throws<ArgumentException>(() =>
                {
                    int received = socket.ReceiveFrom(new byte[1], ref receivedFrom);
                });
            }
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveFromV4BoundToSpecificV4_Success()
        {
            ReceiveFrom_Helper(IPAddress.Loopback, IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveFromV4BoundToAnyV4_Success()
        {
            ReceiveFrom_Helper(IPAddress.Any, IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveFromV6BoundToSpecificV6_Success()
        {
            ReceiveFrom_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveFromV6BoundToAnyV6_Success()
        {
            ReceiveFrom_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveFromV6BoundToSpecificV4_NotReceived()
        {
            Assert.Throws<SocketException>(() =>
            {
                ReceiveFrom_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback);
            });
        }

        [Fact]
        [PlatformSpecific(~(PlatformID.Linux | PlatformID.OSX))]
        public void ReceiveFromV4BoundToSpecificV6_NotReceived()
        {
            Assert.Throws<SocketException>(() =>
            {
                ReceiveFrom_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback);
            });
        }

        // NOTE: on Linux, the OS IP stack changes a dual-mode socket back to a
        //       normal IPv6 socket once the socket is bound to an IPv6-specific
        //       address. As a result, the argument validation checks in
        //       ReceiveFrom that check that the supplied endpoint is compatible
        //       with the socket's address family fail. We've decided that this is
        //       an acceptable difference due to the extra state that would otherwise
        //       be necessary to emulate the Winsock behavior.
        [Fact]
        [PlatformSpecific(PlatformID.Linux)]
        public void ReceiveFromV4BoundToSpecificV6_NotReceived_Linux()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                ReceiveFrom_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback);
            });
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveFromV6BoundToAnyV4_NotReceived()
        {
            Assert.Throws<SocketException>(() =>
            {
                ReceiveFrom_Helper(IPAddress.Any, IPAddress.IPv6Loopback);
            });
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveFromV4BoundToAnyV6_Success()
        {
            ReceiveFrom_Helper(IPAddress.IPv6Any, IPAddress.Loopback);
        }

        private void ReceiveFrom_Helper(IPAddress listenOn, IPAddress connectTo)
        {
            using (Socket serverSocket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                serverSocket.ReceiveTimeout = 500;
                int port = serverSocket.BindToAnonymousPort(listenOn);

                SocketUdpClient client = new SocketUdpClient(_log, serverSocket, connectTo, port);

                EndPoint receivedFrom = new IPEndPoint(connectTo, port);
                int received = serverSocket.ReceiveFrom(new byte[1], ref receivedFrom);

                Assert.Equal(1, received);
                Assert.Equal<Type>(receivedFrom.GetType(), typeof(IPEndPoint));

                IPEndPoint remoteEndPoint = receivedFrom as IPEndPoint;
                Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
                Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);
            }
        }

        #endregion ReceiveFrom Sync

        #region ReceiveFrom Begin/End

        [Fact] // Base case
        // "The supplied EndPoint of AddressFamily InterNetwork is not valid for this Socket, use InterNetworkV6 instead."
        public void Socket_BeginReceiveFromV4IPEndPointFromV4Client_Throws()
        {
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.DualMode = false;

            EndPoint receivedFrom = new IPEndPoint(IPAddress.Loopback, UnusedPort);
            Assert.Throws<ArgumentException>(() =>
            {
                socket.BeginReceiveFrom(new byte[1], 0, 1, SocketFlags.None, ref receivedFrom, null, null);
            });
        }

        [Fact] // Base case
        [PlatformSpecific(~PlatformID.OSX)]
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public void Socket_BeginReceiveFromDnsEndPoint_Throws()
        {
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                int port = socket.BindToAnonymousPort(IPAddress.IPv6Loopback);
                EndPoint receivedFrom = new DnsEndPoint("localhost", port, AddressFamily.InterNetworkV6);

                Assert.Throws<ArgumentException>(() =>
                {
                    socket.BeginReceiveFrom(new byte[1], 0, 1, SocketFlags.None, ref receivedFrom, null, null);
                });
            }
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void BeginReceiveFromV4BoundToSpecificV4_Success()
        {
            BeginReceiveFrom_Helper(IPAddress.Loopback, IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void BeginReceiveFromV4BoundToAnyV4_Success()
        {
            BeginReceiveFrom_Helper(IPAddress.Any, IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void BeginReceiveFromV6BoundToSpecificV6_Success()
        {
            BeginReceiveFrom_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void BeginReceiveFromV6BoundToAnyV6_Success()
        {
            BeginReceiveFrom_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void BeginReceiveFromV6BoundToSpecificV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                BeginReceiveFrom_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, expectedToTimeout: true);
            });
        }

        [Fact]
        [PlatformSpecific(~(PlatformID.Linux | PlatformID.OSX))]
        public void BeginReceiveFromV4BoundToSpecificV6_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                BeginReceiveFrom_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, expectedToTimeout: true);
            });
        }

        // NOTE: on Linux, the OS IP stack changes a dual-mode socket back to a
        //       normal IPv6 socket once the socket is bound to an IPv6-specific
        //       address. As a result, the argument validation checks in
        //       ReceiveFrom that check that the supplied endpoint is compatible
        //       with the socket's address family fail. We've decided that this is
        //       an acceptable difference due to the extra state that would otherwise
        //       be necessary to emulate the Winsock behavior.
        [Fact]
        [PlatformSpecific(PlatformID.Linux)]
        public void BeginReceiveFromV4BoundToSpecificV6_NotReceived_Linux()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                BeginReceiveFrom_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, expectedToTimeout: true);
            });
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void BeginReceiveFromV6BoundToAnyV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                BeginReceiveFrom_Helper(IPAddress.Any, IPAddress.IPv6Loopback, expectedToTimeout: true);
            });
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void BeginReceiveFromV4BoundToAnyV6_Success()
        {
            BeginReceiveFrom_Helper(IPAddress.IPv6Any, IPAddress.Loopback);
        }

        private void BeginReceiveFrom_Helper(IPAddress listenOn, IPAddress connectTo, bool expectedToTimeout = false)
        {
            using (Socket serverSocket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                serverSocket.ReceiveTimeout = 500;
                int port = serverSocket.BindToAnonymousPort(listenOn);

                EndPoint receivedFrom = new IPEndPoint(connectTo, port);
                IAsyncResult async = serverSocket.BeginReceiveFrom(new byte[1], 0, 1, SocketFlags.None, ref receivedFrom, null, null);

                // Behavior difference from Desktop: receivedFrom will _not_ change during the synchronous phase.

                // IPEndPoint remoteEndPoint = receivedFrom as IPEndPoint;
                // Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
                // Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);

                SocketUdpClient client = new SocketUdpClient(_log, serverSocket, connectTo, port);
                bool success = async.AsyncWaitHandle.WaitOne(expectedToTimeout ? Configuration.FailingTestTimeout : Configuration.PassingTestTimeout);
                if (!success)
                {
                    throw new TimeoutException();
                }

                receivedFrom = new IPEndPoint(connectTo, port);
                int received = serverSocket.EndReceiveFrom(async, ref receivedFrom);

                Assert.Equal(1, received);
                Assert.Equal<Type>(receivedFrom.GetType(), typeof(IPEndPoint));

                IPEndPoint remoteEndPoint = receivedFrom as IPEndPoint;
                Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
                Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);
            }
        }

        #endregion ReceiveFrom Begin/End

        #region ReceiveFrom Async/Event

        [Fact] // Base case
        // "The supplied EndPoint of AddressFamily InterNetwork is not valid for this Socket, use InterNetworkV6 instead."
        public void Socket_ReceiveFromAsyncV4IPEndPointFromV4Client_Throws()
        {
            Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, UnusedPort);
            args.SetBuffer(new byte[1], 0, 1);

            Assert.Throws<ArgumentException>(() =>
            {
                socket.ReceiveFromAsync(args);
            });
        }

        [Fact] // Base case
        [PlatformSpecific(~PlatformID.OSX)]
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public void Socket_ReceiveFromAsyncDnsEndPoint_Throws()
        {
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                int port = socket.BindToAnonymousPort(IPAddress.IPv6Loopback);
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new DnsEndPoint("localhost", port, AddressFamily.InterNetworkV6);
                args.SetBuffer(new byte[1], 0, 1);

                Assert.Throws<ArgumentException>(() =>
                {
                    socket.ReceiveFromAsync(args);
                });
            }
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveFromAsyncV4BoundToSpecificV4_Success()
        {
            ReceiveFromAsync_Helper(IPAddress.Loopback, IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveFromAsyncV4BoundToAnyV4_Success()
        {
            ReceiveFromAsync_Helper(IPAddress.Any, IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveFromAsyncV6BoundToSpecificV6_Success()
        {
            ReceiveFromAsync_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveFromAsyncV6BoundToAnyV6_Success()
        {
            ReceiveFromAsync_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveFromAsyncV6BoundToSpecificV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                ReceiveFromAsync_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback);
            });
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveFromAsyncV4BoundToSpecificV6_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                ReceiveFromAsync_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback);
            });
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveFromAsyncV6BoundToAnyV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                ReceiveFromAsync_Helper(IPAddress.Any, IPAddress.IPv6Loopback);
            });
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveFromAsyncV4BoundToAnyV6_Success()
        {
            ReceiveFromAsync_Helper(IPAddress.IPv6Any, IPAddress.Loopback);
        }

        private void ReceiveFromAsync_Helper(IPAddress listenOn, IPAddress connectTo, bool expectedToTimeout = false)
        {
            using (Socket serverSocket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                int port = serverSocket.BindToAnonymousPort(listenOn);

                ManualResetEvent waitHandle = new ManualResetEvent(false);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new IPEndPoint(listenOn, port);
                args.SetBuffer(new byte[1], 0, 1);
                args.UserToken = waitHandle;
                args.Completed += AsyncCompleted;

                bool async = serverSocket.ReceiveFromAsync(args);
                SocketUdpClient client = new SocketUdpClient(_log, serverSocket, connectTo, port);
                if (async && !waitHandle.WaitOne(expectedToTimeout ? Configuration.FailingTestTimeout : Configuration.PassingTestTimeout))
                {
                    throw new TimeoutException();
                }

                if (args.SocketError != SocketError.Success)
                {
                    throw new SocketException((int)args.SocketError);
                }

                Assert.Equal(1, args.BytesTransferred);
                Assert.Equal<Type>(args.RemoteEndPoint.GetType(), typeof(IPEndPoint));
                IPEndPoint remoteEndPoint = args.RemoteEndPoint as IPEndPoint;
                Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
                Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);
            }
        }

        #endregion ReceiveFrom Async/Event

        [Fact]
        [PlatformSpecific(PlatformID.OSX)]
        public void ReceiveFrom_NotSupported()
        {
            using (Socket sock = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                sock.Bind(ep);

                byte[] buf = new byte[1];

                Assert.Throws<PlatformNotSupportedException>(() => sock.ReceiveFrom(buf, ref ep));
                Assert.Throws<PlatformNotSupportedException>(() => sock.ReceiveFrom(buf, SocketFlags.None, ref ep));
                Assert.Throws<PlatformNotSupportedException>(() => sock.ReceiveFrom(buf, buf.Length, SocketFlags.None, ref ep));
                Assert.Throws<PlatformNotSupportedException>(() => sock.ReceiveFrom(buf, 0, buf.Length, SocketFlags.None, ref ep));
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.OSX)]
        public void ReceiveMessageFrom_NotSupported()
        {
            using (Socket sock = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                sock.Bind(ep);

                byte[] buf = new byte[1];
                SocketFlags flags = SocketFlags.None;
                IPPacketInformation packetInfo;

                Assert.Throws<PlatformNotSupportedException>(() => sock.ReceiveMessageFrom(buf, 0, buf.Length, ref flags, ref ep, out packetInfo));
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.OSX)]
        public void ReceiveFromAsync_NotSupported()
        {
            using (Socket sock = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                byte[] buf = new byte[1];
                EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                sock.Bind(ep);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.SetBuffer(buf, 0, buf.Length);
                args.RemoteEndPoint = ep;

                Assert.Throws<PlatformNotSupportedException>(() => sock.ReceiveFromAsync(args));
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.OSX)]
        public void ReceiveMessageFromAsync_NotSupported()
        {
            using (Socket sock = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                byte[] buf = new byte[1];
                EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                sock.Bind(ep);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.SetBuffer(buf, 0, buf.Length);
                args.RemoteEndPoint = ep;

                Assert.Throws<PlatformNotSupportedException>(() => sock.ReceiveMessageFromAsync(args));
            }
        }
		
        [Fact] // Base case
        // "The supplied EndPoint of AddressFamily InterNetwork is not valid for this Socket, use InterNetworkV6 instead."
        public void Socket_ReceiveMessageFromV4IPEndPointFromV4Client_Throws()
        {
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.DualMode = false;

            EndPoint receivedFrom = new IPEndPoint(IPAddress.Loopback, UnusedPort);
            SocketFlags socketFlags = SocketFlags.None;
            IPPacketInformation ipPacketInformation;
            Assert.Throws<ArgumentException>(() =>
            {
                int received = socket.ReceiveMessageFrom(new byte[1], 0, 1, ref socketFlags, ref receivedFrom, out ipPacketInformation);
            });
        }

        [Fact] // Base case
        [PlatformSpecific(~PlatformID.OSX)]
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public void Socket_ReceiveMessageFromDnsEndPoint_Throws()
        {
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                int port = socket.BindToAnonymousPort(IPAddress.IPv6Loopback);
                EndPoint receivedFrom = new DnsEndPoint("localhost", port, AddressFamily.InterNetworkV6);
                SocketFlags socketFlags = SocketFlags.None;
                IPPacketInformation ipPacketInformation;

                Assert.Throws<ArgumentException>(() =>
                {
                    int received = socket.ReceiveMessageFrom(new byte[1], 0, 1, ref socketFlags, ref receivedFrom, out ipPacketInformation);
                });
            }
        }

        [Fact] // Base case
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveMessageFromV4BoundToSpecificMappedV4_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.Loopback.MapToIPv6(), IPAddress.Loopback);
        }

        [Fact] // Base case
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveMessageFromV4BoundToAnyMappedV4_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.Any.MapToIPv6(), IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveMessageFromV4BoundToSpecificV4_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.Loopback, IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveMessageFromV4BoundToAnyV4_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.Any, IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveMessageFromV6BoundToSpecificV6_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveMessageFromV6BoundToAnyV6_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveMessageFromV6BoundToSpecificV4_NotReceived()
        {
            Assert.Throws<SocketException>(() =>
            {
                ReceiveMessageFrom_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback);
            });
        }

        [Fact]
        [PlatformSpecific(~(PlatformID.Linux | PlatformID.OSX))]
        public void ReceiveMessageFromV4BoundToSpecificV6_NotReceived()
        {
            Assert.Throws<SocketException>(() =>
            {
                ReceiveMessageFrom_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback);
            });
        }

        // NOTE: on Linux, the OS IP stack changes a dual-mode socket back to a
        //       normal IPv6 socket once the socket is bound to an IPv6-specific
        //       address. As a result, the argument validation checks in
        //       ReceiveFrom that check that the supplied endpoint is compatible
        //       with the socket's address family fail. We've decided that this is
        //       an acceptable difference due to the extra state that would otherwise
        //       be necessary to emulate the Winsock behavior.
        [Fact]
        [PlatformSpecific(PlatformID.Linux)]
        public void ReceiveMessageFromV4BoundToSpecificV6_NotReceived_Linux()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                ReceiveMessageFrom_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback);
            });
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveMessageFromV6BoundToAnyV4_NotReceived()
        {
            Assert.Throws<SocketException>(() =>
            {
                ReceiveMessageFrom_Helper(IPAddress.Any, IPAddress.IPv6Loopback);
            });
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveMessageFromV4BoundToAnyV6_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.IPv6Any, IPAddress.Loopback);
        }

        private void ReceiveMessageFrom_Helper(IPAddress listenOn, IPAddress connectTo)
        {
            using (Socket serverSocket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                serverSocket.ReceiveTimeout = 500;
                int port = serverSocket.BindToAnonymousPort(listenOn);

                EndPoint receivedFrom = new IPEndPoint(connectTo, port);
                SocketFlags socketFlags = SocketFlags.None;
                IPPacketInformation ipPacketInformation;
                int received = 0;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Assert.Throws<SocketException>(() =>
                    {
                        // This is a false start.
                        // http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.receivemessagefrom.aspx
                        // "...the returned IPPacketInformation object will only be valid for packets which arrive at the
                        // local computer after the socket option has been set. If a socket is sent packets between when
                        // it is bound to a local endpoint (explicitly by the Bind method or implicitly by one of the Connect,
                        // ConnectAsync, SendTo, or SendToAsync methods) and its first call to the ReceiveMessageFrom method,
                        // calls to ReceiveMessageFrom method will return invalid IPPacketInformation objects for these packets."
                        received = serverSocket.ReceiveMessageFrom(new byte[1], 0, 1, ref socketFlags, ref receivedFrom, out ipPacketInformation);
                    });
                }
                else
                {
                    // *nix may throw either a SocketException or ArgumentException in this case, depending on how the IP stack
                    // behaves w.r.t. dual-mode sockets bound to IPv6-specific addresses.
                    Assert.ThrowsAny<Exception>(() =>
                    {
                        received = serverSocket.ReceiveMessageFrom(new byte[1], 0, 1, ref socketFlags, ref receivedFrom, out ipPacketInformation);
                    });
                }

                SocketUdpClient client = new SocketUdpClient(_log, serverSocket, connectTo, port);

                receivedFrom = new IPEndPoint(connectTo, port);
                socketFlags = SocketFlags.None;
                received = serverSocket.ReceiveMessageFrom(new byte[1], 0, 1, ref socketFlags, ref receivedFrom, out ipPacketInformation);

                Assert.Equal(1, received);
                Assert.Equal<Type>(receivedFrom.GetType(), typeof(IPEndPoint));

                IPEndPoint remoteEndPoint = receivedFrom as IPEndPoint;
                Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
                Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);

                Assert.Equal(SocketFlags.None, socketFlags);
                Assert.NotNull(ipPacketInformation);

                Assert.Equal(connectTo, ipPacketInformation.Address);

                // TODO (#7854): Move to NetworkInformation tests.
                // Assert.Equal(NetworkInterface.IPv6LoopbackInterfaceIndex, ipPacketInformation.Interface);
            }
        }

        [Fact] // Base case
        // "The supplied EndPoint of AddressFamily InterNetwork is not valid for this Socket, use InterNetworkV6 instead."
        public void Socket_BeginReceiveMessageFromV4IPEndPointFromV4Client_Throws()
        {
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.DualMode = false;

            EndPoint receivedFrom = new IPEndPoint(IPAddress.Loopback, UnusedPort);
            SocketFlags socketFlags = SocketFlags.None;

            Assert.Throws<ArgumentException>(() =>
            {
                socket.BeginReceiveMessageFrom(new byte[1], 0, 1, socketFlags, ref receivedFrom, null, null);
            });
        }

        [Fact] // Base case
        [PlatformSpecific(~PlatformID.OSX)]
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public void Socket_BeginReceiveMessageFromDnsEndPoint_Throws()
        {
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                int port = socket.BindToAnonymousPort(IPAddress.IPv6Loopback);

                EndPoint receivedFrom = new DnsEndPoint("localhost", port, AddressFamily.InterNetworkV6);
                SocketFlags socketFlags = SocketFlags.None;
                Assert.Throws<ArgumentException>(() =>
                {
                    socket.BeginReceiveMessageFrom(new byte[1], 0, 1, socketFlags, ref receivedFrom, null, null);
                });
            }
        }

        [Fact] // Base case
        [PlatformSpecific(~PlatformID.OSX)]
        public void BeginReceiveMessageFromV4BoundToSpecificMappedV4_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.Loopback.MapToIPv6(), IPAddress.Loopback);
        }

        [Fact] // Base case
        [PlatformSpecific(~PlatformID.OSX)]
        public void BeginReceiveMessageFromV4BoundToAnyMappedV4_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.Any.MapToIPv6(), IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void BeginReceiveMessageFromV4BoundToSpecificV4_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.Loopback, IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void BeginReceiveMessageFromV4BoundToAnyV4_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.Any, IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void BeginReceiveMessageFromV6BoundToSpecificV6_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void BeginReceiveMessageFromV6BoundToAnyV6_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void BeginReceiveMessageFromV6BoundToSpecificV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                BeginReceiveMessageFrom_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, expectedToTimeout: true);
            });
        }

        [Fact]
        [PlatformSpecific(~(PlatformID.Linux | PlatformID.OSX))]
        public void BeginReceiveMessageFromV4BoundToSpecificV6_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                BeginReceiveMessageFrom_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, expectedToTimeout: true);
            });
        }

        // NOTE: on Linux, the OS IP stack changes a dual-mode socket back to a
        //       normal IPv6 socket once the socket is bound to an IPv6-specific
        //       address. As a result, the argument validation checks in
        //       ReceiveFrom that check that the supplied endpoint is compatible
        //       with the socket's address family fail. We've decided that this is
        //       an acceptable difference due to the extra state that would otherwise
        //       be necessary to emulate the Winsock behavior.
        [Fact]
        [PlatformSpecific(PlatformID.Linux)]
        public void BeginReceiveMessageFromV4BoundToSpecificV6_NotReceived_Linux()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                BeginReceiveMessageFrom_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, expectedToTimeout: true);
            });
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void BeginReceiveMessageFromV6BoundToAnyV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                BeginReceiveMessageFrom_Helper(IPAddress.Any, IPAddress.IPv6Loopback, expectedToTimeout: true);
            });
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void BeginReceiveMessageFromV4BoundToAnyV6_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.IPv6Any, IPAddress.Loopback);
        }

        private void BeginReceiveMessageFrom_Helper(IPAddress listenOn, IPAddress connectTo, bool expectedToTimeout = false)
        {
            using (Socket serverSocket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                int port = serverSocket.BindToAnonymousPort(listenOn);

                EndPoint receivedFrom = new IPEndPoint(connectTo, port);
                SocketFlags socketFlags = SocketFlags.None;
                IPPacketInformation ipPacketInformation;
                IAsyncResult async = serverSocket.BeginReceiveMessageFrom(new byte[1], 0, 1, socketFlags, ref receivedFrom, null, null);

                // Behavior difference from Desktop: receivedFrom will _not_ change during the synchronous phase.

                // IPEndPoint remoteEndPoint = receivedFrom as IPEndPoint;
                // Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
                // Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);

                SocketUdpClient client = new SocketUdpClient(_log, serverSocket, connectTo, port);
                bool success = async.AsyncWaitHandle.WaitOne(expectedToTimeout ? Configuration.FailingTestTimeout : Configuration.PassingTestTimeout);
                if (!success)
                {
                    throw new TimeoutException();
                }

                receivedFrom = new IPEndPoint(connectTo, port);
                int received = serverSocket.EndReceiveMessageFrom(async, ref socketFlags, ref receivedFrom, out ipPacketInformation);

                Assert.Equal(1, received);
                Assert.Equal<Type>(receivedFrom.GetType(), typeof(IPEndPoint));

                IPEndPoint remoteEndPoint = receivedFrom as IPEndPoint;
                Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
                Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);

                Assert.Equal(SocketFlags.None, socketFlags);
                Assert.NotNull(ipPacketInformation);
                Assert.Equal(connectTo, ipPacketInformation.Address);

                // TODO (#7854): Move to NetworkInformation tests.
                //Assert.Equal(NetworkInterface.IPv6LoopbackInterfaceIndex, ipPacketInformation.Interface);
            }
        }

        [Fact] // Base case
        // "The supplied EndPoint of AddressFamily InterNetwork is not valid for this Socket, use InterNetworkV6 instead."
        public void Socket_ReceiveMessageFromAsyncV4IPEndPointFromV4Client_Throws()
        {
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.DualMode = false;

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, UnusedPort);
            args.SetBuffer(new byte[1], 0, 1);

            Assert.Throws<ArgumentException>(() =>
            {
                socket.ReceiveMessageFromAsync(args);
            });
        }

        [Fact] // Base case
        [PlatformSpecific(~PlatformID.OSX)]
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public void Socket_ReceiveMessageFromAsyncDnsEndPoint_Throws()
        {
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                int port = socket.BindToAnonymousPort(IPAddress.IPv6Loopback);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new DnsEndPoint("localhost", port, AddressFamily.InterNetworkV6);
                args.SetBuffer(new byte[1], 0, 1);

                Assert.Throws<ArgumentException>(() =>
                {
                    socket.ReceiveMessageFromAsync(args);
                });
            }
        }

        [Fact] // Base case
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveMessageFromAsyncV4BoundToSpecificMappedV4_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.Loopback.MapToIPv6(), IPAddress.Loopback);
        }

        [Fact] // Base case
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveMessageFromAsyncV4BoundToAnyMappedV4_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.Any.MapToIPv6(), IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveMessageFromAsyncV4BoundToSpecificV4_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.Loopback, IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveMessageFromAsyncV4BoundToAnyV4_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.Any, IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveMessageFromAsyncV6BoundToSpecificV6_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveMessageFromAsyncV6BoundToAnyV6_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveMessageFromAsyncV6BoundToSpecificV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                ReceiveMessageFromAsync_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback);
            });
        }

        [Fact]
        [PlatformSpecific(~(PlatformID.Linux | PlatformID.OSX))]
        public void ReceiveMessageFromAsyncV4BoundToSpecificV6_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                ReceiveMessageFromAsync_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback);
            });
        }

        // NOTE: on Linux, the OS IP stack changes a dual-mode socket back to a
        //       normal IPv6 socket once the socket is bound to an IPv6-specific
        //       address. As a result, the argument validation checks in
        //       ReceiveFrom that check that the supplied endpoint is compatible
        //       with the socket's address family fail. We've decided that this is
        //       an acceptable difference due to the extra state that would otherwise
        //       be necessary to emulate the Winsock behavior.
        [Fact]
        [PlatformSpecific(PlatformID.Linux)]
        public void ReceiveMessageFromAsyncV4BoundToSpecificV6_NotReceived_Linux()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                ReceiveFrom_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback);
            });
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveMessageFromAsyncV6BoundToAnyV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                ReceiveMessageFromAsync_Helper(IPAddress.Any, IPAddress.IPv6Loopback);
            });
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void ReceiveMessageFromAsyncV4BoundToAnyV6_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.IPv6Any, IPAddress.Loopback);
        }

        private void ReceiveMessageFromAsync_Helper(IPAddress listenOn, IPAddress connectTo, bool expectedToTimeout = false)
        {
            using (Socket serverSocket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                serverSocket.ReceiveTimeout = expectedToTimeout ? Configuration.FailingTestTimeout : Configuration.PassingTestTimeout;
                int port = serverSocket.BindToAnonymousPort(listenOn);

                ManualResetEvent waitHandle = new ManualResetEvent(false);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new IPEndPoint(connectTo, port);
                args.SetBuffer(new byte[1], 0, 1);
                args.Completed += AsyncCompleted;
                args.UserToken = waitHandle;

                bool async = serverSocket.ReceiveMessageFromAsync(args);
                Assert.True(async);

                SocketUdpClient client = new SocketUdpClient(_log, serverSocket, connectTo, port);
                if (!waitHandle.WaitOne(serverSocket.ReceiveTimeout))
                {
                    throw new TimeoutException();
                }

                Assert.Equal(1, args.BytesTransferred);
                Assert.Equal<Type>(args.RemoteEndPoint.GetType(), typeof(IPEndPoint));

                IPEndPoint remoteEndPoint = args.RemoteEndPoint as IPEndPoint;
                Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
                Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);

                Assert.Equal(SocketFlags.None, args.SocketFlags);
                Assert.NotNull(args.ReceiveMessageFromPacketInfo);
                Assert.Equal(connectTo, args.ReceiveMessageFromPacketInfo.Address);

                // TODO (#7854): Move to NetworkInformation tests.
                // Assert.Equal(NetworkInterface.IPv6LoopbackInterfaceIndex, args.ReceiveMessageFromPacketInfo.Interface);
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.OSX)]
        public void BeginReceiveFrom_NotSupported()
        {
            using (Socket sock = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                sock.Bind(ep);

                byte[] buf = new byte[1];

                Assert.Throws<PlatformNotSupportedException>(() => sock.BeginReceiveFrom(buf, 0, buf.Length, SocketFlags.None, ref ep, null, null));
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.OSX)]
        public void BeginReceiveMessageFrom_NotSupported()
        {
            using (Socket sock = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                sock.Bind(ep);

                byte[] buf = new byte[1];

                Assert.Throws<PlatformNotSupportedException>(() => sock.BeginReceiveMessageFrom(buf, 0, buf.Length, SocketFlags.None, ref ep, null, null));
            }
        }

        #endregion Connectionless

        #region GC Finalizer test
        // This test assumes sequential execution of tests and that it is going to be executed after other tests
        // that used Sockets. 
        [Fact]
        public void TestFinalizers()
        {
            // Making several passes through the FReachable list.
            for (int i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        #endregion

        #region Helpers

        public static readonly object[][] DualMode_Connect_IPAddress_DualMode_Data = {
            new object[] { IPAddress.Loopback, false },
            new object[] { IPAddress.IPv6Loopback, false },
            new object[] { IPAddress.IPv6Any, true },
        };

        public static readonly object[][] DualMode_IPAddresses_ListenOn_DualMode_Data = {
            new object[] { new IPAddress[] { IPAddress.Loopback, IPAddress.IPv6Loopback }, IPAddress.Loopback, false },
            new object[] { new IPAddress[] { IPAddress.IPv6Loopback, IPAddress.Loopback }, IPAddress.Loopback, false },
            new object[] { new IPAddress[] { IPAddress.Loopback, IPAddress.IPv6Loopback }, IPAddress.IPv6Loopback, false },
            new object[] { new IPAddress[] { IPAddress.IPv6Loopback, IPAddress.Loopback }, IPAddress.IPv6Loopback, false },
            new object[] { new IPAddress[] { IPAddress.Loopback, IPAddress.IPv6Loopback }, IPAddress.IPv6Any, true },
            new object[] { new IPAddress[] { IPAddress.IPv6Loopback, IPAddress.Loopback }, IPAddress.IPv6Any, true },
        };

        public static readonly object[][] DualMode_IPAddresses_ListenOn_DualMode_Throws_Data = {
            new object[] { new IPAddress[] { IPAddress.Loopback.MapToIPv6() }, IPAddress.IPv6Loopback, false },
            new object[] { new IPAddress[] { IPAddress.Loopback }, IPAddress.IPv6Loopback, false },
            new object[] { new IPAddress[] { IPAddress.Loopback }, IPAddress.IPv6Any, false },
            new object[] { new IPAddress[] { IPAddress.Loopback }, IPAddress.IPv6Loopback, true },
        };

        public static readonly object[][] DualMode_IPAddresses_ListenOn_DualMode_Success_Data = {
            new object[] { new IPAddress[] { IPAddress.Loopback.MapToIPv6() }, IPAddress.Loopback, false },
            new object[] { new IPAddress[] { IPAddress.Loopback }, IPAddress.Loopback, false },
            new object[] { new IPAddress[] { IPAddress.Loopback }, IPAddress.IPv6Any, true },
            new object[] { new IPAddress[] { IPAddress.Loopback, IPAddress.IPv6Loopback }, IPAddress.Loopback, false },
            new object[] { new IPAddress[] { IPAddress.IPv6Loopback, IPAddress.Loopback }, IPAddress.Loopback, false },
            new object[] { new IPAddress[] { IPAddress.Loopback, IPAddress.IPv6Loopback }, IPAddress.IPv6Loopback, false },
            new object[] { new IPAddress[] { IPAddress.IPv6Loopback, IPAddress.Loopback }, IPAddress.IPv6Loopback, false },
            new object[] { new IPAddress[] { IPAddress.Loopback, IPAddress.IPv6Loopback }, IPAddress.IPv6Any, true },
            new object[] { new IPAddress[] { IPAddress.IPv6Loopback, IPAddress.Loopback }, IPAddress.IPv6Any, true }
        };

        private class SocketServer : IDisposable
        {
            private readonly ITestOutputHelper _output;
            private Socket _server;
            private Socket _acceptedSocket;
            private EventWaitHandle _waitHandle = new AutoResetEvent(false);

            public EventWaitHandle WaitHandle
            {
                get { return _waitHandle; }
            }

            public SocketServer(ITestOutputHelper output, IPAddress address, bool dualMode, out int port)
            {
                _output = output;

                if (dualMode)
                {
                    _server = new Socket(SocketType.Stream, ProtocolType.Tcp);
                }
                else
                {
                    _server = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                }

                port = _server.BindToAnonymousPort(address);
                _server.Listen(1);

                IPAddress remoteAddress = address.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any;
                EndPoint remote = new IPEndPoint(remoteAddress, 0);
                SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                e.RemoteEndPoint = remote;
                e.Completed += new EventHandler<SocketAsyncEventArgs>(Accepted);
                e.UserToken = _waitHandle;

                _server.AcceptAsync(e);
            }

            private void Accepted(object sender, SocketAsyncEventArgs e)
            {
                EventWaitHandle handle = (EventWaitHandle)e.UserToken;
                _output.WriteLine(
                    "Accepted: " + e.GetHashCode() + " SocketAsyncEventArgs with manual event " +
                    handle.GetHashCode() + " error: " + e.SocketError);

                _acceptedSocket = e.AcceptSocket;

                handle.Set();
            }

            public void Dispose()
            {
                try
                {
                    _server.Dispose();
                    if (_acceptedSocket != null)
                        _acceptedSocket.Dispose();
                }
                catch (Exception) { }
            }
        }

        private class SocketClient
        {
            private IPAddress _connectTo;
            private Socket _serverSocket;
            private int _port;
            private readonly ITestOutputHelper _output;

            private EventWaitHandle _waitHandle = new AutoResetEvent(false);
            public EventWaitHandle WaitHandle
            {
                get { return _waitHandle; }
            }

            public SocketError Error
            {
                get;
                private set;
            }

            public SocketClient(ITestOutputHelper output, Socket serverSocket, IPAddress connectTo, int port)
            {
                _output = output;
                _connectTo = connectTo;
                _serverSocket = serverSocket;
                _port = port;
                Error = SocketError.Success;

                Task.Run(() => ConnectClient(null));
            }

            private void ConnectClient(object state)
            {
                try
                {
                    Socket socket = new Socket(_connectTo.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                    e.Completed += new EventHandler<SocketAsyncEventArgs>(Connected);
                    e.RemoteEndPoint = new IPEndPoint(_connectTo, _port);
                    e.UserToken = _waitHandle;

                    if (!socket.ConnectAsync(e))
                    {
                        Connected(socket, e);
                    }
                }
                catch (SocketException ex)
                {
                    Error = ex.SocketErrorCode;
                    Task.Delay(Configuration.FailingTestTimeout).Wait(); // Give the other end a chance to call Accept().
                    _serverSocket.Dispose(); // Cancels the test
					_waitHandle.Set();
                }
            }
            private void Connected(object sender, SocketAsyncEventArgs e)
            {
                EventWaitHandle handle = (EventWaitHandle)e.UserToken;
                _output.WriteLine(
                    "Connected: " + e.GetHashCode() + " SocketAsyncEventArgs with manual event " +
                    handle.GetHashCode() + " error: " + e.SocketError);

                Error = e.SocketError;
                if (Error != SocketError.Success)
                {
                    Task.Delay(Configuration.FailingTestTimeout).Wait(); // Give the other end a chance to call Accept().
                    _serverSocket.Dispose(); // Cancels the test
                }
                handle.Set();
            }
        }

        private class SocketUdpServer : IDisposable
        {
            private readonly ITestOutputHelper _output;
            private Socket _server;
            private EventWaitHandle _waitHandle = new AutoResetEvent(false);

            public EventWaitHandle WaitHandle
            {
                get { return _waitHandle; }
            }

            public SocketUdpServer(ITestOutputHelper output, IPAddress address, bool dualMode, out int port)
            {
                _output = output;

                if (dualMode)
                {
                    _server = new Socket(SocketType.Dgram, ProtocolType.Udp);
                }
                else
                {
                    _server = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                }

                port = _server.BindToAnonymousPort(address);

                SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                e.SetBuffer(new byte[1], 0, 1);
                e.Completed += new EventHandler<SocketAsyncEventArgs>(Received);
                e.UserToken = _waitHandle;

                _server.ReceiveAsync(e);
            }

            private void Received(object sender, SocketAsyncEventArgs e)
            {
                EventWaitHandle handle = (EventWaitHandle)e.UserToken;
                _output.WriteLine(
                    "Received: " + e.GetHashCode() + " SocketAsyncEventArgs with manual event " +
                    handle.GetHashCode() + " error: " + e.SocketError);

                handle.Set();
            }

            public void Dispose()
            {
                try
                {
                    _server.Dispose();
                }
                catch (Exception) { }
            }
        }

        private class SocketUdpClient
        {
            private readonly ITestOutputHelper _output;

            private int _port;
            private IPAddress _connectTo;
            private Socket _serverSocket;

            public SocketUdpClient(ITestOutputHelper output, Socket serverSocket, IPAddress connectTo, int port)
            {
                _output = output;

                _connectTo = connectTo;
                _port = port;
                _serverSocket = serverSocket;

                Task.Run(() => ClientSend(null));
            }

            private void ClientSend(object state)
            {
                try
                {
                    Socket socket = new Socket(_connectTo.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                    for (int i = 0; i < Configuration.UDPRedundancy; i++)
                    {
                        SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                        e.RemoteEndPoint = new IPEndPoint(_connectTo, _port);
                        e.SetBuffer(new byte[1], 0, 1);

                        socket.SendToAsync(e);
                    }
                }
                catch (SocketException)
                {
                    _serverSocket.Dispose(); // Cancels the test
                }
            }
        }

        private void AsyncCompleted(object sender, SocketAsyncEventArgs e)
        {
            EventWaitHandle handle = (EventWaitHandle)e.UserToken;

            _log.WriteLine(
                "AsyncCompleted: " + e.GetHashCode() + " SocketAsyncEventArgs with manual event " +
                handle.GetHashCode() + " error: " + e.SocketError);

            handle.Set();
        }

        #endregion Helpers
    }
}
