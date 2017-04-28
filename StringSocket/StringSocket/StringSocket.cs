// Written by Joe Zachary for CS 3500, November 2012
// Revised by Joe Zachary April 2016
// Revised extensively by Joe Zachary April 2017

using System;
using System.Text;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;

namespace CustomNetworking
{
    /// <summary>
    /// The type of delegate that is called when a StringSocket send has completed.
    /// </summary>
    public delegate void SendCallback(bool wasSent, object payload);

    /// <summary>
    /// The type of delegate that is called when a receive has completed.
    /// </summary>
    public delegate void ReceiveCallback(String s, object payload);

    /// <summary> 
    /// A StringSocket is a wrapper around a Socket.  It provides methods that
    /// asynchronously read lines of text (strings terminated by newlines) and 
    /// write strings. (As opposed to Sockets, which read and write raw bytes.)  
    ///
    /// StringSockets are thread safe.  This means that two or more threads may
    /// invoke methods on a shared StringSocket without restriction.  The
    /// StringSocket takes care of the synchronization.
    /// 
    /// Each StringSocket contains a Socket object that is provided by the client.  
    /// A StringSocket will work properly only if the client refrains from calling
    /// the contained Socket's read and write methods.
    /// 
    /// We can write a string to a StringSocket ss by doing
    /// 
    ///    ss.BeginSend("Hello world", callback, payload);
    ///    
    /// where callback is a SendCallback (see below) and payload is an arbitrary object.
    /// This is a non-blocking, asynchronous operation.  When the StringSocket has 
    /// successfully written the string to the underlying Socket, or failed in the 
    /// attempt, it invokes the callback.  The parameter to the callback is the payload.  
    /// 
    /// We can read a string from a StringSocket ss by doing
    /// 
    ///     ss.BeginReceive(callback, payload)
    ///     
    /// where callback is a ReceiveCallback (see below) and payload is an arbitrary object.
    /// This is non-blocking, asynchronous operation.  When the StringSocket has read a
    /// string of text terminated by a newline character from the underlying Socket, or
    /// failed in the attempt, it invokes the callback.  The parameters to the callback are
    /// a string and the payload.  The string is the requested string (with the newline removed).
    /// </summary>

    public class StringSocket : IDisposable
    {
        // Underlying socket
        private Socket socket;

        // Encoding used for sending and receiving
        private Encoding encoding;

        //Storage for the decoder
        private Decoder decoder;

        //Queue for the Send Requests
        private Queue<StringSocket.SendRequest> sendRequests;

        //Queue for the Receieve Requests
        private Queue<StringSocket.ReceiveRequest> receiveRequests;

        //The incoming string
        private StringBuilder incoming;

        //Incoming bytes
        private byte[] incomingBuffer;
        
        //An outgoing string
        private StringBuilder outgoing;

        // For synchronizing sends
        private readonly object sendSync = new object();

        // For synchronizing receipts
        private readonly object recSync = new object();


        // Bytes that we are actively trying to send, along with the
        // index of the leftmost byte whose send has not yet been completed
        private byte[] pendingBytes = new byte[0];
        private int pendingIndex = 0;

        // Records whether an asynchronous send attempt is ongoing
        private bool sendIsOngoing = false;

        /// <summary>
        /// Creates a StringSocket from a regular Socket, which should already be connected.  
        /// The read and write methods of the regular Socket must not be called after the
        /// StringSocket is created.  Otherwise, the StringSocket will not behave properly.  
        /// The encoding to use to convert between raw bytes and strings is also provided.
        /// </summary>
        internal StringSocket(Socket s, Encoding e)
        {
            socket = s;
            encoding = e;
            incoming = new StringBuilder();
            outgoing = new StringBuilder();

            incomingBuffer = new byte[1024]; //Max number of bytes per socket.

            //sets the decoder to the encoders decoder
            decoder = encoding.GetDecoder();
 
            //initializes the queue for the send requests
            sendRequests = new Queue<StringSocket.SendRequest>();

            //intializes the queue for the receive requests
            receiveRequests = new Queue<StringSocket.ReceiveRequest>();

            socket.BeginReceive(incomingBuffer, 0, incomingBuffer.Length, SocketFlags.None, MessageReceived, incomingBuffer);
        }

