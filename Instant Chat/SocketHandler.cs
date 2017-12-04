using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Instant_Chat
{
    class SocketHandler
    {
        protected static Socket socket;
        private static string hashKey = "$66oow$$sjdHhvGRFT**7&kekwhVX";    //this hash will be used to encrypt password
        private static getResponse del;
        private static socketDisconnected socketClosed;
        BackgroundWorker worker;
        private static String response = String.Empty;
        private static int messageLength = 0;

        public SocketHandler()
        {
            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(receiveData);
            worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            del(e.UserState.ToString().Trim());
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (socket.Connected)
            {
                socket.Disconnect(true);
            }
        }


        //this method will be used to initialize a socket connection with server
        public void initSocket(string host, int port, string token, getResponse r, socketDisconnected x)
        {
            try
            {
                socketClosed = x;
                del = r;
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(host, port);
                socket.Send(Encoding.ASCII.GetBytes(token));
                if (!worker.IsBusy)
                {
                    worker.RunWorkerAsync();
                }
            }
            catch (SocketException e)
            {
                string response = "{'response_type': 'auth','response': 'error','message' : '" + e.Message + "'}";
                del(response);
            }
        }

        //Once a connection successfully made then this method will be called to
        //check if any new message came from server
        public void receiveData(object sender, DoWorkEventArgs e)
        {
            try
            {
                int bytesRead;
                byte[] buffer = new byte[256];
                while ((bytesRead = socket.Receive(buffer)) > 0 && socket.Connected)
                {
                    File.AppendAllText("log.txt", Encoding.ASCII.GetString(buffer, 0, bytesRead) + "\n\n");
                    response += Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    //Console.WriteLine("String 1st:" +response);
                    if (Regex.IsMatch(response, @"^\d+"))
                    {
                        string lengthString = new String(response.TakeWhile(Char.IsDigit).ToArray());
                        int lengthStringSize = lengthString.Length;
                        messageLength = Int32.Parse(lengthString);
                        response = response.Substring(lengthStringSize);
                    }

                    if (messageLength > 0 && response.Length >= messageLength)
                    {
                        string realResponse = response.Substring(0, messageLength);
                        response = response.Substring(messageLength);

                        if (IsValidJson(realResponse))
                        {
                            worker.ReportProgress(0, realResponse);
                            messageLength = 0;
                        }
                        else
                        {
                            response = string.Empty;
                        }
                    }
                    
                    Console.WriteLine("Response String:" +response);
                }
            }
            catch (SocketException err)
            {
                closeThread();
                socketClosed();
                Console.WriteLine(err.Message);
            }


        }

        //This method will take string as parameter and will send that string
        //to chat server
        public void sendData(string data)
        {
            byte[] msg = Encoding.UTF8.GetBytes(data);
            try
            {
                // Get reply from the server.
                int i = socket.Send(msg);
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.Message);
            }
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

        public void closeSocket()
        {
            if (socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Disconnect(true);
            }
            closeThread();
        }

        public void closeThread()
        {
            if (worker.IsBusy)
            {
                worker.CancelAsync();
            }
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
}