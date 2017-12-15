// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;

namespace RpcTest_Netty
{
    public class EchoServerHandler : ChannelHandlerAdapter
    {
        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var buffer = message as IByteBuffer;
            if (buffer != null)
            {
                //Console.WriteLine("Received from client: " + buffer.ToString(Encoding.UTF8));
                var initialMessage = Unpooled.Buffer(256);
                byte[] messageBytes = Encoding.UTF8.GetBytes("Hello " + buffer.ToString(Encoding.UTF8));
                initialMessage.WriteBytes(messageBytes);

                context.WriteAsync(initialMessage);
            }
            else
            {
                context.WriteAsync(message);
            }
        }

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Console.WriteLine("Exception: " + exception);
            context.CloseAsync();
        }
    }
}