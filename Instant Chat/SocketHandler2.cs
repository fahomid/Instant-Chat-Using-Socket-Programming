using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Instant_Chat
{
    public class SocketHandler2
    {
        protected static Socket socket;
        private static string hashKey = "$66oow$$sjdHhvGRFT**7&kekwhVX";    //this hash will be used to encrypt password
        private static getResponse del;
        private static socketDisconnected socketClosed;

        // ManualResetEvent instances signal completion.
        private static ManualResetEvent connectDone = new ManualResetEvent(false);
        private static ManualResetEvent sendDone = new ManualResetEvent(false);
        private static ManualResetEvent receiveDone = new ManualResetEvent(false);

        // The response from the remote device.
        private Socket client;
        private static String response = String.Empty;
        private static int messageLength = 0;

        public void initSocket(string host, int port, string token, getResponse r, socketDisconnected x)
        {
            // Connect to a remote device.
            try
            {
                socketClosed = x;
                del = r;
                // Establish the remote endpoint for the socket.
                IPAddress ipAddress = IPAddress.Parse(host);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                // Create a TCP/IP socket.
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.
                client.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), client);
                connectDone.WaitOne();

                // Send test data to the remote device.
                sendData(token);
                sendDone.WaitOne();

                // Receive the response from the remote device.
                Receive(client);
                receiveDone.WaitOne();
            }
            catch (Exception e)
            {
                string response = "{'response_type': 'auth','response': 'error','message' : '" + e.Message + "'}";
                del(response);
            }
        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);

                Console.WriteLine("Socket connected to {0}",
                    client.RemoteEndPoint.ToString());

                // Signal that the connection has been made.
                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Receive(Socket client)
        {
            try
            {
                // Create the state object.
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ReceiveCallback(IAsyncResult bufferArray)
        {
            try
            {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StateObject state = (StateObject)bufferArray.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.
                int bytesRead = client.EndReceive(bufferArray);
                Console.WriteLine("Received: "+bytesRead);
                
                if(string.IsNullOrEmpty(response)) response = Encoding.ASCII.GetString(state.buffer, 0, bytesRead);

                if (bytesRead > 0)
                {
                    if (Regex.IsMatch(response, @"^\d+"))
                    {
                        string lengthString = new String(response.TakeWhile(Char.IsDigit).ToArray());
                        int lengthStringSize = lengthString.Length;
                        messageLength = Int32.Parse(lengthString);
                        Console.WriteLine("responseX: " + response);
                        response = response.Substring(lengthStringSize);
                        if (response.Length >= messageLength)
                        {
                            string realResponse = response.Substring(0, messageLength);
                            if (response.Length > messageLength)
                            {
                                response = response.Substring(messageLength);
                            }
                            else
                            {
                                response = string.Empty;
                            }
                            Console.WriteLine(realResponse);
                            if (IsValidJson(realResponse))
                            {
                                del(realResponse);
                                messageLength = 0;
                            }
                        }
                        Console.WriteLine("response: " + response);
                        Console.WriteLine("lengthString: " + lengthString);
                        Console.WriteLine("messageLength: " + messageLength);
                        Console.WriteLine("lengthStringSize: " + lengthStringSize);
                    }
                    else
                    {
                        response += Encoding.ASCII.GetString(state.buffer, 0, bytesRead);
                        if (response.Length >= messageLength)
                        {
                            string realResponse = response.Substring(0, messageLength);
                            if (response.Length > messageLength)
                            {
                                response = response.Substring(messageLength);
                            }
                            else
                            {
                                response = string.Empty;
                            }
                            if (IsValidJson(realResponse))
                            {
                                del(realResponse);
                                messageLength = 0;
                            }
                        }
                        Console.WriteLine("ELSE: " + response);
                    }

                    // Get the rest of the data.
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void sendData(String data)
        {
            Send(client, data);
        }

        private static void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), client);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);

                // Signal that all bytes have been sent.
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void closeSocket()
        {
            // Release the socket.
            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }

        //this method will take string as parameter and will return encrypted password
        public string sha256_hash(string value)
        {
            using (SHA256 hash = SHA256Managed.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(string.Concat(value, hashKey));
                byte[] generatedHash = hash.ComputeHash(bytes);
                return GetStringFromHash(generatedHash);
            }
        }



        //this method will convert byte array into string
        private string GetStringFromHash(byte[] hash)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                result.Append(hash[i].ToString("x2"));
            }
            return result.ToString();
        }

        private static bool IsValidJson(string strInput)
        {

            try
            {
                var obj = JObject.Parse(strInput);
                return true;
            }
            catch (Exception ex) //some other exception
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }

    // State object for receiving data from remote device.
    public class StateObject
    {
        // Client socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 256;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }
}
