﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NationalInstruments.DAQmx;

namespace Stimulus
{
    class DAQdevice
    {
        private Task myTask;
        private Task myAOtask;
        AnalogMultiChannelReader reader = null;
        AnalogSingleChannelWriter writer = null;
        public int sampleRate = 6000; // default 6000, which gives 6 KHz 
        public volatile bool taskStarted = false;
        public void StartTask(Form1 sender)
        {
            try
            {
                myTask = new Task();
                taskStarted = true;
                //Channel1 (ephys)
                myTask.AIChannels.CreateVoltageChannel(sender.behaviorChannelComboBox1.Text, "",
                        (AITerminalConfiguration)(-1), Convert.ToDouble(sender.numericUpDownVmin.Value), Convert.ToDouble(sender.numericUpDownVmax.Value), AIVoltageUnits.Volts);

                //Channel2 (ephys)
                myTask.AIChannels.CreateVoltageChannel(sender.behaviorChannelComboBox2.Text, "",
                        (AITerminalConfiguration)(-1), Convert.ToDouble(sender.numericUpDownVmin.Value), Convert.ToDouble(sender.numericUpDownVmax.Value), AIVoltageUnits.Volts);

                // external trigger (camera)
                myTask.AIChannels.CreateVoltageChannel(sender.camTriggerChannelComboBox.Text, "",
                        (AITerminalConfiguration)(-1), Convert.ToDouble(sender.numericUpDownPhysCh4Vmin.Value), Convert.ToDouble(sender.numericUpDownPhysCh4Vmax.Value), AIVoltageUnits.Volts);
                
                // external trigger (2Photon)
                myTask.AIChannels.CreateVoltageChannel(sender.twophotonTriggerChannelComboBox.Text, "",
                        (AITerminalConfiguration)(-1), Convert.ToDouble(sender.numericUpDownPhysCh4Vmin.Value), Convert.ToDouble(sender.numericUpDownPhysCh4Vmax.Value), AIVoltageUnits.Volts);
                
                // temp. sensor
                myTask.AIChannels.CreateVoltageChannel(sender.comboBoxTempSensor.Text, "",
                        (AITerminalConfiguration)(-1), 0d, 5.0d, AIVoltageUnits.Volts);

                // patch channel1
                myTask.AIChannels.CreateVoltageChannel(sender.patchingChannelComboBox1.Text, "",
                        (AITerminalConfiguration)(-1), Convert.ToDouble(sender.numericUpDownVminPatching.Value), Convert.ToDouble(sender.numericUpDownVmaxPatching.Value), AIVoltageUnits.Volts);

                // patch channel2
                myTask.AIChannels.CreateVoltageChannel(sender.patchingChannelComboBox2.Text, "",
                        (AITerminalConfiguration)(-1), Convert.ToDouble(sender.numericUpDownVminPatching.Value), Convert.ToDouble(sender.numericUpDownVmaxPatching.Value), AIVoltageUnits.Volts);


                // hardware-timed data acquisition, internal clock. Last arg is the number of samples used for buffer size
                myTask.Timing.ConfigureSampleClock("", (double)sampleRate, SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, 600000);

                reader = new AnalogMultiChannelReader(myTask.Stream);
                myTask.Start();
                //Verify the Task
                //myTask.Control(TaskAction.Verify);
            }
            catch (DaqException exception)
            {
                MessageBox.Show(exception.Message);
                myTask.Dispose();
                return;
            }
        }

        public void StartAOtask(Form1 sender)
        {
            try
            {
                myAOtask = new Task();
                myAOtask.AOChannels.CreateVoltageChannel(sender.ComboBoxLEDcontrol.Text, "", 0d, 5d, AOVoltageUnits.Volts);
                writer = new AnalogSingleChannelWriter(myAOtask.Stream);
                myAOtask.Start();
            }
            catch (DaqException exception)
            {
                MessageBox.Show(exception.Message);
                return;
            }
        }
        public double[,] ReadInput(int samplesPerChan)
        {
            double[,] readData;
            try
            {
                readData = reader.ReadMultiSample(samplesPerChan); //-1 for samplesPerChannel tells to read all available sampls available in the buffer
                return readData;
            }
            catch (DaqException exception)
            {
                MessageBox.Show("file DAQdevise.cs, line 85: " + exception.Message);
                readData = null;
                return readData;
            }
        }
        public void writeAOvalue(double val)
        {
            try
            {
                writer.WriteSingleSample(false, val);
            }
            catch (DaqException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void StopTask()
        {
                myTask.Stop();
                myTask.Dispose();
                myAOtask.Stop();
                myAOtask.Dispose();
        }
    }
}
