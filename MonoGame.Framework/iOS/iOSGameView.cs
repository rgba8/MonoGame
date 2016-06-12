#region License
/*
Microsoft Public License (Ms-PL)
MonoGame - Copyright © 2009-2012 The MonoGame Team

All rights reserved.

This license governs use of the accompanying software. If you use the software,
you accept this license. If you do not accept the license, do not use the
software.

1. Definitions

The terms "reproduce," "reproduction," "derivative works," and "distribution"
have the same meaning here as under U.S. copyright law.

A "contribution" is the original software, or any additions or changes to the
software.

A "contributor" is any person that distributes its contribution under this
license.

"Licensed patents" are a contributor's patent claims that read directly on its
contribution.

2. Grant of Rights

(A) Copyright Grant- Subject to the terms of this license, including the
license conditions and limitations in section 3, each contributor grants you a
non-exclusive, worldwide, royalty-free copyright license to reproduce its
contribution, prepare derivative works of its contribution, and distribute its
contribution or any derivative works that you create.

(B) Patent Grant- Subject to the terms of this license, including the license
conditions and limitations in section 3, each contributor grants you a
non-exclusive, worldwide, royalty-free license under its licensed patents to
make, have made, use, sell, offer for sale, import, and/or otherwise dispose of
its contribution in the software or derivative works of the contribution in the
software.

3. Conditions and Limitations

(A) No Trademark License- This license does not grant you rights to use any
contributors' name, logo, or trademarks.

(B) If you bring a patent claim against any contributor over patents that you
claim are infringed by the software, your patent license from such contributor
to the software ends automatically.

(C) If you distribute any portion of the software, you must retain all
copyright, patent, trademark, and attribution notices that are present in the
software.

(D) If you distribute any portion of the software in source code form, you may
do so only under this license by including a complete copy of this license with
your distribution. If you distribute any portion of the software in compiled or
object code form, you may only do so under a license that complies with this
license.

(E) The software is licensed "as-is." You bear the risk of using it. The
contributors give no express warranties, guarantees or conditions. You may have
additional consumer rights under your local laws which this license cannot
change. To the extent permitted under your local laws, the contributors exclude
the implied warranties of merchantability, fitness for a particular purpose and
non-infringement.
*/


#endregion License

using System;
using System.Drawing;

using CoreAnimation;
using Foundation;
using ObjCRuntime;
using OpenGLES;
using UIKit;
using CoreGraphics;

using OpenTK.Graphics;
using OpenTK.Graphics.ES20;
using OpenTK.Platform.iPhoneOS;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;

namespace Microsoft.Xna.Framework {

    [Register("iOSGameView")]
	partial class iOSGameView : UIView {
		private readonly iOSGamePlatform _platform;
		private int _colorbuffer;
		private int _depthbuffer;
		private int _framebuffer;

		#region Construction/Destruction
		public iOSGameView (iOSGamePlatform platform, CGRect frame)
			: base(frame)
		{
			if (platform == null)
				throw new ArgumentNullException ("platform");
			_platform = platform;
			Initialize ();
		}

		private void Initialize ()
		{
			MultipleTouchEnabled = true;
			Opaque = true;
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				if (__renderbuffergraphicsContext != null)
					DestroyContext();
			}

			base.Dispose (disposing);
			_isDisposed = true;
		}

		#endregion Construction/Destruction

		#region Properties

		private bool _isDisposed;

		public bool IsDisposed {
			get { return _isDisposed; }
		}

		#endregion Properties

		[Export ("layerClass")]
		public static Class GetLayerClass ()
		{
			return new Class (typeof (CAEAGLLayer));
		}

		public override bool CanBecomeFirstResponder {
			get { return true; }
		}

		private new CAEAGLLayer Layer {
			get { return base.Layer as CAEAGLLayer; }
		}

		// FIXME: Someday, hopefully it will be possible to move
		//        GraphicsContext into an iOS-specific GraphicsDevice.
		//        Some level of cooperation with the UIView/Layer will
		//        probably always be necessary, unfortunately.
		private GraphicsContext __renderbuffergraphicsContext;
		private IOpenGLApi _glapi;
		private void CreateContext ()
		{
			AssertNotDisposed ();

			try {
				__renderbuffergraphicsContext = new GraphicsContext (null, null, 2, 0, GraphicsContextFlags.Embedded);
				_glapi = new Gles20Api ();
			} catch {
				__renderbuffergraphicsContext = new GraphicsContext (null, null, 1, 1, GraphicsContextFlags.Embedded);
				_glapi = new Gles11Api ();
			}

			this.MakeCurrent();
		}

