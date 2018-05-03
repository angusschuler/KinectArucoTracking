using System;
using System.Collections.Generic;
using Emgu.CV;
using KinectArucoTracking.Controls;
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

        private List<Component> _gameComponents;

        public KinectArucoTracking()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferHeight = 1080;
            graphics.PreferredBackBufferWidth = 1920;
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {

            IsMouseVisible = true;

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

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            capture = new FormVideoCapture(background, GraphicsDevice);

            while (capture.getCapture() == null)
            {

            }

            var button = new Button(Content.Load<Texture2D>("Controls/Button"), Content.Load<SpriteFont>("Fonts/font"))
            {
                Position = new Vector2(5, 5),
                Text = "Calibrate",
                PenColor = Color.Black
            };

            button.Click += Button_Click;

            _gameComponents = new List<Component>()
            {
                button
            };
            
            background = new Texture2D(GraphicsDevice, capture.getCapture().Width, capture.getCapture().Height); // Size of Kinect Stream 1920x1080. Graphics Viewport is 800x640

            capture.SetTexture(background);

            mainFrame = new Microsoft.Xna.Framework.Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            capture.startCapture();
        }

        private void Button_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Clicked");
            capture.calibrateCamera();
        }


        protected override void Update(GameTime gameTime)
        {

            if (orbit)
            {
                //                Matrix rotationMatrix = Matrix.CreateRotationY(
                //                                        MathHelper.ToRadians(1f));
                Mat rvec = capture.getRvecs().Row(0);
                float[] values = new float[3];
                rvec.CopyTo(values);

                Console.WriteLine(String.Format("Rotation: x:{0} y:{1} z:{2}", values[0], values[1], values[2]));
                Matrix rotationMatrix = Matrix.CreateFromYawPitchRoll(MathHelper.ToRadians(values[1]), MathHelper.ToRadians(values[0]), MathHelper.ToRadians(values[2]));//values[0], values[1], values[2]);
                camPosition = Vector3.Transform(camPosition,
                    rotationMatrix);
            }

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

            

            viewMatrix = Matrix.CreateLookAt(camPosition, camTarget,
                         Vector3.Up);

            foreach (var component in _gameComponents)
            {
                component.Update(gameTime);
            }

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

            foreach (var component in _gameComponents)
            {
                component.Draw(gameTime, spriteBatch);
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