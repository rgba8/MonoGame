// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;

#if MONOMAC
using MonoMac.OpenGL;
using GLPrimitiveType = MonoMac.OpenGL.BeginMode;
#endif

#if WINDOWS || LINUX
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using GLPrimitiveType = OpenTK.Graphics.OpenGL.PrimitiveType;
#endif

#if ANGLE
using OpenTK.Graphics;
#endif

#if GLES
using OpenTK.Graphics.ES30;
using RenderbufferStorage = OpenTK.Graphics.ES30.All;
using GLPrimitiveType = OpenTK.Graphics.ES30.BeginMode;
#endif


namespace Microsoft.Xna.Framework.Graphics
{
    public partial class GraphicsDevice
    {
#if WINDOWS || LINUX || ANGLE
        internal IGraphicsContext Context { get; private set; }
#endif

#if GLES
        private DrawBufferMode[] _drawBuffers;
#else
        private DrawBuffersEnum[] _drawBuffers;
#endif
        static List<Action> disposeActions = new List<Action>();
        static object disposeActionsLock = new object();

        private readonly ShaderProgramCache _programCache = new ShaderProgramCache();

        private ShaderProgram _shaderProgram = null;

        static readonly float[] _posFixup = new float[4];

        private bool[] _enabledVertexAttributes = null;

        internal FramebufferHelper framebufferHelper;

        internal int glFramebuffer = 0;
        internal int MaxVertexAttributes;        
        internal List<string> _extensions = new List<string>();
        internal int _maxTextureSize = 0;

        // Keeps track of last applied state to avoid redundant OpenGL calls
        internal bool _lastBlendEnable = false;
        internal BlendState _lastBlendState = new BlendState();
        internal DepthStencilState _lastDepthStencilState = new DepthStencilState();
        internal RasterizerState _lastRasterizerState = new RasterizerState();
        private Vector4 _lastClearColor = Vector4.Zero;
        private float _lastClearDepth = 1.0f;
        private int _lastClearStencil = 0;

        internal void SetVertexAttributeArray(bool[] attrs)
        {
            int length = attrs.Length;
            for(int x = 0; x < length; x++)
            {
                bool enabled = _enabledVertexAttributes[x];
                if (attrs[x])
                {
                    if (!enabled)
                    {
                        _enabledVertexAttributes[x] = true;
                        GL.EnableVertexAttribArray(x);
                        GraphicsExtensions.CheckGLError();
                    }
                }
                else if (enabled)
                {
                    _enabledVertexAttributes[x] = false;
                    GL.DisableVertexAttribArray(x);
                    GraphicsExtensions.CheckGLError();
                }
            }
        }

        private void PlatformSetup()
        {
#if WINDOWS || LINUX || ANGLE
            GraphicsMode mode = GraphicsMode.Default;
            var wnd = (Game.Instance.Window as OpenTKGameWindow).Window.WindowInfo;

#if GLES
            // Create an OpenGL ES 2.0 context
            var flags = GraphicsContextFlags.Embedded;
            int major = 2;
            int minor = 0;
#else
            // Create an OpenGL compatibility context
            var flags = GraphicsContextFlags.Default;
            int major = 1;
            int minor = 0;
#endif

            if (Context == null || Context.IsDisposed)
            {
                var color = PresentationParameters.BackBufferFormat.GetColorFormat();
                var depth =
                    PresentationParameters.DepthStencilFormat == DepthFormat.None ? 0 :
                    PresentationParameters.DepthStencilFormat == DepthFormat.Depth16 ? 16 :
                    24;
                var stencil =
                    PresentationParameters.DepthStencilFormat == DepthFormat.Depth24Stencil8 ? 8 :
                    0;

                var samples = 0;
                if (Game.Instance.graphicsDeviceManager.PreferMultiSampling)
                {
                    // Use a default of 4x samples if PreferMultiSampling is enabled
                    // without explicitly setting the desired MultiSampleCount.
                    if (PresentationParameters.MultiSampleCount == 0)
                    {
                        PresentationParameters.MultiSampleCount = 4;
                    }

                    samples = PresentationParameters.MultiSampleCount;
                }

                mode = new GraphicsMode(color, depth, stencil, samples);
                try
                {
                    Context = new GraphicsContext(mode, wnd, major, minor, flags);
                }
                catch (Exception e)
                {
                    Game.Instance.Log("Failed to create OpenGL context, retrying. Error: " +
                        e.ToString());
                    major = 1;
                    minor = 0;
                    flags = GraphicsContextFlags.Default;
                    Context = new GraphicsContext(mode, wnd, major, minor, flags);
                }
            }
            Context.MakeCurrent(wnd);
            (Context as IGraphicsContextInternal).LoadAll();
            Context.SwapInterval = PresentationParameters.PresentationInterval.GetSwapInterval();

            // Provide the graphics context for background loading
            // Note: this context should use the same GraphicsMode,
            // major, minor version and flags parameters as the main
            // context. Otherwise, context sharing will very likely fail.
            if (Threading.BackgroundContext == null)
            {
                Threading.BackgroundContext = new GraphicsContext(mode, wnd, major, minor, flags);
                Threading.WindowInfo = wnd;
                Threading.BackgroundContext.MakeCurrent(null);
            }
            Context.MakeCurrent(wnd);
#endif

            MaxTextureSlots = 16;

            GL.GetInteger(GetPName.MaxTextureImageUnits, out MaxTextureSlots);
            GraphicsExtensions.CheckGLError();

            GL.GetInteger(GetPName.MaxVertexAttribs, out MaxVertexAttributes);
            GraphicsExtensions.CheckGLError();

            GL.GetInteger(GetPName.MaxTextureSize, out _maxTextureSize);
            GraphicsExtensions.CheckGLError();

            // Initialize draw buffer attachment array
            int maxDrawBuffers;
            GL.GetInteger(GetPName.MaxDrawBuffers, out maxDrawBuffers);
#if GLES
            _drawBuffers = new DrawBufferMode[maxDrawBuffers];
#else
            _drawBuffers = new DrawBuffersEnum[maxDrawBuffers];
#endif
            for (int i = 0; i < maxDrawBuffers; i++)
            {
                _drawBuffers[i] =
#if GLES
                    (DrawBufferMode)
#else
                    (DrawBuffersEnum)
#endif
                    (FramebufferAttachment.ColorAttachment0 + i);
            }

            _extensions = GetGLExtensions();

            _enabledVertexAttributes = new bool[MaxVertexAttributes];
        }

