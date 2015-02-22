using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using GoblinXNA;
using Microsoft.Xna.Framework.Content;

namespace GoblinToonShader
{
    interface ToonInterface
    {
        RenderTarget2D getSceneRenderTarget();
        RenderTarget2D getNormalDepthRenderTarget();
        void setrenderTarget();
    }

    public class ToonInterfaceImplClass :  Microsoft.Xna.Framework.Game , ToonInterface
    {
        PresentationParameters pp;
        RenderTarget2D sceneRenderTarget;
        RenderTarget2D normalDepthRenderTarget;

        GraphicsDeviceManager graphics;
        public GraphicsDevice device { get; set; }

        // Manages Sprites to be drawn on 2D screen
        SpriteBatch spriteBatch;
        public SpriteFont textFont;

        public ToonShader toonShader;
        public ToonPostProcessShader toonPostProcessShader;
        private Texture2D sketchTexture;
        private Effect toonEffect;

           
        // Explicit interface member implementation: 
        RenderTarget2D ToonInterface.getSceneRenderTarget()
        {
            return sceneRenderTarget;
        }

        RenderTarget2D ToonInterface.getNormalDepthRenderTarget()
        {
            return normalDepthRenderTarget;
        }

        void ToonInterface.setrenderTarget()
        {
            pp = graphics.GraphicsDevice.PresentationParameters;

            sceneRenderTarget = new RenderTarget2D(graphics.GraphicsDevice,
                                                pp.BackBufferWidth, pp.BackBufferHeight, false,
                                                pp.BackBufferFormat, pp.DepthStencilFormat);

            normalDepthRenderTarget = new RenderTarget2D(graphics.GraphicsDevice,
                                                        pp.BackBufferWidth, pp.BackBufferHeight, false,
                                                        pp.BackBufferFormat, pp.DepthStencilFormat);
        }

         // Constructor
        public ToonInterfaceImplClass(ref GraphicsDeviceManager ref_graphics, ContentManager ref_Content)
        {
            graphics = ref_graphics;
            Content = ref_Content;
            LoadContent(ref ref_Content);
            Initialize(ref ref_Content);

        }

        protected void LoadContent(ref ContentManager Content)
        {
            device = graphics.GraphicsDevice;
            spriteBatch = new SpriteBatch(graphics.GraphicsDevice);
            
            textFont = Content.Load<SpriteFont>("Arial-bold");
            sketchTexture = Content.Load<Texture2D>("SketchTexture");

            toonEffect = Content.Load<Effect>("toonEffect");
        
            pp = graphics.GraphicsDevice.PresentationParameters;

            sceneRenderTarget = new RenderTarget2D(graphics.GraphicsDevice,
                                                pp.BackBufferWidth, pp.BackBufferHeight, false,
                                                pp.BackBufferFormat, pp.DepthStencilFormat);

            normalDepthRenderTarget = new RenderTarget2D(graphics.GraphicsDevice,
                                                        pp.BackBufferWidth, pp.BackBufferHeight, false,
                                                        pp.BackBufferFormat, pp.DepthStencilFormat);
        }

        protected void Initialize(ref ContentManager ref_Content)
        {
            base.Initialize();
            State.InitGoblin(graphics, ref_Content, "");
            InitializeShaders();
        }

        private void InitializeShaders()
        {
            toonShader = new ToonShader();
            toonPostProcessShader = new ToonPostProcessShader();
            toonPostProcessShader.graphics = graphics;
            toonPostProcessShader.device = device;
            toonPostProcessShader.normalDepthRenderTarget = normalDepthRenderTarget;
            toonPostProcessShader.sceneRenderTarget = sceneRenderTarget;
            toonPostProcessShader.spriteBatch = spriteBatch;
            toonPostProcessShader.spriteFont = textFont;
            toonPostProcessShader.Sketch_texture = sketchTexture;

        }
       
    }
}
