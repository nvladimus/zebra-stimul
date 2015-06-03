using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.IO;
using System.Threading;

namespace BehaveAndScan01
{
    public partial class StimWindow : Form
    {

        float ww = 4f;//1f;//4f;  // size of landscape in x,y
        float hh = 3f;//2f;
        int repx = 3;   // periodicity in x,y  ((if any))
        int repy = 3;
        float h_ = 1f;//1f*1f;         // size of visible area
        float w_ = 4f/3f;//1f*4f / 3f;
        float yPosition, xPosition, orientation;
        StimEphysOscilloscopeControl stimSender = null;

        bool LeftRight = false;


        public StimWindow()
        {
            InitializeComponent();
            if (!InitializeDirect3D())
                return;

            vertices = new VertexBuffer(typeof(CustomVertex.PositionColored), // Type of vertex
               4,      // How many
               device, // What device
               0,      // No special usage
               CustomVertex.PositionColored.Format,
               Pool.Managed);

            vertices2 = new VertexBuffer(typeof(CustomVertex.PositionColored), // Type of vertex
               20,      // How many
               device, // What device
               0,      // No special usage
               CustomVertex.PositionColored.Format,
               Pool.Managed);

            // Load the background texture image
            //           backgroundT = TextureLoader.FromFile(device, "../../../gratingStim.bmp");
           //backgroundT = TextureLoader.FromFile(device, "../../../coloredNoiseStim.bmp");
            //backgroundT = TextureLoader.FromFile(device, "../../../BWbackground2.bmp");
            backgroundT = TextureLoader.FromFile(device, "../../../BWbackground2_white.bmp");
            //backgroundT = TextureLoader.FromFile(device, "../../../apple.bmp");

            // Create a vertex buffer for the background image we will draw
            backgroundV = new VertexBuffer(typeof(CustomVertex.PositionColoredTextured), // Type of vertex
                4,      // How many
                device, // What device
                0,      // No special usage
                CustomVertex.PositionColoredTextured.Format,
                Pool.Managed);

            // Fill the vertex buffer with the corners of a rectangle that covers
            // the entire playing surface.
            GraphicsStream stm = backgroundV.Lock(0, 0, 0);     // Lock the background vertex list
            int clr = System.Drawing.Color.Transparent.ToArgb();
//            stm.Write(new CustomVertex.PositionColoredTextured(-1.33f, -1f, 0, clr, 0, 1));   // here the size of the background
//            stm.Write(new CustomVertex.PositionColoredTextured(-1.33f, 2f, 0, clr, 0, 0));    // bmp is set, also the shape
//            stm.Write(new CustomVertex.PositionColoredTextured(2.67f, 2f, 0, clr, 1, 0));     // so needs to match with the bitmap file
//            stm.Write(new CustomVertex.PositionColoredTextured(2.67f, -1f, 0, clr, 1, 1));
            stm.Write(new CustomVertex .PositionColoredTextured(-ww/3f, -hh/3f, 0, clr, 0, 1));   // here the size of the background
            stm.Write(new CustomVertex.PositionColoredTextured(-ww/3f, hh*2f/3f, 0, clr, 0, 0));    // bmp is set, also the shape
            stm.Write(new CustomVertex.PositionColoredTextured(ww*2f/3f, hh*2f/3f, 0, clr, 1, 0));     // so needs to match with the bitmap file
            stm.Write(new CustomVertex.PositionColoredTextured(ww*2f/3f, -hh/3f, 0, clr, 1, 1));

            backgroundV.Unlock();

            // Determine the last time
            stopwatch.Start();
            lastTime = stopwatch.ElapsedMilliseconds;

            

        }

        private Microsoft.DirectX.Direct3D.Device device = null;
        //private bool fullScreen = false;     // Change to true for full screen


        private VertexBuffer vertices = null;       // Vertex buffer for our drawing
        private VertexBuffer vertices2 = null;       // Vertex buffer for our drawing

        // Background texture management
        private Texture backgroundT = null;         // Background texture map
        private VertexBuffer backgroundV = null;    // Background vertex buffer

   //     private Vector2 playerLoc = new Vector2(1.4f, 1);   // Where our player is

        // Time management
        private long lastTime;                      // What the last time reading was
        private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

        //double gain = 0;
        //double[] threshold = { .1, .1 };



