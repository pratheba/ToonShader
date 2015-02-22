using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using GoblinXNA;
using GoblinXNA.Graphics;
using GoblinXNA.SceneGraph;
using Model = GoblinXNA.Graphics.Model;
using GoblinXNA.Graphics.Geometry;
using GoblinXNA.Device.Generic;
using GoblinXNA.UI.UI2D;



namespace GoblinToonShader
{
    public class ToonShaderClass : Microsoft.Xna.Framework.Game
    {
        // Goblin Scene
        Scene scene;

        private Camera camera;
 
        // Texture setting for the Object
        Random random = new Random();
        GraphicsDeviceManager graphics;
        ToonInterfaceImplClass toonImplClass_obj;
        private SpriteFont textFont;
        private TimeSpan timeToNextJitter;
        GeometryNode shipNode;
        private double shipAngle;
   
        // Constructor
        public ToonShaderClass()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
           
            graphics.PreferredBackBufferWidth = 1024;
            graphics.PreferredBackBufferHeight = 780;
           
        }

        // Initialize objects
        protected override void Initialize()
        {
            base.Initialize();
            scene = new Scene();
            scene.BackgroundColor = Color.Black;
            scene.EnablePostProcessing = true;

            CreateLights();
            CreateObject();
            CreateCamera();

            KeyboardInput.Instance.KeyPressEvent += new HandleKeyPress(input_KeyPressHandleFunction);

        }

        private void input_KeyPressHandleFunction(Keys key,KeyModifier keyModifier)
        {
            if (key == Keys.A)
            {
                toonImplClass_obj.toonPostProcessShader.ChangeEffect();
            }
        }
        



        protected override void LoadContent()
        {
           toonImplClass_obj = new ToonInterfaceImplClass(ref graphics, Content);
           (toonImplClass_obj.toonShader).toonPPShader = toonImplClass_obj.toonPostProcessShader;
        }

        

        private void CreateLights()
        {    
            {
                LightSource lightSource = new LightSource();
                lightSource.Direction = new Vector3(1.0f, -1.0f, -1.0f);
                lightSource.Direction.Normalize();
                lightSource.Diffuse = Color.Red.ToVector4();
                

                // Create a light node to hold the light source
                LightNode lightNode = new LightNode();
                lightNode.LightSource = lightSource;
                lightNode.AmbientLightColor = new Vector4(0.1f, 0.1f, 0.1f, 1);
                lightNode.Name = "lightNode";


                // Use a transform node to control the light
                TransformNode dirLightTrans = new TransformNode();
                dirLightTrans.AddChild(lightNode);
                dirLightTrans.Name = "dirLightTrans";

                scene.RootNode.AddChild(dirLightTrans);
            }

            
        }

        private void CreateCamera()
        {
            // Create a camera 
            camera = new Camera();

            float aspectRatio = toonImplClass_obj.device.Viewport.AspectRatio;
            float fov = MathHelper.PiOver4;
            Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4,
                toonImplClass_obj.device.Viewport.AspectRatio, 1.0f, 300.0f);


            camera.Projection = projection;
            camera.AspectRatio = aspectRatio;

            camera.View = Matrix.CreateLookAt(new Vector3(60, 100, -80), Vector3.Zero, Vector3.UnitY);

            camera.FieldOfViewY = fov;
            camera.ZNearPlane = 1.0f;
            camera.ZFarPlane = 300.0f;
            

            // Now assign this camera to a camera node, and add this camera node to our scene graph
            CameraNode cameraNode = new CameraNode(camera);
            cameraNode.Name = "cameraNode";

            scene.RootNode.AddChild(cameraNode);

            // Assign the camera node to be our scene graph's current camera node
            scene.CameraNode = cameraNode;
        }

        private void CreateObject()
        {
                       
            // Loads a textured model of a ship
            ModelLoader loader = new ModelLoader();
            Model shipModel = (Model)loader.Load("", "Ship"); 
 
            // Create a geometry node of a loaded ship model
            shipNode = new GeometryNode("Ship");
           
            shipNode.Model = shipModel;
            shipNode.Model.Shader = toonImplClass_obj.toonShader;
            
            ((Model)shipNode.Model).UseNonPhotoRealisticRendering = true;
            ((Model)shipNode.Model).ReRenderModel = true;
            ((Model)shipNode.Model).PostProcessShader = toonImplClass_obj.toonPostProcessShader;
            ((Model)shipNode.Model).UseInternalMaterials = true;
            

            // Create a transform node to define the transformation for the ship
            TransformNode shipTransNode = new TransformNode();
            shipTransNode.Translation = new Vector3(0, 5, -12);
            shipTransNode.Scale = new Vector3(0.02f, 0.02f, 0.02f); // It's huge!
            shipTransNode.Rotation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0),
                MathHelper.ToRadians(90));
            shipTransNode.Name = "shipTransNode";

            shipTransParentNode = new TransformNode();
            shipTransParentNode.Translation = Vector3.Zero;
            shipTransParentNode.Name = "shipTransParentNode";

            // Now add the above nodes to the scene graph in appropriate order
            scene.RootNode.AddChild(shipTransParentNode);
            shipTransParentNode.AddChild(shipTransNode);
            shipTransNode.AddChild(shipNode);
            
        }

        protected override void Dispose(bool disposing)
        {
            scene.Dispose();
        }

        protected override void Update(GameTime gameTime)
        {
            UpdateControl(gameTime);

            shipAngle += gameTime.ElapsedGameTime.TotalSeconds;
            // Rotate the ship model about the origin along Z axis
            shipTransParentNode.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY,
                (float)shipAngle);

            // Update the sketch overlay texture jitter animation.
            if (toonImplClass_obj.toonPostProcessShader.Settings.sketchJitterSpeed > 0)
            {
                timeToNextJitter -= gameTime.ElapsedGameTime;

                if (timeToNextJitter <= TimeSpan.Zero)
                {
                    toonImplClass_obj.toonPostProcessShader.Sketch_Jitter = new Vector2((float)random.NextDouble(), (float)random.NextDouble());
                    timeToNextJitter += TimeSpan.FromSeconds(toonImplClass_obj.toonPostProcessShader.Settings.SketchJitterSpeed);
                }
            }

            base.Update(gameTime);

            scene.Update(gameTime.ElapsedGameTime, gameTime.IsRunningSlowly, this.IsActive);          
        }

       

        protected void UpdateControl(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            scene.Draw(gameTime.ElapsedGameTime, gameTime.IsRunningSlowly);
        }

        public TransformNode shipTransParentNode { get; set; }
    }
}