        List<string> GetGLExtensions()
        {
            // Setup extensions.
            List<string> extensions = new List<string>();
            var extstring = GL.GetString(StringName.Extensions);
            GraphicsExtensions.CheckGLError();
            if (!string.IsNullOrEmpty(extstring))
            {
                extensions.AddRange(extstring.Split(' '));
#if ANDROID
                Android.Util.Log.Debug("MonoGame", "Supported extensions:");
#else
                System.Diagnostics.Debug.WriteLine("Supported extensions:");
#endif
                foreach (string extension in extensions)
#if ANDROID
                    Android.Util.Log.Debug("MonoGame", extension);
#else
                    System.Diagnostics.Debug.WriteLine(extension);
#endif
            }

            return extensions;
        }

        private void PlatformInitialize()
        {
            _viewport = new Viewport(0, 0, PresentationParameters.BackBufferWidth, PresentationParameters.BackBufferHeight);

            // Ensure the vertex attributes are reset
            for (int i = 0; i < _enabledVertexAttributes.Length; ++i)
            { _enabledVertexAttributes[i] = false; }

            // Free all the cached shader programs. 
            _programCache.Clear();
            _shaderProgram = null;

            if (GraphicsCapabilities.SupportsFramebufferObjectARB)
            {
                this.framebufferHelper = new FramebufferHelper(this);
            }
#if !(GLES || MONOMAC)
            else if (GraphicsCapabilities.SupportsFramebufferObjectEXT)
            {
                this.framebufferHelper = new FramebufferHelperEXT(this);
            }
#endif
            else
            {
                throw new PlatformNotSupportedException(
                    "MonoGame requires either ARB_framebuffer_object or EXT_framebuffer_object." +
                    "Try updating your graphics drivers.");
            }

            // Force reseting states
            this.BlendState.PlatformApplyState(this, true);
            this.DepthStencilState.PlatformApplyState(this, true);
            this.RasterizerState.PlatformApplyState(this, true);            
        }
        
        private DepthStencilState clearDepthStencilState = new DepthStencilState { StencilEnable = true };

        public void ClearBuffer(int index, float[] color)
        {
            GL.ClearBuffer(
#if GLES
                OpenTK.Graphics.ES30.ClearBuffer.Color
#else
                OpenTK.Graphics.OpenGL.ClearBuffer.Color
#endif
                , index, color);
            GraphicsExtensions.CheckGLError();
        }

        public void OitBlendState()
        {
            //GL.Enable(IndexedEnableCap.Blend, 1);
            //GraphicsExtensions.CheckGLError();

            //GL.BlendEquation(1, BlendEquationMode.FuncAdd);
            //GraphicsExtensions.CheckGLError();

            //GL.BlendFunc(1, BlendingFactorSrc.Zero, BlendingFactorDest.OneMinusSrcColor);
            //GraphicsExtensions.CheckGLError();
        }

        public void PlatformClear(ClearOptions options, Vector4 color, float depth, int stencil)
        {
            // TODO: We need to figure out how to detect if we have a
            // depth stencil buffer or not, and clear options relating
            // to them if not attached.

            // Unlike with XNA and DirectX...  GL.Clear() obeys several
            // different render states:
            //
            //  - The color write flags.
            //  - The scissor rectangle.
            //  - The depth/stencil state.
            //
            // So overwrite these states with what is needed to perform
            // the clear correctly and restore it afterwards.
            //
		    var prevScissorRect = ScissorRectangle;
		    var prevDepthStencilState = DepthStencilState;
            var prevBlendState = BlendState;
            ScissorRectangle = _viewport.Bounds;
            // DepthStencilState.Default has the Stencil Test disabled; 
            // make sure stencil test is enabled before we clear since
            // some drivers won't clear with stencil test disabled
            DepthStencilState = this.clearDepthStencilState;
		    BlendState = BlendState.Opaque;
            PlatformApplyState(false);

            ClearBufferMask bufferMask = 0;
            if ((options & ClearOptions.Target) == ClearOptions.Target)
            {
                if (color != _lastClearColor)
                {
                    GL.ClearColor(color.X, color.Y, color.Z, color.W);
                    GraphicsExtensions.CheckGLError();
                    _lastClearColor = color;
                }
                bufferMask = bufferMask | ClearBufferMask.ColorBufferBit;
            }
			if ((options & ClearOptions.Stencil) == ClearOptions.Stencil)
            {
                if (stencil != _lastClearStencil)
                {
				    GL.ClearStencil(stencil);
                    GraphicsExtensions.CheckGLError();
                    _lastClearStencil = stencil;
                }
                bufferMask = bufferMask | ClearBufferMask.StencilBufferBit;
			}

			if ((options & ClearOptions.DepthBuffer) == ClearOptions.DepthBuffer) 
            {
                if (depth != _lastClearDepth)
                {
#if GLES
                    GL.ClearDepth (depth);
#else
                    GL.ClearDepth((double)depth);
#endif
                    GraphicsExtensions.CheckGLError();
                    _lastClearDepth = depth;
                }
				bufferMask = bufferMask | ClearBufferMask.DepthBufferBit;
			}


			GL.Clear(bufferMask);
            GraphicsExtensions.CheckGLError();
           		
            // Restore the previous render state.
		    ScissorRectangle = prevScissorRect;
		    DepthStencilState = prevDepthStencilState;
		    BlendState = prevBlendState;
        }

        private void PlatformDispose()
        {
            // Free all the cached shader programs.
            _programCache.Dispose();

            GraphicsDevice.AddDisposeAction(() =>
                                            {
#if WINDOWS || LINUX || ANGLE
                Context.Dispose();
                Context = null;

                if (Threading.BackgroundContext != null)
                {
                    Threading.BackgroundContext.Dispose();
                    Threading.BackgroundContext = null;
                    Threading.WindowInfo = null;
                }
#endif
            });
        }

        /// <summary>
        /// Adds a dispose action to the list of pending dispose actions. These are executed at the end of each call to Present().
        /// This allows GL resources to be disposed from other threads, such as the finalizer.
        /// </summary>
        /// <param name="disposeAction">The action to execute for the dispose.</param>
        static private void AddDisposeAction(Action disposeAction)
        {
            if (disposeAction == null)
                throw new ArgumentNullException("disposeAction");
            if (Threading.IsOnUIThread())
            {
                disposeAction();
            }
            else
            {
                lock (disposeActionsLock)
                {
                    disposeActions.Add(disposeAction);
                }
            }
        }

        public void PlatformPresent()
        {
#if WINDOWS || LINUX || ANGLE
            Context.SwapBuffers();
#endif
            GraphicsExtensions.CheckGLError();

            // Dispose of any GL resources that were disposed in another thread
            lock (disposeActionsLock)
            {
                if (disposeActions.Count > 0)
                {
                    foreach (var action in disposeActions)
                        action();
                    disposeActions.Clear();
                }
            }
        }

