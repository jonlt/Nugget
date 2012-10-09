using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Nugget.Server.Extensions;

namespace Nugget.Server
{
    public class DataPackage
    {
        public const int BufferSize = 1024;

        private class ReadState
        {
            public Socket Socket { get; set; }
            public Action<IEnumerable<DataFragment>> Callback { get; set; }
            public List<DataFragment> Fragments { get; set; }
            public List<byte[]> Buffers { get; set; }
            public int CurrentLength { get; set; }
            public int ExpectedLength { get; set; }

            public ReadState(Socket socket, Action<IEnumerable<DataFragment>> callback)
            {
                Socket = socket;
                Callback = callback;
                Fragments = new List<DataFragment>();
                Buffers = new List<byte[]>();
                CurrentLength = 0;
                ExpectedLength = 0;
            }

            public ReadState(Socket socket, Action<IEnumerable<DataFragment>> callback, List<DataFragment> fragments)
                : this(socket, callback)
            {
                Fragments = fragments;
            }
        }

        public static void Read(Socket socket, Action<IEnumerable<DataFragment>> callback)
        {
            var state = new ReadState(socket, callback);
            ReadMore(state);
        }


        private static void ReadMore(ReadState state)
        {
            var buffer = new byte[BufferSize];
            state.Socket.AsyncReceive(buffer, (len) =>
            {
                DataFragment fragment = null;

                if (!state.Buffers.Any())
                {
                    fragment = new DataFragment(buffer);
                    state.ExpectedLength = fragment.Length;
                }

                state.Buffers.Add(buffer.Take(len).ToArray());

                state.CurrentLength += len;
                if (state.CurrentLength < state.ExpectedLength)
                {
                    ReadMore(state);
                }
                else
                {
                    if (fragment == null)
                    {
                        var ba = state.Buffers.SelectMany(b => b).ToArray();
                        fragment = new DataFragment(ba);
                    }
                    state.Fragments.Add(fragment);
                    if (!fragment.Fin)
                    {
                        ReadMore(new ReadState(state.Socket, state.Callback, state.Fragments));
                    }
                    else
                    {
                        state.Callback(state.Fragments);
                    }
                }
            });
        }
    }
}