        private bool InitializeDirect3D()
        {
            try
            {
                // Now let's setup our D3D stuff
                PresentParameters presentParams = new PresentParameters();
                presentParams.Windowed = true;
                presentParams.SwapEffect = SwapEffect.Discard;
                device = new Microsoft.DirectX.Direct3D.Device(0, DeviceType.Hardware, this, CreateFlags.SoftwareVertexProcessing, presentParams);
            }
            catch (DirectXException)
            {
                return false;
            }

            return true;
        }



        public void Render(object sender)
        {
           
                stimSender = (StimEphysOscilloscopeControl)sender;
                //MessageBox.Show("WHY AM I WORKING");
                //MessageBox.Show(stimSender.stimState.xPosition.ToString());
            //textBox1.Text = (Int32.Parse(textBox1.Text) + 1).ToString();
               DisplayClear();
                //StimEphysOscilloscopeControl.StimState testStimState = new StimEphysOscilloscopeControl.StimState();
               SetDrawWorld(stimSender.stimState);
                



                if (stimSender.senderWindow.InstStimParams.blob_ON)
                {
                    DrawPredator((float)(-Math.Cos(stimSender.displayAngle) * (stimSender.blob_x + stimSender.senderWindow.InstStimParams.xOffset) - Math.Sin(stimSender.displayAngle) * (stimSender.senderWindow.InstStimParams.yOffset)), (float)(Math.Sin(stimSender.displayAngle) * (stimSender.blob_x + stimSender.senderWindow.InstStimParams.xOffset) - Math.Cos(stimSender.displayAngle) * (stimSender.senderWindow.InstStimParams.yOffset)), .15f);
                }

                if (stimSender.senderWindow.InstStimParams.looming_ON)
                {

                    
                    DrawPredatorCenterLocked((float)(-Math.Cos(stimSender.displayAngle) * (stimSender.blob_x + stimSender.senderWindow.InstStimParams.xOffset) - Math.Sin(stimSender.displayAngle) * (stimSender.blob_y + stimSender.senderWindow.InstStimParams.yOffset)), (float)(Math.Sin(stimSender.displayAngle) * (stimSender.blob_x + stimSender.senderWindow.InstStimParams.xOffset) - Math.Cos(stimSender.displayAngle) * (stimSender.blob_y + stimSender.senderWindow.InstStimParams.yOffset)), stimSender.blobSize, stimSender.stimState);
                    if(stimSender.doubleBlob)
                        DrawPredatorCenterLocked((float)(-Math.Cos(stimSender.displayAngle) * (-stimSender.blob_x + stimSender.senderWindow.InstStimParams.xOffset) - Math.Sin(stimSender.displayAngle) * (stimSender.blob_y + stimSender.senderWindow.InstStimParams.yOffset)), (float)(Math.Sin(stimSender.displayAngle) * (-stimSender.blob_x + stimSender.senderWindow.InstStimParams.xOffset) - Math.Cos(stimSender.displayAngle) * (stimSender.blob_y + stimSender.senderWindow.InstStimParams.yOffset)), stimSender.doubleblobSize, stimSender.stimState);
                }

                if (stimSender.senderWindow.InstStimParams.testCenter_ON)
                    DrawTestCenter((float)(-Math.Cos(stimSender.displayAngle) * (stimSender.senderWindow.InstStimParams.xOffset) - Math.Sin(stimSender.displayAngle) * (stimSender.senderWindow.InstStimParams.yOffset)), (float)(Math.Sin(stimSender.displayAngle) * (stimSender.blob_x + stimSender.senderWindow.InstStimParams.xOffset) - Math.Cos(stimSender.displayAngle) * (stimSender.senderWindow.InstStimParams.yOffset)));

                if (stimSender.senderWindow.InstStimParams.phototax_ON)
                {
                    stimSender.stimState.orientation = 0;
                    DrawLightDark(stimSender.senderWindow.InstStimParams.xOffset, (float)(stimSender.displayAngle), stimSender.photoState);
                }
                if (stimSender.senderWindow.InstStimParams.phototax2_ON)
                {
                    stimSender.stimState.orientation = 0;
                    DrawLightDark(stimSender.senderWindow.InstStimParams.xOffset, (float)(stimSender.displayAngle), stimSender.photoState);
                }


                if (stimSender.senderWindow.InstStimParams.OMRstripes_ON)
                {
                    DrawGrating((float)stimSender.displayAngle, stimSender.stripesY, stimSender.stripesContrast);
                }

                if (stimSender.senderWindow.InstStimParams.OMRstripesLateral_ON)
                {
                    DrawGrating(-(float)Math.PI / 2, stimSender.stripesY, 150);
                }

                if (stimSender.senderWindow.InstStimParams.OMRstripesLongitudinal_ON)
                {
                    DrawGrating(0f, stimSender.stripesY, 150);
                }

                if (stimSender.senderWindow.InstStimParams.acceleration_ON)
                {
                    DrawGrating(0f, stimSender.stripesY, 150);
                }

                if (stimSender.senderWindow.InstStimParams.closedLoop1D_ON)   // display grating - more response probably
                {
                    //!!!!!!!!!!!!!!!! NEED TO FIX THIS !!!!!!!!!!!!!!!!!!!! STOPWATCH
                    //!!!!!!!!!!!!!!!! NEED TO FIX THIS !!!!!!!!!!!!!!!!!!!! STOPWATCH
                    //!!!!!!!!!!!!!!!! NEED TO FIX THIS !!!!!!!!!!!!!!!!!!!! STOPWATCH
                    //!!!!!!!!!!!!!!!! NEED TO FIX THIS !!!!!!!!!!!!!!!!!!!! STOPWATCH
                    //!!!!!!!!!!!!!!!! NEED TO FIX THIS !!!!!!!!!!!!!!!!!!!! STOPWATCH
                    //!!!!!!!!!!!!!!!! NEED TO FIX THIS !!!!!!!!!!!!!!!!!!!! STOPWATCH
                    //!!!!!!!!!!!!!!!! NEED TO FIX THIS !!!!!!!!!!!!!!!!!!!! STOPWATCH
                    //!!!!!!!!!!!!!!!! NEED TO FIX THIS !!!!!!!!!!!!!!!!!!!! STOPWATCH//!!!!!!!!!!!!!!!! NEED TO FIX THIS !!!!!!!!!!!!!!!!!!!! STOPWATCH
                    //!!!!!!!!!!!!!!!! NEED TO FIX THIS !!!!!!!!!!!!!!!!!!!! STOPWATCH//!!!!!!!!!!!!!!!! NEED TO FIX THIS !!!!!!!!!!!!!!!!!!!! STOPWATCH
                    //!!!!!!!!!!!!!!!! NEED TO FIX THIS !!!!!!!!!!!!!!!!!!!! STOPWATCH
                    //stripesY += vel;   
      //              if (stimSender.forceClosedLoopVelSlowBackward == false)
      //              {
                    if (stimSender.LeftRight)
                    {
                        DrawGrating((float)stimSender.displayAngle, stimSender.stripesY, (byte)stimSender.stim1DclosedLoopContrast, stimSender.LR_);//150);//stimState.xPosition, 200);
                    }
                    else
                    {
                        DrawGrating((float)stimSender.displayAngle, stimSender.stripesY, (byte)stimSender.stim1DclosedLoopContrast);//150);//stimState.xPosition, 200);
                    }
              //              }
      //              else
      //              {
      //                  DrawLightDark(stimSender.senderWindow.InstStimParams.xOffset, (float)(stimSender.displayAngle), 3, 0, 50);
      //              }
                }

                //if (stimFixedDist)
                //{
                //    //!!!!!!!!!!!!!!!! NEED TO FIX THIS !!!!!!!!!!!!!!!!!!!! STOPWATCH
                //    //!!!!!!!!!!!!!!!! NEED TO FIX THIS !!!!!!!!!!!!!!!!!!!! STOPWATCH
                //    //!!!!!!!!!!!!!!!! NEED TO FIX THIS !!!!!!!!!!!!!!!!!!!! STOPWATCH
                //    //!!!!!!!!!!!!!!!! NEED TO FIX THIS !!!!!!!!!!!!!!!!!!!! STOPWATCH
                //    //!!!!!!!!!!!!!!!! NEED TO FIX THIS !!!!!!!!!!!!!!!!!!!! STOPWATCH
                //    //!!!!!!!!!!!!!!!! NEED TO FIX THIS !!!!!!!!!!!!!!!!!!!! STOPWATCH
                //    //!!!!!!!!!!!!!!!! NEED TO FIX THIS !!!!!!!!!!!!!!!!!!!! STOPWATCH
                //    //!!!!!!!!!!!!!!!! NEED TO FIX THIS !!!!!!!!!!!!!!!!!!!! STOPWATCH//!!!!!!!!!!!!!!!! NEED TO FIX THIS !!!!!!!!!!!!!!!!!!!! STOPWATCH
                //    //!!!!!!!!!!!!!!!! NEED TO FIX THIS !!!!!!!!!!!!!!!!!!!! STOPWATCH//!!!!!!!!!!!!!!!! NEED TO FIX THIS !!!!!!!!!!!!!!!!!!!! STOPWATCH
                //    //!!!!!!!!!!!!!!!! NEED TO FIX THIS !!!!!!!!!!!!!!!!!!!! STOPWATCH
                //    //stripesY += vel;   // was -= before 8 Dec '10, when I introduced a mirror so velocity needs to be inverted.
                //    DrawGrating((float)stimSender.displayAngle, stimSender.stripesY, 150);//stimState.xPosition, 200);
                //}



                if (stimSender.senderWindow.InstStimParams.RFmap1_ON)
                {
                    drawRFmap1Circle(stimSender.RFx, stimSender.RFy, stimSender.RFR, stimSender.RFbw);
                }

                if (stimSender.senderWindow.InstStimParams.antiphototax_ON)
                {
                    stimSender.stimState.orientation = 0;
                    DrawLightDark(stimSender.senderWindow.InstStimParams.xOffset, (float)(stimSender.displayAngle), stimSender.photoState);
                }

                if (stimSender.senderWindow.InstStimParams.darkStartle_ON)
                    DrawLightDark(stimSender.senderWindow.InstStimParams.xOffset, (float)stimSender.displayAngle, stimSender.photoState);

                Flip();

            
        }

