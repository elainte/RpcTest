// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;

namespace RpcTest
{
    public class EchoClientHandler : ChannelHandlerAdapter
    {
        readonly IByteBuffer initialMessage;
        private int count = 2000;

        public EchoClientHandler()
        {
            this.initialMessage = Unpooled.Buffer(256);
            byte[] messageBytes = Encoding.UTF8.GetBytes("world");
            this.initialMessage.WriteBytes(messageBytes);
        }

        public override void ChannelActive(IChannelHandlerContext context) => context.WriteAndFlushAsync(this.initialMessage);

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            //var byteBuffer = message as IByteBuffer;
            //if (byteBuffer != null)
            //{
            //    Console.WriteLine("Received from server: " + byteBuffer.ToString(Encoding.UTF8));
            //}
            if (count == 0)
                return;
            context.WriteAsync(this.initialMessage);
            count--;
        }

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Console.WriteLine("Exception: " + exception);
            context.CloseAsync();
        }
    }
}