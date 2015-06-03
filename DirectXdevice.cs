using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace Stimulus
{
    public class DirectXdevice
    {
        public Microsoft.DirectX.Direct3D.Device device;
        private PresentParameters presentParams;
        public Texture texture;
        private VertexBuffer vertBuff;
        int nTilesX = 5;
        int nTilesY = 5;
        public float moveX = 0.0f, moveY = 0.0f, shiftA = 0f;
        public bool deviceLost = false;
        public float zoom = 1.0F;
        public DirectXdevice(System.Windows.Forms.Control panel)
        {
            presentParams = new PresentParameters();
            presentParams.Windowed = true;
            presentParams.SwapEffect = SwapEffect.Discard;
            device = new Microsoft.DirectX.Direct3D.Device(0, Microsoft.DirectX.Direct3D.DeviceType.Hardware, panel, CreateFlags.SoftwareVertexProcessing, presentParams);
            try
            {
                texture = TextureLoader.FromFile(device, Application.StartupPath + "\\default_stimulus.bmp");
            }
            catch
            {
                MessageBox.Show("File 'default_stimulus.bmp' not found");
            }
            SetupDevice();
            device.DeviceResizing += new System.ComponentModel.CancelEventHandler(this.CancelResize);
            device.DeviceReset += new EventHandler(this.OnDeviceReset);
            device.DeviceLost += new EventHandler(this.OnDeviceLost); 
        }

        private void SetupDevice()
        {
            device.RenderState.CullMode = Cull.None;
            device.RenderState.Lighting = false;
            device.SetTexture(0, texture);
            device.Transform.View = Matrix.LookAtLH(new Vector3(0, 0, -1), new Vector3(), new Vector3(0, 1, 0));
            device.Transform.Projection = Matrix.PerspectiveFovLH((float)Math.PI / (2.0F * zoom), 1.0F, -0.1F, 0.1F);
            vertBuff = CreateVertexBufferPlane(device);
        }
        public void SetTexture(Texture txtr)
        {
            device.SetTexture(0, txtr);
        }
        protected void OnDeviceLost(object sender, EventArgs e)
        {
            // Clean up the VertexBuffer
            vertBuff.Dispose();
        }

        protected void OnDeviceReset(object sender, EventArgs e)
        {
            // We use the same setup code to reset as we do for initial creation
            SetupDevice();
        }
        protected void CancelResize(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }
        protected VertexBuffer CreateVertexBufferPlane(Microsoft.DirectX.Direct3D.Device device)
        {
            int nTriangles = 2;
            int nTiles = nTilesX * nTilesY;
            VertexBuffer buf = new VertexBuffer(typeof(CustomVertex.PositionTextured), nTriangles * 3 * nTiles, device, 0,
                                CustomVertex.PositionTextured.Format, Pool.Default);

            CustomVertex.PositionTextured[] verts = (CustomVertex.PositionTextured[])buf.Lock(0, 0);

            float lowLeftX;// = -1.0f;
            float lowLeftY;// = -1.0f;
            float width = 2; //screen coordinates range from -1 to 1 in each axis
            float height = 2;
            for (int j = 0; j < nTilesY; j++)
                for (int i = 0; i < nTilesX; i++)
                {
                    lowLeftX = -1 - width * (nTilesX - 1) / 2 + i * width;
                    lowLeftY = -1 - height * (nTilesY - 1) / 2 + j * height;
                    // triangle 1
                    verts[0 + (j * nTilesX + i) * 6] = new CustomVertex.PositionTextured(lowLeftX, lowLeftY, 0, 0, 1);
                    verts[1 + (j * nTilesX + i) * 6] = new CustomVertex.PositionTextured(lowLeftX, lowLeftY + 2, 0, 0, 0);
                    verts[2 + (j * nTilesX + i) * 6] = new CustomVertex.PositionTextured(lowLeftX + 2, lowLeftY, 0, 1, 1);
                    // traingle 2
                    verts[3 + (j * nTilesX + i) * 6] = new CustomVertex.PositionTextured(lowLeftX + 2, lowLeftY, 0, 1, 1);
                    verts[4 + (j * nTilesX + i) * 6] = new CustomVertex.PositionTextured(lowLeftX, lowLeftY + 2, 0, 0, 0);
                    verts[5 + (j * nTilesX + i) * 6] = new CustomVertex.PositionTextured(lowLeftX + 2, lowLeftY + 2, 0, 1, 0);
                }
            buf.Unlock();
            return buf;
        }

        public void RenderStill()
        {
            if (deviceLost)
            {
                AttemptRecovery();
            }
            // If we couldn't get the device back, don't try to render
            if (deviceLost)
            {
                return;
            }
            device.Transform.Projection = Matrix.PerspectiveFovLH((float)Math.PI / (2.0F * zoom), 1.0F, -0.1F, 0.1F);
            device.Clear(ClearFlags.Target, System.Drawing.Color.White, 0, 0);
            device.BeginScene();
            device.SetStreamSource(0, vertBuff, 0);
            device.VertexFormat = CustomVertex.PositionTextured.Format;
            device.DrawPrimitives(PrimitiveType.TriangleList, 0, 2 * nTilesX * nTilesY);
            device.EndScene();
            try
            {
                device.Present();
            }
            catch (DeviceLostException)
            {
                deviceLost = true;
            }
        }
        public void RenderDirectX(float swimVel, float dt, float deltaAngle, bool rotationMode, bool dirSelTest)
        {
            if (deviceLost)
            {
                AttemptRecovery();
            }
            // If we couldn't get the device back, don't try to render
            if (deviceLost)
            {
                return;
            }
            //Clear the backbuffer to a blue color 
            device.Transform.Projection = Matrix.PerspectiveFovLH((float)Math.PI / (2.0F * zoom), 1.0F, -0.1F, 0.1F);
            device.Clear(ClearFlags.Target, System.Drawing.Color.Blue, 0, 0);
            device.BeginScene();
            if (!rotationMode && !dirSelTest)
            {
                shiftA += deltaAngle;
                moveX += swimVel * dt * (float)Math.Cos(shiftA);
                moveY += swimVel * dt * (float)Math.Sin(shiftA);
                // reset position if close to boundary:
                if (moveX >= 2)
                {
                    moveX = swimVel * dt * (float)Math.Cos(shiftA);
                }
                if (moveX <= -2)
                {
                    moveX = swimVel * dt * (float)Math.Cos(shiftA);
                }
                if (moveY >= 2)
                {
                    moveY = swimVel * dt * (float)Math.Sin(shiftA);
                }
                if (moveY <= -2)
                {
                    moveY = swimVel * dt * (float)Math.Sin(shiftA);
                }
                RotateTranslateWorld(moveY, moveX, shiftA);
                //TranslateWorld(0.0f, shiftY);
            }
            else if (rotationMode)
            {
                shiftA += (float)(swimVel * dt);
                RotateWorld(shiftA);
            }
            else if (dirSelTest)
            {
                shiftA += deltaAngle;
                moveX += swimVel * dt * (float)Math.Sin(-shiftA);
                moveY += swimVel * dt * (float)Math.Cos(-shiftA);
                if (moveX >= 2)
                {
                    moveX = moveX - 2;
                }
                if (moveX <= -2)
                {
                    moveX = moveX + 2;
                }
                if (moveY >= 2)
                {
                    moveY = moveY - 2;
                }
                if (moveY <= -2)
                {
                    moveY = moveY + 2;
                }
                RotateTranslateView(moveX, moveY, shiftA);
            }
            device.SetStreamSource(0, vertBuff, 0);
            device.VertexFormat = CustomVertex.PositionTextured.Format;
            device.DrawPrimitives(PrimitiveType.TriangleList, 0, 2 * nTilesX * nTilesY);
            device.EndScene();
            try
            {
                device.Present();
            }
            catch (DeviceLostException)
            { 
                deviceLost = true;
            }
        }
        private void TranslateWorld(float dx, float dy)
        {
            Matrix mTranslationX = Matrix.Translation(-dx, 0, 0);
            Matrix mTranslationY = Matrix.Translation(0, -dy, 0);
            device.Transform.World = mTranslationX * mTranslationY;
        }
        private void RotateWorld(float dA)
        {
            Matrix mRotation = Matrix.RotationZ(dA);
            device.Transform.World = mRotation;
        }
        private void RotateTranslateWorld(float dx, float dy, float dA)
        {
            Matrix mTranslationX = Matrix.Translation(-dx, 0, 0);
            Matrix mTranslationY = Matrix.Translation(0, -dy, 0);
            Matrix mRotation = Matrix.RotationZ(dA);
            device.Transform.World = mTranslationX * mTranslationY * mRotation;
        }

        private void RotateTranslateView(float dx, float dy, float dA)
        {
            Matrix mTranslationXY = Matrix.Translation(dx, dy, 0);
            Matrix mRotation = Matrix.RotationZ(dA);
            device.Transform.View = Matrix.LookAtLH(new Vector3(0, 0, -1), new Vector3(), new Vector3(0, 1, 0)) * mRotation * mTranslationXY;
        }

        protected void AttemptRecovery()
        {
            try
            {
                device.TestCooperativeLevel();
            }
            catch (DeviceLostException)
            {
            }
            catch (DeviceNotResetException)
            {
                try
                {
                    device.Reset(presentParams);
                    deviceLost = false;
                    MessageBox.Show("Device successfully reset");
                }
                catch (DeviceLostException ex)
                {
                    MessageBox.Show("Device lost" + ex.Message);
                }
            }
        }

        public void Dispose()
        {
            vertBuff.Dispose();
            device.Dispose();
            texture.Dispose();
        }
    }
}