        public void DisplayClear()
        {
            if (device == null)
                return;

//          device.Clear(ClearFlags.Target, System.Drawing.Color.Blue, 1.0f, 0);
            device.Clear(ClearFlags.Target, System.Drawing.Color.Black, 1.0f, 0);

            device.RenderState.ZBufferEnable = false;   // We'll not use this feature
            device.RenderState.Lighting = false;        // Or this one...
            device.RenderState.CullMode = Cull.None;    // Or this one...

            //Begin the scene
            device.BeginScene();

        }

        public void SetDrawWorld(StimEphysOscilloscopeControl.StimState stimState_) //(IAsyncResult ar)
        {
            //            w_ = playingW / 3;  // 2
            //            h_ = playingH / 3;

            yPosition = stimState_.yPosition;
            xPosition = stimState_.xPosition;
            orientation = stimState_.orientation;


            if (stimState_.wrap)
            {
                xPosition = (float)(((xPosition % (ww / repx)) + (ww / repx)) % (ww / repx));
                // visible field defined by 4x3 and has 2 periods in x and y, so need modulo 2 and modulo 1.5
//                if (xPosition >= 0)        // need a modulus type sawtooth for periodicity. Bit clumsy but can't think of anything simpler.
//                    xPosition = (float)((double)xPosition % (ww / repx));
//                else
//                    xPosition = (ww / repx) - (float)(-(double)xPosition % (ww / repx));    // WAIT... isn't this just xPosition % w_ simply?  ...

                yPosition = (float)(((yPosition % (hh / repy)) + (hh / repy)) % (hh / repy));
//                if (yPosition >= 0)        // need a modulus type sawtooth for periodicity. Bit clumsy but can't think of anything simpler.
//                    yPosition = (float)((double)yPosition % (hh / repy));
//                else
//                    yPosition = (hh / repy) - (float)(-(double)yPosition % (hh / repy));
            }

            device.Transform.World = Matrix.Translation(xPosition, yPosition, 0) *     // translation
                                     Matrix.Translation(-(w_ / 2), -(h_ / 2), 0) *     // rotation about center of screen
                                     Matrix.RotationZ(orientation) *                   // c'd
                                     Matrix.Translation((w_ / 2), (h_ / 2), 0);        // c'd

            device.SetTexture(0, backgroundT);
            device.SetStreamSource(0, backgroundV, 0);
            device.VertexFormat = CustomVertex.PositionColoredTextured.Format;
            device.DrawPrimitives(PrimitiveType.TriangleFan, 0, 2);
            device.SetTexture(0, null);

        }