		private void DestroyContext ()
		{
			AssertNotDisposed ();
			AssertValidContext ();

			__renderbuffergraphicsContext.Dispose ();
			__renderbuffergraphicsContext = null;
			_glapi = null;
		}

        [Export("doTick")]
        void DoTick()
        {
            _platform.Tick();
        }

		private void CreateFramebuffer ()
		{
			this.MakeCurrent();
			
			// HACK:  GraphicsDevice itself should be calling
			//        glViewport, so we shouldn't need to do it
			//        here and then force the state into
			//        GraphicsDevice.  However, that change is a
			//        ways off, yet.

            var depthBufferFormat = All.None;
            var colorFormat = EAGLColorFormat.RGBA8;
            var gdm = _platform.Game.Services.GetService(
                typeof(IGraphicsDeviceManager)) as GraphicsDeviceManager;
            if (gdm != null)
            {
                switch (gdm.PreferredDepthStencilFormat)
                {
                    case DepthFormat.Depth16: depthBufferFormat = All.DepthComponent16; break;
                    case DepthFormat.Depth24: depthBufferFormat = All.DepthComponent24Oes; break;
                    case DepthFormat.Depth24Stencil8: depthBufferFormat = All.Depth24Stencil8Oes; break;
                }

                switch (gdm.PreferredBackBufferFormat)
                {
                    case SurfaceFormat.Color: colorFormat = EAGLColorFormat.RGBA8; break;
                    case SurfaceFormat.Bgr565: colorFormat = EAGLColorFormat.RGB565; break;
                    default: throw new ArgumentOutOfRangeException("The BackBufferFormat requested is invalid.");
                }
            }

            // RetainedBacking controls if the content of the colorbuffer should be preserved after being displayed
            // This is the XNA equivalent to set PreserveContent when initializing the GraphicsDevice
            // (should be false by default for better performance)

            Layer.ContentsScale = Window.Screen.Scale;
            Layer.Opaque = true;
            Layer.DrawableProperties = NSDictionary.FromObjectsAndKeys(
                new NSObject[] {
					NSNumber.FromBoolean (false), 
					colorFormat
				},
                new NSObject[] {
					EAGLDrawableProperty.RetainedBacking,
					EAGLDrawableProperty.ColorFormat
				});

			GL.GenFramebuffers(1, out _framebuffer);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);