        /// <summary>
        /// Shuts down this StringSocket.
        /// </summary>
        public void Shutdown(SocketShutdown mode)
        {
            socket.Shutdown(mode);
        }

        /// <summary>
        /// Closes this StringSocket.
        /// </summary>
        public void Close()
        {
            socket.Close();
        }

        /// <summary>
        /// We can write a string to a StringSocket ss by doing
        /// 
        ///    ss.BeginSend("Hello world", callback, payload);
        ///    
        /// where callback is a SendCallback (see below) and payload is an arbitrary object.
        /// This is a non-blocking, asynchronous operation.  When the StringSocket has 
        /// successfully written the string to the underlying Socket it invokes the callback.  
        /// The parameters to the callback are true and the payload.
        /// 
        /// If it is impossible to send because the underlying Socket has closed, the callback 
        /// is invoked with false and the payload as parameters.
        ///
        /// This method is non-blocking.  This means that it does not wait until the string
        /// has been sent before returning.  Instead, it arranges for the string to be sent
        /// and then returns.  When the send is completed (at some time in the future), the
        /// callback is called on another thread.
        /// 
        /// This method is thread safe.  This means that multiple threads can call BeginSend
        /// on a shared socket without worrying around synchronization.  The implementation of
        /// BeginSend must take care of instead.  On a given StringSocket, each
        /// string arriving via a BeginSend method call must be sent (in its entirety) before
        /// a later arriving string can be sent.
        /// </summary>
        public void BeginSend(String s, SendCallback callback, object payload)
        {
            lock(sendSync)//lock (this.sendRequests)
            {
                //checks the status of the socket 
                bool socketAvailable = socket.Poll(500, SelectMode.SelectRead);

                //checks the amount of data in the socket
                bool socketHasData = socket.Available == 0;

                //checks to see if the socket is connected
                bool socketConnected = socket.Connected;

                //checks if the socket is closed
                //if it isn't closed then we invoke callback with false and the payload
                if (!((socketAvailable && socketHasData) || socketConnected))
                {
                    callback(false, payload);
                }

                //Add the outgoing string to our StringSocket 
                outgoing.Append(s);

                //Create a new request and enqueue it.
                SendRequest sr = new SendRequest();
                sr._Callback = callback;
                sr._Payload = payload;

                //enqueues the current request
                sendRequests.Enqueue(sr);

                //while the queue has something in it, it will send the items
                if(!sendIsOngoing)
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
                socket.BeginSend(pendingBytes, pendingIndex, pendingBytes.Length - pendingIndex,
                                 SocketFlags.None, MessageSent, null);
            }

            // If we're not currently dealing with a block of bytes, make a new block of bytes
            // out of outgoing and start sending that.
            else if (outgoing.Length > 0)
            {
                SendRequest SR = sendRequests.Dequeue();
                byte[] outgoingBytes = encoding.GetBytes(outgoing.ToString());
                outgoing = new StringBuilder();
                socket.BeginSend(outgoingBytes, 0, outgoingBytes.Length, SocketFlags.None, MessageSent, outgoingBytes);
                ThreadPool.QueueUserWorkItem(state => { SR._Callback(true, SR._Payload); });
            }

            // If there's nothing to send, shut down for the time being.
            else
            {
                sendIsOngoing = false;
            }
        }