        public void DrawPredator(float xPos, float yPos, float size_)
        {

            //drawCircle(vertices2, device, xPos + .2f * (float)Math.Sin(Math.PI * 0.63), yPos + 1f * h_ + .2f * (float)Math.Cos(Math.PI * 0.63), .1f, 0);
            drawCircle(vertices2, device, xPos + w_ / 2, yPos + h_ / 2, size_, 0);
            //           drawCircle(vertices2, device, xPos + w_ / 2, yPos + h_ / 2, .10f, 90); //90
            //            drawCircle(vertices2, device, xPos + w_ / 2, yPos + h_ / 2, .06f, 0);
            //            drawCircle(vertices2, device, xPos + w_ / 2, yPos + h_ / 2, .04f, 90);
            //            drawCircle(vertices2, device, xPos + w_ / 2, yPos + h_ / 2, .02f, 0);
        }

        public void DrawPredatorCenterLocked(float xPos, float yPos, float size_, StimEphysOscilloscopeControl.StimState stimState_)
        {

            float yPosition = stimState_.yPosition;
            float xPosition = stimState_.xPosition;
            float orientation = stimState_.orientation;


            if (stimState_.wrap)
            {
                xPosition = (float)(((xPosition % (ww / repx)) + (ww / repx)) % (ww / repx));

                yPosition = (float)(((yPosition % (hh / repy)) + (hh / repy)) % (hh / repy));
            }
            drawCircle(vertices2, device, xPos + w_ / 2 - xPosition, yPos + h_ / 2 - yPosition, size_, 0);
        }

