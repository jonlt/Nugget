using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics.Contracts;

namespace Nugget.Server.Extensions
{
    public static class SocketExtensions
    {
        // class that wraps the two different kinds of callbacks (with or without state object)
        class Callback
        {
            private Action<int> _cb;
            private Action<int, object> _cbWithState;
            
            public Callback(Action<int> callback)
            {
                _cb = callback;
            }

            public Callback(Action<int,object> callback)
            {
                _cbWithState = callback;
            }

            public IAsyncResult BeginInvoke(int arg, AsyncCallback callback, object obj)
            {
                if (_cb != null)
                {
                    return _cb.BeginInvoke(arg, callback, obj);
                }
                else
                {
                    throw new InvalidCastException("callback<int> is not set");
                }
            }

            public IAsyncResult BeginInvoke(int arg, object state, AsyncCallback callback, object obj)
            {
                if (_cbWithState != null)
                {
                    return _cbWithState.BeginInvoke(arg, state, callback, obj);
                }
                else
                {
                    throw new InvalidCastException("callback<int,object> is not set");
                }
                
            }

            public void EndInvoke(IAsyncResult ar)
            {
                if (_cb != null)
                {
                    _cb.EndInvoke(ar);
                }
                else
                {
                    _cbWithState.EndInvoke(ar);
                }
            }
        }
        
        class State
        {
            public Socket Socket { get; set; }
            public Callback Callback { get; set; }
            public object UserDefinedState { get; set; }
        }

        #region Send

        public static void AsyncSend(this Socket socket, byte[] buffer)
        {
            BeginSend(socket, buffer, new State() { Socket = socket, Callback = null });
        }

        public static void AsyncSend(this Socket socket, byte[] buffer, Action<int> callback)
        {
            BeginSend(socket, buffer, new State() { Socket = socket, Callback = new Callback(callback) });
        }

        public static void AsyncSend(this Socket socket, byte[] buffer, object state, Action<int, object> callback)
        {
            BeginSend(socket, buffer, new State() { Socket = socket, Callback = new Callback(callback), UserDefinedState = state });
        }

        private static void BeginSend(Socket socket, byte [] buffer, State state)
        {
            if (socket == null) throw new ArgumentNullException("socket");
            if (buffer == null) throw new ArgumentNullException("buffer");

            if (socket.Connected)
                socket.BeginSend(buffer, 0, buffer.Length, 0, new AsyncCallback(SendCallback), state);
        }


        private static void SendCallback(IAsyncResult ar)
        {
            var state = (State)ar.AsyncState;
            var count = state.Socket.EndSend(ar);
            if (state.Callback != null)
            {
                if (state.UserDefinedState != null)
                {
                    state.Callback.BeginInvoke(count, state.UserDefinedState, new AsyncCallback(SendCallbackCallback), state);
                }
                else
                {
                    state.Callback.BeginInvoke(count, new AsyncCallback(SendCallbackCallback), state);
                }
            }
        }

        private static void SendCallbackCallback(IAsyncResult ar)
        {
            var state = (State)ar.AsyncState;
            state.Callback.EndInvoke(ar);
        }

        #endregion

        #region Receive

        public static void AsyncReceive(this Socket socket, byte[] buffer)
        {
            BeginReceive(socket, buffer, new State() { Socket = socket, Callback = null });
        }

        public static void AsyncReceive(this Socket socket, byte[] buffer, Action<int> callback)
        {
            BeginReceive(socket, buffer, new State() { Socket = socket, Callback = new Callback(callback) });
        }

        public static void AsyncReceive(this Socket socket, byte[] buffer, object state, Action<int, object> callback)
        {
            if (state == null)
                throw new ArgumentNullException("state");

            BeginReceive(socket, buffer, new State() { Socket = socket, Callback = new Callback(callback), UserDefinedState = state });
        }

        private static void BeginReceive(Socket socket, byte[] buffer, State state)
        {
            if (socket == null) throw new ArgumentNullException("socket");
            if (buffer == null) throw new ArgumentNullException("buffer");
            
            if(socket.Connected)
                socket.BeginReceive(buffer, 0, buffer.Length, 0, new AsyncCallback(ReceiveCallback), state);
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            var state = (State)ar.AsyncState;
            var count = state.Socket.EndReceive(ar);
            if (state.Callback != null)
            {
                if (state.UserDefinedState != null)
                {
                    state.Callback.BeginInvoke(count, state.UserDefinedState, new AsyncCallback(ReceiveCallbackCallback), state);
                }
                else
                {
                    state.Callback.BeginInvoke(count, new AsyncCallback(ReceiveCallbackCallback), state);
                }
                
            }
        }

        private static void ReceiveCallbackCallback(IAsyncResult ar)
        {
            var state = (State)ar.AsyncState;
            state.Callback.EndInvoke(ar);
        }

        #endregion
    }
}