        private void PlatformSetViewport(ref Viewport value)
        {
            if (IsRenderTargetBound)
                GL.Viewport(value.X, value.Y, value.Width, value.Height);
            else
                GL.Viewport(value.X, PresentationParameters.BackBufferHeight - value.Y - value.Height, value.Width, value.Height);
            GraphicsExtensions.LogGLError("GraphicsDevice.Viewport_set() GL.Viewport");
#if GLES
            GL.DepthRange(value.MinDepth, value.MaxDepth);
#else
            GL.DepthRange((double)value.MinDepth, (double)value.MaxDepth);
#endif
            GraphicsExtensions.LogGLError("GraphicsDevice.Viewport_set() GL.DepthRange");
                
            // In OpenGL we have to re-apply the special "posFixup"
            // vertex shader uniform if the viewport changes.
            _vertexShaderDirty = true;

        }

        private void PlatformApplyDefaultRenderTarget()
        {
            this.framebufferHelper.BindFramebuffer(this.glFramebuffer);

            // Reset the raster state because we flip vertices
            // when rendering offscreen and hence the cull direction.
            _rasterizerStateDirty = true;

            // Textures will need to be rebound to render correctly in the new render target.
            Textures.Dirty();
        }

        private class RenderTargetBindingArrayComparer : IEqualityComparer<RenderTargetBinding[]>
        {
            public bool Equals(RenderTargetBinding[] first, RenderTargetBinding[] second)
            {
                if (object.ReferenceEquals(first, second))
                    return true;

                if (first == null || second == null)
                    return false;

                if (first.Length != second.Length)
                    return false;

                for (var i = 0; i < first.Length; ++i)
                {
                    if ((first[i].RenderTarget != second[i].RenderTarget) || (first[i].ArraySlice != second[i].ArraySlice))
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode(RenderTargetBinding[] array)
            {
                if (array != null)
                {
                    unchecked
                    {
                        int hash = 17;
                        foreach (var item in array)
                        {
                            if (item.RenderTarget != null)
                                hash = hash * 23 + item.RenderTarget.GetHashCode();
                            hash = hash * 23 + item.ArraySlice.GetHashCode();
                        }
                        return hash;
                    }
                }
                return 0;
            }
        }

        // FBO cache, we create 1 FBO per RenderTargetBinding combination
        private Dictionary<RenderTargetBinding[], int> glFramebuffers = new Dictionary<RenderTargetBinding[], int>(new RenderTargetBindingArrayComparer());
        // FBO cache used to resolve MSAA rendertargets, we create 1 FBO per RenderTargetBinding combination
        private Dictionary<RenderTargetBinding[], int> glResolveFramebuffers = new Dictionary<RenderTargetBinding[], int>(new RenderTargetBindingArrayComparer());

        internal void PlatformCreateRenderTarget(Texture renderTarget, int width, int height, bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat, int preferredMultiSampleCount, RenderTargetUsage usage)
        {
            var color = 0;
            var depth = 0;
            var stencil = 0;

            if (preferredMultiSampleCount > 0 && this.framebufferHelper.SupportsBlitFramebuffer && preferredFormat != SurfaceFormat.NONE)
            {
                this.framebufferHelper.GenRenderbuffer(out color);
                this.framebufferHelper.BindRenderbuffer(color);
                this.framebufferHelper.RenderbufferStorageMultisample(preferredMultiSampleCount, (int)RenderbufferStorage.Rgba8, width, height);
            }

            var renderTarget2D = renderTarget as RenderTarget2D;
            if (renderTarget2D == null)
            { throw new InvalidOperationException(); }

            renderTarget2D.depthStencilAttachment = 0;
            if (preferredDepthFormat != DepthFormat.None)
            {
                RenderbufferStorage depthInternalFormat = 0;
                switch (preferredDepthFormat)
                {
                    case DepthFormat.Depth16:
                        {
                            depthInternalFormat = RenderbufferStorage.DepthComponent16;
                            renderTarget2D.depthStencilAttachment = FramebufferAttachment.DepthAttachment;
                            break;
                        }
                    case DepthFormat.Depth24:
                        {
                            depthInternalFormat = RenderbufferStorage.DepthComponent24;
                            renderTarget2D.depthStencilAttachment = FramebufferAttachment.DepthAttachment;
                            break;
                        }
                    case DepthFormat.Depth24Stencil8:
                        {
                            depthInternalFormat = RenderbufferStorage.Depth24Stencil8;
                            renderTarget2D.depthStencilAttachment = FramebufferAttachment.DepthStencilAttachment;
                            break;
                        }
                    default: throw new NotSupportedException();
                }

                if (preferredMultiSampleCount > 0)
                {
                    this.framebufferHelper.GenRenderbuffer(out depth);
                    this.framebufferHelper.BindRenderbuffer(depth);
                    this.framebufferHelper.RenderbufferStorageMultisample(preferredMultiSampleCount, (int)depthInternalFormat, width, height);
                }
                //else
                {
                    if (depthInternalFormat == RenderbufferStorage.DepthComponent24)
                        renderTarget2D.DepthTexture = new Texture2D(this, width, height, false, SurfaceFormat.HalfSingle, Texture2D.SurfaceType.Texture);
                    else if (depthInternalFormat == RenderbufferStorage.Depth24Stencil8)
                        renderTarget2D.DepthTexture = new Texture2D(this, width, height, false, SurfaceFormat.U248, Texture2D.SurfaceType.Texture);
                    GL.BindTexture(TextureTarget.Texture2D, renderTarget2D.DepthTexture.glTexture);
                    GraphicsExtensions.CheckGLError();

                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                    GraphicsExtensions.CheckGLError();

                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                    GraphicsExtensions.CheckGLError();

                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    GraphicsExtensions.CheckGLError();

                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                    GraphicsExtensions.CheckGLError();
                }
            }

            if (color != 0)
                renderTarget2D.glColorBuffer = color;
            else
                renderTarget2D.glColorBuffer = renderTarget2D.glTexture;
            renderTarget2D.glDepthBuffer = depth;
            renderTarget2D.glStencilBuffer = depth;
        }

        internal void PlatformDeleteRenderTarget(Texture renderTarget)
        {
            var color = 0;
            var depth = 0;
            var stencil = 0;
            var colorIsRenderbuffer = false;

            var renderTarget2D = renderTarget as RenderTarget2D;
            if (renderTarget2D != null)
            {
                color = renderTarget2D.glColorBuffer;
                depth = renderTarget2D.glDepthBuffer;
                stencil = renderTarget2D.glStencilBuffer;
                colorIsRenderbuffer = color != renderTarget2D.glTexture;
            }

            if (color != 0)
            {
                if (colorIsRenderbuffer)
                    this.framebufferHelper.DeleteRenderbuffer(color);
                if (stencil != 0 && stencil != depth)
                    this.framebufferHelper.DeleteRenderbuffer(stencil);
                if (depth != 0)
                    this.framebufferHelper.DeleteRenderbuffer(depth);

                var bindingsToDelete = new List<RenderTargetBinding[]>();
                foreach (var bindings in this.glFramebuffers.Keys)
                {
                    foreach (var binding in bindings)
                    {
                        if (binding.RenderTarget == renderTarget)
                        {
                            bindingsToDelete.Add(bindings);
                            break;
                        }
                    }
                }

                foreach (var bindings in bindingsToDelete)
                {
                    var fbo = 0;
                    if (this.glFramebuffers.TryGetValue(bindings, out fbo))
                    {
                        this.framebufferHelper.DeleteFramebuffer(fbo);
                        this.glFramebuffers.Remove(bindings);
                    }
                    if (this.glResolveFramebuffers.TryGetValue(bindings, out fbo))
                    {
                        this.framebufferHelper.DeleteFramebuffer(fbo);
                        this.glResolveFramebuffers.Remove(bindings);
                    }
                }
            }
        }

        public void InvalidateFramebuffer(RenderTargetBinding[] renderTargetBindings)
        {
            var renderTargetBinding = renderTargetBindings[0];
            var renderTarget = renderTargetBinding.RenderTarget as RenderTarget2D;

            if (renderTarget.MultiSampleCount > 0 && this.framebufferHelper.SupportsBlitFramebuffer)
            {
                var glResolveFramebuffer = 0;
                if (this.glResolveFramebuffers.TryGetValue(renderTargetBindings, out glResolveFramebuffer))
                {
                    if (this.framebufferHelper.SupportsInvalidateFramebuffer)
                    {
                        this.framebufferHelper.BindFramebuffer(glResolveFramebuffer);
                        this.framebufferHelper.InvalidateReadFramebuffer();
                        this.framebufferHelper.InvalidateDrawFramebuffer();
                    }
                    //Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Black, 1, 0);
                }
            }

            {
                if (this.framebufferHelper.SupportsInvalidateFramebuffer)
                {
                    var framebuffer = 0;
                    if (this.glFramebuffers.TryGetValue(renderTargetBindings, out framebuffer))
                    {
                        this.framebufferHelper.BindFramebuffer(framebuffer);
                        this.framebufferHelper.InvalidateReadFramebuffer();
                        this.framebufferHelper.InvalidateDrawFramebuffer();
                    }
                }
                //Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Black, 1, 0);
            }

            this.framebufferHelper.BindFramebuffer(this.glFramebuffer);
        }

        private void PlatformResolveRenderTargets(bool resolve, bool depthOnly)
        {
            if (this._currentRenderTargetCount == 0)
                return;

            var renderTargetBinding = this._currentRenderTargetBindings[0];
            var renderTarget = renderTargetBinding.RenderTarget as RenderTarget2D;

            var glFramebuffer = this.glFramebuffers[this._currentRenderTargetBindings];

            var glDepthResolveFramebuffer = glFramebuffer;
            if (renderTarget.MultiSampleCount > 0 && this.framebufferHelper.SupportsBlitFramebuffer && resolve)
            {
                var glResolveFramebuffer = 0;
                if (!this.glResolveFramebuffers.TryGetValue(this._currentRenderTargetBindings, out glResolveFramebuffer))
                {
                    this.framebufferHelper.GenFramebuffer(out glResolveFramebuffer);
                    this.glResolveFramebuffers.Add((RenderTargetBinding[])this._currentRenderTargetBindings.Clone(), glResolveFramebuffer);
                //}
                this.framebufferHelper.BindFramebuffer(glResolveFramebuffer);
                    for (var i = 0; i < this._currentRenderTargetCount; ++i)
                    {
                        this.framebufferHelper.FramebufferTexture2D((int)(FramebufferAttachment.ColorAttachment0 + i), (int)renderTarget.glTarget, renderTarget.glTexture);
                    }
                    if (renderTarget.DepthTexture != null)
                        this.framebufferHelper.FramebufferTexture2D((int)renderTarget.depthStencilAttachment, (int)renderTarget.glTarget, renderTarget.DepthTexture.glTexture);
                }
                else
                {
                    this.framebufferHelper.BindFramebuffer(glResolveFramebuffer);
                }
                glDepthResolveFramebuffer = glResolveFramebuffer;

                // The only fragment operations which affect the resolve are the pixel ownership test, the scissor test, and dithering.
                if (this._lastRasterizerState.ScissorTestEnable)
                {
                    GL.Disable(EnableCap.ScissorTest);
                    GraphicsExtensions.CheckGLError();
                }
                this.framebufferHelper.BindReadFramebuffer(glFramebuffer);
                for (var i = 0; i < this._currentRenderTargetCount; ++i)
                {
                    renderTargetBinding = this._currentRenderTargetBindings[i];
                    renderTarget = renderTargetBinding.RenderTarget as RenderTarget2D;
                    var clearBufferMask = depthOnly ? (ClearBufferMask)0 : ClearBufferMask.ColorBufferBit;
                    clearBufferMask |= (renderTarget.DepthStencilFormat != DepthFormat.None ? ClearBufferMask.DepthBufferBit : (ClearBufferMask)0);
                    clearBufferMask |= (renderTarget.DepthStencilFormat == DepthFormat.Depth24Stencil8 ? ClearBufferMask.StencilBufferBit : (ClearBufferMask)0);
                    this.framebufferHelper.BlitFramebuffer(i, renderTarget.Width, renderTarget.Height, clearBufferMask);
                }
                //if (renderTarget.RenderTargetUsage == RenderTargetUsage.DiscardContents && this.framebufferHelper.SupportsInvalidateFramebuffer)
                //    this.framebufferHelper.InvalidateReadFramebuffer();
                if (this._lastRasterizerState.ScissorTestEnable)
                {
                    GL.Enable(EnableCap.ScissorTest);
                    GraphicsExtensions.CheckGLError();
                }
            }

            //for (var i = 0; i < this._currentRenderTargetCount; ++i)
            //{
            //    renderTargetBinding = this._currentRenderTargetBindings[i];
            //    renderTarget = renderTargetBinding.RenderTarget as RenderTarget2D;
            //    if (renderTarget.LevelCount > 1)
            //    {
            //        GL.BindTexture((TextureTarget)renderTarget.glTarget, renderTarget.glTexture);
            //        GraphicsExtensions.CheckGLError();
            //        this.framebufferHelper.GenerateMipmap((int)renderTarget.glTarget);
            //    }
            //}
        }

        public void BlitColor(RenderTargetBinding[] from, RenderTargetBinding[] to)
        {
        }

        public void BlitDepth(RenderTargetBinding[] from, RenderTargetBinding[] to)
        {
            var from2D = from[0].RenderTarget as RenderTarget2D;
            var to2D = to[0].RenderTarget as RenderTarget2D;

            var glFrom = this.glFramebuffers[from];

            if (from2D.MultiSampleCount > 0)
            {
#if GLES
                GL.FramebufferRenderbuffer(FramebufferTarget.ReadFramebuffer, FramebufferSlot.StencilAttachment, RenderbufferTarget.Renderbuffer, from2D.glDepthBuffer);
                GraphicsExtensions.CheckGLError();
                GL.FramebufferRenderbuffer(FramebufferTarget.ReadFramebuffer, FramebufferSlot.DepthAttachment, RenderbufferTarget.Renderbuffer, from2D.glStencilBuffer);
                GraphicsExtensions.CheckGLError();
                GL.FramebufferRenderbuffer(FramebufferTarget.ReadFramebuffer, FramebufferSlot.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, from2D.glDepthBuffer);
                GraphicsExtensions.CheckGLError();
#else
                GL.FramebufferRenderbuffer(FramebufferTarget.ReadFramebuffer, FramebufferAttachment.StencilAttachment, RenderbufferTarget.Renderbuffer, from2D.glDepthBuffer);
                GraphicsExtensions.CheckGLError();
                GL.FramebufferRenderbuffer(FramebufferTarget.ReadFramebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, from2D.glStencilBuffer);
                GraphicsExtensions.CheckGLError();
                GL.FramebufferRenderbuffer(FramebufferTarget.ReadFramebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, from2D.glDepthBuffer);
                GraphicsExtensions.CheckGLError();
#endif
            }
            else
            {
#if GLES
                GL.FramebufferTexture2D(FramebufferTarget.ReadFramebuffer, FramebufferSlot.StencilAttachment, TextureTarget.Texture2D, from2D.DepthTexture.glTexture, 0);
                GraphicsExtensions.CheckGLError();
                GL.FramebufferTexture2D(FramebufferTarget.ReadFramebuffer, FramebufferSlot.DepthAttachment, TextureTarget.Texture2D, from2D.DepthTexture.glTexture, 0);
                GraphicsExtensions.CheckGLError();
                GL.FramebufferTexture2D(FramebufferTarget.ReadFramebuffer, FramebufferSlot.DepthStencilAttachment, TextureTarget.Texture2D, from2D.DepthTexture.glTexture, 0);
                GraphicsExtensions.CheckGLError();
#else
                GL.FramebufferTexture2D(FramebufferTarget.ReadFramebuffer, FramebufferAttachment.StencilAttachment, TextureTarget.Texture2D, from2D.DepthTexture.glTexture, 0);
                GraphicsExtensions.CheckGLError();
                GL.FramebufferTexture2D(FramebufferTarget.ReadFramebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, from2D.DepthTexture.glTexture, 0);
                GraphicsExtensions.CheckGLError();
                GL.FramebufferTexture2D(FramebufferTarget.ReadFramebuffer, FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2D, from2D.DepthTexture.glTexture, 0);
                GraphicsExtensions.CheckGLError();
#endif
            }


            var glTo = 0;
            if (!this.glFramebuffers.TryGetValue(to, out glTo))
            {
                this.framebufferHelper.GenFramebuffer(out glTo);
                this.glFramebuffers.Add((RenderTargetBinding[])to.Clone(), glTo);

                this.framebufferHelper.BindDrawFramebuffer(glTo);
                GraphicsExtensions.CheckGLError();


                GL.FramebufferTexture2D(FramebufferTarget.DrawFramebuffer,
#if GLES
                    (FramebufferSlot)
#endif
                    to2D.depthStencilAttachment, TextureTarget.Texture2D, to2D.DepthTexture.glTexture, 0);
                GraphicsExtensions.CheckGLError();

#if DEBUG
                this.framebufferHelper.CheckFramebufferStatus();
#endif

            }
            else
            {
                this.framebufferHelper.BindDrawFramebuffer(glTo);
                GL.FramebufferTexture2D(FramebufferTarget.DrawFramebuffer,
#if GLES
                    (FramebufferSlot)
#endif
                    to2D.depthStencilAttachment, TextureTarget.Texture2D, to2D.DepthTexture.glTexture, 0);
                GraphicsExtensions.CheckGLError();

#if DEBUG
                this.framebufferHelper.CheckFramebufferStatus();
#endif
            }

#if !GLES
            GL.DrawBuffers(1, this._drawBuffers);
            GraphicsExtensions.CheckGLError();
#endif

            //Clear(ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Black, 1, 0);
            GraphicsExtensions.CheckGLError();


            //else
            //{
            //this.framebufferHelper.BindReadFramebuffer(glFrom);
            //GraphicsExtensions.CheckGLError();

            //}


            if (this._lastRasterizerState.ScissorTestEnable)
            {
                GL.Disable(EnableCap.ScissorTest);
                GraphicsExtensions.CheckGLError();
            }

            ClearBufferMask clearBufferMask = ClearBufferMask.DepthBufferBit/* | ClearBufferMask.StencilBufferBit*/;
            GL.BlitFramebuffer(0, 0, from2D.width, from2D.height, 0, 0, from2D.width, from2D.height, clearBufferMask, BlitFramebufferFilter.Nearest);
            GraphicsExtensions.CheckGLError();

            this.framebufferHelper.BindFramebuffer(glFrom);
            GraphicsExtensions.CheckGLError();


#if GLES
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferSlot.StencilAttachment, RenderbufferTarget.Renderbuffer, 0);
            GraphicsExtensions.CheckGLError();
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferSlot.DepthAttachment, RenderbufferTarget.Renderbuffer, 0);
            GraphicsExtensions.CheckGLError();
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferSlot.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, 0);
            GraphicsExtensions.CheckGLError();
#else
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.StencilAttachment, RenderbufferTarget.Renderbuffer, 0);
                GraphicsExtensions.CheckGLError();
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, 0);
                GraphicsExtensions.CheckGLError();
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, 0);
                GraphicsExtensions.CheckGLError();
