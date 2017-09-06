// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

#if MONOMAC
using MonoMac.OpenGL;
#elif WINDOWS || LINUX
using OpenTK.Graphics.OpenGL;
#elif GLES
using OpenTK.Graphics.ES30;
#endif

namespace Microsoft.Xna.Framework.Graphics
{
    public abstract partial class Texture
    {
        internal int glTexture = -1;
        internal TextureTarget glTarget;
        internal TextureUnit glTextureUnit = TextureUnit.Texture0;
        internal PixelInternalFormat glInternalFormat;
        internal PixelFormat glFormat;
        internal PixelType glType;
        internal SamplerState[] glLastSamplerStates = null;

        private void PlatformGraphicsDeviceResetting()
        {
            DeleteGLTexture();
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                DeleteGLTexture();
            }

            base.Dispose(disposing);
        }

        private void DeleteGLTexture()
        {
            System.Array.Clear(glLastSamplerStates, 0, glLastSamplerStates.Length);

            if (glTexture > 0)
            {
                int texture = glTexture;
                Threading.BlockOnUIThread(() =>
                {
                    GL.DeleteTextures(1, ref texture);
                    GraphicsExtensions.CheckGLError();
                });
            }
            glTexture = -1;
        }
    }
}

