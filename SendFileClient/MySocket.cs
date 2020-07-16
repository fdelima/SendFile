using System;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.IO;
using System.Text;

namespace SendFileClient
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

        private static Socket Start()
        {
            try
            {
                ServiceConfigurations _serviceConfigurations = GetConfiguration();

                Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

                IPEndPoint server = new IPEndPoint(IPAddress.Parse(_serviceConfigurations.ServerIP), _serviceConfigurations.ServerPort);

                clientSocket.Connect(server);

                Console.WriteLine($"connected {_serviceConfigurations.ServerIP}.");

                return clientSocket;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error starting the server: {ex.Message}");
            }

        }

        public static string Send(FileSystemEventArgs e)
        {
            try
            {
                using (Socket s = Start())
                {

                    if (s == null)
                        throw new ApplicationException("Connection failed");

                    byte[] fileData = File.ReadAllBytes(e.FullPath);
                    byte[] nomeArquivoByte = Encoding.UTF8.GetBytes(e.Name);
                    byte[] nomeArquivoLen = BitConverter.GetBytes(nomeArquivoByte.Length);

                    byte[] bytesToSend = new byte[4 + nomeArquivoByte.Length + fileData.Length];
                    nomeArquivoLen.CopyTo(bytesToSend, 0);
                    nomeArquivoByte.CopyTo(bytesToSend, 4);
                    fileData.CopyTo(bytesToSend, 4 + nomeArquivoByte.Length);

                    s.Send(bytesToSend, bytesToSend.Length, 0);

                    s.Close();                   

                    return $"File transferred [{e.Name}] [{bytesToSend.Length} bytes].";
                }

            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error sending file: {ex.Message}");
            }

        }
    }
}
