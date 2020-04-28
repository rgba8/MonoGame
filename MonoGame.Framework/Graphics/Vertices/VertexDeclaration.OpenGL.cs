// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;

#if MONOMAC
using MonoMac.OpenGL;
#elif WINDOWS || LINUX
using OpenTK.Graphics.OpenGL;
#else
using OpenTK.Graphics.ES30;
#endif

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class VertexDeclaration
    {
        Dictionary<int, VertexDeclarationAttributeInfo> shaderAttributeInfo = new Dictionary<int, VertexDeclarationAttributeInfo>();

        VertexDeclarationAttributeInfo cachedDeclaration = null;
        int cachedShader = -1;
        Int64 cachedOffset = -1;
        VertexBuffer cachedVertexBuffer;
        IndexBuffer cachedIndexBuffer;

		internal void Apply(Shader vertexShader, Shader pixelShader, IntPtr offset)
		{
            VertexDeclarationAttributeInfo attrInfo;
            int shaderHash = vertexShader.HashKey | pixelShader.HashKey;
            if (!shaderAttributeInfo.TryGetValue(shaderHash, out attrInfo))
            {
                // Get the vertex attribute info and cache it
                attrInfo = new VertexDeclarationAttributeInfo(GraphicsDevice.MaxVertexAttributes);

                foreach (var ve in _elements)
                {
                    var attributeLocation = vertexShader.GetAttribLocation(ve.VertexElementUsage, ve.UsageIndex);
                    // XNA appears to ignore usages it can't find a match for, so we will do the same
                    if (attributeLocation >= 0)
                    {
                        attrInfo.Elements.Add(new VertexDeclarationAttributeInfo.Element()
                        {
                            Offset = ve.Offset,
                            AttributeLocation = attributeLocation,
                            NumberOfElements = ve.VertexElementFormat.OpenGLNumberOfElements(),
                            VertexAttribPointerType = ve.VertexElementFormat.OpenGLVertexAttribPointerType(),
                            Normalized = ve.OpenGLVertexAttribNormalized(),
                        });
                        attrInfo.EnabledAttributes[attributeLocation] = true;
                    }
                }

                attrInfo._hashCode = attrInfo.GetHashCode();
                shaderAttributeInfo.Add(shaderHash, attrInfo);
            }


            var offsetI64 = offset.ToInt64();
            //if (cachedOffset != offsetI64 || cachedDeclaration == null || cachedDeclaration._hashCode != attrInfo._hashCode || cachedShader != shaderHash ||
            //    cachedVertexBuffer != this.GraphicsDevice._vertexBuffer || cachedIndexBuffer != this.GraphicsDevice._indexBuffer || this.GraphicsDevice._vertexBuffer == null || this.GraphicsDevice._indexBuffer == null)
            {
                cachedOffset = offsetI64;
                cachedDeclaration = attrInfo;
                cachedShader = shaderHash;
                cachedVertexBuffer = this.GraphicsDevice._vertexBuffer;
                cachedIndexBuffer = this.GraphicsDevice._indexBuffer;
                // Apply the vertex attribute info
                foreach (var element in attrInfo.Elements)
                {
                    GL.VertexAttribPointer(element.AttributeLocation,
                        element.NumberOfElements,
                        element.VertexAttribPointerType,
                        element.Normalized,
                        this.VertexStride,
                        (IntPtr)(offset.ToInt64() + element.Offset));
                    GraphicsExtensions.CheckGLError();
                    GraphicsDevice.SetVertexAttributeArray(attrInfo.EnabledAttributes);
                }
            }
            //else
            //{
            //    int dummyint = 0 ;
            //    dummyint++;
            //}
        }

        public void ClearCache()
        {
            cachedOffset = -1;
            cachedDeclaration = null;
            cachedShader = -1;
            cachedVertexBuffer = null;
            cachedIndexBuffer = null;
        }

        /// <summary>
        /// Vertex attribute information for a particular shader/vertex declaration combination.
        /// </summary>
        class VertexDeclarationAttributeInfo
        {
            internal bool[] EnabledAttributes;
            internal int _hashCode;

            internal class Element
            {
                public int Offset;
                public int AttributeLocation;
                public int NumberOfElements;
                public VertexAttribPointerType VertexAttribPointerType;
                public bool Normalized;
            }

            internal List<Element> Elements;

            internal VertexDeclarationAttributeInfo(int maxVertexAttributes)
            {
                EnabledAttributes = new bool[maxVertexAttributes];
                Elements = new List<Element>();
            }
        }
    }
}
