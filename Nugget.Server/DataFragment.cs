using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Nugget.Server
{
    public class DataFragment
    {
        [Flags]
        public enum FragmentHead
        {
            Fin = 0x80,
            RSV1 = 0x40,
            RSV2 = 0x20,
            RSV3 = 0x10,
        }

        public enum FragmentOpcode : byte
        {
            Continuation = 0x0,
            Text = 0x1,
            Binary = 0x2,
            // reserved for further non-control frames
            Close = 0x8,
            Ping = 0x9,
            Pong = 0xa,
            // reserved for further control frames
        }

        private const byte _opcodeMask = 0x0f;
        private const byte _maskFlagMask = 0x80;
        private const byte _payloadLengthMask = 0x7f;

        private FragmentHead _head;
        private byte[] _fragment;
        private int _maskKeyPos;
        private int _payloadPos;

        public int PayloadLength { get; private set; }
        public int Length { get { return _payloadPos + PayloadLength; } }

        public bool Fin { get { return _head.HasFlag(FragmentHead.Fin); } }
        public bool RSV1 { get { return _head.HasFlag(FragmentHead.RSV1); } }
        public bool RSV2 { get { return _head.HasFlag(FragmentHead.RSV2); } }
        public bool RSV3 { get { return _head.HasFlag(FragmentHead.RSV3); } }

        public bool Masked { get; private set; }
        private bool _hasMaskingKey = false;

        public FragmentOpcode Opcode { get; private set; }

        public DataFragment(byte[] fragment)
        {
            _fragment = fragment;

            _head = (FragmentHead)_fragment[0];
            Opcode = (FragmentOpcode)(_fragment[0] & _opcodeMask);
            Masked = (_fragment[1] & _maskFlagMask) != 0;
            _hasMaskingKey = Masked;

            CalculateLengthAndPositions();
        }

        public static DataFragment Create(byte[] payload, FragmentHead head, FragmentOpcode opcode, bool mask)
        {
            var payloadLength = payload.Length;
            var lengthBytes = new List<byte>();
            if (payloadLength < 126)
            {
                lengthBytes.Add((byte)payloadLength);
            }
            else if (payloadLength < ushort.MaxValue)
            {
                lengthBytes.Add(126);
                var lbytes = BitConverter.GetBytes((ushort)payloadLength);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lbytes);

                lengthBytes.AddRange(lbytes);
            }
            else
            {
                lengthBytes.Add(127);
                var lbytes = BitConverter.GetBytes((ulong)payloadLength);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lbytes);

                lengthBytes.AddRange(lbytes);
            }

            byte[] maskingKey = new byte[0];
            if (mask)
            {
                var rnd = new Random();
                maskingKey = new byte[4];
                rnd.NextBytes(maskingKey);
                ToggleMasking(payload, 0, maskingKey);
                lengthBytes[0] = (byte)(lengthBytes[0] + _maskFlagMask);
            }

            var bytes = new List<byte>();
            bytes.Add((byte)((byte)head + (byte)opcode));
            bytes.AddRange(lengthBytes);
            bytes.AddRange(maskingKey);
            bytes.AddRange(payload);

            return new DataFragment(bytes.ToArray());
        }

        public void UnMaskPayload()
        {
            if (Masked)
            {
                var maskingKey = new ArraySegment<byte>(_fragment, _maskKeyPos, 4).ToArray();
                ToggleMasking(_fragment, _payloadPos, maskingKey);
                Masked = false;
            }
        }

        public static void ToggleMasking(byte[] buffer, int offset, byte[] key)
        {
            for (int i = 0; i < buffer.Length - offset; i++)
            {
                var index = i + offset;

                var j = i % 4;
                var masking_key_octet_j = key[j];
                var original_octet_i = buffer[index];
                var transformed_octet_i = original_octet_i ^ masking_key_octet_j;

                buffer[index] = (byte)transformed_octet_i;
            }
        }

        public byte[] GetPayload()
        {
            return new ArraySegment<byte>(_fragment, _payloadPos, PayloadLength).ToArray();
        }

        public byte[] GetBytes()
        {
            return _fragment;
        }

        private void CalculateLengthAndPositions()
        {
            var payloadLength7 = (byte)(_fragment[1] & _payloadLengthMask);

            var payloadPos = 2;
            if (payloadLength7 < 126)
            {
                PayloadLength = payloadLength7;
            }
            if (payloadLength7 == 126)
            {
                payloadPos += 2;
                var lengthBytes = new ArraySegment<byte>(_fragment, 2, 2).ToArray();
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lengthBytes);

                PayloadLength = BitConverter.ToUInt16(lengthBytes, 0);
            }
            else if (payloadLength7 == 127)
            {
                payloadPos += 8;
                var lengthBytes = new ArraySegment<byte>(_fragment, 2, 8).ToArray();
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lengthBytes);

                var length = BitConverter.ToUInt64(lengthBytes, 0);

                if (length > int.MaxValue)
                    throw new OverflowException("The fragment is too big");

                PayloadLength = (int)length;
            }

            _maskKeyPos = payloadPos;
            _payloadPos = (_hasMaskingKey) ? payloadPos + 4 : payloadPos;
        }



    }
}
