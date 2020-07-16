using System;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace SendFileServer
{
    /// <summary>
    /// Reference: https://docs.microsoft.com/pt-br/dotnet/api/system.net.sockets.socket?view=netcore-3.1
    /// </summary>
    public class MySocket
    {
        static ServiceConfigurations GetConfiguration()
        {
            var builder = new ConfigurationBuilder()
                                                .SetBasePath(Directory.GetCurrentDirectory())
                                                .AddJsonFile("appsettings.json");

            var configuration = builder.Build();

            var _serviceConfigurations = new ServiceConfigurations();
            new ConfigureFromConfigurationOptions<ServiceConfigurations>(
                configuration.GetSection("ServiceConfigurations"))
                    .Configure(_serviceConfigurations);
            return _serviceConfigurations;
        }

        private static Socket Start(string server, int port)
        {

            try
            {
                Socket ServerSocket = null;

                IPHostEntry hostEntry = Dns.GetHostEntry(server);
                IPAddress ipAddress = hostEntry.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
                IPEndPoint ipe = new IPEndPoint(ipAddress, port);

                ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                ServerSocket.Bind(ipe);
                ServerSocket.Listen(100);

                Console.WriteLine($"{ipAddress} in attendance and waiting to receive file ...");

                return ServerSocket;

            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error starting the server: {ex.Message}");
            }
        }

        public static string WaitForFile(string server, int port)
        {
            try
            {
                using (Socket ServerSocket = Start(server, port))
                {

                    if (ServerSocket == null)
                        throw new ApplicationException("Connection failed");

                    Socket clienteSock = ServerSocket.Accept();
                    clienteSock.ReceiveBufferSize = 16384; //16Kb

                    int bytes = 0;
                    byte[] buffer = new byte[1024 * 5000]; //5Mb

                    int tamanhoBytesRecebidos = clienteSock.Receive(buffer, buffer.Length, 0);
                    int tamnhoNomeArquivo = BitConverter.ToInt32(buffer, 0);
                    string nomeArquivo = Encoding.UTF8.GetString(buffer, 4, tamnhoNomeArquivo);

                    ServiceConfigurations _serviceConfigurations = GetConfiguration();
                    string path = Path.Combine(_serviceConfigurations.Folder, nomeArquivo);

                    BinaryWriter bWrite = new BinaryWriter(File.Open(path, FileMode.Append));
                    bWrite.Write(buffer, 4 + tamnhoNomeArquivo, tamanhoBytesRecebidos - 4 - tamnhoNomeArquivo);

                    do
                    {
                        bytes = clienteSock.Receive(buffer, buffer.Length, 0);
                        if (bytes == 0)
                        {
                            bWrite.Close();
                        }
                        else
                        {
                            bWrite.Write(buffer, 0, tamanhoBytesRecebidos);
                        }
                    }
                    while (bytes > 0);

                    return $"Received file [{nomeArquivo}] [{tamanhoBytesRecebidos} bytes].";
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error sending file: {ex.Message}.");
            }
        }
    }
}
