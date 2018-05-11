using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private float sizeHalf = 250 / 2;
        private Vector3[] glyphModel;

        FormVideoCapture capture;

        private List<Component> _gameComponents;

        private Stopwatch timer = new Stopwatch();
        private Matrix past_translation = Matrix.CreateTranslation(0, 0, 0);

        public KinectArucoTracking()
        {
            graphics = new GraphicsDeviceManager(this);
//            graphics.PreferredBackBufferHeight = 1080;
//            graphics.PreferredBackBufferWidth = 1920;
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            base.Initialize();

            timer.Start();

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

            glyphModel = new Vector3[]
            {
                new Vector3( -sizeHalf, 0,  sizeHalf ),
                new Vector3(  sizeHalf, 0,  sizeHalf ),
                new Vector3(  sizeHalf, 0, -sizeHalf ),
                new Vector3( -sizeHalf, 0, -sizeHalf ),
            };
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

            _gameComponents = new List<Component>()
            {
                button
            };
            
            background = new Texture2D(GraphicsDevice, capture.getCapture().Width, capture.getCapture().Height); // Size of Kinect Stream 1920x1080. Graphics Viewport is 800x640

            capture.SetTexture(background);

            mainFrame = new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            capture.startCapture();
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back ==
                ButtonState.Pressed || Keyboard.GetState().IsKeyDown(
                Keys.Escape))
                Exit();

            if (Keyboard.GetState().IsKeyDown(Keys.Tab))
                Console.WriteLine("Clicked");
                capture.calibrateCamera();
           
            foreach (var component in _gameComponents)
            {
                component.Update(gameTime);
            }

            graphics.ApplyChanges();
            base.Update(gameTime);
        }


        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);
            
            spriteBatch.Begin();
            background = capture.getBackground();
            if (background != null)
            {
                spriteBatch.Draw(background, mainFrame, Microsoft.Xna.Framework.Color.White);
            }

            foreach (var component in _gameComponents)
            {
//                component.Draw(gameTime, spriteBatch);
            }

            spriteBatch.End();

            foreach (ModelMesh mesh in model.Meshes)
            {
                float time = (float) timer.Elapsed.TotalSeconds;

                Matrix rotation = Matrix.CreateRotationY(time * 0.7f);
                Matrix translation = Matrix.CreateTranslation(0, 0, 0);

                if (capture != null)
                {
                    Mat rvec = capture.getRvecs();
                    Mat tvec = capture.getTvecs();
                    float[] r_values = new float[6];
                    float[] t_values = new float[6];

                    if (!rvec.IsEmpty && !tvec.IsEmpty)
                    {
                        rvec.Row(0).CopyTo(r_values);
                        tvec.Row(0).CopyTo(t_values);

//                        rotation = Matrix.CreateRotationX(r_values[0])
//                                   * Matrix.CreateRotationY(r_values[1])
//                                   * Matrix.CreateRotationZ(r_values[2]);
                        translation = Matrix.CreateTranslation(t_values[0], t_values[1], -t_values[2]);
                    }
                }

                camTarget = new Vector3(0f, 0f, 0f);
                camPosition = new Vector3(0f, 0f, -10);
               
                // create transform matrices
                viewMatrix = Matrix.CreateLookAt(camPosition, camTarget,
                    new Vector3(0f, 1f, 0f));// Y up


                projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                    MathHelper.ToRadians(45f), graphics.
                        GraphicsDevice.Viewport.AspectRatio,
                    1f, 1000f);


                Matrix scaling = Matrix.CreateScale(sizeHalf * 2);

//                worldMatrix = Matrix.CreateScale(1 / mesh.BoundingSphere.Radius) *
//                              scaling * rotation * translation;
               

                worldMatrix = 
                    Matrix.CreateWorld(camTarget, Vector3.
                    Forward, Vector3.Up) *
//                              scaling *
                              rotation *
                              (translation * past_translation);

                past_translation = translation * -Matrix.Identity;

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