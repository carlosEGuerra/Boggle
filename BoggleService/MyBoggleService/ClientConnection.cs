using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MyBoggleService
{
    public class ClientConnection
    {
        //incoming/outgoing is UTF8 Encoded
        private static System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();

        //Buffer size for reading incoming bytes
        private const int BUFFER_SIZE = 1024;

        //The socket though through which we communicate with the remote client
        private Socket socket;

        //Text that has been reveived from the client but not yet dealt with
        private StringBuilder incoming;

        //Text that needs to be sent to the client but which we have yet started sending
        private StringBuilder outgoing;

        //For decoding incoming UTF8 encoded byte streams.
        private Decoder decoder = encoding.GetDecoder();

        //Buffers that will contain incoming bytes and characters
        private byte[] incomingBytes = new byte[BUFFER_SIZE];
        private char[] incomingChars = new char[BUFFER_SIZE];

        //Holds all of the data of the request, split up by whitespace.
        private int incomingData = 0;

        //Integer to keep track of which part of the data we're on.
        private int curDataPos = 0;

        //To keep track of the current type of request we're dealing with.
        private string curRequestType;

        //Keeps the content length of the JSON object, ommitting whitespacec
        private int contentLength = 0;

        //Holds the current jsonContent;
        private string jsonContent;

        //Holds the current URL with the put, post or get request.
        private string curURL;

        //Records whether an asynchronous send attempt is ongoing
        private bool sendIsOngoing = false;


        //Records if the request has been completed.
        private bool requestCompleted = false;

        //For synchronizing sends
        private readonly object sendSync = new object();

        //Bytes that we are actively trying to send, along with the
        //index of the leftmost byte whose send has not yet been completed
        private byte[] pendingBytes = new byte[0];
        private int pendingIndex = 0;

        private BoggleService server;

        /// <summary>
        /// Creates a ClientConnection from the socket, then begins communicating with it.
        /// </summary>
        public ClientConnection(Socket s, BoggleService server)
        {
            // Record the socket and server and initialize incoming/outgoing
            this.server = server;
            socket = s;
            incoming = new StringBuilder();
            outgoing = new StringBuilder();

            try
            {
                // Ask the socket to call MessageReceive as soon as up to 1024 bytes arrive.
                socket.BeginReceive(incomingBytes, 0, incomingBytes.Length,
                                    SocketFlags.None, MessageReceived, null);
            }
            catch (ObjectDisposedException)
            {
            }

        }

        /// <summary>
        /// Called when some data has been received.
        /// </summary>
        private void MessageReceived(IAsyncResult result)
        {
            // Figure out how many bytes have come in
            int bytesRead = socket.EndReceive(result);

            //Lets us know we've collected all the content.
            bool contentCollected = false;

            // If no bytes were received, it means the client closed its side of the socket.
            // Report that to the console and close our socket.
            if (bytesRead == 0)
            {
                Console.WriteLine("Socket closed");
                server.RemoveClient(this);
                socket.Close();
            }

            // Otherwise, decode and display the incoming bytes.  Then request more bytes.
            else
            {

                // Convert the bytes into characters and appending to incoming
                int charsRead = decoder.GetChars(incomingBytes, 0, bytesRead, incomingChars, 0, false);
                incoming.Append(incomingChars, 0, charsRead);
                Console.WriteLine(incoming);

                // Echo any complete lines, after capitalizing them
                int lastNewline = -1;
                int start = 0;
                for (int i = 0; i < incoming.Length; i++)
                {
                    if (incoming[i] == '\n' || incoming[i] == '}')
                    {
                        String line = incoming.ToString(start, i + 1 - start);

                        string[] splitString = line.Split();

                        incomingData++; //Keeps track of how many lines of the socket we've received.

                        //If we have incoming data.
                        if (incomingData > 0)
                        {
                            //OUT PARAMETERS
                            string gameID;
                            string urlRequest; // /games, /users
                            string brief;

                            if (incomingData == 1) //If we only have 1 item in the incoming data, figure out what type of request we have.
                            {
                                GetRequestType(splitString);
                                ExtractServiceParams(splitString, out urlRequest, out gameID, out brief);
                                curURL = urlRequest;
                            }

                            if (curRequestType != null && !requestCompleted)//Check for content type.
                            {
                                //"content-length" @ index 7
                                if (splitString.Contains("content-length:") && splitString.Length >= 9)//if (splitString[7].ToUpper() == "CONTENT-LENGTH:")
                                {
                                    int cLength;
                                    int.TryParse(splitString[8], out cLength);
                                    contentLength = cLength;
                                    //NEED A CASE TO SAY THAT THE CONTENT IS COLLECTED IF WE HAVE A GET WITH NO   
                                }

                            }

                            //When we finally have the content length and we need to begin reading the bytes of content.
                            if (splitString.Length >= 16 && incoming.Length >= (83 + contentLength) && contentCollected == false) //Collect content only when we have the complete content
                            {
                                //16 is the starting index.
                                //Compose the JSON string.
                                for (int l = 99; l <= 99 + contentLength; l++)
                                {
                                    if (!string.IsNullOrEmpty(incoming[l].ToString()))
                                    {
                                        jsonContent += incoming[l];
                                        if (incoming[l] == '}')
                                        {
                                            contentCollected = true;
                                            break;
                                        }
                                    }
                                    if(string.IsNullOrEmpty(incoming[l].ToString()) || (String.IsNullOrWhiteSpace(incoming[l].ToString())))
                                    {
                                        contentLength++;
                                    }
                                }

                                //Content is collected FLAG
                                contentCollected = true;
                            }

                            //For the case of the get
                            if (!splitString.Contains("content-length:") && curRequestType == "GET")//If we have a get with no content length
                            {
                                contentCollected = true;
                            }

                            if (contentCollected)
                            {
                                //Call the service method.
                                CallServerMethod();
                            }
                        }
                    }
                }
                incoming.Remove(0, lastNewline + 1);

                try
                {
                    // Ask for some more data
                    socket.BeginReceive(incomingBytes, 0, incomingBytes.Length,
                        SocketFlags.None, MessageReceived, null);
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }
        /// <summary>
        /// Sends a string to the client
        /// </summary>
        public void SendMessage(string lines)
        {
            // Get exclusive access to send mechanism
            lock (sendSync)
            {
                // Append the message to the outgoing lines
                outgoing.Append(lines);

                // If there's not a send ongoing, start one.
                if (!sendIsOngoing)
                {
                    sendIsOngoing = true;
                    SendBytes();
                }
            }
        }

        /// <summary>
        /// Attempts to send the entire outgoing string.
        /// This method should not be called unless sendSync has been acquired.
        /// </summary>
        private void SendBytes()
        {
            // If we're in the middle of the process of sending out a block of bytes,
            // keep doing that.
            if (pendingIndex < pendingBytes.Length)
            {
                try
                {
                    socket.BeginSend(pendingBytes, pendingIndex, pendingBytes.Length - pendingIndex,
                                     SocketFlags.None, MessageSent, null);
                }
                catch (ObjectDisposedException)
                {
                }
            }

            // If we're not currently dealing with a block of bytes, make a new block of bytes
            // out of outgoing and start sending that.
            else if (outgoing.Length > 0)
            {
                pendingBytes = encoding.GetBytes(outgoing.ToString());
                pendingIndex = 0;
                outgoing.Clear();
                try
                {
                    socket.BeginSend(pendingBytes, 0, pendingBytes.Length,
                                     SocketFlags.None, MessageSent, null);
                }
                catch (ObjectDisposedException)
                {
                }
            }

            // If there's nothing to send, shut down for the time being.
            else
            {
                sendIsOngoing = false;
            }
        }

        /// <summary>
        /// Called when a message has been successfully sent
        /// </summary>
        private void MessageSent(IAsyncResult result)
        {
            // Find out how many bytes were actually sent
            int bytesSent = socket.EndSend(result);

            // Get exclusive access to send mechanism
            lock (sendSync)
            {
                // The socket has been closed
                if (bytesSent == 0)
                {
                    socket.Close();
                    server.RemoveClient(this);
                    Console.WriteLine("Socket closed");
                }

                // Update the pendingIndex and keep trying
                else
                {
                    pendingIndex += bytesSent;
                    SendBytes();
                }
            }
        }

        /// <summary>
        /// Gets and sets the type of request we're currently dealing with.
        /// Also gets the parameters of the URL
        /// </summary>
        private void GetRequestType(string[] request)
        {
            curRequestType = request[0];
            return;
        }

        /// <summary>
        /// Sets the global service parameters if there are any.
        /// </summary>
        /// <param name="request"></param>
        private void ExtractServiceParams(string[] request, out string urlRequest, out string gameID, out string brief)
        {
            //Regex gamesIDReg = new Regex(@"games/[0-9]+");
            string url = request[1]; //Eg. /games/0
            string[] urlTrim = url.Split('/');
            urlRequest = urlTrim[1];

            gameID = null;
            brief = null;

            if (urlRequest == "users")
            {
                gameID = null;
                brief = null;
                return;
            }

            if (urlRequest == "games")
            {
                if (urlTrim.Length == 1)
                {
                    gameID = null;
                    brief = null;
                    return;
                }

                if (urlTrim.Length == 2)
                {
                    gameID = urlTrim[1];
                    brief = null;
                    return;
                }

                if (urlTrim.Length == 3)
                {
                    gameID = urlTrim[1];
                    brief = urlTrim[2].Substring(2);
                    return;
                }
            }
        }

        /// <summary>
        /// Returns the result of a call to the proper service method according to the socket request.
        /// </summary>
        /// <returns></returns>
        private void CallServerMethod()
        {
            object returnContent;
            //CreateUser
            if (curRequestType == "POST")
            {
                //Deals with Create User
                if (curURL == "users")
                {
                    CreateUserData content = JsonConvert.DeserializeObject<CreateUserData>(jsonContent);
                     returnContent = server.CreateUser(content); //Do the CreateUser work.
                     
                }
                //Deals with Join Game
                else if (curURL == "games")
                {
                    JoinGameData content = JsonConvert.DeserializeObject<JoinGameData>(jsonContent);
                    returnContent = server.JoinGame(content);
                }
            }
            //for the case when the Request Type is JOIN
            else if (curRequestType == "PUT")
            {
                string gameID = this.curURL[1].ToString();
                //need to fix the return object
                //Deals with PlayWord
                if (curURL == "games" && !string.IsNullOrEmpty(gameID))
                {
                    CancelJoinData content = JsonConvert.DeserializeObject<CancelJoinData>(jsonContent);
                }
                //Deals with CancelJoinGame
                if(curURL == "games")
                {

                }
            }
            //deals with GameStatus
            else if (curRequestType == "GET")
            {
                //For when Brief is No or Null
                if(this.curURL[1].ToString() == "no" || string.IsNullOrEmpty(this.curURL[1].ToString()))
                {

                }

                //For when Brief is Yes
                if(this.curURL[1].ToString() == "yes")
                {

                }
            }

            return;
        }
    }
}