#endif


            GL.DrawBuffers(this._currentRenderTargetCount, this._drawBuffers);
            GraphicsExtensions.CheckGLError();

            //if (renderTarget.RenderTargetUsage == RenderTargetUsage.DiscardContents && this.framebufferHelper.SupportsInvalidateFramebuffer)
            //    this.framebufferHelper.InvalidateReadFramebuffer();
            if (this._lastRasterizerState.ScissorTestEnable)
            {
                GL.Enable(EnableCap.ScissorTest);
                GraphicsExtensions.CheckGLError();
            }
        }

        public void BindDepth(RenderTargetBinding[] target, bool msaa)
        {
            var renderTargetBinding = target[0];
            var renderTarget = renderTargetBinding.RenderTarget as RenderTarget2D;

            int glDepthTexture = renderTarget.DepthTexture != null ? renderTarget.DepthTexture.glTexture : 0;

            if (glDepthTexture != 0)
            {
                //GL.BindTexture(TextureTarget.Texture2D, glDepthTexture);
                //GraphicsExtensions.CheckGLError();

                //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                //GraphicsExtensions.CheckGLError();

                //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                //GraphicsExtensions.CheckGLError();

                //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                //GraphicsExtensions.CheckGLError();

                //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                //GraphicsExtensions.CheckGLError();
            }

            if (msaa)
            {
                this.framebufferHelper.FramebufferRenderbuffer((int)FramebufferAttachment.DepthAttachment, renderTarget.glDepthBuffer, 0);
                this.framebufferHelper.FramebufferRenderbuffer((int)FramebufferAttachment.StencilAttachment, renderTarget.glStencilBuffer, 0);
            }
            else
            {
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
#if GLES
                        (FramebufferSlot)
#endif
                        FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, glDepthTexture, 0);
                GraphicsExtensions.CheckGLError();
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
#if GLES
                        (FramebufferSlot)
#endif
                        FramebufferAttachment.StencilAttachment, TextureTarget.Texture2D, glDepthTexture, 0);
                GraphicsExtensions.CheckGLError();
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
#if GLES
                        (FramebufferSlot)
