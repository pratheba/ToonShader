using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using GoblinXNA.Graphics;
using Microsoft.Xna.Framework.Graphics;
using GoblinXNA.Shaders;
using Microsoft.Xna.Framework;

namespace GoblinToonShader
{
    public class ToonPostProcessShader : PostProcessShader
    {
        int settingsIndex;
        public GraphicsDeviceManager graphics;
        public GraphicsDevice device;
        public RenderTarget2D sceneRenderTarget;
        public RenderTarget2D normalDepthRenderTarget;
        public SpriteBatch spriteBatch;
        public SpriteFont spriteFont;

        public NonPhotoRealisticRendering Settings;
        
        KeyValuePair<string, int> result = new KeyValuePair<string, int>();
   
        private EffectParameter
            sketchThreshold,
            sketchBrightness,
            enableSketch,
            sketchInColor, 
            sketchJitterSpeed,
            sketchJitter,

            edgeWidth,
            edgeIntensity,
            screenResolution,
            sceneTexture,
            sketchTexture,
            normalDepthTexture;

        public Vector2 Sketch_Jitter {get; set;}
        public Texture2D Sketch_texture { get; set; }

        public ToonPostProcessShader() 
        {
          settingsIndex = 0;
          Settings = NonPhotoRealisticRendering.PresetSettings[settingsIndex];
        }

        public void ChangeEffect()
        {
            settingsIndex = (settingsIndex + 1) %
                                NonPhotoRealisticSettings.PresetSettings.Length;
            Settings = NonPhotoRealisticRendering.PresetSettings[settingsIndex];
        }

        public KeyValuePair<string, int> getDefaultTechniqueSettings(int SettingNo)
        {
            
            switch (SettingNo)
            {
                case 0:
                    {
                        if (Settings.enableEdgeDetect)
                        {
                            device.SetRenderTarget(normalDepthRenderTarget);
                            device.Clear(Color.Black);
                            result = new KeyValuePair<string, int>("NormalDepth", 1);
                            
                        }
                    }
                    return result;
                case 1:
                    {

                        if (Settings.enableEdgeDetect || Settings.EnableSketch)
                            device.SetRenderTarget(sceneRenderTarget);
                        else
                            device.SetRenderTarget(null);

                        device.Clear(Color.CornflowerBlue);

                        if (Settings.EnableToonShading)
                            result = new KeyValuePair<string, int>("Toon", 1);
                        else
                            result = new KeyValuePair<string, int>("Lambert", 1);

                    }
                    return result;
                case 2:
                    {
                        if (Settings.EnableEdgeDetect || Settings.EnableSketch)
                        {
                            device.SetRenderTarget(null);
                        }
                        result = new KeyValuePair<string, int>(result.Key, 0);
                    }
                    return result;
                default:
                    result = new KeyValuePair<string, int>("", -1);
                    return result;
                    
            }
            return result;
        }
        

        public NonPhotoRealisticRendering getSettings(int settingsIndex)
        {
            Settings = NonPhotoRealisticRendering.PresetSettings[settingsIndex];
            return Settings;
        }

        protected override void GetParameters()
        {
            edgeWidth = effect.Parameters["EdgeWidth"];
            edgeIntensity = effect.Parameters["EdgeIntensity"];
            screenResolution = effect.Parameters["ScreenResolution"];
            sceneTexture = effect.Parameters["SceneTexture"];
            normalDepthTexture = effect.Parameters["NormalDepthTexture"];
            sketchTexture = effect.Parameters["SketchTexture"];

            sketchThreshold = effect.Parameters["SketchThreshold"];
            sketchBrightness = effect.Parameters["SketchBrightness"];
            enableSketch = effect.Parameters["EnableSketch"];
            sketchInColor = effect.Parameters["SketchInColor"];
            sketchJitterSpeed = effect.Parameters["SketchJitterSpeed"];
            sketchJitter = effect.Parameters["SketchJitter"];
        }

        public override void SetParameters(NonPhotoRealisticRendering NPRrender)
        {
            SetParameters();
        }
        public void SetParameters()
        {
            if (Settings.EnableSketch)
            {
                sketchThreshold.SetValue(Settings.SketchThreshold);
                sketchBrightness.SetValue(Settings.SketchBrightness);
                sketchJitter.SetValue(Sketch_Jitter);
                sketchTexture.SetValue(Sketch_texture);
            }

            if (Settings.EnableEdgeDetect)
            {
                Vector2 resolution = new Vector2(sceneRenderTarget.Width,
                                                  sceneRenderTarget.Height);

                Texture2D depthTexture = normalDepthRenderTarget;

                edgeWidth.SetValue((float)Settings.EdgeWidth);
                edgeIntensity.SetValue(Settings.EdgeIntensity);
                screenResolution.SetValue(resolution);
                normalDepthTexture.SetValue(depthTexture);
               
                // Choose which effect technique to use.
                if (Settings.EnableSketch)
                {
                    if (Settings.SketchInColor)
                        effect.CurrentTechnique = effect.Techniques["EdgeDetectColorSketch"];
                    else
                        effect.CurrentTechnique = effect.Techniques["EdgeDetectMonoSketch"];
                }
                else
                    effect.CurrentTechnique = effect.Techniques["EdgeDetect"];

            }
            else
            {
                // If edge detection is off, just pick one of the sketch techniques.
                if (Settings.SketchInColor)
                    effect.CurrentTechnique = effect.Techniques["ColorSketch"];
                else
                    effect.CurrentTechnique = effect.Techniques["MonoSketch"];
            }

            // Draw a fullscreen sprite to apply the postprocessing effect.
            spriteBatch.Begin(0, BlendState.Opaque, null, null, null, effect);
            spriteBatch.Draw(sceneRenderTarget, Vector2.Zero, Color.White);
            spriteBatch.End();

            DrawOverlayText();
        }

        public void DrawOverlayText()
        {
           string text = "A = settings (" + Settings.Name + ")";

            spriteBatch.Begin();
            spriteBatch.DrawString(spriteFont, text, new Vector2(65, 65), Color.Black);
            spriteBatch.DrawString(spriteFont, text, new Vector2(64, 64), Color.White);

            spriteBatch.End();
        }

    }
}
