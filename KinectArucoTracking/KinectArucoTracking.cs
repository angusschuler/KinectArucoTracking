using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace KinectArucoTracking
{

    public class KinectArucoTracking : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        //Camera
        Vector3 camTarget;
        Vector3 camPosition;
        Matrix projectionMatrix;
        Matrix viewMatrix;
        Matrix worldMatrix;

        //Geometric info
        Model model;

        Texture2D background;
        Microsoft.Xna.Framework.Rectangle mainFrame;

        //Orbit
        bool orbit = false;

        FormVideoCapture capture;

        public KinectArucoTracking()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferHeight = 1080;
            graphics.PreferredBackBufferWidth = 1920;
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            base.Initialize();

            //Setup Camera
            camTarget = new Vector3(0f, 0f, 0f);
            camPosition = new Vector3(0f, 0f, -5);
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                               MathHelper.ToRadians(45f), graphics.
                               GraphicsDevice.Viewport.AspectRatio,
                1f, 1000f);
            viewMatrix = Matrix.CreateLookAt(camPosition, camTarget,
                         new Vector3(0f, 1f, 0f));// Y up
            worldMatrix = Matrix.CreateWorld(camTarget, Vector3.
                          Forward, Vector3.Up);

            model = Content.Load<Model>("MonoCube");

            
        }        

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            capture = new FormVideoCapture(background, GraphicsDevice);

            while (capture.getCapture() == null)
            {

            }
            
            background = new Texture2D(GraphicsDevice, capture.getCapture().Width, capture.getCapture().Height); // Size of Kinect Stream 1920x1080. Graphics Viewport is 800x640

            capture.SetTexture(background);

            mainFrame = new Microsoft.Xna.Framework.Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            capture.startCapture();
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back ==
                ButtonState.Pressed || Keyboard.GetState().IsKeyDown(
                Keys.Escape))
                Exit();

            if (Keyboard.GetState().IsKeyDown(Keys.Left))
            {
                camPosition.X -= 0.1f;
                camTarget.X -= 0.1f;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.Right))
            {
                camPosition.X += 0.1f;
                camTarget.X += 0.1f;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.Up))
            {
                camPosition.Y -= 0.1f;
                camTarget.Y -= 0.1f;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.Down))
            {
                camPosition.Y += 0.1f;
                camTarget.Y += 0.1f;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.OemPlus))
            {
                camPosition.Z += 0.1f;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.OemMinus))
            {
                camPosition.Z -= 0.1f;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.Space))
            {
                orbit = !orbit;
            }

            if (orbit)
            {
                Matrix rotationMatrix = Matrix.CreateRotationY(
                                        MathHelper.ToRadians(1f));
                camPosition = Vector3.Transform(camPosition,
                              rotationMatrix);
            }
            viewMatrix = Matrix.CreateLookAt(camPosition, camTarget,
                         Vector3.Up);

            graphics.ApplyChanges();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.White);
            
            spriteBatch.Begin();
            background = capture.getBackground();
            if (background != null)
            {
                spriteBatch.Draw(background, mainFrame, Microsoft.Xna.Framework.Color.White);
            }
            spriteBatch.End();


            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    //effect.EnableDefaultLighting();
                    //effect.AmbientLightColor = new Vector3(1f, 0, 0);
                    effect.View = viewMatrix;
                    effect.World = worldMatrix;
                    effect.Projection = projectionMatrix;
                }
                mesh.Draw();
            }
            base.Draw(gameTime);
        }       
    }
}