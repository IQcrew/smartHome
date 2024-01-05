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

namespace SmartHomeApp
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private Button btn;
        private Button SendButton;
        private BluetoothSocket _socket = null;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            btn = FindViewById<Button>(Resource.Id.button1);
            btn.Click += OnButtonClicked;

            SendButton = FindViewById<Button>(Resource.Id.button2);
            SendButton.Click += SendInformationOnClick;
            ConnectToEsp();
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
        private void OnButtonClicked(object sender, EventArgs eventArgs)
        {

            
        }
        public void SendInformationOnClick(object sender, EventArgs e)
        {
            SendSerialMessage("TEST");
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

        private void ConnectToEsp()
        {
            BluetoothAdapter bluetoothAdapter = BluetoothAdapter.DefaultAdapter;

            if (bluetoothAdapter == null || !bluetoothAdapter.IsEnabled)
            {

                return;
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
        
    }
}