        public void DrawGrating(float displayAngle, float yPos, byte contrast)
        {
            //MessageBox.Show("I'm here");

            float W = 3f;
//            float dH = 0.15f/1.6f;  // /1.6f since bigger screen
            float dH = 0.15f / 2.2f;  // /1.6f since bigger screen

            yPos = yPos % (2 * dH);

            for (float H = -1f + yPos - 1f; H < 1 + yPos; H += 2 * dH)
            {
                float[] xx = {
                          (float)(Math.Cos(displayAngle)*(-W) + Math.Sin(displayAngle)*H),
                          (float)(Math.Cos(displayAngle)*W + Math.Sin(displayAngle)*H),
                          (float)(Math.Cos(displayAngle)*W + Math.Sin(displayAngle)*(H+dH)),
                          (float)(Math.Cos(displayAngle)*(-W) + Math.Sin(displayAngle)*(H+dH)),
                          };

                float[] yy = {
                          (float)(-Math.Sin(displayAngle)*(-W) + Math.Cos(displayAngle)*H),
                          (float)(-Math.Sin(displayAngle)*W + Math.Cos(displayAngle)*H),
                          (float)(-Math.Sin(displayAngle)*W + Math.Cos(displayAngle)*(H+dH)),
                          (float)(-Math.Sin(displayAngle)*(-W) + Math.Cos(displayAngle)*(H+dH)),
                          };

                if (contrast > 0)
                {
                    if (contrast < 50)
                        drawPoly(vertices2, device, xx, yy, 50 - contrast);//127 - contrast);
                    else
                        drawPoly(vertices2, device, xx, yy, 0);//127 - contrast);
                }
                else
                {
                    drawPoly(vertices2, device, xx, yy, 255);
                }

                float[] xx1 = {
                          (float)(Math.Cos(displayAngle)*(-W) + Math.Sin(displayAngle)*(H+dH)),
                          (float)(Math.Cos(displayAngle)*W + Math.Sin(displayAngle)*(H+dH)),
                          (float)(Math.Cos(displayAngle)*W + Math.Sin(displayAngle)*(H+2*dH)),
                          (float)(Math.Cos(displayAngle)*(-W) + Math.Sin(displayAngle)*(H+2*dH)),
                          };

                float[] yy1 = {
                          (float)(-Math.Sin(displayAngle)*(-W) + Math.Cos(displayAngle)*(H+dH)),
                          (float)(-Math.Sin(displayAngle)*W + Math.Cos(displayAngle)*(H+dH)),
                          (float)(-Math.Sin(displayAngle)*W + Math.Cos(displayAngle)*(H+2*dH)),
                          (float)(-Math.Sin(displayAngle)*(-W) + Math.Cos(displayAngle)*(H+2*dH)),
                          };

                if (contrast > 0)
                    drawPoly(vertices2, device, xx1, yy1, 50 + contrast);
                else
                {
                    drawPoly(vertices2, device, xx, yy, 255);
                }

            }
        }

