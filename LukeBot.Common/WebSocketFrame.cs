using System;

namespace LukeBot.Common
{
    class WebSocketFrame
    {
        public enum Op
        {
            Continuation = 0x0,
            Text = 0x1,
            Binary = 0x2,
            Close = 0x8,
            Ping = 0x9,
            Pong = 0xA
        };

        const byte USE_PAYLOAD_LENGTH_B = 126;
        const byte USE_PAYLOAD_LENGTH_C = 127;

        public bool Final { get; private set; }
        public Op Opcode { get; private set; }
        bool mMasked;
        byte mPayloadLength;
        long mPayloadLengthExt;
        byte[] mMaskingKey;
        byte[] mPayload;

        Op OpFromInt(int val)
        {
            switch (val)
            {
            case 0x0: return Op.Continuation;
            case 0x1: return Op.Text;
            case 0x2: return Op.Binary;
            case 0x8: return Op.Close;
            case 0x9: return Op.Ping;
            case 0xA: return Op.Pong;
            default:
                throw new ArgumentException("Invalid value - " + val.ToString("x") + " is not a valid WebSocket opcode");
            }
        }

        void CheckBufferLength(long alreadyRead, long toRead, long length, string stage)
        {
            if (alreadyRead + toRead > length)
                throw new InvalidOperationException(String.Format(
                    "Provided buffer is too short - expected {0}, length is {1} (stage {2})",
                    alreadyRead + toRead,
                    length,
                    stage
                ));
        }

        // returns how many bytes were read from buffer
        public long FillFromBuffer(byte[] buf, long offset)
        {
            long read = 0;

            Logger.Trace("WebSocketFrame fill trace:");

            // byte 0 - Fin flag and Opcode
            CheckBufferLength(offset + read, 1, buf.LongLength, "FIN/Opcode");
            Final = (buf[offset + read] & 0x80) > 0;
            Logger.Trace(" -> mFinal: {0}", Final);
            Opcode = OpFromInt(buf[offset + read] & 0x0F);
            Logger.Trace(" -> mOpcode: {0}", Opcode);
            read++;

            // byte 1 - Masked flag and payload length
            CheckBufferLength(offset + read, 1, buf.LongLength, "MASK/PayloadLenA");
            mMasked = (buf[offset + read] & 0x80) > 0;
            Logger.Trace(" -> mMasked: {0}", mMasked);
            mPayloadLength = (byte)(buf[offset + read] & 0x7F);
            Logger.Trace(" -> mPayloadLength: {0}", mPayloadLength);
            read++;

            if (mPayloadLength == USE_PAYLOAD_LENGTH_B)
            {
                CheckBufferLength(offset + read, 2, buf.LongLength, "PayloadLenExt16");
                mPayloadLengthExt = (buf[offset + read] << 8) | (buf[offset + read + 1]);
                read += 2;
            }
            else if (mPayloadLength == USE_PAYLOAD_LENGTH_C)
            {
                CheckBufferLength(offset + read, 8, buf.LongLength, "PayloadLenExt64");
                mPayloadLengthExt =
                    (buf[offset + read + 0] << 56) |
                    (buf[offset + read + 1] << 48) |
                    (buf[offset + read + 2] << 40) |
                    (buf[offset + read + 3] << 32) |
                    (buf[offset + read + 4] << 24) |
                    (buf[offset + read + 5] << 16) |
                    (buf[offset + read + 6] <<  8) |
                    (buf[offset + read + 7] <<  0);
                read += 8;
            }
            else
            {
                // a "shortcut" to not have to do the if USE_B or USE_C check every time
                mPayloadLengthExt = mPayloadLength;
            }
            Logger.Trace(" -> mPayloadLengthExt: {0}", mPayloadLengthExt);

            if (mMasked)
            {
                CheckBufferLength(offset + read, 4, buf.LongLength, "MaskingKey");
                mMaskingKey = new byte[4];
                Array.Copy(buf, offset + read, mMaskingKey, 0, 4);
                string keyStr = "";
                foreach (var x in mMaskingKey)
                    keyStr += x.ToString("x") + " ";
                Logger.Trace(" -> mMaskingKey: {0}", keyStr);
                read += 4;
            }

            CheckBufferLength(offset + read, mPayloadLengthExt, buf.LongLength, "Payload");
            mPayload = new byte[mPayloadLengthExt];
            if (mMasked)
            {
                for (long i = 0; i < mPayloadLengthExt; ++i)
                {
                    mPayload[i] = (byte)(buf[offset + i + read] ^ mMaskingKey[i % 4]);
                }
            }
            else
            {
                Array.Copy(buf, offset + read, mPayload, 0, mPayloadLengthExt);
            }

            read += mPayloadLengthExt;

            string outString = "";
            foreach (var c in mPayload)
                outString += c.ToString("x") + " ";

            Logger.Trace(" -> mPayload (unmasked): {0}", outString);

            return read;
        }