        /// <summary>
        /// We can read a string from the StringSocket by doing
        /// 
        ///     ss.BeginReceive(callback, payload)
        ///     
        /// where callback is a ReceiveCallback (see below) and payload is an arbitrary object.
        /// This is non-blocking, asynchronous operation.  When the StringSocket has read a
        /// string of text terminated by a newline character from the underlying Socket, it 
        /// invokes the callback.  The parameters to the callback are a string and the payload.  
        /// The string is the requested string (with the newline removed).
        /// 
        /// Alternatively, we can read a string from the StringSocket by doing
        /// 
        ///     ss.BeginReceive(callback, payload, length)
        ///     
        /// If length is negative or zero, this behaves identically to the first case.  If length
        /// is positive, then it reads and decodes length bytes from the underlying Socket, yielding
        /// a string s.  The parameters to the callback are s and the payload
        ///
        /// In either case, if there are insufficient bytes to service a request because the underlying
        /// Socket has closed, the callback is invoked with null and the payload.
        /// 
        /// This method is non-blocking.  This means that it does not wait until a line of text
        /// has been received before returning.  Instead, it arranges for a line to be received
        /// and then returns.  When the line is actually received (at some time in the future), the
        /// callback is called on another thread.
        /// 
        /// This method is thread safe.  This means that multiple threads can call BeginReceive
        /// on a shared socket without worrying around synchronization.  The implementation of
        /// BeginReceive must take care of synchronization instead.  On a given StringSocket, each
        /// arriving line of text must be passed to callbacks in the order in which the corresponding
        /// BeginReceive call arrived.
        /// 
        /// Note that it is possible for there to be incoming bytes arriving at the underlying Socket
        /// even when there are no pending callbacks.  StringSocket implementations should refrain
        /// from buffering an unbounded number of incoming bytes beyond what is required to service
        /// the pending callbacks.
        /// </summary>
        public void BeginReceive(ReceiveCallback callback, object payload, int length = 0)
        {
            lock (recSync)
            {
                //Save everything into the new receive request.
                ReceiveRequest RR = new ReceiveRequest();
                RR._CallBack = callback;
                RR._Payload = payload;

                receiveRequests.Enqueue(RR);

                ReceiveBytes();
                
            }
        }
        /// <summary>
        /// Our helper method for receiving bytes.
        /// </summary>
        private void ReceiveBytes()
        {
            int i;
            while ((i = incoming.ToString().IndexOf('\n')) >= 0)
            {
                if (receiveRequests.Count > 0) //If we still have things to send.
                {
                    string line = incoming.ToString().Substring(0, i); //Get everything before the newline character.

                    StringBuilder temp = new StringBuilder();
                    temp.Append(incoming.ToString().Substring(i + 1));

                    incoming = temp;

                    ReceiveRequest RR = receiveRequests.Dequeue();
                    ThreadPool.QueueUserWorkItem(state => { RR._CallBack(line, RR._Payload); });
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Callback when a socket message is received.
        /// </summary>
        /// <param name="result"></param>
        private void MessageReceived(IAsyncResult result)
        {
            try
            {
                lock (recSync)
                {
                    int bytes = socket.EndSend(result);
                    incomingBuffer = (byte[])(result.AsyncState);

                    if (bytes == 0)
                    {
                        socket.Close();
                    }
                    else
                    {
                        incoming.Append(encoding.GetString(incomingBuffer, 0, bytes)); //Add the incoming buffer bytes to our incoming string.
                        ReceiveBytes();
                        socket.BeginReceive(incomingBuffer, 0, incomingBuffer.Length, SocketFlags.None, MessageReceived, incomingBuffer);
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Frees resources associated with this StringSocket.
        /// </summary>
        public void Dispose()
        {
            Shutdown(SocketShutdown.Both);
            Close();
        }

        /// <summary>
        /// 
        /// </summary>
        struct SendRequest
        {
            public SendCallback _Callback { get; set; }
            public string _Text { get; set; }
            public object _Payload { get; set; }
        }

        struct ReceiveRequest
        {
            public ReceiveCallback _CallBack { get; set; }
            public object _Payload { get; set; }
            public int _Length { get; set; }
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
                }

                // Update the pendingIndex and keep trying
                else
                {
                    pendingIndex += bytesSent;
                    SendBytes();
                }
            }
        }
    }
}
