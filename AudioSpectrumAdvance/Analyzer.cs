using System;
using System.Collections.Generic;
using System.IO.Ports;
//using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using Un4seen.Bass;
using Un4seen.BassWasapi;
using System.Windows.Forms.DataVisualization.Charting;
using System.Threading.Tasks;

namespace AudioSpectrumAdvance
{

    internal class Analyzer
    {
        private bool _enable;               //enabled status
        private DispatcherTimer _t;         //timer that refreshes the display
        public float[] _fft;               //buffer for fft data
        private ProgressBar _l, _r;         //progressbars for left and right channel intensity
        private WASAPIPROC _process;        //callback function to obtain data
        private int _lastlevel;             //last output level
        private int _hanctr;                //last output level counter
        public List<byte> _spectrumdata;   //spectrum data buffer
        private Spectrum _spectrum;         //spectrum dispay control
        private ComboBox _devicelist;       //device list
        private bool _initialized;          //initialized flag
        private int devindex;               //used device index
        private Chart _chart;
        public static int high_low;

        private GroupBox _optionen;
        private RadioButton _red_btn, _green_btn, _blue_btn, _yellow_btn, _cyan_btn, _magenta_btn, _white_btn, _fade_btn;
        private decimal fade_clr, fade_durchlauf = 0;
        private int fade_durchlauf_ganz = 0;

        private SerialPort port = new SerialPort("COM3", 9600);

        private int _lines = 64;            // number of spectrum lines

        //ctor
        public Analyzer(ProgressBar left, ProgressBar right, Spectrum spectrum, ComboBox devicelist , Chart chart, GroupBox optionen, RadioButton red_btn, RadioButton green_btn, RadioButton blue_btn, RadioButton yellow_btn, RadioButton cyan_btn, RadioButton magenta_btn, RadioButton white_btn, RadioButton fade_btn)
        {
            BassNet.Registration("marius.niveri@gmail.com", "2X2232923152222");

            _fft = new float[8192];
            _lastlevel = 0;
            _hanctr = 0;
            _t = new DispatcherTimer();
            _t.Tick += _t_Tick;
            _t.Interval = TimeSpan.FromMilliseconds(25); //40hz refresh rate//25
            _t.IsEnabled = false;
            _l = left;
            _r = right;
            _l.Minimum = 0;
            _r.Minimum = 0;
            _r.Maximum = (ushort.MaxValue);
            _l.Maximum = (ushort.MaxValue);
            _process = new WASAPIPROC(Process);
            _spectrumdata = new List<byte>();
            _spectrum = spectrum;
            _chart = chart;
            _devicelist = devicelist;
            _initialized = false;

            _optionen = optionen;
            _red_btn = red_btn;
            _green_btn = green_btn;
            _blue_btn = blue_btn;
            _yellow_btn = yellow_btn;
            _cyan_btn = cyan_btn;
            _magenta_btn = magenta_btn;
            _white_btn = white_btn;
            _fade_btn = fade_btn;

            chart.Series.Add("wave");
            chart.Series["wave"].ChartType = SeriesChartType.FastLine;
            chart.Series["wave"].ChartArea = "ChartArea1";

            chart.ChartAreas["ChartArea1"].AxisX.MajorGrid.Enabled = false;
            chart.ChartAreas["ChartArea1"].AxisY.MajorGrid.Enabled = false;
            chart.ChartAreas["ChartArea1"].AxisY.Maximum = 255;
            chart.ChartAreas["ChartArea1"].AxisY.Minimum = 0;
            chart.ChartAreas["ChartArea1"].AxisX.Maximum = 32;
            chart.ChartAreas["ChartArea1"].AxisX.Minimum = 0;
            for (int i = 0; i < chart.ChartAreas["ChartArea1"].AxisX.Maximum; i++)
            {
                chart.Series["wave"].Points.Add(0);
            }

            Init();
        }