#endif
                        FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2D, glDepthTexture, 0);
                GraphicsExtensions.CheckGLError();
            }
        }

        public void UnbindTexture(int slot)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + slot);
            GraphicsExtensions.CheckGLError();

            GL.BindTexture(TextureTarget.Texture2D, 0);
            GraphicsExtensions.CheckGLError();
        }

        public void UnbindDepth(RenderTargetBinding[] target)
        {
            this.framebufferHelper.FramebufferRenderbuffer((int)FramebufferAttachment.DepthAttachment, 0, 0);
            this.framebufferHelper.FramebufferRenderbuffer((int)FramebufferAttachment.StencilAttachment, 0, 0);



            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
#if GLES
                        (FramebufferSlot)
#endif
                        FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, 0, 0);
            GraphicsExtensions.CheckGLError();
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
#if GLES
                        (FramebufferSlot)
#endif
                        FramebufferAttachment.StencilAttachment, TextureTarget.Texture2D, 0, 0);
            GraphicsExtensions.CheckGLError();
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
#if GLES
                        (FramebufferSlot)
#endif
                        FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2D, 0, 0);
            GraphicsExtensions.CheckGLError();
        }

        private IRenderTarget PlatformApplyRenderTargets()
        {
            var glFramebuffer = 0;
            if (!this.glFramebuffers.TryGetValue(this._currentRenderTargetBindings, out glFramebuffer))
            {
                this.framebufferHelper.GenFramebuffer(out glFramebuffer);
                this.glFramebuffers.Add((RenderTargetBinding[])_currentRenderTargetBindings.Clone(), glFramebuffer);
            }

            {
                this.framebufferHelper.BindFramebuffer(glFramebuffer);
                var renderTargetBinding = this._currentRenderTargetBindings[0];
                var renderTarget = renderTargetBinding.RenderTarget as RenderTarget2D;
                if (renderTarget.MultiSampleCount > 0)
                {
                    this.framebufferHelper.FramebufferRenderbuffer((int)FramebufferAttachment.DepthAttachment, renderTarget.glDepthBuffer, 0);
                    this.framebufferHelper.FramebufferRenderbuffer((int)FramebufferAttachment.StencilAttachment, renderTarget.glStencilBuffer, 0);
                }
                else
                {
                    int glDepthTexture = renderTarget.DepthTexture != null ? renderTarget.DepthTexture.glTexture : 0;
                        if (glDepthTexture == 0)
                    {
                        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
#if GLES
                        (FramebufferSlot)
#endif
                        FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, 0, 0);
                        GraphicsExtensions.CheckGLError();
                        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
#if GLES
                        (FramebufferSlot)
#endif
                        FramebufferAttachment.StencilAttachment, TextureTarget.Texture2D, 0, 0);
                        GraphicsExtensions.CheckGLError();
                        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
#if GLES
                        (FramebufferSlot)
#endif
                        FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2D, 0, 0);
                        GraphicsExtensions.CheckGLError();

                    }
                    else
                    {
                        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
#if GLES
                        (FramebufferSlot)
#endif
                        renderTarget.depthStencilAttachment, TextureTarget.Texture2D, glDepthTexture, 0);
                        GraphicsExtensions.CheckGLError();
                    }
                }

                for (var i = 0; i < this._currentRenderTargetCount; ++i)
                {
                    renderTargetBinding = this._currentRenderTargetBindings[i];
                    renderTarget = renderTargetBinding.RenderTarget as RenderTarget2D;
                    var attachement = (int)(FramebufferAttachment.ColorAttachment0 + i);
                    if (renderTarget.glColorBuffer != renderTarget.glTexture)
                        this.framebufferHelper.FramebufferRenderbuffer(attachement, renderTarget.glColorBuffer, 0);
                    else
                        this.framebufferHelper.FramebufferTexture2D(attachement, (int)renderTarget.glTarget, renderTarget.glTexture, 0, renderTarget.MultiSampleCount);
                }