        public void DrawGrating(float displayAngle, float yPos, byte contrast, int LR__)
        {
            //MessageBox.Show("I'm here");

            float xPos = 0f;

            float W = 3f;
            //            float dH = 0.15f/1.6f;  // /1.6f since bigger screen
            float dH = 0.15f / 2.2f;  // /1.6f since bigger screen

            yPos = yPos % (2 * dH);

            for (float H = -1f + yPos - 1f; H < 1 + yPos; H += 2 * dH)
            {
                float[] xx = {
                          (float)(Math.Cos(displayAngle)*(-W) + Math.Sin(displayAngle)*H),
                          (float)(Math.Cos(displayAngle)*W + Math.Sin(displayAngle)*H),
                          (float)(Math.Cos(displayAngle)*W + Math.Sin(displayAngle)*(H+dH)),
                          (float)(Math.Cos(displayAngle)*(-W) + Math.Sin(displayAngle)*(H+dH)),
                          };

                float[] yy = {
                          (float)(-Math.Sin(displayAngle)*(-W) + Math.Cos(displayAngle)*H),
                          (float)(-Math.Sin(displayAngle)*W + Math.Cos(displayAngle)*H),
                          (float)(-Math.Sin(displayAngle)*W + Math.Cos(displayAngle)*(H+dH)),
                          (float)(-Math.Sin(displayAngle)*(-W) + Math.Cos(displayAngle)*(H+dH)),
                          };

                if (contrast > 0)
                {
                    if (contrast < 50)
                        drawPoly(vertices2, device, xx, yy, 50 - contrast);//127 - contrast);
                    else
                        drawPoly(vertices2, device, xx, yy, 0);//127 - contrast);
                }
                else
                {
                    drawPoly(vertices2, device, xx, yy, 255);
                }

                float[] xx1 = {
                          (float)(Math.Cos(displayAngle)*(-W) + Math.Sin(displayAngle)*(H+dH)),
                          (float)(Math.Cos(displayAngle)*W + Math.Sin(displayAngle)*(H+dH)),
                          (float)(Math.Cos(displayAngle)*W + Math.Sin(displayAngle)*(H+2*dH)),
                          (float)(Math.Cos(displayAngle)*(-W) + Math.Sin(displayAngle)*(H+2*dH)),
                          };

                float[] yy1 = {
                          (float)(-Math.Sin(displayAngle)*(-W) + Math.Cos(displayAngle)*(H+dH)),
                          (float)(-Math.Sin(displayAngle)*W + Math.Cos(displayAngle)*(H+dH)),
                          (float)(-Math.Sin(displayAngle)*W + Math.Cos(displayAngle)*(H+2*dH)),
                          (float)(-Math.Sin(displayAngle)*(-W) + Math.Cos(displayAngle)*(H+2*dH)),
                          };

                if (contrast > 0)
                    drawPoly(vertices2, device, xx1, yy1, 50 + contrast);
                else
                {
                    drawPoly(vertices2, device, xx, yy, 255);
                }

            }

            int dark = 0;

            if (LR__ == 1)
            {
                float[] xx =  {-5, 
                              (float)(w_/2 - Math.Cos(displayAngle)*xPos + Math.Sin(displayAngle)*5),
                              (float)(w_/2 - Math.Cos(displayAngle)*xPos - Math.Sin(displayAngle)*5),
                              -5};

                float[] yy =  {5, 
                              (float)(h_/2 + Math.Sin(displayAngle)*xPos + Math.Cos(displayAngle)*5),
                              (float)(h_/2 + Math.Sin(displayAngle)*xPos - Math.Cos(displayAngle)*5),
                              -5};

                drawPoly(vertices2, device, xx, yy, dark);
            }
            else if (LR__ == 0)
            {
                float[] xx =  {5, 
                              (float)(w_/2 - Math.Cos(displayAngle)*xPos + Math.Sin(displayAngle)*5),
                              (float)(w_/2 - Math.Cos(displayAngle)*xPos - Math.Sin(displayAngle)*5),
                              5};

                float[] yy =  {5, 
                              (float)(h_/2 + Math.Sin(displayAngle)*xPos + Math.Cos(displayAngle)*5),
                              (float)(h_/2 + Math.Sin(displayAngle)*xPos - Math.Cos(displayAngle)*5),
                              -5};

                drawPoly(vertices2, device, xx, yy, dark);
            }
            else if (LR__ == 2)
            {
                float[] xx =  {-5, 
                              (float)(w_/2 - Math.Cos(displayAngle)*xPos + Math.Sin(displayAngle)*5),
                              (float)(w_/2 - Math.Cos(displayAngle)*xPos - Math.Sin(displayAngle)*5),
                              5};

                float[] yy =  {5, 
                              (float)(h_/2 + Math.Sin(displayAngle)*xPos + Math.Cos(displayAngle)*5),
                              (float)(h_/2 + Math.Sin(displayAngle)*xPos - Math.Cos(displayAngle)*5),
                              -5};

                drawPoly(vertices2, device, xx, yy, dark);
            }
        }