            GL.GenRenderbuffers(1, out _colorbuffer);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _colorbuffer);

            var ctx = ((IGraphicsContextInternal)__renderbuffergraphicsContext).Implementation as iPhoneOSGraphicsContext;
            ctx.EAGLContext.RenderBufferStorage((uint)All.Renderbuffer, Layer);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferSlot.ColorAttachment0, RenderbufferTarget.Renderbuffer, _colorbuffer);

            var viewportWidth = 0;
            var viewportHeight = 0;
            GL.GetRenderbufferParameter(RenderbufferTarget.Renderbuffer, RenderbufferParameterName.RenderbufferWidth, out viewportWidth);
            GL.GetRenderbufferParameter(RenderbufferTarget.Renderbuffer, RenderbufferParameterName.RenderbufferHeight, out viewportHeight);

            var renderbufferInternalFormat = 0;
            GL.GetRenderbufferParameter(RenderbufferTarget.Renderbuffer, RenderbufferParameterName.RenderbufferInternalFormat, out renderbufferInternalFormat);

            var renderbufferDepthSize = 0;
            var renderbufferStencilSize = 0;
            if (depthBufferFormat != All.None)
            {
                GL.GenRenderbuffers(1, out _depthbuffer);
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthbuffer);
                GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, (RenderbufferInternalFormat)depthBufferFormat, viewportWidth, viewportHeight);
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferSlot.DepthAttachment, RenderbufferTarget.Renderbuffer, _depthbuffer);
                if (depthBufferFormat == All.Depth24Stencil8Oes)
                    GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferSlot.StencilAttachment, RenderbufferTarget.Renderbuffer, _depthbuffer);

                GL.GetRenderbufferParameter(RenderbufferTarget.Renderbuffer, RenderbufferParameterName.RenderbufferDepthSize, out renderbufferDepthSize);
                GL.GetRenderbufferParameter(RenderbufferTarget.Renderbuffer, RenderbufferParameterName.RenderbufferStencilSize, out renderbufferStencilSize);
            }

			var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
			if (status != FramebufferErrorCode.FramebufferComplete)
				throw new InvalidOperationException (
					"Framebuffer was not created correctly: " + status);

			GL.Viewport(0, 0, viewportWidth, viewportHeight);
            GL.Scissor(0, 0, viewportWidth, viewportHeight);

			var gds = _platform.Game.Services.GetService(
                typeof (IGraphicsDeviceService)) as IGraphicsDeviceService;

			if (gds != null && gds.GraphicsDevice != null)
			{
                var pp = gds.GraphicsDevice.PresentationParameters;
                int height = viewportHeight;
                int width = viewportWidth;

                if (this.NextResponder is iOSGameViewController)
                {
                    var displayOrientation = _platform.Game.Window.CurrentOrientation;
                    if (displayOrientation == DisplayOrientation.LandscapeLeft || displayOrientation == DisplayOrientation.LandscapeRight)
                    {
                        height = Math.Min(viewportHeight, viewportWidth);
                        width = Math.Max(viewportHeight, viewportWidth);
                    }
                    else
                    {
                        height = Math.Max(viewportHeight, viewportWidth);
                        width = Math.Min(viewportHeight, viewportWidth);
                    }
                }

                pp.BackBufferHeight = height;
                pp.BackBufferWidth = width;
                // We assume we got what we requested since iOS support is consistent accross gles 2.0+ devices
                // Unsupported backbuffer format will throw an exception before getting here.
                pp.BackBufferFormat = gdm.PreferredBackBufferFormat;
                pp.DepthStencilFormat = gdm.PreferredDepthStencilFormat;

				gds.GraphicsDevice.Viewport = new Viewport(
					0, 0,
					pp.BackBufferWidth,
					pp.BackBufferHeight);
				
				// FIXME: These static methods on GraphicsDevice need
				//        to go away someday.
				gds.GraphicsDevice.glFramebuffer = _framebuffer;
			}

            if (Threading.BackgroundContext == null)
                Threading.BackgroundContext = new OpenGLES.EAGLContext(ctx.EAGLContext.API, ctx.EAGLContext.ShareGroup);
		}

		private void DestroyFramebuffer ()
		{
			AssertNotDisposed ();
			AssertValidContext ();

			__renderbuffergraphicsContext.MakeCurrent (null);

			GL.DeleteFramebuffers (1, ref _framebuffer);
			_framebuffer = 0;

			GL.DeleteRenderbuffers (1, ref _colorbuffer);
			_colorbuffer = 0;
			
            if (_depthbuffer != 0)
            {
			    GL.DeleteRenderbuffers (1, ref _depthbuffer);
			    _depthbuffer = 0;
            }
		}

        private static readonly All[] attachements = new All[] { All.DepthAttachment, All.StencilAttachment };

		// FIXME: This logic belongs in GraphicsDevice.Present, not
		//        here.  If it can someday be moved there, then the
		//        normal call to Present in Game.Tick should cover
		//        this.  For now, iOSGamePlatform will call Present
		//        in the Draw/Update loop handler.
		public void Present ()
		{
            AssertNotDisposed ();
            AssertValidContext ();

            this.MakeCurrent();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, this._colorbuffer);
            GraphicsDevice.FramebufferHelper.GLDiscardFramebufferExt(All.Framebuffer, 2, attachements);
            __renderbuffergraphicsContext.SwapBuffers();
		}

		// FIXME: This functionality belongs in GraphicsDevice.
		public void MakeCurrent ()
		{
			AssertNotDisposed ();
			AssertValidContext ();

            if (!__renderbuffergraphicsContext.IsCurrent)
            {
			    __renderbuffergraphicsContext.MakeCurrent (null);
            }
		}

		public override void LayoutSubviews ()
		{
			base.LayoutSubviews ();

            var gds = _platform.Game.Services.GetService (
                typeof (IGraphicsDeviceService)) as IGraphicsDeviceService;

            if (gds == null || gds.GraphicsDevice == null)
                return;

			if (_framebuffer != 0)
				DestroyFramebuffer ();
			if (__renderbuffergraphicsContext == null)
				CreateContext();
			CreateFramebuffer ();
		}

		#region UIWindow Notifications

		[Export ("didMoveToWindow")]
		public virtual void DidMoveToWindow ()
		{

            if (Window != null) {
                
                if (__renderbuffergraphicsContext == null)
                    CreateContext ();
                if (_framebuffer == 0)
                    CreateFramebuffer ();
            }
		}

		#endregion UIWindow Notifications

		private void AssertNotDisposed ()
		{
			if (_isDisposed)
				throw new ObjectDisposedException (GetType ().Name);
		}

		private void AssertValidContext ()
		{
			if (__renderbuffergraphicsContext == null)
				throw new InvalidOperationException (
					"GraphicsContext must be created for this operation to succeed.");
		}
	}
}
