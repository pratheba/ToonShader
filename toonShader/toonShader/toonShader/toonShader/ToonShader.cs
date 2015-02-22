/************************************************************************************ 
 * Copyright (c) 2008-2011, Columbia University
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Columbia University nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY COLUMBIA UNIVERSITY ''AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL <copyright holder> BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 * 
 * 
 * ===================================================================================
 * Author: Fan Lin (linfan68@gmail.com)
 *         Ohan Oda (ohan@cs.columbia.edu) modified for XNA 4.0
 * 
 *************************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using GoblinXNA;
using GoblinXNA.Graphics;
using GoblinXNA.SceneGraph;
using GoblinXNA.Shaders;
using Model = GoblinXNA.Graphics.Model;
using GoblinXNA.Graphics.Geometry;
using GoblinXNA.Helpers;
using Texture = Microsoft.Xna.Framework.Graphics.Texture2D;
using toonShader;


namespace GoblinToonShader
{

    public class ToonShader : Shader
    {
        private EffectParameter
            worldForNormal,
            cameraPosition,

            //Material Paramters
            emissiveColor,
            diffuseColor,
            specularColor,
            specularPower,
            toonTexture,
            textureEnabled,

            //Light paramters
            lights,
            light,
            numberOfLights,
            ambientLightColor;


        private List<LightSource> lightSources;
        private List<LightSource> dirLightSources;
       
        private int maxNumLightsPerPass;
        private bool is_3_0;
        private bool forcePS20;
        private bool isPostProcessing;

        BlendState blendState;
        DepthStencilState depthState;
        DepthStencilState depthState2;

        private Vector3 cameraPos;
        private string defaultTechnique;

        SpriteBatch spriteBatch;
        private static int count;
        private static bool reRender;

        public ToonPostProcessShader toonPPShader;
 
        /// <summary>
        /// Creates a DirectX shader that uses 'DirectXShader.fx' shader file. 
        /// </summary>
        public ToonShader()
            : base("tooneffect")
        {
            lightSources = new List<LightSource>();
            dirLightSources = new List<LightSource>();
 
            blendState = new BlendState();
            blendState.ColorBlendFunction = BlendFunction.Add;
            blendState.ColorSourceBlend = Blend.One;
            blendState.ColorDestinationBlend = Blend.One;
            blendState.AlphaDestinationBlend = Blend.One;

            depthState = new DepthStencilState();
            depthState.DepthBufferEnable = true;
            depthState.DepthBufferFunction = CompareFunction.LessEqual;
            depthState.DepthBufferWriteEnable = true;

            depthState2 = new DepthStencilState();
            depthState2.DepthBufferEnable = true;
            depthState2.DepthBufferFunction = CompareFunction.LessEqual;
            depthState2.DepthBufferWriteEnable = false;
            defaultTechnique = "NormalDepth";
            maxNumLightsPerPass = 1;
            Dictionary<Effect, Effect> effectMapping = new Dictionary<Effect, Effect>();

            is_3_0 = false;
            forcePS20 = true;
            isPostProcessing = false;

            if ((State.Device.GraphicsProfile == GraphicsProfile.HiDef) && !forcePS20)
            {
                is_3_0 = true;
            }
            else
            {
                is_3_0 = false;
            }
            reRender = true;
           
        }

        /// <summary>
        /// Gets or sets whether to force this shader to use Pixel Shader 2.0 profile even if
        /// the graphics card support Pixel Shader 3.0. By default, this is set to true.
        /// </summary>
        /// <remarks>
        /// When there are less than fifty lights, it's faster to use Pixel Shader 2.0 profile.
        /// However, if you have more than fifty lights, Pixel Shader 3.0 will perform better.
        /// </remarks>
        public bool UsePS20
        {
            get { return forcePS20; }
            set { forcePS20 = value; }
        }

       
        public override int MaxLights
        {
            get { return 1000; }
        }

        private void GetMinimumParameters()
        {
            world = effect.Parameters["world"];
            viewProj = effect.Parameters["viewProjection"];
            worldForNormal = effect.Parameters["worldForNormal"];
            cameraPosition = effect.Parameters["cameraPosition"];

            lights = effect.Parameters["lights"];
            light = effect.Parameters["light"];
            ambientLightColor = effect.Parameters["ambientLightColor"];

        }

        protected override void GetParameters()
        {
            //Binding the effect parameters in to Effect File;

            // Geometry
            world = effect.Parameters["world"];
            viewProj = effect.Parameters["viewProjection"];
            worldForNormal = effect.Parameters["worldForNormal"];
            cameraPosition = effect.Parameters["cameraPosition"];

            // Material
            emissiveColor = effect.Parameters["emissiveColor"];
            diffuseColor = effect.Parameters["diffuseColor"];
            specularColor = effect.Parameters["specularColor"];
            specularPower = effect.Parameters["specularPower"];
            toonTexture = effect.Parameters["toonTexture"];
            textureEnabled = effect.Parameters["TextureEnabled"];

            // Lights
            light = effect.Parameters["light"];
            ambientLightColor = effect.Parameters["ambientLightColor"];
            numberOfLights = effect.Parameters["numberOfLights"];
        }

        public override void MappingEffect(ref Dictionary<Effect, Effect> effectMapping, BasicEffect oldEffect)
        {
            if (!effectMapping.ContainsKey(oldEffect))
            {
                Effect newEffect =  this.effect.Clone();
               
                // Copy across the texture from the original effect.
                newEffect.Parameters["toonTexture"].SetValue(oldEffect.Texture);
                newEffect.Parameters["TextureEnabled"].SetValue(oldEffect.TextureEnabled);

                effectMapping.Add(oldEffect, newEffect);
            }
        }

        public override void SetParameters(Material material)
        {
            if (material.InternalEffect != null)
            {
                effect = material.InternalEffect;
                GetMinimumParameters();              
            }
            else
            {
                emissiveColor.SetValue(material.Emissive);
                diffuseColor.SetValue(material.Diffuse);
                specularColor.SetValue(material.Specular);
                specularPower.SetValue(material.SpecularPower);
                GetParameters();
                if (material.HasTexture)
                {
                    toonTexture.SetValue(material.Texture);
                    textureEnabled.SetValue(material.HasTexture);
                }
                else
                    textureEnabled.SetValue(material.HasTexture);            
            }
        }

        public override void SetParameters(CameraNode camera)
        {
            cameraPos = camera.WorldTransformation.Translation;

            cameraPosition.SetValue(cameraPos);
        }

        public override void SetParameters(List<LightNode> globalLights, List<LightNode> localLights)
        {
            bool ambientSet = false;
            this.lightSources.Clear();
            LightNode lNode = null;
            Vector4 ambientLightColor = new Vector4(0, 0, 0, 1);

            // traverse the local lights in reverse order in order to get closest light sources
            // in the scene graph
            for (int i = localLights.Count - 1; i >= 0; i--)
            {
                lNode = localLights[i];
                // only set the ambient light color if it's not set yet and not the default color (0, 0, 0, 1)
                if (!ambientSet && (!lNode.AmbientLightColor.Equals(ambientLightColor)))
                {
                    ambientLightColor = lNode.AmbientLightColor;
                    ambientSet = true;
                }

                // skip the light source if not enabled
                if (!lNode.LightSource.Enabled)
                    continue;

                LightSource source = new LightSource(lNode.LightSource);
                if (lNode.LightSource.Type != LightType.Point)
                    source.Direction = lNode.LightSource.TransformedDirection;

                lightSources.Add(source);
            }

            // Next, traverse the global lights in normal order
            for (int i = 0; i < globalLights.Count; i++)
            {
                lNode = globalLights[i];
                // only set the ambient light color if it's not set yet and not the default color (0, 0, 0, 1)
                if (!ambientSet && (!lNode.AmbientLightColor.Equals(ambientLightColor)))
                {
                    ambientLightColor = lNode.AmbientLightColor;
                    ambientSet = true;
                }

                // skip the light source if not enabled
                if (!lNode.LightSource.Enabled)
                    continue;

                LightSource source = new LightSource(lNode.LightSource);
                if (lNode.LightSource.Type != LightType.Point)
                    source.Direction = lNode.LightSource.TransformedDirection;

                lightSources.Add(source);
            }

            dirLightSources.Clear();
           
            foreach (LightSource l in lightSources)
            {
                switch (l.Type)
                {
                    case LightType.Directional:
                        dirLightSources.Add(l);
                        break;
                  
                }
            }
            this.ambientLightColor.SetValue(ambientLightColor);
        }

        public override void ReRender(Model model,ref Matrix worldMatrix, Material material)
        {
            int count = 0;
            KeyValuePair<string, int> pair_techNvalue;
            do
            {
                pair_techNvalue = toonPPShader.getDefaultTechniqueSettings(count++);
                defaultTechnique = pair_techNvalue.Key;

                model.RenderModel(ref worldMatrix, material);
            } while (pair_techNvalue.Value == 1);
            
        }


        public override void Render(ref Matrix worldMatrix, String techniqueName, RenderHandler renderDelegate)
        {
            if (renderDelegate == null)
                throw new GoblinException("renderDelegate is null");

                world.SetValue(worldMatrix);
                viewProj.SetValue(State.ViewProjectionMatrix);
                worldForNormal.SetValue(Matrix.Transpose(Matrix.Invert(worldMatrix)));

               
            // Start shader
            effect.CurrentTechnique = effect.Techniques[defaultTechnique];
            
            
            {
                BlendState origBlendState = State.Device.BlendState;
                DepthStencilState origDepthState = State.Device.DepthStencilState;

                State.Device.DepthStencilState = depthState;
                State.Device.BlendState = BlendState.Opaque;
               
                effect.CurrentTechnique.Passes["P0"].Apply();
                renderDelegate();
                State.Device.BlendState = blendState;

                State.Device.DepthStencilState = depthState2;

                if (is_3_0)
                {
                    DoRendering30(renderDelegate, dirLightSources);

                }
                else
                {
                    DoRendering20(renderDelegate, dirLightSources);

                }

                State.Device.BlendState = origBlendState;
                State.Device.DepthStencilState = origDepthState;
            }
        }

        private void DoRendering30(RenderHandler renderDelegate, List<LightSource> lightSources)
        {
            string passName;
            if (lightSources.Count == 0)
            {
                return;
            }
            else
            {
                passName = "P0";
            }
            for (int passCount = 0; passCount < (((lightSources.Count - 1) / maxNumLightsPerPass) + 1); passCount++)
            {

                int count = 0;
                for (int l = 0; l < maxNumLightsPerPass; l++)
                {
                    int lightIndex = (passCount * maxNumLightsPerPass) + l;
                    if (lightIndex >= lightSources.Count)
                    {
                        break;
                    }

                    SetUpLightSource(lightSources[lightIndex], l);
                    count++;
                }

                numberOfLights.SetValue(count);

                effect.CurrentTechnique.Passes[passName].Apply();
                renderDelegate();
            }
        }

        private void DoRendering20(RenderHandler renderDelegate, List<LightSource> lightSources)
        {
            string passName;
            if (lightSources.Count == 0)
            {
                return;
            }
            else
            {
                passName = "P0";
            }
            for (int passCount = 0; passCount < lightSources.Count; passCount++)
            {
                SetUpSingleLightSource(lightSources[passCount]);
                effect.CurrentTechnique.Passes[passName].Apply();
                renderDelegate();
            }
        }

        private void SetUpLightSource(LightSource lightSource, int index)
        {
            lights.Elements[index].StructureMembers["direction"].SetValue(lightSource.Direction);
            lights.Elements[index].StructureMembers["position"].SetValue(lightSource.Position);
            lights.Elements[index].StructureMembers["falloff"].SetValue(lightSource.Falloff);
            lights.Elements[index].StructureMembers["range"].SetValue(lightSource.Range);
            lights.Elements[index].StructureMembers["color"].SetValue(lightSource.Diffuse);
            lights.Elements[index].StructureMembers["attenuation0"].SetValue(lightSource.Attenuation0);
            lights.Elements[index].StructureMembers["attenuation1"].SetValue(lightSource.Attenuation1);
            lights.Elements[index].StructureMembers["attenuation2"].SetValue(lightSource.Attenuation2);
            lights.Elements[index].StructureMembers["innerConeAngle"].SetValue(lightSource.InnerConeAngle);
            lights.Elements[index].StructureMembers["outerConeAngle"].SetValue(lightSource.OuterConeAngle);
        }
        private void SetUpSingleLightSource(LightSource lightSource)
        {
            light.StructureMembers["direction"].SetValue(lightSource.Direction);
            light.StructureMembers["position"].SetValue(lightSource.Position);
            light.StructureMembers["falloff"].SetValue(lightSource.Falloff);
            light.StructureMembers["range"].SetValue(lightSource.Range);
            light.StructureMembers["color"].SetValue(lightSource.Diffuse);
            light.StructureMembers["attenuation0"].SetValue(lightSource.Attenuation0);
            light.StructureMembers["attenuation1"].SetValue(lightSource.Attenuation1);
            light.StructureMembers["attenuation2"].SetValue(lightSource.Attenuation2);
            light.StructureMembers["innerConeAngle"].SetValue(lightSource.InnerConeAngle);
            light.StructureMembers["outerConeAngle"].SetValue(lightSource.OuterConeAngle);
        }

        static private int compareLightSource(LightSource l1, LightSource l2)
        {
            if ((int)l1.Type > (int)l2.Type)
            {
                return 1;
            }
            else if (l1.Type == l2.Type)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }

        private string GetPassName(LightType type)
        {
            switch (type)
            {
                case LightType.Directional:
                    return "DirectionalLight";
               
            }
            return null;
        }

        public override void Dispose()
        {
            base.Dispose();

            lightSources.Clear();
            dirLightSources.Clear();
           
        }
    }
}
