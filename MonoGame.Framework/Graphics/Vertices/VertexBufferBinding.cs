using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Xna.Framework.Graphics
{
    public class VertexBufferBinding
    {
        public VertexBuffer VertexBuffer { get; private set; }
        public int VertexOffset { get; private set; }
        public int InstanceFrequency { get; private set; }

        public VertexBufferBinding(VertexBuffer vertexBuffer, int vertexOffset, int instanceFrequency)
        {
            if (vertexBuffer == null)
            {
                throw new ArgumentNullException("vertexBuffer");
            }
            if ((vertexOffset < 0) || (vertexOffset >= vertexBuffer.VertexCount))
            {
                throw new ArgumentOutOfRangeException("vertexOffset");
            }
            if (instanceFrequency < 0)
            {
                throw new ArgumentOutOfRangeException("instanceFrequency");
            }
            this.VertexBuffer = vertexBuffer;
            this.VertexOffset = vertexOffset;
            this.InstanceFrequency = instanceFrequency;
        }

        public VertexBufferBinding(VertexBuffer vertexBuffer, int vertexOffset) : this(vertexBuffer, 0, 0)
        {
        }

        public VertexBufferBinding(VertexBuffer vertexBuffer) : this(vertexBuffer, 0, 0)
        {
        }

        public static implicit operator VertexBufferBinding(VertexBuffer vertexBuffer)
        {
            return new VertexBufferBinding(vertexBuffer);
        }
    }
}
