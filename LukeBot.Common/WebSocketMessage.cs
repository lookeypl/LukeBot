using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LukeBot.Common
{
    public class WebSocketMessage
    {
        private string mText;
        public string TextMessage
        {
            get
            {
                if (mText.Length == 0 && mFrames.Count > 0 &&
                    mFrames[0].Opcode == WebSocketFrame.Op.Text)
                {
                    mText = "";
                    foreach (WebSocketFrame f in mFrames)
                    {
                        mText += f.PayloadAsString();
                    }
                }

                return mText;
            }
        }

        List<WebSocketFrame> mFrames;

        public WebSocketMessage()
        {
            mText = "";
            mFrames = new List<WebSocketFrame>();
        }

        public void Clear()
        {
            mText = "";
            mFrames.Clear();
        }

        public bool FromString(string str)
        {
            WebSocketFrame frame = new WebSocketFrame();
            long read = frame.FromString(str);
            if (read == 0)
                return false;

            mFrames.Add(frame);
            return true;
        }

        public bool FromBuffer(byte[] buf)
        {
            // TODO supprot multiple frames
            WebSocketFrame frame = new WebSocketFrame();
            long read = frame.FromBuffer(buf, 0);
            if (read == 0)
                return false;

            mFrames.Add(frame);
            return true;
        }

        public long FromReceivedData(byte[] buf, long offset)
        {
            if (buf.Length == 0 || offset == buf.Length)
                return 0;

            long read = 0;
            long totalRead = 0;
            while (true)
            {
                WebSocketFrame frame = new WebSocketFrame();
                read = frame.FillFromBuffer(buf, offset + read);
                mFrames.Add(frame);
                totalRead += read;
                if (frame.Final)
                    break;
            }

            return totalRead;
        }

        public byte[] ToSendBuffer()
        {
            byte[] buf = new byte[0];

            foreach (WebSocketFrame f in mFrames)
            {
                buf = buf.Concat(f.ToSendBuffer()).ToArray<byte>();
            }

            return buf;
        }
    }
}
