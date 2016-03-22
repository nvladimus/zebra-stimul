using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Threading;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.IO;
using NationalInstruments.DAQmx;

namespace Stimulus
{
    public partial class Form1 : Form
    {
        Dictionary<string, string> mySettings;
        // Thread Add Data delegate
        public delegate void DrawDataDelegate();
        public DrawDataDelegate drawDataDel;
        // Chart data adding thread
        private Thread readDataRunnerOscope1;
        private Thread renderStimRunner;
        private Thread GUIthread;
        volatile bool bStopped;
        volatile bool readDataRunnerOscopeStarted, renderStimRunnerStarted, GUIthreadStarted;
        private Random rand = new Random();
        Series oSeries0, oSeries1, filtSeries0, filtSeries1;
        Series patchSeries0, patchSeries1;
        Screen[] screens;
        private FullScreenForm FSForm1;
        private DirectXdevice DXdev0, DXdev1;
        private double oscopeOut() { return rand.Next(0, 20); }
        private bool boolEmulation = false;
        //NI DAQ-mx part:
        DAQdevice DAQdev0;
        double[,] readData; // 2 channels, data from buffer
        //float[,] readDataFromFile;
        double prevMaxTimepoint;
        private static int nHistBins = 5000;
        double[] cumHistCh0;
        double[] cumHistCh1;
        int cumSamplesHist = 0;
        double[] xbins = new double[nHistBins];
        volatile float threshCh0, threshCh1;
        volatile float threshScaling;
        bool useCh0, useCh1;
        int buttThreshNumber = 0;
        volatile List<float> ch0Display;//behavior1
        volatile List<float> ch1Display;//behavior2
        volatile List<float> ch2Display;//patch1
        volatile List<float> ch3Display;//patch2
        volatile List<float> filtCh0Display;
        volatile List<float> filtCh1Display;
        volatile List<float> timeDisplay;
        long lastTriggerCount;
        //float[] data_arrayCh0Save; //store the whole recording at 6000 Hz for 4 hrs
        //float[] data_arrayCh0AuxSave; //store the whole recording at 6000 Hz for 4 hrs
        //float[] data_arrayCh1Save; //store the whole recording at 6000 Hz for 4 hrs
        //float[] data_arrayCh1AuxSave; //store the whole recording at 6000 Hz for 4 hrs
        //int idata_arrayChSave = 0;
        //        static int dataL = 120; //intended data length read each time from device
        static int subSampleWin = 10;
        //double dtAcquisition = 1.0d / 6000; // 1.0/DAQdev0.sampleRate, dt of sampling
        double dtSub = 1.0d / 6000 * subSampleWin; //dt of subsampling
        private double dtSubSample;
        //
        volatile float swimVel, currentVel;
        float VelMax;
        bool startDecay;
        bool doBinarySwims, doBinarySwimsNow;
        volatile float surprizeSwimVel;
        long surprizeTimeOld, surprizeTimeNew, gainOutTimeOld, gainOutTimeNew;
        bool addSurprize;
        volatile bool swimmingNow, swimSurpNow;
        volatile float surprizeInterval, gainOutInterval;

        volatile float deltaAngle = 0f;
        double Voffset0 = 0d, Voffset1 = 0d;
        System.Diagnostics.Stopwatch stopWatch;
        long newTimeRendering = 0, oldTimeRendering = 0, dTimeRendering = 0, newTimeReading = 0, oldTimeReading = 0, dTimeReading = 0, newTimeGUIupdating = 0, oldTimeGUIupdating = 0;
        List<long> dTimeListRendering = new List<long>();
        List<long> dTimeListReading = new List<long>();
        List<long> dTimeListGraphUpdating = new List<long>();
        //
        volatile List<float> buffSwimVel = new List<float>();
        volatile float gain, drift;
        float gain0, gain1, lastGain, gain2, drift2, drift3, drift4;
        float[] drift1Array, drift3Array;
        short drift1Index, drift3Index;
        float gain0Interval, gain1Interval, gain2Interval, drift1Interval, drift2Interval, drift3Interval, drift4Interval;
        float turningGain1, turningGain2;
        int gainNum, driftNum;
        long gainTimeOld, gainTimeNew, driftTimeOld, driftTimeNew;
        float decayVel;
        bool rotationMode;
        long triggerCount;
        double triggerThresh;
        double trigger_previous_last;
        double tempVin, currentTemp;
        //short nPlanes, planeOffset;

        bool doReplay = false;
        bool replayNow = false;
        int playTimeMinutes, playNtimes;
        int swimVelReplayCount;
        int swimBanTimePoints = (int)(6000 * 0.7), swimInt; // minimum interval between consecutive swims, only for binary swims

        bool dirSelectivityTest;
        long dirSelTimeOld, dirSelTimeNew;

        volatile bool started;
        volatile float LEDpower;
        volatile bool buttonFlashPressed;

        double max_data_Ch0, max_data_Ch1, max_data_drift; //values calculated for current stack duration

        FileStream fs, fs2;
        BinaryWriter writeFileStream;
        BinaryReader readFileStream;
        //StreamWriter logWrite;
        //StreamReader logRead;