        public void DrawTestCenter(float xPos, float yPos)
        {
            int dark = 0;
            int bright = 255;
            // xPos + w_ / 2, yPos + h_ / 2
                float[] xx1 = { -5, 5, 5, -5 };
                float[] yy1 = { 5, 5, -5, -5 };
                //                drawPoly(vertices2, device, xx1, yy1, 200);
                drawPoly(vertices2, device, xx1, yy1, bright);

                float[] xx =  {-5, 
                              (float)(xPos + w_/2),
                              (float)(xPos + w_/2),
                              -5};

                float[] yy =  {5, 
                              5,
                              (float)(yPos + h_/2),
                              (float)(yPos + h_/2)};

                drawPoly(vertices2, device, xx, yy, dark);


                float[] xx2 =  {5,
                              (float)(xPos + w_/2),
                              (float)(xPos + w_/2),
                              5};

                float[] yy2 =  {-5, 
                               -5,
                              (float)(yPos + h_/2),
                              (float)(yPos + h_/2)};

                drawPoly(vertices2, device, xx2, yy2, dark);

        }



        public void DrawLightDark(float xPos, float displayAngle, int mode)
        {
            int dark = 0;
            int bright = 255;

            if (mode == 1)          // dark light
            {
                float[] xx1 = { -5, 5, 5, -5 };
                float[] yy1 = { 5, 5, -5, -5 };
                //                drawPoly(vertices2, device, xx1, yy1, 200);
                drawPoly(vertices2, device, xx1, yy1, bright);

                float[] xx =  {-5, 
                              (float)(w_/2 - Math.Cos(displayAngle)*xPos + Math.Sin(displayAngle)*5),
                              (float)(w_/2 - Math.Cos(displayAngle)*xPos - Math.Sin(displayAngle)*5),
                              -5};

                float[] yy =  {5, 
                              (float)(h_/2 + Math.Sin(displayAngle)*xPos + Math.Cos(displayAngle)*5),
                              (float)(h_/2 + Math.Sin(displayAngle)*xPos - Math.Cos(displayAngle)*5),
                              -5};

                drawPoly(vertices2, device, xx, yy, dark);
            }
            else if (mode == 2)
            {
                float[] xx1 = { -5, 5, 5, -5 };
                float[] yy1 = { 5, 5, -5, -5 };
                drawPoly(vertices2, device, xx1, yy1, dark);

                float[] xx =  {-5, 
                              (float)(w_/2 - Math.Cos(displayAngle)*xPos + Math.Sin(displayAngle)*5),
                              (float)(w_/2 - Math.Cos(displayAngle)*xPos - Math.Sin(displayAngle)*5),
                              -5};

                float[] yy =  {5, 
                              (float)(h_/2 + Math.Sin(displayAngle)*xPos + Math.Cos(displayAngle)*5),
                              (float)(h_/2 + Math.Sin(displayAngle)*xPos - Math.Cos(displayAngle)*5),
                              -5};

                //                drawPoly(vertices2, device, xx, yy, 200);
                drawPoly(vertices2, device, xx, yy, bright);
            }
            else if (mode == 3)
            {
                float[] xx1 = { -5, 5, 5, -5 };
                float[] yy1 = { 5, 5, -5, -5 };
                //                drawPoly(vertices2, device, xx1, yy1, 200);
                drawPoly(vertices2, device, xx1, yy1, bright);
            }
            else if (mode == 4)
            {
                float[] xx1 = { -5, 5, 5, -5 };
                float[] yy1 = { 5, 5, -5, -5 };
                drawPoly(vertices2, device, xx1, yy1, dark);
            }
        }