        // flag for display enable
        public bool DisplayEnable { get; set; }

        //flag for enabling and disabling program functionality
        public bool Enable
        {
            get { return _enable; }
            set
            {
                _enable = value;
                if (value)
                {
                    if (!_initialized)
                    {
                        var array = (_devicelist.Items[_devicelist.SelectedIndex] as string).Split(' ');
                        devindex = Convert.ToInt32(array[0]);
                        bool result = BassWasapi.BASS_WASAPI_Init(devindex, 0, 0, BASSWASAPIInit.BASS_WASAPI_BUFFER, 1f, 0.05f, _process, IntPtr.Zero);
                        if (!result)
                        {
                            var error = Bass.BASS_ErrorGetCode();
                            MessageBox.Show(error.ToString());
                        }
                        else
                        {
                            _initialized = true;
                            _devicelist.Enabled = false;
                        }
                    }
                    BassWasapi.BASS_WASAPI_Start();
                }
                else BassWasapi.BASS_WASAPI_Stop(true);
                System.Threading.Thread.Sleep(500);
                _t.IsEnabled = value;
            }
        }

        // initialization
        private void Init()
        {
            bool result = false;
            for (int i = 0; i < BassWasapi.BASS_WASAPI_GetDeviceCount(); i++)
            {
                var device = BassWasapi.BASS_WASAPI_GetDeviceInfo(i);
                if (device.IsEnabled && device.IsLoopback)
                {
                    _devicelist.Items.Add(string.Format("{0} - {1}", i, device.name));
                }
            }
            _devicelist.SelectedIndex = 0;
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATETHREADS, false);
            result = Bass.BASS_Init(0, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
            if (!result) throw new Exception("Init Error");
        }