#if DEBUG
                this.framebufferHelper.CheckFramebufferStatus();
#endif
            }
            //else
            //{
            //    this.framebufferHelper.BindFramebuffer(glFramebuffer);
            //}

            GL.DrawBuffers(this._currentRenderTargetCount, this._drawBuffers);

            // Reset the raster state because we flip vertices
            // when rendering offscreen and hence the cull direction.
            _rasterizerStateDirty = true;

            // Textures will need to be rebound to render correctly in the new render target.
            Textures.Dirty();

            return _currentRenderTargetBindings[0].RenderTarget as IRenderTarget;
        }

        private static GLPrimitiveType PrimitiveTypeGL(PrimitiveType primitiveType)
        {
            switch (primitiveType)
            {
                case PrimitiveType.PointList:
                    return GLPrimitiveType.Points;
                case PrimitiveType.LineList:
                    return GLPrimitiveType.Lines;
                case PrimitiveType.LineStrip:
                    return GLPrimitiveType.LineStrip;
                case PrimitiveType.TriangleList:
                    return GLPrimitiveType.Triangles;
                case PrimitiveType.TriangleStrip:
                    return GLPrimitiveType.TriangleStrip;
            }

            throw new ArgumentException();
        }


        /// <summary>
        /// Activates the Current Vertex/Pixel shader pair into a program.         
        /// </summary>
        private void ActivateShaderProgram()
        {
            // Lookup the shader program.
            var shaderProgram = _programCache.GetProgram(VertexShader, PixelShader);
            if (shaderProgram.Program == -1)
                return;
            // Set the new program if it has changed.
            if (_shaderProgram != shaderProgram)
            {
                GL.UseProgram(shaderProgram.Program);
                GraphicsExtensions.CheckGLError();
                _shaderProgram = shaderProgram;
            }

            var posFixupLoc = shaderProgram.GetUniformLocation("posFixup");
            if (posFixupLoc == null)
                return;

            // Apply vertex shader fix:
            // The following two lines are appended to the end of vertex shaders
            // to account for rendering differences between OpenGL and DirectX:
            //
            // gl_Position.y = gl_Position.y * posFixup.y;
            // gl_Position.xy += posFixup.zw * gl_Position.ww;
            //
            // (the following paraphrased from wine, wined3d/state.c and wined3d/glsl_shader.c)
            //
            // - We need to flip along the y-axis in case of offscreen rendering.
            // - D3D coordinates refer to pixel centers while GL coordinates refer
            //   to pixel corners.
            // - D3D has a top-left filling convention. We need to maintain this
            //   even after the y-flip mentioned above.
            // In order to handle the last two points, we translate by
            // (63.0 / 128.0) / VPw and (63.0 / 128.0) / VPh. This is equivalent to
            // translating slightly less than half a pixel. We want the difference to
            // be large enough that it doesn't get lost due to rounding inside the
            // driver, but small enough to prevent it from interfering with any
            // anti-aliasing.
            //
            // OpenGL coordinates specify the center of the pixel while d3d coords specify
            // the corner. The offsets are stored in z and w in posFixup. posFixup.y contains
            // 1.0 or -1.0 to turn the rendering upside down for offscreen rendering. PosFixup.x
            // contains 1.0 to allow a mad.

            _posFixup[0] = 1.0f;
            _posFixup[1] = 1.0f;
            _posFixup[2] = (63.0f/64.0f)/Viewport.Width;
            _posFixup[3] = -(63.0f/64.0f)/Viewport.Height;

            //If we have a render target bound (rendering offscreen)
            if (IsRenderTargetBound)
            {
                //flip vertically
                _posFixup[1] *= -1.0f;
                _posFixup[3] *= -1.0f;
            }

            GL.Uniform4(posFixupLoc.location, 1, _posFixup);
            GraphicsExtensions.CheckGLError();
        }

        public event EventHandler StateOverride;
        internal void PlatformApplyState(bool applyShaders)
        {
            Threading.EnsureUIThread();

            if ( _scissorRectangleDirty )
	        {
                var scissorRect = _scissorRectangle;
                if (!IsRenderTargetBound)
                    scissorRect.Y = _viewport.Height - scissorRect.Y - scissorRect.Height;
                GL.Scissor(scissorRect.X, scissorRect.Y, scissorRect.Width, scissorRect.Height);
                GraphicsExtensions.CheckGLError();
	            _scissorRectangleDirty = false;
	        }

            if (_blendStateDirty)
            {
                _blendState.PlatformApplyState(this);
                _blendStateDirty = false;
            }
	        if ( _depthStencilStateDirty )
            {
	            _depthStencilState.PlatformApplyState(this);
                _depthStencilStateDirty = false;
            }
	        if ( _rasterizerStateDirty )
            {
	            _rasterizerState.PlatformApplyState(this);
	            _rasterizerStateDirty = false;
            }

            // If we're not applying shaders then early out now.
            if (!applyShaders)
                return;

            if (_indexBufferDirty)
            {
                if (_indexBuffer != null)
                {
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer.ibo);
                    GraphicsExtensions.CheckGLError();
                }
                _indexBufferDirty = false;
            }

            if (_vertexBufferDirty)
            {
                if (_vertexBuffer != null)
                {
                    GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer.vbo);
                    GraphicsExtensions.CheckGLError();
                }
            }

            if (_vertexShader == null)
                throw new InvalidOperationException("A vertex shader must be set!");
            if (_pixelShader == null)
                throw new InvalidOperationException("A pixel shader must be set!");

            if (_vertexShaderDirty || _pixelShaderDirty)
            {
                ActivateShaderProgram();
                _vertexShaderDirty = _pixelShaderDirty = false;
            }

            _vertexConstantBuffers.SetConstantBuffers(this, _shaderProgram);
            _pixelConstantBuffers.SetConstantBuffers(this, _shaderProgram);

            Textures.SetTextures(this);
            SamplerStates.PlatformSetSamplers(this);
            if (StateOverride != null)
            { StateOverride(this, null); }
        }

        private void PlatformDrawIndexedPrimitives(PrimitiveType primitiveType, int baseVertex, int startIndex, int primitiveCount)
        {
            PlatformApplyState(true);

            var shortIndices = _indexBuffer.IndexElementSize == IndexElementSize.SixteenBits;

			var indexElementType = shortIndices ? DrawElementsType.UnsignedShort : DrawElementsType.UnsignedInt;
            var indexElementSize = shortIndices ? 2 : 4;
			var indexOffsetInBytes = (IntPtr)(startIndex * indexElementSize);
			var indexElementCount = GetElementCountArray(primitiveType, primitiveCount);
			var target = PrimitiveTypeGL(primitiveType);
			var vertexOffset = (IntPtr)(_vertexBuffer.VertexDeclaration.VertexStride * baseVertex);

			_vertexBuffer.VertexDeclaration.Apply(_vertexShader, _pixelShader, vertexOffset);

            GL.DrawElements(target,
                                     indexElementCount,
                                     indexElementType,
                                     indexOffsetInBytes);
            GraphicsExtensions.CheckGLError();
        }

        private void PlatformDrawUserPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset, VertexDeclaration vertexDeclaration, int vertexCount) where T : struct
        {
            PlatformApplyState(true);

            // Unbind current VBOs.
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GraphicsExtensions.CheckGLError();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GraphicsExtensions.CheckGLError();
            _vertexBufferDirty = _indexBufferDirty = true;

            // Pin the buffers.
            var vbHandle = GCHandle.Alloc(vertexData, GCHandleType.Pinned);

            // Setup the vertex declaration to point at the VB data.
            vertexDeclaration.GraphicsDevice = this;
            vertexDeclaration.Apply(_vertexShader, _pixelShader, vbHandle.AddrOfPinnedObject());

            //Draw
            GL.DrawArrays(PrimitiveTypeGL(primitiveType),
                          vertexOffset,
                          vertexCount);
            GraphicsExtensions.CheckGLError();

            // Release the handles.
            vbHandle.Free();
        }

        private void PlatformDrawUserPrimitives(PrimitiveType primitiveType, IntPtr vertexDataPtr, int vertexOffset, VertexDeclaration vertexDeclaration, int vertexCount)
        {
            PlatformApplyState(true);

            // Unbind current VBOs.
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GraphicsExtensions.CheckGLError();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GraphicsExtensions.CheckGLError();
            _vertexBufferDirty = _indexBufferDirty = true;

            var vertexAddr = (IntPtr)(vertexDataPtr.ToInt64() + vertexDeclaration.VertexStride * vertexOffset);

            // Setup the vertex declaration to point at the VB data.
            vertexDeclaration.GraphicsDevice = this;
            vertexDeclaration.Apply(_vertexShader, _pixelShader, vertexAddr);

            //Draw
            GL.DrawArrays(PrimitiveTypeGL(primitiveType), vertexOffset, vertexCount);
            GraphicsExtensions.CheckGLError();
        }

        private void PlatformDrawPrimitives(PrimitiveType primitiveType, int vertexStart, int vertexCount)
        {
            PlatformApplyState(true);

            _vertexBuffer.VertexDeclaration.Apply(_vertexShader, _pixelShader, IntPtr.Zero);

			GL.DrawArrays(PrimitiveTypeGL(primitiveType),
			              vertexStart,
			              vertexCount);
            GraphicsExtensions.CheckGLError();
        }

        private void PlatformDrawUserIndexedPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int numVertices, short[] indexData, int indexOffset, int primitiveCount, VertexDeclaration vertexDeclaration) where T : struct
        {
            PlatformApplyState(true);

            // Unbind current VBOs.
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GraphicsExtensions.CheckGLError();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GraphicsExtensions.CheckGLError();
            _vertexBufferDirty = _indexBufferDirty = true;

            // Pin the buffers.
            var vbHandle = GCHandle.Alloc(vertexData, GCHandleType.Pinned);
            var ibHandle = GCHandle.Alloc(indexData, GCHandleType.Pinned);

            var vertexAddr = (IntPtr)(vbHandle.AddrOfPinnedObject().ToInt64() + vertexDeclaration.VertexStride * vertexOffset);

            // Setup the vertex declaration to point at the VB data.
            vertexDeclaration.GraphicsDevice = this;
            vertexDeclaration.Apply(_vertexShader, _pixelShader, vertexAddr);

            //Draw
            GL.DrawElements(    PrimitiveTypeGL(primitiveType),
                                GetElementCountArray(primitiveType, primitiveCount),
                                DrawElementsType.UnsignedShort,
                                (IntPtr)(ibHandle.AddrOfPinnedObject().ToInt64() + (indexOffset * sizeof(short))));
            GraphicsExtensions.CheckGLError();

            // Release the handles.
            ibHandle.Free();
            vbHandle.Free();
        }

        private void PlatformDrawUserIndexedPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int numVertices, int[] indexData, int indexOffset, int primitiveCount, VertexDeclaration vertexDeclaration) where T : struct, IVertexType
        {
            PlatformApplyState(true);

            // Unbind current VBOs.
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GraphicsExtensions.CheckGLError();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GraphicsExtensions.CheckGLError();
            _vertexBufferDirty = _indexBufferDirty = true;

            // Pin the buffers.
            var vbHandle = GCHandle.Alloc(vertexData, GCHandleType.Pinned);
            var ibHandle = GCHandle.Alloc(indexData, GCHandleType.Pinned);

            var vertexAddr = (IntPtr)(vbHandle.AddrOfPinnedObject().ToInt64() + vertexDeclaration.VertexStride * vertexOffset);

            // Setup the vertex declaration to point at the VB data.
            vertexDeclaration.GraphicsDevice = this;
            vertexDeclaration.Apply(_vertexShader, _pixelShader, vertexAddr);

            //Draw
            GL.DrawElements(    PrimitiveTypeGL(primitiveType),
                                GetElementCountArray(primitiveType, primitiveCount),
                                DrawElementsType.UnsignedInt,
                                (IntPtr)(ibHandle.AddrOfPinnedObject().ToInt64() + (indexOffset * sizeof(int))));
            GraphicsExtensions.CheckGLError();

            // Release the handles.
            ibHandle.Free();
            vbHandle.Free();
        }

        private void PlatformDrawUserIndexedPrimitives(PrimitiveType primitiveType, IntPtr vertexDataPtr, int vertexOffset, int numVertices, IndexElementSize indexSize, IntPtr indexDataPtr, int indexOffset, int primitiveCount, VertexDeclaration vertexDeclaration)
        {
            PlatformApplyState(true);

            // Unbind current VBOs.
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GraphicsExtensions.CheckGLError();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GraphicsExtensions.CheckGLError();
            _vertexBufferDirty = _indexBufferDirty = true;

            var vertexAddr = (IntPtr)(vertexDataPtr.ToInt64() + vertexDeclaration.VertexStride * vertexOffset);

            // Setup the vertex declaration to point at the VB data.
            vertexDeclaration.GraphicsDevice = this;
            vertexDeclaration.Apply(_vertexShader, _pixelShader, vertexAddr);

            //Draw
            GL.DrawElements(PrimitiveTypeGL(primitiveType),
                                GetElementCountArray(primitiveType, primitiveCount),
                                indexSize == IndexElementSize.SixteenBits ? DrawElementsType.UnsignedShort : DrawElementsType.UnsignedInt,
                                (IntPtr)(indexDataPtr.ToInt64() + (indexOffset * (indexSize == IndexElementSize.SixteenBits ? 2 : 4))));
            GraphicsExtensions.CheckGLError();
        }

        private static GraphicsProfile PlatformGetHighestSupportedGraphicsProfile(GraphicsDevice graphicsDevice)
        {
           return GraphicsProfile.HiDef;
        }
    }
}