        public void DrawLightDark(float xPos, float displayAngle, int mode, int dark, int bright)
        {
//            int dark = 0;
 //           int bright = 255;

            if (mode == 1)          // dark light
            {
                float[] xx1 = { -5, 5, 5, -5 };
                float[] yy1 = { 5, 5, -5, -5 };
                //                drawPoly(vertices2, device, xx1, yy1, 200);
                drawPoly(vertices2, device, xx1, yy1, bright);

                float[] xx =  {-5, 
                              (float)(w_/2 - Math.Cos(displayAngle)*xPos + Math.Sin(displayAngle)*5),
                              (float)(w_/2 - Math.Cos(displayAngle)*xPos - Math.Sin(displayAngle)*5),
                              -5};

                float[] yy =  {5, 
                              (float)(h_/2 + Math.Sin(displayAngle)*xPos + Math.Cos(displayAngle)*5),
                              (float)(h_/2 + Math.Sin(displayAngle)*xPos - Math.Cos(displayAngle)*5),
                              -5};

                drawPoly(vertices2, device, xx, yy, dark);
            }
            else if (mode == 2)
            {
                float[] xx1 = { -5, 5, 5, -5 };
                float[] yy1 = { 5, 5, -5, -5 };
                drawPoly(vertices2, device, xx1, yy1, dark);

                float[] xx =  {-5, 
                              (float)(w_/2 - Math.Cos(displayAngle)*xPos + Math.Sin(displayAngle)*5),
                              (float)(w_/2 - Math.Cos(displayAngle)*xPos - Math.Sin(displayAngle)*5),
                              -5};

                float[] yy =  {5, 
                              (float)(h_/2 + Math.Sin(displayAngle)*xPos + Math.Cos(displayAngle)*5),
                              (float)(h_/2 + Math.Sin(displayAngle)*xPos - Math.Cos(displayAngle)*5),
                              -5};

                //                drawPoly(vertices2, device, xx, yy, 200);
                drawPoly(vertices2, device, xx, yy, bright);
            }
            else if (mode == 3)
            {
                float[] xx1 = { -5, 5, 5, -5 };
                float[] yy1 = { 5, 5, -5, -5 };
                //                drawPoly(vertices2, device, xx1, yy1, 200);
                drawPoly(vertices2, device, xx1, yy1, bright);
            }
            else if (mode == 4)
            {
                float[] xx1 = { -5, 5, 5, -5 };
                float[] yy1 = { 5, 5, -5, -5 };
                drawPoly(vertices2, device, xx1, yy1, dark);
            }
        }


        public void Flip()
        {
            // set the "camera"
            device.Transform.Projection = Matrix.OrthoOffCenterLH(0,    // size of the visible area is determined here
                                                                  w_,
                                                                  0,
                                                                  h_,
                                                                  0, 1);
            //End the scene
            device.EndScene();

            device.Present();
        }

        public void drawCircle(VertexBuffer vertices2, Microsoft.DirectX.Direct3D.Device device, float centerX, float centerY, float radius, int grayCol)
        {
            GraphicsStream gs2 = vertices2.Lock(0, 0, 0);     // Lock the vertex list
            int clr = Color.FromArgb(grayCol, grayCol, grayCol).ToArgb();

            for (int a = 0; a < 20; a++)
            {
                gs2.Write(new CustomVertex.PositionColored(centerX + (float)(radius * Math.Cos(2 * a * Math.PI / 20)), centerY + (float)(radius * Math.Sin(2.0 * a * Math.PI / 20)), 0, clr));
            }

            vertices2.Unlock();

            device.SetStreamSource(0, vertices2, 0);
            device.VertexFormat = CustomVertex.PositionColored.Format;
            device.DrawPrimitives(PrimitiveType.TriangleFan, 0, 18);
        }

        public void drawRFmap1Circle(double RFx, double RFy, double RFR, int RFbw)
        {
            drawCircle(vertices2, device, (float)RFx + w_ / 2, (float)RFy + h_ / 2, (float)6, (1-RFbw) * 255);
            drawCircle(vertices2, device, (float)RFx + w_ / 2, (float)RFy + h_ / 2, (float)RFR, RFbw * 255);
        }

        public void drawPoly(VertexBuffer vertices2, Microsoft.DirectX.Direct3D.Device device, float[] x, float[] y, int grayCol)
        {
            GraphicsStream gs2 = vertices2.Lock(0, 0, 0);     // Lock the vertex list
            int clr = Color.FromArgb(grayCol, grayCol, grayCol).ToArgb();

            for (int i = 0; i < x.Length; i++)
            {
                gs2.Write(new CustomVertex.PositionColored(x[i], y[i], 0, clr));
            }

            vertices2.Unlock();

            device.SetStreamSource(0, vertices2, 0);
            device.VertexFormat = CustomVertex.PositionColored.Format;
            device.DrawPrimitives(PrimitiveType.TriangleFan, 0, x.Length - 2);
        }
        
    }
}
