using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.Data.ConnectionUI
{
    public struct UdpState
    {
        public UdpClient u;
        public IPEndPoint e;
    }

    public class SqlServerScanner
    {
        private static DataTable serverInstances = new DataTable();

        private static DateTime lastScanAttemptTime = DateTime.MinValue;
        private static readonly TimeSpan cacheValidTimeSpan = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan responseTimeout = TimeSpan.FromSeconds(10);

        private static readonly List<UdpClient> listenSockets = new List<UdpClient>();
        private static readonly int SqlBrowserPort = 1434; // Port SQL Server Browser service listens on.
        private const string ServerName = "ServerName";

        public static DataTable GetList()
        {
            InitializeTable();
            ScanServers();
            //CloseSockets();
            return serverInstances;
        }

        private static void ScanServers()
        {
            lock (serverInstances)
            {
                if ((DateTime.Now - lastScanAttemptTime) < cacheValidTimeSpan)
                {
                    Debug.WriteLine("Using cached SQL Server instance list");
                    return;
                }

                lastScanAttemptTime = DateTime.Now;
                serverInstances.Clear();

                try
                {
                    var networksInterfaces = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
                    foreach (var networkInterface in networksInterfaces.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork))
                    {
                        //Debug.WriteLine("Setting up an SQL Browser listen socket for {address}", networkInterface);

                        var socket = new UdpClient { ExclusiveAddressUse = false };
                        socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        socket.Client.Bind(new IPEndPoint(networkInterface, 0));
                        socket.BeginReceive(new AsyncCallback(ResponseCallback), socket);
                        listenSockets.Add(socket);

                        //Debug.WriteLine("Sending message to all SQL Browser instances from {address}", networkInterface);
                        using (var broadcastSocket = new UdpClient { ExclusiveAddressUse = false })
                        {
                            broadcastSocket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                            broadcastSocket.Client.Bind(new IPEndPoint(networkInterface, ((IPEndPoint)socket.Client.LocalEndPoint).Port));
                            var bytes = new byte[] { 0x02, 0x00, 0x00 };
                            broadcastSocket.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, SqlBrowserPort));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex, "Failed to initiate SQL Server browser scan");
                    throw;
                }
            }
        }

        private static void InitializeTable()
        {
            if (serverInstances.Columns.Count == 0)
            {
                serverInstances.Columns.Add("ServerName", typeof(string));
                serverInstances.Columns.Add("InstanceName", typeof(string));
                serverInstances.Columns.Add("IsClustered", typeof(string));
                serverInstances.Columns.Add("Version", typeof(string));
            }
        }

        private static void CloseSockets()
        {
            foreach (var socket in listenSockets)
            {
                socket.Close();
                socket.Dispose();
            }
            listenSockets.Clear();
        }

        private static void ResponseCallback(IAsyncResult asyncResult)
        {
            try
            {
                var socket = asyncResult.AsyncState as UdpClient;
                if (socket != null && socket.Client != null)
                {
                    var localEndpoint = socket?.Client?.LocalEndPoint as IPEndPoint;

                    socket.BeginReceive(new AsyncCallback(ResponseCallback), socket);

                    var bytes = socket.EndReceive(asyncResult, ref localEndpoint);

                    if (bytes.Length == 0)
                    {
                        Debug.WriteLine("Received nothing from SQL Server browser");
                        return;
                    }

                    var response = System.Text.Encoding.UTF8.GetString(bytes);
                    //Debug.WriteLine("Found SQL Server instance(s): {data}", response);

                    foreach (var instance in ParseInstancesString(response))
                    {
                        serverInstances.Rows.Add(instance);
                    }
                }

                socket.Client.Close();
                socket.Close();
                socket.Dispose();
            }
            catch (Exception ex) when (ex is NullReferenceException || ex is ObjectDisposedException)
            {
                Debug.WriteLine("SQL Browser response received after preset timeout {0}", responseTimeout);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "Failed to process SQL Browser response");
            }
        }

        /// <summary>
        /// Parses the response string into DataRow objects.
        /// A single server may have multiple named instances
        /// </summary>
        /// <param name="response">The raw string received from the Browser service</param>
        /// <returns></returns>
        static private IEnumerable<DataRow> ParseInstancesString(string response)
        {
            if (!response.EndsWith(";;"))
            {
                Debug.WriteLine("Instances string unexpectedly terminates");
                yield break;
            }

            // Remove cruft from instances string.
            var firstRecord = response.IndexOf(ServerName);
            response = response.Remove(0, firstRecord);
            response = response.Substring(0, response.Length - 2);

            var instance = response.Split(';');
            for (int i = 0; i < instance.Length; i++)
            {
                if (instance[i].Equals("ServerName"))
                {
                    var row = serverInstances.NewRow();
                    row["ServerName"] = instance[i + 1];
                    row["InstanceName"] = instance[i + 3];
                    row["IsClustered"] = instance[i + 5].Equals("Yes");
                    row["Version"] = instance[i + 7];
                    yield return row;
                }
            }
        }
    }
}