        public Form1()
        {
            InitializeComponent();
            FSForm1 = new FullScreenForm();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (!bStopped)
            {
                DXdev0.RenderStill();
                DXdev1.RenderStill();
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            readDataRunnerOscope1 = new Thread(new ThreadStart(ReadDataThreadLoop));
            renderStimRunner = new Thread(new ThreadStart(RenderStimThreadLoop));
            GUIthread = new Thread(new ThreadStart(drawSignalLoop));
            readDataRunnerOscope1.Priority = ThreadPriority.Highest;
            renderStimRunner.Priority = ThreadPriority.Highest;
            drawDataDel += new DrawDataDelegate(drawSignal);
            readDataRunnerOscope1.IsBackground = true;
            renderStimRunner.IsBackground = true;
            GUIthread.IsBackground = true;
            // Data arrays.
            string[] seriesArray = { "Ch1", "Ch2", "Filtered Ch1", "Filtered Ch2", "Patch Ch1", "Patch Ch2" };

            // Add data series to behavior chart
            oSeries0 = this.oscilloscopeChart1.Series.Add(seriesArray[0]);
            oSeries1 = this.oscilloscopeChart1.Series.Add(seriesArray[1]);
            filtSeries0 = this.oscilloscopeChart1.Series.Add(seriesArray[2]);
            filtSeries1 = this.oscilloscopeChart1.Series.Add(seriesArray[3]);
            oSeries0.ChartType = SeriesChartType.Line;
            oSeries1.ChartType = SeriesChartType.Line;
            filtSeries0.ChartType = SeriesChartType.Line;
            filtSeries1.ChartType = SeriesChartType.Line;
            oSeries0.Color = Color.Yellow;
            oSeries1.Color = Color.Red;
            filtSeries0.Color = Color.Yellow;
            filtSeries1.Color = Color.Red;
            oscilloscopeChart1.Series["Ch1"].ChartArea = "ChartArea1";
            oscilloscopeChart1.Series["Ch2"].ChartArea = "ChartArea1";
            oscilloscopeChart1.Series["Filtered Ch1"].ChartArea = "ChartArea2";
            oscilloscopeChart1.Series["Filtered Ch2"].ChartArea = "ChartArea2";

            // Add data series to patching chart
            patchSeries0 = this.chartPatching.Series.Add(seriesArray[4]);
            patchSeries1 = this.chartPatching.Series.Add(seriesArray[5]);
            patchSeries0.ChartType = SeriesChartType.Line;
            patchSeries1.ChartType = SeriesChartType.Line;
            patchSeries0.Color = Color.Yellow;
            patchSeries1.Color = Color.Red;
            chartPatching.Series["Patch Ch1"].ChartArea = "ChartArea1";
            chartPatching.Series["Patch Ch2"].ChartArea = "ChartArea2";

            screens = Screen.AllScreens;
            labelNscreens.Text = Screen.AllScreens.Length.ToString();
            for (int i = 0; i < screens.Length; i++)
            {
                textBoxScreens.AppendText("Screen " + Convert.ToString(i) + ": " + screens[i].DeviceName + "\n");
                textBoxScreens.AppendText("Working area " + screens[i].WorkingArea.ToString() + "\n");
                textBoxScreens.AppendText("Bounds " + screens[i].Bounds.ToString() + "\n");
            }

            if (screens.Length == 2)
            {
                FSForm1.Size = new System.Drawing.Size(screens[1].Bounds.Width, screens[1].Bounds.Height);
                FSForm1.Location = new System.Drawing.Point(Screen.FromControl(this).Bounds.X + Screen.FromControl(this).Bounds.Width, 0);
                textBoxScreens.AppendText("Projector window location " + FSForm1.Location.ToString());
                FSForm1.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            }
            FSForm1.Show();
            DXdev0 = new DirectXdevice(panelProjector);
            DXdev1 = new DirectXdevice(FSForm1);
            behaviorChannelComboBox1.Items.AddRange(DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External));
            if (behaviorChannelComboBox1.Items.Count > 0)
                behaviorChannelComboBox1.SelectedIndex = 0;
            behaviorChannelComboBox2.Items.AddRange(DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External));
            if (behaviorChannelComboBox2.Items.Count > 0)
                behaviorChannelComboBox2.SelectedIndex = 1;
            twophotonTriggerChannelComboBox.Items.AddRange(DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External)); // 2Photon trigger AI
            if (twophotonTriggerChannelComboBox.Items.Count > 0)
                twophotonTriggerChannelComboBox.SelectedIndex = 5;
            camTriggerChannelComboBox.Items.AddRange(DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External)); // Camera trigger AI channel 
            if (camTriggerChannelComboBox.Items.Count > 0)
                camTriggerChannelComboBox.SelectedIndex = 2;
            ComboBoxLEDcontrol.Items.AddRange(DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AO, PhysicalChannelAccess.External));
            if (ComboBoxLEDcontrol.Items.Count > 0)
                ComboBoxLEDcontrol.SelectedIndex = 0;
            comboBoxTempSensor.Items.AddRange(DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External));
            if (comboBoxTempSensor.Items.Count > 0)
                comboBoxTempSensor.SelectedIndex = 7;

            patchingChannelComboBox1.Items.AddRange(DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External));
            if (patchingChannelComboBox1.Items.Count > 0)
                patchingChannelComboBox1.SelectedIndex = 3;
            patchingChannelComboBox2.Items.AddRange(DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External));
            if (patchingChannelComboBox2.Items.Count > 0)
                patchingChannelComboBox2.SelectedIndex = 4;
            //
            DAQdev0 = new DAQdevice();
            readParam("param.log");
            //data_arrayCh0Save = new float[6000 * 3600 * 4]; //store the whole recording at 6000 Hz for 4 hrs
            //data_arrayCh1Save = new float[6000 * 3600 * 4]; //store the whole recording at 6000 Hz for 4 hrs
            stopWatch = new System.Diagnostics.Stopwatch();
        }
        /// <summary>
        /// Main loop for the thread that adds data to the chart.
        /// The main purpose of this function is to Invoke AddData
        /// function every N ms.
        /// </summary>
        private void ReadDataThreadLoop()
        {
            try
            {
                while (true)
                {
                    ReadData();
                    Thread.Sleep(10);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Data acquisition error, line 177:" + e.Message);
            }
        }

        private void RenderStimThreadLoop()
        {
            try
            {
                while (true)
                {
                    renderStimRunnerStarted = true;
                    if (started && !bStopped)
                    {
                        oldTimeRendering = newTimeRendering;
                        newTimeRendering = stopWatch.ElapsedMilliseconds;
                        dTimeRendering = newTimeRendering - oldTimeRendering;
                        dTimeListRendering.Add(dTimeRendering);
                        float dtSpeedUpdate = Convert.ToSingle(dTimeRendering * 0.001f);
                        Object thisLock = new Object();
                        float vel;
                        //                        lock (thisLock)
                        //                        {
                        //vel = buffSwimVel.Last();
                        vel = swimVel - drift;
                        //                        }
                        if (!dirSelectivityTest)
                        {
                            DXdev0.RenderDirectX(vel * 0.1f, dtSpeedUpdate, deltaAngle, rotationMode, dirSelectivityTest);
                            DXdev1.RenderDirectX(vel * 0.1f, dtSpeedUpdate, deltaAngle, rotationMode, dirSelectivityTest);
                        }
                        else
                        {
                            dirSelTimeNew = stopWatch.ElapsedMilliseconds;
                            if ((dirSelTimeNew - dirSelTimeOld) / 1000F <= 10F)
                            {
                                DXdev0.RenderDirectX(-vel * 0.1f, dtSpeedUpdate, 0F, rotationMode, dirSelectivityTest);
                                DXdev1.RenderDirectX(-vel * 0.1f, dtSpeedUpdate, 0F, rotationMode, dirSelectivityTest);
                            }
                            else
                            {
                                dirSelTimeOld = dirSelTimeNew;
                                DXdev0.RenderDirectX(-vel * 0.1f, dtSpeedUpdate, -(float)Math.PI / 4, rotationMode, dirSelectivityTest);
                                DXdev1.RenderDirectX(-vel * 0.1f, dtSpeedUpdate, -(float)Math.PI / 4, rotationMode, dirSelectivityTest);
                            }
                        }
                    }
                    // Thread is inactive for AT LEAST 10 ms. Windows timing is inaccurate, innately. 
                    //Actual interval is variable 9 to 50 ms
                    Thread.Sleep(10);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Rendering exception, line 210:" + e.Message);
            }
        }

        private void drawSignalLoop()
        {
            try
            {
                while (true)
                {
                    // Invoke method must be used to interact with the chart
                    // control on the form!
                    // oldTimeGUIupdating = newTimeGUIupdating;
                    // newTimeGUIupdating = stopWatch.ElapsedMilliseconds;
                    // dTimeListGraphUpdating.Add(newTimeGUIupdating - oldTimeGUIupdating);
                    if (!bStopped)
                    {
                        oscilloscopeChart1.Invoke(drawDataDel);
                    }
                    Thread.Sleep(980);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Line 226: " + e.Message);
            }
        }

        private void drawSignal()
        {
            GUIthreadStarted = true;
            turningGain1 = Convert.ToSingle(numericUpDownTurningGain1.Value);
            turningGain2 = Convert.ToSingle(numericUpDownTurningGain2.Value);
            Voffset0 = Convert.ToDouble(numericUpDownVoffset0.Value);
            Voffset1 = Convert.ToDouble(numericUpDownVoffset1.Value);
            threshScaling = Convert.ToSingle(numericUpDownThreshScale.Value);
            oscilloscopeChart1.ChartAreas[0].AxisY.Maximum = Convert.ToDouble(numericUpDownVmaxi.Value);
            oscilloscopeChart1.ChartAreas[0].AxisY.Minimum = Convert.ToDouble(numericUpDownVmini.Value);
            oscilloscopeChart1.ChartAreas[1].AxisY.Maximum = Convert.ToDouble(numericUpDownFiltMaxY.Value);
            
            useCh0 = checkBoxUseCh1.Checked;
            useCh1 = checkBoxUseCh2.Checked;
            oSeries0.Points.Clear();
            oSeries1.Points.Clear();
            filtSeries0.Points.Clear();
            filtSeries1.Points.Clear();
            patchSeries0.Points.Clear();
            patchSeries1.Points.Clear();
            //                   Object thisLock = new Object();
            //                   lock (thisLock)
            //                 {
            if (timeDisplay.Count == ch0Display.Count && ch0Display.Count > 0)
            {
                for (int i = 0; i < ch0Display.Count - 1; i++)
                {
                    oSeries0.Points.AddXY((double)timeDisplay[i], (double)ch0Display[i] + Voffset0);
                    oSeries1.Points.AddXY((double)timeDisplay[i], (double)ch1Display[i] + Voffset1);
                    patchSeries0.Points.AddXY((double)timeDisplay[i], (double)ch2Display[i]);
                    patchSeries1.Points.AddXY((double)timeDisplay[i], (double)ch3Display[i]);
                    //oSeries1.Points.AddXY((double)timeDisplay[i], (double)ch1Display[i] + Voffset1);
                    //logFileStream.Write("{0}\t{1}\t{2}\n", timeDisplay[i], ch0Display[i], ch1Display[i]);
                }
            }
            if (filtCh0Display.Count > 0)
            {
                for (int i = 0; i < filtCh0Display.Count - 1; i++)
                {
                    filtSeries0.Points.Add((double)filtCh0Display[i]);
                    filtSeries1.Points.Add((double)filtCh1Display[i]);
                }
            }
            if (buttThreshNumber == 0)
            {
                oscilloscopeChart1.ChartAreas[1].CursorY.Position = (double)threshCh0;
                oscilloscopeChart1.ChartAreas[1].CursorY.LineColor = Color.Yellow;
            }
            else
            {
                oscilloscopeChart1.ChartAreas[1].CursorY.Position = (double)threshCh1;
                oscilloscopeChart1.ChartAreas[1].CursorY.LineColor = Color.Red;
            }
            oscilloscopeChart1.Invalidate();
            chartPatching.Invalidate();
            labelCurrentGain.Text = Convert.ToString(gain);
            labelCurrentDrift.Text = Convert.ToString(drift);
            if (currentTemp > 0) textBoxTemp.Text = string.Format("{0:N1}",currentTemp);
        }

        public void ReadData()
        {
            readDataRunnerOscopeStarted = true;
            int dataL = 120;
            int readDataL; // actual data length read from device, may be 0 occasionally
            dtSubSample = 1.0d / DAQdev0.sampleRate * subSampleWin;
            //double dataPoint = 0;
            if (!bStopped)
            {
                readDataL = 0;
                    try //real data mode
                    {
                        //Read the available data from the channels
                        //data = reader.ReadSingleSample();
                        readData = DAQdev0.ReadInput(dataL); // double[numChannels, bufferLength]
                        // bufferLength must be SamplingRate*[Thread sleep time]/1000
                        readDataL = readData.GetLength(1);
                    }
                    catch (DaqException exception)
                    {
                        MessageBox.Show("Line 332:" + exception.Message);
                    }
                if(boolEmulation) // emulation mode, read data from file
                {
                    //readDataFromFile = new float[3, dataL];
                    float dump = 0;
                    try
                    {
                        for (int i = 0; i < dataL; i++)
                        {
                            readData[0, i] = readFileStream.ReadSingle(); //ch0
                            readData[1, i] = readFileStream.ReadSingle(); //ch1
                            dump = readFileStream.ReadSingle(); //3
                            dump = readFileStream.ReadSingle(); //4
                            dump = readFileStream.ReadSingle(); //5
                            drift = readFileStream.ReadSingle(); //6, stimulus drift
                            swimVel = readFileStream.ReadSingle(); //7, speed
                            readData[2, i] = readFileStream.ReadSingle(); //8, trigger channel
                            dump = readFileStream.ReadSingle(); //9
                            dump = readFileStream.ReadSingle(); //10
                        }
                        readDataL = dataL;
                    }
                    catch (EndOfStreamException e)
                    {
                        MessageBox.Show("End of file reached, program terminates");
                        readFileStream.Close();
                        bStopped = true;
                        readDataL = 0;
                    }
                }
                if (readDataL > 0)
                {
                    int subDataL = readDataL / subSampleWin; // x10 subsampled data length
                    double[] data_arrayCh0 = new double[readDataL];//behavior1
                    double[] data_arrayCh1 = new double[readDataL];//behavior2
                    double[] data_arrayCh2 = new double[readDataL];//patching1
                    double[] data_arrayCh3 = new double[readDataL];//patching2
                    double[] trigger_channel = new double[readDataL];// external trigger (camera)
                    double[] trigger_channel2Photon = new double[readDataL];// external trigger (2Photon)
                    double[] temp_channel = new double[readDataL];// temperature reading
                    double[] subData_arrayCh0 = new double[subDataL];
                    double[] subData_arrayCh1 = new double[subDataL];
                    double[] subData_arrayCh2 = new double[subDataL];
                    double[] subData_arrayCh3 = new double[subDataL];
                    double[,] hist_tempCh0 = new double[2, readDataL];
                    double[,] hist_tempCh1 = new double[2, readDataL];
                    Object thisLock = new Object();
                    for (int i = 0; i < readDataL; i++)
                    {
                        data_arrayCh0[i] = readData[0, i];
                        data_arrayCh1[i] = readData[1, i];
                        trigger_channel[i] = readData[2, i];
                        trigger_channel2Photon[i] = readData[3, i];
                        temp_channel[i] = (readData[4, i] - 0.805858 * tempVin) / (-0.0056846 * tempVin);
                        currentTemp = mean(temp_channel);
                        data_arrayCh2[i] = readData[5, i];
                        data_arrayCh3[i] = readData[6, i];
                    }
                    //                        lock (thisLock)
                    //                        {
                    subData_arrayCh0 = subSample(data_arrayCh0, subSampleWin); //these subsampled data are used only for display.
                    subData_arrayCh1 = subSample(data_arrayCh1, subSampleWin);  //these subsampled data are used only for display.
                    subData_arrayCh2 = subSample(data_arrayCh2, subSampleWin); //these subsampled data are used only for display.
                    subData_arrayCh3 = subSample(data_arrayCh3, subSampleWin);  //these subsampled data are used only for display.
                    //                        }
                    int lengthWin = (int)(0.010 * DAQdev0.sampleRate); // 10 ms window at full samplingRate, 6 KHz
                    double[] filtData0, filtData1;
                    filtData0 = get_powerVAR(data_arrayCh0, lengthWin);
                    filtData1 = get_powerVAR(data_arrayCh1, lengthWin);
                    hist_tempCh0 = histogramMinMax(filtData0, nHistBins, 0.0d, 0.1d);
                    hist_tempCh1 = histogramMinMax(filtData1, nHistBins, 0.0d, 0.1d);
                    //                        lock (thisLock)
                    //                        {
                    for (int i = 0; i < nHistBins; i++)
                    {
                        cumHistCh0[i] += hist_tempCh0[1, i];
                        cumHistCh1[i] += hist_tempCh1[1, i];
                        xbins[i] = hist_tempCh0[0, i];
                    }
                    cumSamplesHist += readDataL;
                    if (cumSamplesHist >= DAQdev0.sampleRate * 15) // every 15 s hist 'forgets' by 0.5
                    {
                        cumSamplesHist = 0;
                        for (int i = 0; i < nHistBins; i++)
                        {
                            cumHistCh0[i] /= 2.0d;
                            cumHistCh1[i] /= 2.0d;
                        }
                    }
                    //                        }
                    int iHistMinCh0 = 0, iHistMinCh1 = 0;
                    int iHistMaxCh0 = 1, iHistMaxCh1 = 1;
                    // find iHistMax, index at which hist reaches Max
                    for (int i = 0; i < nHistBins; i++)
                    {
                        if (cumHistCh0[i] > cumHistCh0[iHistMaxCh0]) iHistMaxCh0 = i;
                        if (cumHistCh1[i] > cumHistCh1[iHistMaxCh1]) iHistMaxCh1 = i;
                    }
                    // find iHistMin, index at which hist is really small
                    for (int i = 0; i < iHistMaxCh0; i++)
                    {
                        if (cumHistCh0[i] > 0 && cumHistCh0[i] < cumHistCh0[iHistMaxCh0] / 100d) iHistMinCh0 = i;
                    }
                    for (int i = 0; i < iHistMaxCh1; i++)
                    {
                        if (cumHistCh1[i] > 0 && cumHistCh1[i] < cumHistCh1[iHistMaxCh1] / 100d) iHistMinCh1 = i;
                    }
                    // get the threshold
                    threshCh0 = (float)(xbins[iHistMaxCh0] + (float)threshScaling * (xbins[iHistMaxCh0] - xbins[iHistMinCh0]));
                    threshCh1 = (float)(xbins[iHistMaxCh1] + (float)threshScaling * (xbins[iHistMaxCh1] - xbins[iHistMinCh1]));
                    // save max stimulus and behavior value for current stack
                    max_data_Ch0 = Math.Max(max(filtData0), max_data_Ch0);
                    max_data_Ch1 = Math.Max(max(filtData1), max_data_Ch1);
                    max_data_drift = (double)drift;
                    //count camera triggers, only first trigger of the stack, thresh 3.8 V
                    lastTriggerCount = triggerCount;
                    if (trigger_previous_last < triggerThresh && trigger_channel[0] >= triggerThresh) triggerCount++;
                    for (int i = 0; i < trigger_channel.Length - 1; i++)
                    {
                        if (trigger_channel[i] < triggerThresh && trigger_channel[i + 1] >= triggerThresh) triggerCount++;
                    }
                    trigger_previous_last = trigger_channel[trigger_channel.Length - 1];
                    if (checkBoxSaveFileAfterStack.Checked && triggerCount > lastTriggerCount) // trigger occured, write ephys file
                    {
                        writeFileAfterEachStack(lastTriggerCount, max_data_drift, max_data_Ch0, max_data_Ch1);
                        max_data_drift = 0d;
                        max_data_Ch0 = 0d;
                        max_data_Ch1 = 0d;
                        lastTriggerCount = triggerCount;
                    }
                    // update gain:
                    if (started)
                    {
                        updateGain(checkBoxTimeInStacks.Checked);
                        if (!boolEmulation)
                        {
                            updateDrift(triggerCount);
                        }
                        //if (checkBoxSaveFileAfterStack.Checked) writeFileAfterEachStack(triggerCount);
                        if (doBinarySwims && oldTimeReading <= 1000F * gain0Interval && newTimeReading > 1000F * gain0Interval)
                        {
                            doBinarySwimsNow = true;
                        }

                        updateSwim(doBinarySwimsNow); // only when doBinarySwimsNow = true
                        surprizeTimeNew = stopWatch.ElapsedMilliseconds;
                        if (doBinarySwimsNow && addSurprize && (surprizeTimeNew - surprizeTimeOld) >= surprizeInterval * 1000F && !swimmingNow)
                        {
                            swimSurpNow = true;
                            surprizeTimeOld = surprizeTimeNew;
                            surprizeInterval = Convert.ToSingle(numericUpDownSurprizeInterval.Value) + (float)rand.Next(-5, 5);
                        }

                        oldTimeReading = newTimeReading;
                        newTimeReading = stopWatch.ElapsedMilliseconds;
                        dTimeReading = newTimeReading - oldTimeReading;
                        dTimeListReading.Add(dTimeReading);
                        // handle open-loop replay
                        if (doReplay && oldTimeReading < 1000F * (60F * playTimeMinutes + gain0Interval) && newTimeReading >= 1000F * (60F * playTimeMinutes + gain0Interval))
                        {
                            replayNow = true;
                            swimVelReplayCount = 0;
                            SetText("Replaying");
                        }
                        for (int i = 1; i <= playNtimes; i++)
                        {
                            if (doReplay && oldTimeReading < 1000F * (60F * playTimeMinutes * (i + 1) + gain0Interval) && newTimeReading >= 1000F * (60F * playTimeMinutes * (i + 1) + gain0Interval))
                            {
                                swimVelReplayCount = 0;
                            }
                        }
                        if (doReplay && newTimeReading > 1000F * (60F * playTimeMinutes * (playNtimes + 1) + gain0Interval))
                        {
                            bStopped = true;
                            doReplay = false;
                            replayNow = false;
                            doBinarySwims = false;
                            doBinarySwimsNow = false;
                        }
                    }

                    // start when first trigger comes
                    if (checkBoxSynchStim.Checked == true && started == false && triggerCount > 0)
                    {
                        started = true;
                        stopWatch.Start();
                        long currentStopWatch = stopWatch.ElapsedMilliseconds;
                        if (checkBoxTimeInStacks.Checked)
                        {
                            gainTimeNew = 1;
                            gainTimeOld = 1;
                            driftTimeNew = 1;
                            driftTimeOld = 1;
                        }
                        else
                        {
                            gainTimeNew = currentStopWatch;
                            gainTimeOld = currentStopWatch;
                            driftTimeNew = currentStopWatch;
                            driftTimeOld = currentStopWatch;
                        }
                        surprizeTimeOld = currentStopWatch;
                        surprizeTimeNew = currentStopWatch;
                        gainOutTimeNew = currentStopWatch;
                        gainOutTimeOld = currentStopWatch;
                        dirSelTimeOld = currentStopWatch;
                        dirSelTimeNew = currentStopWatch;
                        newTimeReading = currentStopWatch;
                        newTimeRendering = currentStopWatch;
                        newTimeGUIupdating = currentStopWatch;
                    }
                    if (started)
                    {
                        if (!replayNow)
                        {
                            float swimPow0 = 0, swimPow1 = 0;
                            for (int i = 0; i < filtData0.Length; i++)
                            {
                                if (filtData0[i] >= threshCh0 && useCh0) swimPow0 += (float)filtData0[i];
                                if (filtData1[i] >= threshCh1 && useCh1) swimPow1 += (float)filtData1[i];
                                if (i == filtData0.Length - 1) //update the speed
                                    lock (thisLock)
                                    {
                                        if (boolEmulation) gain = 0;
                                        else if (!doBinarySwimsNow)
                                        {
                                            //gain blackout for 1 s, at the onset of a swim
                                            gainOutTimeNew = stopWatch.ElapsedMilliseconds;
                                            if(((swimPow0 + swimPow1) / 2.0 > 0) && checkBoxGainBlackout.Checked)
                                            {
                                                makeGainBlackout(); // only in the start of a swim, but randomized in time.
                                            }
                                            if (checkBoxGainBlackout.Checked && gain == 0f && (gainOutTimeNew - gainOutTimeOld) >= 1000F) // return gain to normal value after 1000 ms
                                            {
                                                gain = lastGain;
                                                gainOutTimeOld = gainOutTimeNew;
                                                gainOutInterval = Convert.ToSingle(numericUpDownGainOutInt.Value) + (float)rand.Next(-5, 5);
                                            }

                                            if (((swimPow0 + swimPow1) / 2.0 > 0) && (gain > 0))
                                            {
                                                swimVel = 0.75F * swimVel + 0.25F * gain * (swimPow0 + swimPow1) / 2.0F;
                                                if (swimVel > VelMax) swimVel = VelMax; //too fast!
                                                deltaAngle = turningGain2 * swimPow1 - turningGain1 * swimPow0;
                                            }
                                            else // no bout, linear decay
                                            {
                                                swimVel = swimVel - decayVel;
                                                if (swimVel < 0F) swimVel = 0F;
                                                deltaAngle = 0f;
                                            }
                                        }
                                        else if (doBinarySwimsNow) // all-or-none (binary) swims
                                        {
                                            if ((((swimPow0 + swimPow1) / 2.0 > 0) || swimSurpNow) && swimVel <= 0F && (swimInt >= swimBanTimePoints) && !startDecay)
                                            {
                                                    swimVel = swimVel + currentVel / 10F; //gradually increase swim vel, so that velocity grows naturally
                                                    swimmingNow = true;
                                                    if (swimSurpNow)
                                                    {
                                                        surprizeSwimVel = swimVel;
                                                    }
                                                swimInt = 0; //start new interval in any case
                                            }
                                            else if (swimVel > 0F && swimVel < currentVel && !startDecay)
                                            {
                                                swimVel = swimVel + currentVel / 10F;
                                                swimmingNow = true;
                                                if (swimSurpNow)
                                                {
                                                    surprizeSwimVel = swimVel;
                                                }
                                            }
                                            if (swimVel >= currentVel)
                                            {
                                                swimVel = currentVel;
                                                swimmingNow = true;
                                                startDecay = true;
                                                if (swimSurpNow)
                                                {
                                                    surprizeSwimVel = swimVel;
                                                }
                                            }
                                            if (startDecay) // no bout, linear decay
                                            {
                                                swimVel = swimVel - decayVel;
                                                swimmingNow = true;
                                                if (swimSurpNow) { surprizeSwimVel = swimVel; } //only for surprize swims
                                                if (swimVel < 0F)
                                                {
                                                    swimVel = 0F;
                                                    startDecay = false;
                                                    swimmingNow = false;
                                                    if (swimSurpNow)
                                                    {
                                                        surprizeSwimVel = swimVel;
                                                        swimSurpNow = false;//only for surprize swims
                                                    }
                                                }
                                            }
                                            swimInt += dataL; //120 time points, 20 ms
                                        }
                                        long currentStopWatch = stopWatch.ElapsedMilliseconds;
                                        if (currentStopWatch > gain0Interval * 1000)
                                        {
                                            buffSwimVel.Add(swimVel);
                                        }
                                        swimPow0 = 0;
                                        swimPow1 = 0;
                                    }
                            }
                        }
                        else //if replay now
                        {
                            if (swimVelReplayCount < buffSwimVel.Count)
                            {
                                swimVelReplayCount++;
                                swimVel = buffSwimVel[swimVelReplayCount];
                                buffSwimVel.Add(swimVel);
                            }
                        }
                        // write cumulative data for display by GUI thread
                        lock (thisLock)
                        {
                            for (int i = 0; i < subData_arrayCh0.Length; i++)
                            {
                                ch0Display.Add((float)subData_arrayCh0[i]);
                                ch1Display.Add((float)subData_arrayCh1[i]);
                                ch2Display.Add((float)subData_arrayCh2[i]);
                                ch3Display.Add((float)subData_arrayCh3[i]);
                                timeDisplay.Add((float)(prevMaxTimepoint) + (float)((i + 1) * dtSubSample));
                                if (prevMaxTimepoint > 1.0d && ch0Display.Count > 0)
                                {
                                    ch0Display.RemoveAt(0);
                                    ch1Display.RemoveAt(0);
                                    ch2Display.RemoveAt(0);
                                    ch3Display.RemoveAt(0);
                                    timeDisplay.RemoveAt(0);
                                }
                            }
                            //logFileStream.Write("{0}\n", prevMaxTimepoint);
                            for (int i = 0; i < filtData0.Length; i++)
                            {
                                filtCh0Display.Add((float)filtData0[i]);
                                filtCh1Display.Add((float)filtData1[i]);
                                if (prevMaxTimepoint > 1.0d && filtCh0Display.Count > 0)
                                {
                                    filtCh0Display.RemoveAt(0);
                                    filtCh1Display.RemoveAt(0);
                                }
                            }
                            prevMaxTimepoint += subData_arrayCh0.Length * dtSubSample;
                        }
                        // save the stuff
                        try
                        {
                            if (writeFileStream != null)
                            {
                                for (int i = 0; i < readDataL; i++)
                                {
                                    writeFileStream.Write((float)data_arrayCh0[i]); //behavior1
                                    writeFileStream.Write((float)data_arrayCh1[i]); //behavior2
                                    writeFileStream.Write((float)data_arrayCh2[i]); //patch1
                                    writeFileStream.Write((float)data_arrayCh3[i]); //patch2
                                    writeFileStream.Write(deltaAngle); //4
                                    writeFileStream.Write(gain); //5
                                    writeFileStream.Write(drift); //6
                                    writeFileStream.Write(swimVel); //7
                                    writeFileStream.Write((float)trigger_channel[i]); //8, camera trigger
                                    writeFileStream.Write((float)trigger_channel2Photon[i]); //9, 2Photon trigger
                                    writeFileStream.Write((float)temp_channel[i]); //temperature, in C
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Line 486:" + ex.Message);
                        }
                    } //end of if(started)
                }
                else
                {
                    //do nothing, buffer is empty
                }
            }
        }


        private void buttonStart_Click(object sender, EventArgs e)
        {
            DAQdev0.StartTask(this);
            DAQdev0.StartAOtask(this);
            DXdev0.shiftA = 0F;
            DXdev1.shiftA = 0F;
            cumHistCh0 = new double[nHistBins];
            cumHistCh1 = new double[nHistBins];

            started = false;
            if (checkBoxSynchStim.Checked == false)
            {
                stopWatch.Start();
                started = true;
                gainTimeNew = stopWatch.ElapsedMilliseconds;
                gainTimeOld = gainTimeNew;
                driftTimeNew = gainTimeNew;
                driftTimeOld = gainTimeNew;
                surprizeTimeOld = gainTimeNew;
                surprizeTimeNew = gainTimeNew;
                dirSelTimeOld = gainTimeNew;
                dirSelTimeNew = gainTimeNew;
                newTimeReading = gainTimeNew;
                newTimeRendering = gainTimeNew;
                newTimeGUIupdating = gainTimeNew;
            }
            else { started = false; }
            startDecay = false;
            swimInt = 6001;
            gain0 = Convert.ToSingle(numericUpDownGain0.Value);
            gain1 = Convert.ToSingle(numericUpDownGain1.Value);
            gain2 = Convert.ToSingle(numericUpDownGain2.Value);
            labelCurrentGain.Text = Convert.ToString(gain);
            gain0Interval = Convert.ToSingle(numericUpDownGain0Interval.Value);
            gain1Interval = Convert.ToSingle(numericUpDownGain1Interval.Value);
            gain2Interval = Convert.ToSingle(numericUpDownGain2Interval.Value);
            turningGain1 = Convert.ToSingle(numericUpDownTurningGain1.Value);
            turningGain2 = Convert.ToSingle(numericUpDownTurningGain2.Value);
            if (gain0Interval > 0)
            {
                gain = gain0;
                lastGain = gain0;
                gainNum = 0;
            }
            else
            {
                gain = gain1;
                lastGain = gain1;
                gainNum = 1;
            }

            float drift1 = Convert.ToSingle(numericUpDownDrift1.Value);
            float drift1_2 = Convert.ToSingle(numericUpDownDrift1_2.Value);
            float drift1_3 = Convert.ToSingle(numericUpDownDrift1_3.Value);
            float drift1_4 = Convert.ToSingle(numericUpDownDrift1_4.Value);
            drift1Array = new float[]{drift1, drift1_2, drift1_3, drift1_4};
            drift1Index = 1;
            drift = drift1;

            drift2 = Convert.ToSingle(numericUpDownDrift2.Value);

            drift3 = Convert.ToSingle(numericUpDownDrift3.Value);
            float drift3_2 = Convert.ToSingle(numericUpDownDrift3_2.Value);
            float drift3_3 = Convert.ToSingle(numericUpDownDrift3_3.Value);
            float drift3_4 = Convert.ToSingle(numericUpDownDrift3_4.Value);
            drift3Array = new float[] { drift3, drift3_2, drift3_3, drift3_4 };
            drift3Index = 1;
            driftNum = 1;
            drift1Interval = Convert.ToSingle(numericUpDownDrift1Interval.Value);
            drift2Interval = Convert.ToSingle(numericUpDownDrift2Interval.Value);
            drift3Interval = Convert.ToSingle(numericUpDownDrift3Interval.Value);
            drift4Interval = Convert.ToSingle(numericUpDownDrift4Interval.Value);

            drift4 = Convert.ToSingle(numericUpDownDrift4.Value);

            threshScaling = Convert.ToSingle(numericUpDownThreshScale.Value);
            buttonStart.Enabled = false;
            buttonStop.Enabled = true;
            rotationMode = checkBoxRotation.Checked;
            prevMaxTimepoint = 0d;

            ch0Display = new List<float>();
            ch1Display = new List<float>();
            ch2Display = new List<float>();
            ch3Display = new List<float>();
            filtCh0Display = new List<float>();
            filtCh1Display = new List<float>();
            timeDisplay = new List<float>();
            useCh0 = checkBoxUseCh1.Checked;
            useCh1 = checkBoxUseCh2.Checked;

            lastTriggerCount = 0;
            triggerCount = 0;
            trigger_previous_last = 0;
            triggerThresh = Convert.ToDouble(numericUpDownSyncTriggerTresh.Value);

            VelMax = Convert.ToSingle(numericUpDownVelMax.Value);// max swimming velocity
            decayVel = Convert.ToSingle(numericUpDownDecay.Value);

            surprizeSwimVel = 0F;
            addSurprize = checkBoxAddSurprize.Checked;
            swimSurpNow = false;

            surprizeInterval = Convert.ToSingle(numericUpDownSurprizeInterval.Value) + (float)rand.Next(-5, 5);
            gainOutInterval = Convert.ToSingle(numericUpDownGainOutInt.Value) + (float)rand.Next(-5, 5);

            dirSelectivityTest = checkBoxDirSelectivity.Checked;

            LEDpower = 0;
            buttonFlashPressed = false;

            playTimeMinutes = Convert.ToInt32(numericUpDownPlayTmin.Value);
            if (checkBoxReplay.Checked)
            {
                doReplay = true;
                playNtimes = Convert.ToInt32(numericUpDownReplayN.Value);
            }
            else
            {
                doReplay = false;
            }
            if (checkBoxCloopBinarySwims.Checked)
            {
                doBinarySwims = true;
                doBinarySwimsNow = false;
            }
            if (!boolEmulation) SetText("Normal mode");
            else SetText("Emulation mode");
            // start worker threads.
            if (!readDataRunnerOscopeStarted)
            {
                readDataRunnerOscope1.Start();
            }

            if (!renderStimRunnerStarted)
            {
                renderStimRunner.Start();
            }
            if (!GUIthreadStarted)
            {
                GUIthread.Start();
            }
            try
            {
                fs = new FileStream("_data.10ch", FileMode.Create);
                writeFileStream = new BinaryWriter(fs);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Line 747:" + ex.Message);
            }
            max_data_Ch0 = 0d; 
            max_data_Ch1 = 0d;
            max_data_drift = 0d;

            tempVin = Convert.ToDouble(textBoxVin.Text);
            System.Threading.Thread.Sleep(500);// allows time to create DAQms task, before reading from it
            bStopped = false;
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            bStopped = true;
            buttonStart.Enabled = true;
            buttonStop.Enabled = false;
            stopWatch.Stop();
            DAQdev0.StopTask();
            writeFileStream.Close();
            if (boolEmulation) readFileStream.Close();
            Save();
        }

        private void updateGain(bool timeInStacks)
        {
            if (!timeInStacks) // if time is in milliseconds
            {
                gainTimeNew = stopWatch.ElapsedMilliseconds;
                if ((gainNum == 0) && (gainTimeNew - gainTimeOld) >= (long)(gain0Interval * 1000F - 10F))
                {
                    gain = gain1; // switch the gain, only once!
                    lastGain = gain1;
                    gainNum = 1;
                    gainTimeOld = gainTimeNew;
                }
                if ((gainNum == 1) && (gainTimeNew - gainTimeOld) >= (long)(gain1Interval * 1000F - 10F))
                {
                    gain = gain2; // switch the gain
                    lastGain = gain2;
                    gainNum = 2;
                    gainTimeOld = gainTimeNew;
                }
                if ((gainNum == 2) && (gainTimeNew - gainTimeOld) >= (long)(gain2Interval * 1000F - 10F))
                {
                    gain = gain1; // switch the gain 
                    lastGain = gain1;
                    gainNum = 1;
                    gainTimeOld = gainTimeNew;
                }

            }
            else // if time is in stacks
            {
                gainTimeNew = triggerCount;
                if ((gainNum == 0) && (gainTimeNew - gainTimeOld) >= gain0Interval)
                {
                    gain = gain1; // switch the gain, only once!
                    lastGain = gain1;
                    gainNum = 1;
                    gainTimeOld = gainTimeNew;
                }
                if ((gainNum == 1) && (gainTimeNew - gainTimeOld) >= gain1Interval)
                {
                    gain = gain2; // switch the gain
                    gainNum = 2;
                    gainTimeOld = gainTimeNew;
                }
                if ((gainNum == 2) && (gainTimeNew - gainTimeOld) >= gain2Interval)
                {
                    gain = gain1; // switch the gain 
                    gainNum = 1;
                    gainTimeOld = gainTimeNew;
                }
            }
            if (replayNow) gain = 0;
        }

        private void makeGainBlackout()
        {
            // insert randomized gain blackouts
            if ( (gainOutTimeNew - gainOutTimeOld) >= gainOutInterval * 1000F)
            {
                gain = 0f;
                gainOutTimeOld = gainOutTimeNew;
                gainOutInterval = Convert.ToSingle(numericUpDownGainOutInt.Value) + (float)rand.Next(-5, 5);
            }
        }

        private void updateSwim(bool doBinarySwimsNow0)
        {
            if (doBinarySwimsNow0)
            {
                if (gainNum == 1)
                {
                    currentVel = Convert.ToSingle(numericUpDownSpeed1.Value);
                    gain = gain1 / 10F; // fake gain value
                }
                if (gainNum == 2)
                {
                    currentVel = Convert.ToSingle(numericUpDownSpeed2.Value);
                    gain = gain2 / 10F; // fake gain value
                }
            }
        }

        private double min(double[] source)
        {
            double Min = source[0];
            int n = source.GetLength(0);
            for (int i = 0; i < n; i++)
            {
                if (Min > source[i]) Min = source[i];
            }
            return Min;
        }

        private double max(double[] source)
        {
            double Max = source[0];
            int n = source.GetLength(0);
            for (int i = 0; i < n; i++)
            {
                if (Max < source[i]) Max = source[i];
            }
            return Max;
        }
        /// <summary>
        /// Returns [2, nbins] array: hist. bin centers (array [0, i]), histogram counts (array [1,i]). 
        /// Bin boundaries are set between min and max of the source data
        /// </summary>
        /// <param name="source"></param>
        /// <param name="bins"></param>
        /// <returns></returns>
        private double[,] histogram(double[] source, int nbins)
        {
            double[,] hist = new Double[2, nbins];
            double Min, Max;
            Min = min(source);
            Max = max(source);
            int n = source.GetLength(0);
            double dx = (double)((Max - Min) / nbins);
            int ibin;
            for (int i = 0; i < n; i++)
            {
                if (source[i] > Max - dx)
                {
                    ibin = nbins - 1;
                }
                else
                {
                    ibin = (int)Math.Floor((source[i] - Min) / dx);
                }
                hist[1, ibin]++;
            }
            for (int i = 0; i < nbins; i++)
            {
                hist[0, i] = Min + dx / 2.0d + dx * i; //set centers of bins
                hist[1, i] /= (double)n; // normalize 
            }
            return hist;
        }

        /// <summary>
        /// Returns [2, nbins] array: hist. bin centers (array [0, i]), histogram counts (array [1,i]). 
        /// Bin boundaries are set between min and max, which are parameters
        /// </summary>
        /// <param name="source"></param>
        /// <param name="bins"></param>
        /// <returns></returns>
        private double[,] histogramMinMax(double[] source, int nbins, double min, double max)
        {
            double[,] hist = new Double[2, nbins];
            int n = source.GetLength(0);
            double dx = (double)((max - min) / nbins);
            int ibin;
            for (int i = 0; i < n; i++)
            {
                if (source[i] > max - dx)
                {
                    ibin = nbins - 1;
                }
                else
                {
                    ibin = (int)Math.Floor((source[i] - min) / dx);
                }
                hist[1, ibin]++;
            }
            for (int i = 0; i < nbins; i++)
            {
                hist[0, i] = min + dx / 2.0d + dx * i; //set centers of bins
                hist[1, i] /= (double)n; // normalize 
            }
            return hist;
        }

        /// <summary>
        /// window is number of samples averaged, i.e. window/2 samples are included on each side of a time point
        /// </summary>
        /// <param name="source"></param>
        /// <param name="window"></param>
        /// <returns></returns>
        private double[] get_powerSTD(double[] source, int window)
        {
            int n = source.Length - window + 1;
            double[] result = new double[n];
            for (int i = 0; i < n; i++)
            {
                result[i] = std_subset(source, i, i + window - 1);
            }
            return result;
        }

        private double[] get_powerVAR(double[] source, int window)
        {
            int n = source.Length - window + 1;
            double[] result = new double[n];
            for (int i = 0; i < n; i++)
            {
                result[i] = var_subset(source, i, i + window - 1);
            }
            return result;
        }

        private double mean(double[] source)
        {
            int n = source.Length;
            double m = 0;
            for (int i = 0; i < n; i++) { m += source[i]; }
            return (m / n);
        }

        private double mean_subset(double[] source, int istart, int iend)
        {
            int n = iend - istart + 1;
            double m = 0;
            for (int i = istart; i <= iend; i++) { m += source[i]; }
            return (m / n);
        }

        private double std(double[] source)
        {
            int n = source.Length;
            double m = mean(source);
            double s = 0;
            for (int i = 0; i < n; i++) { s += (source[i] - m) * (source[i] - m); }
            return Math.Sqrt(s / n);
        }

        private double std_subset(double[] source, int istart, int iend) //st. deviation
        {
            int n = iend - istart + 1;
            double m = mean_subset(source, istart, iend);
            double s = 0;
            for (int i = istart; i <= iend; i++) { s += (source[i] - m) * (source[i] - m); }
            return Math.Sqrt(s / n);
        }

        private double var_subset(double[] source, int istart, int iend) //variance
        {
            int n = iend - istart + 1;
            double m = mean_subset(source, istart, iend);
            double s = 0;
            for (int i = istart; i <= iend; i++) { s += (source[i] - m) * (source[i] - m); }
            return s / n;
        }

        private double[] subSample(double[] source, int window)
        {
            int N = source.Length;
            int n = (int)(N / window);
            double[] result = new double[n];
            for (int i = 0; i < n; i++)
            {
                result[i] = mean_subset(source, i * window, (i + 1) * window - 1);
            }
            return result;
        }

        /*   public void Dispose()
           {
               Dispose(true);
           }
           /// <summary>
           /// Clean up any resources being used.
           /// </summary>
           /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
           protected override void Dispose(bool disposing)
           {
               if (disposing && (components != null))
               {
                   components.Dispose();
                   DXdev0.Dispose();
                   DXdev1.Dispose();
               }
               base.Dispose(disposing);
           }

   */
        private void loadStimulusImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = Application.StartupPath;
            openFileDialog1.Filter = "Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.GIF|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    DXdev0.SetTexture(TextureLoader.FromFile(DXdev0.device, openFileDialog1.FileName));
                    DXdev1.SetTexture(TextureLoader.FromFile(DXdev1.device, openFileDialog1.FileName));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read texture file from disk. Original error: " + ex.Message);
                }
            }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.InitialDirectory = Environment.CurrentDirectory;
            saveFileDialog1.Filter = "Data Files(*.10ch)|*.10ch";
            saveFileDialog1.RestoreDirectory = false;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                // Use Path class to manipulate file and directory paths. 
                string sourceFile = System.IO.Path.Combine(Application.StartupPath, "_data.10ch");
                System.IO.File.Copy(sourceFile, saveFileDialog1.FileName, true);
            }
        }
        private void Save()
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.InitialDirectory = Environment.CurrentDirectory;
            saveFileDialog1.Filter = "Data Files(*.10ch)|*.10ch";
            saveFileDialog1.RestoreDirectory = true;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                // Use Path class to manipulate file and directory paths. 
                string sourceFile = System.IO.Path.Combine(Application.StartupPath, "_data.10ch");
                System.IO.File.Copy(sourceFile, saveFileDialog1.FileName, true);
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Author: Nikita Vladimirov <vladimirovn@janelia.hhmi.org>");
        }

        private void load10chDataFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = Environment.CurrentDirectory;
            openFileDialog1.Filter = "10-channel Files(*.10ch)|*.10ch";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    fs2 = new FileStream(openFileDialog1.FileName, FileMode.Open);
                    readFileStream = new BinaryReader(fs2);
                    boolEmulation = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file. Original error: " + ex.Message);
                }
            }
        }

        private void buttonThreshNumber_Click(object sender, EventArgs e)
        {
            if (buttThreshNumber == 0)
            {
                buttThreshNumber = 1;
                buttonThreshNumber.Text = "Showing Thresh Ch2";
            }
            else
            {
                buttThreshNumber = 0;
                buttonThreshNumber.Text = "Showing Thresh Ch1";
            }
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void updateDrift(long triggerCount1)
        {
            if (!checkBoxTimeInStacks.Checked)
            {
                driftTimeNew = stopWatch.ElapsedMilliseconds;
                if ((driftNum == 1) && (driftTimeNew - driftTimeOld) >= (long)(drift1Interval * 1000F - 10F) )
                {
                    drift = drift2; // switch drift2
                    driftNum = 2;
                    driftTimeOld = driftTimeNew;
                }
                else if ((driftNum == 2) && (driftTimeNew - driftTimeOld) >= (long)(drift2Interval * 1000F - 10F))
                {
                    drift = drift3Array[drift3Index]; // switch drift3
                    drift3Index++;
                    if (drift3Index == drift3Array.Count())
                    {
                        drift3Index = 0;
                        if (checkBoxShuffle.Checked) Shuffle(drift3Array);
                    }
                    driftNum = 3;
                    driftTimeOld = driftTimeNew;
                }
                else if ((driftNum == 3) && (driftTimeNew - driftTimeOld) >= (long)(drift3Interval * 1000F - 10F))
                {
                    drift = drift4; // switch to drift4
                    driftNum = 4;
                    driftTimeOld = driftTimeNew;
                }
                else if ((driftNum == 4) && (driftTimeNew - driftTimeOld) >= (long)(drift4Interval * 1000F - 10F))
                {
                    drift = drift1Array[drift1Index]; // switch to drift1 again
                    drift1Index++;
                    if (drift1Index == drift1Array.Count())
                    {
                        drift1Index = 0;
                        if (checkBoxShuffle.Checked) Shuffle(drift1Array);
                    }
                    driftNum = 1;
                    driftTimeOld = driftTimeNew;
                }
            }
            else // if time is in stacks
            {
                driftTimeNew = triggerCount;
                if ((driftNum == 1) && (driftTimeNew - driftTimeOld) >= drift1Interval)
                {
                    drift = drift2; // switch drift2
                    driftNum = 2;
                    driftTimeOld = driftTimeNew;
                }
                else if ((driftNum == 2) && (driftTimeNew - driftTimeOld) >= drift2Interval)
                {
                    drift = drift3Array[drift3Index]; // switch drift3
                    drift3Index++;
                    if (drift3Index == drift3Array.Count())
                    {
                        drift3Index = 0;
                        if (checkBoxShuffle.Checked) Shuffle(drift3Array);
                    }
                    driftNum = 3;
                    driftTimeOld = driftTimeNew;
                }
                else if ((driftNum == 3) && (driftTimeNew - driftTimeOld) >= drift3Interval)
                {
                    drift = drift4; // switch drift4
                    driftNum = 4;
                    driftTimeOld = driftTimeNew;
                }
                else if ((driftNum == 4) && (driftTimeNew - driftTimeOld) >= drift4Interval)
                {
                    drift = drift1Array[drift1Index]; // switch drift1 again
                    drift1Index++;
                    if (drift1Index == drift1Array.Count())
                    {
                        drift1Index = 0;
                        if (checkBoxShuffle.Checked) Shuffle(drift1Array);
                    }
                    driftNum = 1;
                    driftTimeOld = driftTimeNew;
                } 
            }
        }

        delegate void SetTextCallback(string text);
        private void SetText(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.labelReplay.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.labelReplay.Text = text;
            }
        }

        private void numericUpDownZoom_ValueChanged(object sender, EventArgs e)
        {
            DXdev0.zoom = Convert.ToSingle(numericUpDownZoom.Value);
            DXdev1.zoom = Convert.ToSingle(numericUpDownZoom.Value);
            if (!bStopped)
            {
                DXdev0.RenderStill();
                DXdev1.RenderStill();
            }
        }

        private void buttonFlash_Click(object sender, EventArgs e)
        {
            if (!buttonFlashPressed)
            {
                buttonFlashPressed = true;
                LEDpower = (float)numericUpDownLEDinput.Value;
                buttonFlash.BackColor = Color.Gold;
            }
            else
            {
                buttonFlashPressed = false;
                LEDpower = 0f;
                buttonFlash.BackColor = Color.Silver;
            }
            DAQdev0.writeAOvalue((double)LEDpower);
        }

        private void checkBoxCloopBinarySwims_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxCloopBinarySwims.Checked == true)
            {
                numericUpDownGain1.Enabled = false;
                numericUpDownGain2.Enabled = false;
                numericUpDownSpeed1.Enabled = true;
                numericUpDownSpeed2.Enabled = true;
            }
            else
            {
                numericUpDownGain1.Enabled = true;
                numericUpDownGain2.Enabled = true;
                numericUpDownSpeed1.Enabled = false;
                numericUpDownSpeed2.Enabled = false;
            }
        }

        private void checkBoxAddSurprize_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxAddSurprize.Checked)
            {
                checkBoxCloopBinarySwims.Checked = true;
                numericUpDownGain1.Enabled = false;
                numericUpDownGain2.Enabled = false;
                numericUpDownSpeed1.Enabled = true;
                numericUpDownSpeed2.Enabled = true;
            }
            else
            {
                checkBoxCloopBinarySwims.Checked = false;
                numericUpDownGain1.Enabled = true;
                numericUpDownGain2.Enabled = true;
                numericUpDownSpeed1.Enabled = false;
                numericUpDownSpeed2.Enabled = false;
            }
        }
        /// <summary>
        /// Shuffle the array.
        /// </summary>
        /// <typeparam name="T">Array element type.</typeparam>
        /// <param name="array">Array to shuffle.</param>
        public void Shuffle<T>(T[] array)
        {
            var random = rand;
            for (int i = array.Length; i > 1; i--)
            {
                // Pick random element to swap.
                int j = random.Next(i); // 0 <= j <= i-1
                // Swap.
                T tmp = array[j];
                array[j] = array[i - 1];
                array[i - 1] = tmp;
            }
        }

        private void checkBoxOpenLoop_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxOpenLoop.Checked)
            {
                numericUpDownGain0.Value = 0;
                numericUpDownGain1.Value = 0;
                numericUpDownGain2.Value = 0;
            }
        }
        private void readParam(string filename)
        {
            string[] lines = System.IO.File.ReadAllLines(filename);
            mySettings = new Dictionary<string, string>();
            foreach (string line in lines)
            {
                string[] keyAndValue = line.Split(new char[] { '=' });
                mySettings.Add(keyAndValue[0].Trim(), keyAndValue[1].Trim());
            }
            if (mySettings.ContainsKey("[drift V1]"))
            {
                string value = mySettings["[drift V1]"];
                numericUpDownDrift1.Value = Convert.ToDecimal(value);
            }
            if (mySettings.ContainsKey("[drift V1_2]"))
            {
                string value = mySettings["[drift V1_2]"];
                numericUpDownDrift1_2.Value = Convert.ToDecimal(value);
            }
            if (mySettings.ContainsKey("[drift V1_3]"))
            {
                string value = mySettings["[drift V1_3]"];
                numericUpDownDrift1_3.Value = Convert.ToDecimal(value);
            }
            if (mySettings.ContainsKey("[drift V1_4]"))
            {
                string value = mySettings["[drift V1_4]"];
                numericUpDownDrift1_4.Value = Convert.ToDecimal(value);
            }
            if (mySettings.ContainsKey("[drift V2]"))
            {
                string value = mySettings["[drift V2]"];
                numericUpDownDrift2.Value = Convert.ToDecimal(value);
            }
            if (mySettings.ContainsKey("[drift V3]"))
            {
                string value = mySettings["[drift V3]"];
                numericUpDownDrift3.Value = Convert.ToDecimal(value);
            }
            if (mySettings.ContainsKey("[drift V3_2]"))
            {
                string value = mySettings["[drift V3_2]"];
                numericUpDownDrift3_2.Value = Convert.ToDecimal(value);
            }
            if (mySettings.ContainsKey("[drift V3_3]"))
            {
                string value = mySettings["[drift V3_3]"];
                numericUpDownDrift3_3.Value = Convert.ToDecimal(value);
            }
            if (mySettings.ContainsKey("[drift V3_4]"))
            {
                string value = mySettings["[drift V3_4]"];
                numericUpDownDrift3_4.Value = Convert.ToDecimal(value);
            }
            if (mySettings.ContainsKey("[duration V1]"))
            {
                string value = mySettings["[duration V1]"];
                numericUpDownDrift1Interval.Value = Convert.ToDecimal(value);
            }
            if (mySettings.ContainsKey("[duration V2]"))
            {
                string value = mySettings["[duration V2]"];
                numericUpDownDrift2Interval.Value = Convert.ToDecimal(value);
            }
            if (mySettings.ContainsKey("[duration V3]"))
            {
                string value = mySettings["[duration V3]"];
                numericUpDownDrift3Interval.Value = Convert.ToDecimal(value);
            }
            if (mySettings.ContainsKey("[duration V4]"))
            {
                string value = mySettings["[duration V4]"];
                numericUpDownDrift4Interval.Value = Convert.ToDecimal(value);
            }
            if (mySettings.ContainsKey("[shuffle columns?]"))
            {
                string value = mySettings["[shuffle columns?]"];
                checkBoxShuffle.Checked = Convert.ToBoolean(value);
            }
            if (mySettings.ContainsKey("[time in stacks?]"))
            {
                string value = mySettings["[time in stacks?]"];
                checkBoxTimeInStacks.Checked = Convert.ToBoolean(value);
            }
            if (mySettings.ContainsKey("[gain0]"))
            {
                string value = mySettings["[gain0]"];
                numericUpDownGain0.Value = Convert.ToDecimal(value);
            }
            if (mySettings.ContainsKey("[gain1]"))
            {
                string value = mySettings["[gain1]"];
                numericUpDownGain1.Value = Convert.ToDecimal(value);
            }
            if (mySettings.ContainsKey("[gain2]"))
            {
                string value = mySettings["[gain2]"];
                numericUpDownGain2.Value = Convert.ToDecimal(value);
            }
            if (mySettings.ContainsKey("[open loop?]"))
            {
                string value = mySettings["[open loop?]"];
                checkBoxOpenLoop.Checked = Convert.ToBoolean(value);
                if (checkBoxOpenLoop.Checked)
                {
                    numericUpDownGain0.Value = 0;
                    numericUpDownGain1.Value = 0;
                    numericUpDownGain2.Value = 0;
                }
            }
            if (mySettings.ContainsKey("[duration gain0]"))
            {
                string value = mySettings["[duration gain0]"];
                numericUpDownGain0Interval.Value = Convert.ToDecimal(value);
            }
            if (mySettings.ContainsKey("[duration gain1]"))
            {
                string value = mySettings["[duration gain1]"];
                numericUpDownGain1Interval.Value = Convert.ToDecimal(value);
            }
            if (mySettings.ContainsKey("[duration gain2]"))
            {
                string value = mySettings["[duration gain2]"];
                numericUpDownGain2Interval.Value = Convert.ToDecimal(value);
            }
            if (mySettings.ContainsKey("[output directory for stack-by-stack writing]"))
            {
                textBoxFileStream.Text = mySettings["[output directory for stack-by-stack writing]"];
            }
        }

        private void editDefaultParamsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.FileName = "notepad.EXE";
            startInfo.Arguments = "param.log";
            System.Diagnostics.Process.Start(startInfo);
        }
        private void writeFileAfterEachStack(long lastTriggerCount_local, double stim0, double behavior0, double behavior1)
        {
            string numStr = lastTriggerCount_local.ToString("D5");
            string fileName = System.IO.Path.Combine(textBoxFileStream.Text, "TM" + numStr + ".10ch");
            using (BinaryWriter writer = new BinaryWriter(File.Open(fileName, FileMode.Create)))
            {
                //forward drift, baseline 30,000
                double val1;
                if (stim0 >= 0d)
                {
                    val1 = (3d + stim0) * 10000d; //drift > 0 is written in val1
                }
                else
                {
                    val1 = 30000d;
                }
                if(val1 < UInt16.MinValue) val1 = UInt16.MinValue;
                if(val1 > UInt16.MaxValue) val1 = UInt16.MaxValue;
                //backward drift, baseline 30,000
                double val2;
                if (stim0 <= 0d)
                {
                    val2 = (3d - stim0) * 10000d; // drift < 0 is inverted and written in val2
                }
                else
                {
                    val2 = 30000d;
                }
                if (val2 < UInt16.MinValue) val2 = UInt16.MinValue;
                if (val2 > UInt16.MaxValue) val2 = UInt16.MaxValue;
                // summed behavior from both channels
                double val3 = (1d + behavior0 + behavior1) * 10000d;
                if(val3 < UInt16.MinValue) val3 = UInt16.MinValue;
                if(val3 > UInt16.MaxValue) val3 = UInt16.MaxValue;
                writer.Write(Convert.ToUInt16(val1)); //forward drift
                writer.Write(Convert.ToUInt16(val2)); //backward drift
                writer.Write(Convert.ToUInt16(val3)); //behavior
            }
        }

        private void buttonSetOMR_Click(object sender, EventArgs e)
        {
            numericUpDownDrift1.Value = -1M;
            numericUpDownDrift1_2.Value = -1M;
            numericUpDownDrift1_3.Value = -1M;
            numericUpDownDrift1_4.Value = -1M;

            numericUpDownDrift3.Value = 1M;
            numericUpDownDrift3_2.Value = 1M;
            numericUpDownDrift3_3.Value = 1M;
            numericUpDownDrift3_4.Value = 1M;

            numericUpDownDrift2.Value = 0M;
            numericUpDownDrift4.Value = 0M;

            checkBoxOpenLoop.Checked = true;
            numericUpDownGain0.Value = 0M;
            numericUpDownGain1.Value = 0M;
            numericUpDownGain2.Value = 0M;

            numericUpDownDrift1Interval.Value = 9M;
            numericUpDownDrift2Interval.Value = 1M;
            numericUpDownDrift3Interval.Value = 9M;
            numericUpDownDrift4Interval.Value = 1M;
        }

        private void buttonSetdOMR_Click(object sender, EventArgs e)
        {
            numericUpDownDrift1.Value = 0M;
            numericUpDownDrift1_2.Value = -0.5M;
            numericUpDownDrift1_3.Value = -1M;
            numericUpDownDrift1_4.Value = -2M;

            numericUpDownDrift3.Value = 0M;
            numericUpDownDrift3_2.Value = 0.5M;
            numericUpDownDrift3_3.Value = 1M;
            numericUpDownDrift3_4.Value = 2M;

            numericUpDownDrift2.Value = 0M;
            numericUpDownDrift4.Value = 0M;

            checkBoxOpenLoop.Checked = true;
            numericUpDownGain0.Value = 0M;
            numericUpDownGain1.Value = 0M;
            numericUpDownGain2.Value = 0M;

            numericUpDownDrift1Interval.Value = 10M;
            numericUpDownDrift2Interval.Value = 2M;
            numericUpDownDrift3Interval.Value = 26M;
            numericUpDownDrift4Interval.Value = 2M;
        }

        private void buttonSetGA_Click(object sender, EventArgs e)
        {
            numericUpDownDrift1.Value = 1M;
            numericUpDownDrift1_2.Value = 1M;
            numericUpDownDrift1_3.Value = 1M;
            numericUpDownDrift1_4.Value = 1M;

            numericUpDownDrift3.Value = 1M;
            numericUpDownDrift3_2.Value = 1M;
            numericUpDownDrift3_3.Value = 1M;
            numericUpDownDrift3_4.Value = 1M;

            numericUpDownDrift2.Value = 1M;
            numericUpDownDrift4.Value = 1M;

            checkBoxOpenLoop.Checked = false;
            numericUpDownGain0.Value = 0M;
            numericUpDownGain1.Value = 100M;
            numericUpDownGain2.Value = 300M;
            numericUpDownGain1Interval.Value = 20M;
            numericUpDownGain2Interval.Value = 20M;
        }


        private void buttonSetCustom_Click(object sender, EventArgs e)
        {
            numericUpDownDrift1.Value = 0M;
            numericUpDownDrift1_2.Value = 0M;
            numericUpDownDrift1_3.Value = 0M;
            numericUpDownDrift1_4.Value = 0M;

            numericUpDownDrift3.Value = 0M;
            numericUpDownDrift3_2.Value = 0M;
            numericUpDownDrift3_3.Value = 0M;
            numericUpDownDrift3_4.Value = 0M;

            numericUpDownDrift2.Value = 0M;
            numericUpDownDrift4.Value = 0M;

            checkBoxOpenLoop.Checked = true;
            numericUpDownGain0.Value = 0M;
            numericUpDownGain1.Value = 0M;
            numericUpDownGain2.Value = 0M;

            numericUpDownDrift1Interval.Value = 5M;
            numericUpDownDrift2Interval.Value = 0M;
            numericUpDownDrift3Interval.Value = 0M;
            numericUpDownDrift4Interval.Value = 0M;
        }

        private void buttonAllWhite_Click(object sender, EventArgs e)
        {
            try
            {
                DXdev0.SetTexture(TextureLoader.FromFile(DXdev0.device, "stim(1024x768)White.bmp"));
                DXdev1.SetTexture(TextureLoader.FromFile(DXdev1.device, "stim(1024x768)White.bmp"));
                DXdev0.RenderStill();
                DXdev1.RenderStill();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: Could not read texture file from disk. Original error: " + ex.Message);
            }
        }

        private void buttonAllBlack_Click(object sender, EventArgs e)
        {
            try
            {
                DXdev0.SetTexture(TextureLoader.FromFile(DXdev0.device, "stim(1024x768)Black.bmp"));
                DXdev1.SetTexture(TextureLoader.FromFile(DXdev1.device, "stim(1024x768)Black.bmp"));
                DXdev0.RenderStill();
                DXdev1.RenderStill();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: Could not read texture file from disk. Original error: " + ex.Message);
            }
        }

        private void buttonRed_Click(object sender, EventArgs e)
        {
            try
            {
                DXdev0.SetTexture(TextureLoader.FromFile(DXdev0.device, "stim(1024x768)Red8notch.bmp"));
                DXdev1.SetTexture(TextureLoader.FromFile(DXdev1.device, "stim(1024x768)Red8notch.bmp"));
                DXdev0.RenderStill();
                DXdev1.RenderStill();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: Could not read texture file from disk. Original error: " + ex.Message);
            }
        }

        private void buttonGreen_Click(object sender, EventArgs e)
        {
            try
            {
                DXdev0.SetTexture(TextureLoader.FromFile(DXdev0.device, "stim(1024x768)Green8notch.bmp"));
                DXdev1.SetTexture(TextureLoader.FromFile(DXdev1.device, "stim(1024x768)Green8notch.bmp"));
                DXdev0.RenderStill();
                DXdev1.RenderStill();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: Could not read texture file from disk. Original error: " + ex.Message);
            }
        }

        private void buttonBlue_Click(object sender, EventArgs e)
        {
            try
            {
                DXdev0.SetTexture(TextureLoader.FromFile(DXdev0.device, "stim(1024x768)Blue8notch.bmp"));
                DXdev1.SetTexture(TextureLoader.FromFile(DXdev1.device, "stim(1024x768)Blue8notch.bmp"));
                DXdev0.RenderStill();
                DXdev1.RenderStill();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: Could not read texture file from disk. Original error: " + ex.Message);
            }
        }
    }
}
