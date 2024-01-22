using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using Android.Widget;
using Android.Bluetooth;
using System.Linq;
using Xamarin.Essentials;
using Android;
using Java.Util;
using System.Threading;
using Syncfusion.Android.ComboBox;
using System.Collections.Generic;
using Syncfusion;
using Android.Locations;

namespace SmartHomeApp
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        bool loadedValuesFromEsp = false; 
        private TextView temperature;
        private TextView humidity;
        private BluetoothSocket _socket = null;
        Thread bluetoothThread;
        Thread bluetoothSendThread;
        SfComboBox powerCB;
        SfComboBox lightCB;
        SfComboBox neoPixelCB;
        List<string> powerOptions = new List<string>
        {
            "Siet",
            "Solar",
            "Auto",
        };
        List<string> lightOptions = new List<string>
        {  
            "Off",
            "On",
            "Auto",
        };
        List<string> neoPixelOptions = new List<string>
        {
            "Off",
            "RGB",
            "Animation",
        };
        private View colorPreview;
        private SeekBar redSeekBar, greenSeekBar, blueSeekBar;
        private TextView selectedNumberTextView;
        private SeekBar numberSeekBar;
        private TextView electricityCurrent;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);


            temperature = FindViewById<TextView>(Resource.Id.textView1);
            humidity = FindViewById<TextView>(Resource.Id.textView2);
            electricityCurrent = FindViewById<TextView>(Resource.Id.voltageTextView);

            powerCB = FindViewById<SfComboBox>(Resource.Id.sfComboBox1);
            powerCB.DataSource = powerOptions;
            powerCB.SelectedItem = powerOptions[2];

            lightCB = FindViewById<SfComboBox>(Resource.Id.sfComboBox2);
            lightCB.DataSource = lightOptions;
            lightCB.SelectedItem = lightOptions[2];

            neoPixelCB = FindViewById<SfComboBox>(Resource.Id.sfComboBox3);
            neoPixelCB.DataSource = neoPixelOptions;
            neoPixelCB.SelectedItem = neoPixelOptions[0];

            colorPreview = FindViewById<View>(Resource.Id.colorPreview);
            redSeekBar = FindViewById<SeekBar>(Resource.Id.redSeekBar);
            greenSeekBar = FindViewById<SeekBar>(Resource.Id.greenSeekBar);
            blueSeekBar = FindViewById<SeekBar>(Resource.Id.blueSeekBar);

            // Set listeners for SeekBars
            redSeekBar.ProgressChanged += OnSeekBarProgressChanged;
            greenSeekBar.ProgressChanged += OnSeekBarProgressChanged;
            blueSeekBar.ProgressChanged += OnSeekBarProgressChanged;

            selectedNumberTextView = FindViewById<TextView>(Resource.Id.selectedNumberTextView);
            numberSeekBar = FindViewById<SeekBar>(Resource.Id.numberSeekBar);
            numberSeekBar.ProgressChanged += OnNumberSeekBarProgressChanged;

            ConnectToEsp();
            StartBluetoothThread();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            View view = (View) sender;
            Snackbar.Make(view, "Replace with your own action", Snackbar.LengthLong)
                .SetAction("Action", (View.IOnClickListener)null).Show();
        }
 
        public void SendSerialMessage(string message)
        {

            if (_socket != null && _socket.IsConnected)
            {
                try
                {
                    System.IO.Stream os = _socket.OutputStream;
                    byte[] data = System.Text.Encoding.ASCII.GetBytes(message+"\n");
                    os.Write(data);
                }
                catch (Exception e)
                {
                    ConnectToEsp();
                }
            }
        }

        private void StartBluetoothThread()
        {

            // Start a new thread to read Bluetooth messages
            bluetoothThread = new System.Threading.Thread(() =>
            {
                while (_socket != null)
                {
                    ReadBluetoothMessage();
                    System.Threading.Thread.Sleep(100); // Adjust the delay as needed
                }
            });

            bluetoothThread.Start();


            bluetoothSendThread = new System.Threading.Thread(() =>
            {
                while (_socket != null)
                {
                    if(loadedValuesFromEsp)
                        sendMessage();
                    System.Threading.Thread.Sleep(100); // Adjust the delay as needed
                }
            });
            bluetoothSendThread.Start();
        }
        private void OnSeekBarProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            UpdateColorPreview();
        }
        private void OnNumberSeekBarProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            int selectedNumber = e.Progress + 1; // Add 1 to start from 1 instead of 0
            selectedNumberTextView.Text = "Hustota LED: " + selectedNumber;
        }
        private void UpdateColorPreview()
        {
            int red = redSeekBar.Progress;
            int green = greenSeekBar.Progress;
            int blue = blueSeekBar.Progress;

            Android.Graphics.Color color = Android.Graphics.Color.Rgb(red, green, blue);
            colorPreview.SetBackgroundColor(color);
        }
        private void ReadBluetoothMessage()
        {
            if (_socket.IsConnected)
            {
                try
                {
                    System.IO.Stream isStream = _socket.InputStream;
                    byte[] buffer = new byte[1024];  
                    int bytesRead = isStream.Read(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        string receivedMessage = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        // Process the receivedMessage as needed
                        receivedMessage = receivedMessage.Replace("\n", "").Replace("\r", "").Split('#')[1];
                        //message format: (char)powering_(char)outsidelight_(char)stripMode_voltage_temperature_humidity_ledDensity_Red_Green_Blue#
                        if (receivedMessage.Count(x => x == '_') < 9)
                            return;
                        string[] messageSplited = receivedMessage.Split('_');
                        if(loadedValuesFromEsp == false)
                        {
                            switch (messageSplited[0])
                            {
                                case "n":
                                    powerCB.SelectedItem = powerOptions[0];
                                    break;
                                case "s":
                                    powerCB.SelectedItem = powerOptions[1];
                                    break;
                                default:
                                    powerCB.SelectedItem = powerOptions[2];
                                    break;
                            }
                            switch (messageSplited[1])
                            {
                                case "l":
                                    lightCB.SelectedItem = lightOptions[0];
                                    break;
                                case "h":
                                    lightCB.SelectedItem = lightOptions[1];
                                    break;
                                default:
                                    lightCB.SelectedItem = lightOptions[2];
                                    break;
                            }
                            switch (messageSplited[2])
                            {
                                case "c":
                                    neoPixelCB.SelectedItem = neoPixelOptions[1];
                                    break;
                                case "a":
                                    neoPixelCB.SelectedItem = neoPixelOptions[2];
                                    break;
                                default:
                                    neoPixelCB.SelectedItem = neoPixelOptions[0];
                                    break;
                            }
                            numberSeekBar.Progress = Int32.Parse(messageSplited[6])-1;
                            redSeekBar.Progress = Int32.Parse(messageSplited[7]);
                            greenSeekBar.Progress = Int32.Parse(messageSplited[8]);
                            blueSeekBar.Progress = Int32.Parse(messageSplited[9]);

                            loadedValuesFromEsp = true;
                        }
                            electricityCurrent.Text = $"Napätie na batériach: {messageSplited[3]}V";
                            temperature.Text = $"{messageSplited[4]}°C";
                            humidity.Text = $"{messageSplited[5]}KPa";
                        

                    }
                }
                catch (Exception e)
                {

                }
            }
        }
        private void sendMessage()
        {
            //message sending
            string pw;
            switch (powerCB.SelectedItem)
            {
                case "Siet":
                    pw = "n";
                    break;
                case "Solar":
                    pw = "s";
                    break;
                default:
                    pw = "a";
                    break;
            }
            string li;
            switch (lightCB.SelectedItem)
            {
                case "On":
                    li = "h";
                    break;
                case "Off":
                    li = "l";
                    break;
                default:
                    li = "a";
                    break;
            }
            string np;
            switch (neoPixelCB.SelectedItem)
            {
                case "RGB":
                    np = "c";
                    break;
                case "Animation":
                    np = "a";
                    break;
                default:
                    np = "o";
                    break;
            }
            string messageForEsp = $"{pw}_{li}_{np}_{numberSeekBar.Progress}_{redSeekBar.Progress}_{greenSeekBar.Progress}_{blueSeekBar.Progress}#\n";

            //message format: (char)powering_(char)outsidelight_(char)stripMode_ledDensity_Red_Green_Blue#
            SendSerialMessage(messageForEsp);
        }
        private void ConnectToEsp()
        {
            BluetoothAdapter bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
            if(bluetoothAdapter == null)
            {
                return;
            }
            if ( !bluetoothAdapter.IsEnabled)
            {
                bluetoothAdapter.Enable();
                
            }

            string desiredAlias = "ESP32_BTSerial";

            var pairedDevices = bluetoothAdapter.BondedDevices;
            _socket = null;
            foreach (BluetoothDevice device in pairedDevices)
            {
                if (device.Alias == desiredAlias)
                {
                    UUID uuid = UUID.FromString("00001101-0000-1000-8000-00805F9B34FB"); // Standard SerialPortService ID
                    _socket = device.CreateRfcommSocketToServiceRecord(uuid);

                    // Establish the Bluetooth connection
                    _socket.Connect();
                    break;
                }
            }
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
        protected override void OnDestroy()
        {
            bluetoothThread.Abort();
            base.OnDestroy();
            
        }
    }
}