        public long FromString(string str)
        {
            return FromBuffer(System.Text.Encoding.UTF8.GetBytes(str), 0);
        }

        public long FromBuffer(byte[] buffer, long offset)
        {
            // TODO rework to load multiple frames
            Final = true;
            Opcode = Op.Text;
            mMasked = true;

            mPayloadLengthExt = buffer.LongLength;
            if (buffer.LongLength > 125)
                mPayloadLength = USE_PAYLOAD_LENGTH_B;
            else if (buffer.LongLength > 0xFFFF)
                mPayloadLength = USE_PAYLOAD_LENGTH_C;
            else
                mPayloadLength = (byte)mPayloadLengthExt;

            mMaskingKey = new byte[4];
            mMaskingKey[0] = 0x12;
            mMaskingKey[1] = 0x34;
            mMaskingKey[2] = 0x56;
            mMaskingKey[3] = 0x78;

            mPayload = new byte[buffer.LongLength];
            Array.Copy(buffer, offset, mPayload, 0, mPayloadLengthExt);

            return mPayloadLengthExt;
        }

        public string PayloadAsString()
        {
            return System.Text.Encoding.UTF8.GetString(mPayload);
        }

        public byte[] ToSendBuffer()
        {
            // Masking is only client-to-server, ignore those parts when sending data
            long bufSize = 2 // FIN/Opcode/Masked/PayloadLen
                + ((mPayloadLength == USE_PAYLOAD_LENGTH_B) ? 2 : 0)
                + ((mPayloadLength == USE_PAYLOAD_LENGTH_C) ? 8 : 0)
                + mPayloadLengthExt;

            byte[] buffer = new byte[bufSize];

            long written = 0;
            buffer[0] = (byte)((Final ? 0x80 : 0) | (int)Opcode);
            buffer[1] = mPayloadLength; // TODO verify why masking no work
            written += 2;

            if (mPayloadLength == USE_PAYLOAD_LENGTH_B)
            {
                buffer[written + 0] = (byte)(mPayloadLengthExt >> 8);
                buffer[written + 1] = (byte)(mPayloadLengthExt >> 0);
                written += 2;
            }
            else if (mPayloadLength == USE_PAYLOAD_LENGTH_C)
            {
                buffer[written + 0] = (byte)(mPayloadLengthExt >> 56);
                buffer[written + 1] = (byte)(mPayloadLengthExt >> 48);
                buffer[written + 2] = (byte)(mPayloadLengthExt >> 40);
                buffer[written + 3] = (byte)(mPayloadLengthExt >> 32);
                buffer[written + 4] = (byte)(mPayloadLengthExt >> 24);
                buffer[written + 5] = (byte)(mPayloadLengthExt >> 16);
                buffer[written + 6] = (byte)(mPayloadLengthExt >>  8);
                buffer[written + 7] = (byte)(mPayloadLengthExt >>  0);
                written += 8;
            }

            Array.Copy(mPayload, 0, buffer, written, mPayloadLengthExt);
            return buffer;
        }
    }

}