        //timer 
        private void _t_Tick(object sender, EventArgs e)
        {
                int ret = BassWasapi.BASS_WASAPI_GetData(_fft, (int)BASSData.BASS_DATA_FFT8192);  //get ch.annel fft data
                if (ret < -1) return;
                int x, y;
                int b0 = 0;

                //computes the spectrum data, the code is taken from a bass_wasapi sample.
                for (x = 0; x < 64; x++)
                {
                    float peak = 0;
                    int b1 = (int)Math.Pow(2, x * 10.0 / (_lines - 1));
                    if (b1 > 1023) b1 = 1023;
                    if (b1 <= b0) b1 = b0 + 1;
                    for (; b0 < b1; b0++)
                    {
                        if (peak < _fft[1 + b0]) peak = _fft[1 + b0];
                    }
                    y = (int)(Math.Sqrt(peak) * 3 * 255 - 4);
                    if (y > 255) y = 255;
                    if (y < 0) y = 0;
                    _spectrumdata.Add((byte)y);
                    //Console.WriteLine("{0, 3} ", y);
                }



                if (DisplayEnable) _spectrum.Set(_spectrumdata);
                for (int i = 0; i < _spectrumdata.ToArray().Length; i++)
                {
                    try
                    {
                        _chart.Series["wave"].Points.Add(_spectrumdata[i]);
                    }
                    catch (Exception)
                    {
                    }
                    try
                    {
                        _chart.Series["wave"].Points.RemoveAt(0);
                    }
                    catch (Exception)
                    {
                    }

                }

                if (!port.IsOpen)
                {
                    try
                    {
                        int dataforduino = 0;
                        if (high_low == 1)
                        {
                            dataforduino = (_spectrumdata[0] + _spectrumdata[1] + _spectrumdata[2] + _spectrumdata[1]) * 2;
                        }
                        else if (high_low == 2)
                        {
                            dataforduino = (_spectrumdata[11] + _spectrumdata[11] + _spectrumdata[12] + _spectrumdata[12]) * 2;
                        }
                        else if (high_low == 3)
                        {
                            dataforduino = (_spectrumdata[30] + _spectrumdata[30] + _spectrumdata[32] + _spectrumdata[32]) * 2;
                        }
                        dataforduino = dataforduino / 4;

                        port.Open();

                        if (_red_btn.Checked == true)
                        {
                            port.Write(dataforduino + ",0,20,0\n");
                        }
                        else if (_green_btn.Checked == true)
                        {
                            port.Write("0," + dataforduino + ",20,0\n");
                        }
                        else if (_blue_btn.Checked == true)
                        {
                            port.Write("0,20," + dataforduino + ",0\n");
                        }
                        else if (_yellow_btn.Checked == true)
                        {
                            port.Write(dataforduino + "," + dataforduino + ",0,0\n");
                        }
                        else if (_cyan_btn.Checked == true)
                        {
                            port.Write("0," + dataforduino + "," + dataforduino + ",0\n");
                        }
                        else if (_magenta_btn.Checked == true)
                        {
                            port.Write(dataforduino + ",0," + dataforduino + ",0\n");
                        }
                        else if (_white_btn.Checked == true)
                        {
                            port.Write(dataforduino + "," + dataforduino + "," + dataforduino + ",0\n");
                        }
                        else if (_fade_btn.Checked == true)
                        {
                            decimal color_fade = dataforduino;
                            if (fade_durchlauf_ganz == 0)
                            {
                                fade_durchlauf++;
                                if (fade_durchlauf == 256)
                                {
                                    fade_durchlauf = 0;
                                    fade_durchlauf_ganz = 1;
                                }
                                else
                                {
                                    port.Write(Math.Round((color_fade / 255) * (255 - fade_durchlauf), 0) + "," + Math.Round((color_fade / 255) * fade_durchlauf, 0) + "," + "0" + ",0\n");
                                }
                            }
                            else if (fade_durchlauf_ganz == 1)
                            {
                                fade_durchlauf++;
                                if (fade_durchlauf == 256)
                                {
                                    fade_durchlauf = 0;
                                    fade_durchlauf_ganz = 2;
                                }
                                else
                                {
                                    port.Write("0" + "," + Math.Round((color_fade / 255) * (255 - fade_durchlauf), 0) + "," + Math.Round((color_fade / 255) * fade_durchlauf, 0) + ",0\n");
                                }
                            }
                            else if (fade_durchlauf_ganz == 2)
                            {
                                fade_durchlauf++;
                                if (fade_durchlauf == 256)
                                {
                                    fade_durchlauf = 0;
                                    fade_durchlauf_ganz = 0;
                                }
                                else
                                {
                                    port.Write(Math.Round((color_fade / 255) * fade_durchlauf, 0) + "," + "0" + "," + Math.Round((color_fade / 255) * (255 - fade_durchlauf), 0) + ",0\n");
                                }
                            }
                        }

                        port.Close();
                    }
                    catch
                    {

                    }
                }

                _spectrumdata.Clear();


                int level = BassWasapi.BASS_WASAPI_GetLevel();
                _l.Value = (Utils.LowWord32(level));
                _r.Value = (Utils.HighWord32(level));
                if (level == _lastlevel && level != 0) _hanctr++;
                _lastlevel = level;

                //Required, because some programs hang the output. If the output hangs for a 75ms
                //this piece of code re initializes the output so it doesn't make a gliched sound for long.
                if (_hanctr > 3)
                {
                    _hanctr = 0;
                    _l.Value = (0);
                    _r.Value = (0);
                    Free();
                    Bass.BASS_Init(0, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
                    _initialized = false;
                    Enable = true;
                }

        }

        // WASAPI callback, required for continuous recording
        private int Process(IntPtr buffer, int length, IntPtr user)
        {
            return length;
        }

        //cleanup
        public void Free()
        {
            BassWasapi.BASS_WASAPI_Free();
            Bass.BASS_Free();
        }

        private void _low_btn_Click(object sender, EventArgs e)
        {
            MessageBox.Show("HI");
        }
    }
}
