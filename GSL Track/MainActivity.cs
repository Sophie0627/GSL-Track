using System;
using Android.App;
using Android.OS;
using Android.Runtime;

using Android.Views;
using Android.Content.PM;
using Android.Gms.Common.Apis;
using Android.Widget;
using Android.Support.V7.App;
using Android.Gms.Common;
using Android.Net;
using GSL_Track.Managers;
using GSL_Track.Helpers;
using Android.Content;
using GSL_Track.Services;
using Android.Hardware;
using Android.Gms.Location;
using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;

namespace GSL_Track
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", ScreenOrientation = ScreenOrientation.SensorPortrait, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = (ConfigChanges.KeyboardHidden | ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.Locale))]
    public class MainActivity : AppCompatActivity, IResultCallback, GoogleApiClient.IConnectionCallbacks, GoogleApiClient.IOnConnectionFailedListener
    {
        // UI Controls
        TextView textIMEI;
        TextView textConnectionStatus;
        TextView textLatitude;
        TextView textLongitude;
        TextView textAccelerometerX;
        TextView textAccelerometerY;
        TextView textAccelerometerZ;
        TextView textAltitude;
        TextView textCurrentSpeed;
        TextView textCurrentDistance;
        Button buttonStart;

        // State variables
        bool IsStarted = false;
        bool IsConnected = false;
        const int requestCheckSettings = 2002;

        // GooglePlayServices
        GoogleApiClient _apiClient;
        LocationRequest _locRequest;

        // ConnectivityManager
        ConnectivityManager cm = (ConnectivityManager)Application.Context.GetSystemService(Context.ConnectivityService);

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            SetContentView(Resource.Layout.activity_main);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            //FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            //fab.Click += FabOnClick;

            InitControls();
            RegisterDevice();
            StartMotionDevice();
        }

        #region Menu Action

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            if (menu != null)
                menu.FindItem(Resource.Id.action_exit).SetTitle("Exit");

            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_exit)
            {
                ShowExitDialog();
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        /*
        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            View view = (View) sender;
            Snackbar.Make(view, "Replace with your own action", Snackbar.LengthLong)
                .SetAction("Action", (Android.Views.View.IOnClickListener)null).Show();
        }
        */

        #endregion

        #region Initialization of Components

        private void RegisterDevice()
        {
            if (!SessionManager.IMEI.HasValue)
            {
                SessionManager.IMEI = DeviceInfoHelper.GetDeviceUniqID();
                textIMEI.Text = SessionManager.IMEI?.ToString() ?? "NOT IDENTIFIED";
            }
        }

        private void InitControls()
        {
            textIMEI = FindViewById<TextView>(Resource.Id.textIMEI);
            textConnectionStatus = FindViewById<TextView>(Resource.Id.textConnection);
            textLatitude = FindViewById<TextView>(Resource.Id.textLatitude);
            textLongitude = FindViewById<TextView>(Resource.Id.textLongitude);
            textAccelerometerX = FindViewById<TextView>(Resource.Id.textAccelerometerX);
            textAccelerometerY = FindViewById<TextView>(Resource.Id.textAccelerometerY);
            textAccelerometerZ = FindViewById<TextView>(Resource.Id.textAccelerometerZ);
            textAltitude = FindViewById<TextView>(Resource.Id.textAltitude);
            textCurrentSpeed = FindViewById<TextView>(Resource.Id.textCurrentSpeed);
            textCurrentDistance = FindViewById<TextView>(Resource.Id.textCurrentDistance);
            buttonStart = FindViewById<Button>(Resource.Id.buttonStart);

            buttonStart.Click += delegate
            {
                if (!IsStarted)
                {
                    StartTracking();
                }
                else
                {
                    StopTracking();
                }
            };

            if (IsGooglePlayServicesInstalled())
            {
                IsConnected = true;
                textConnectionStatus.Text = "ON";
            }
            else
            {
                Toast.MakeText(this, "Verify the Network Connection.", ToastLength.Long).Show();
                this.Finish();
            }

            if (AccelerometerManager.Instance.SensorManager == null)
            {
                AccelerometerManager.Instance.SensorManager = (SensorManager)GetSystemService(Context.SensorService);

                if (AccelerometerManager.Instance.SensorManager.GetDefaultSensor(SensorType.LinearAcceleration) != null && AccelerometerManager.Instance.SensorManager.GetDefaultSensor(SensorType.RotationVector) != null)
                    AccelerometerManager.Instance.StartAccelerometerTracking();
                else
                {
                    if (AppWrapper.Service != null)
                        AppWrapper.Service.OnlyGPS = true;

                    ShowNoSensorsDialog();
                }
            }

            //if (RotationManager.Instance.SensorManager == null)
            //    RotationManager.Instance.SensorManager = (SensorManager)this.GetSystemService(Context.SensorService);

            if (AccelerometerManager.Instance.CurrentSensorData != null)
            {
                textAccelerometerX.Text = AccelerometerManager.Instance.CurrentSensorData.X.ToString();
                textAccelerometerY.Text = AccelerometerManager.Instance.CurrentSensorData.Y.ToString();
                textAccelerometerZ.Text = AccelerometerManager.Instance.CurrentSensorData.Z.ToString();
            }

            if (LocationManager.Instance.CurrentLocation != null)
            {
                textLatitude.Text = LocationManager.Instance.CurrentLocation.Latitude.ToString();
                textLongitude.Text = LocationManager.Instance.CurrentLocation.Longitude.ToString();
                textAltitude.Text = LocationManager.Instance.Altitude.ToString();
                textCurrentSpeed.Text = LocationManager.Instance.Speed.ToString();

                if (LocationManager.Instance.CurrentDistance > 0)
                    textCurrentDistance.Text = Math.Round(LocationManager.Instance.CurrentDistance, 2).ToString();
            }

            LocationManager.LocationAddressChanged += LocationManager_LocationAddressChanged;
            AccelerometerManager.AccelerometerDataChanged += AccelerometerManager_AccelerometerDataChanged;
            //RotationManager.RotationDataChanged += RotationManager_CalibrationDataChanged;
        }

        private void StartMotionDevice()
        {
            if (AppWrapper.ServiceIntent == null)
            {
                AppWrapper.ServiceIntent = new Intent(this, typeof(MotionService));
                StartService(AppWrapper.ServiceIntent);
            }
        }

        #endregion

        public void StartTracking()
        {
            if (IsConnected)
            {
                if (LocationManager.Instance.IsGoogleApiClientEmpty())
                {
                    buttonStart.Text = "STOP TRACKING";
                    IsStarted = true;
                    ConnectToGooglePlayServicesClient();
                }
            }
            else
            {
                ShowInstallGooglePlayServicesDialog();
            }
        }

        public void StopTracking()
        {
            buttonStart.Text = "START TRACKING";
            textCurrentDistance.Text = "0";
            IsStarted = false;

            LocationManager.Instance.StopLocationRequest();
            AccelerometerManager.Instance.StopAccelerometerTracking();
            AccelerometerManager.Instance.SensorManager = null;

            LocationManager.Instance.DisconnectGoogleApiClient();
            LocationManager.Instance._apiClient = null;
        }

        #region Event Handlers

        void LocationManager_LocationAddressChanged()
        {
            this.RunOnUiThread(() =>
            {
                textLatitude.Text = LocationManager.Instance.CurrentLocation.Latitude.ToString();
                textLongitude.Text = LocationManager.Instance.CurrentLocation.Longitude.ToString();
                textAltitude.Text = LocationManager.Instance.Altitude.ToString();
                textCurrentSpeed.Text = LocationManager.Instance.Speed.ToString();

                // Post the LocationData
                if (IsStarted && IsConnected)
                {
                    var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
                    httpClient.DefaultRequestHeaders.Add("SOAPAction", "http://tempuri.org/PostIMEGeoLocation");
                    string IME = SessionManager.IMEI.ToString();
                    double Lat = LocationManager.Instance.CurrentLocation.Latitude;
                    double Lon = LocationManager.Instance.CurrentLocation.Longitude;
                    string soapstr = string.Format(@"<?xml version=""1.0"" encoding=""utf-8""?>
                                <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                                    <soap:Body>
                                        <PostIMEGeoLocation xmlns=""http://tempuri.org/"">
                                            <IME>{0}</IME>
                                            <Lat>{1}</Lat>
                                            <Lon>{2}</Lon>
                                        </PostIMEGeoLocation>
                                    </soap:Body>
                                </soap:Envelope>", IME, Lat, Lon);

                    var response = httpClient.PostAsync("https://www.gsltrack.com/Services/ApiService.asmx?op=PostIMEGeoLocation", new StringContent(soapstr, Encoding.UTF8, "text/xml")).Result;
                    var content = response.Content.ReadAsStringAsync().Result;
                }

                if (LocationManager.Instance.CurrentDistance > 0)
                {
                    textCurrentDistance.Text = Math.Round(LocationManager.Instance.CurrentDistance, 2).ToString();
                }
            });
        }

        void AccelerometerManager_AccelerometerDataChanged()
        {
            this.RunOnUiThread(() =>
            {
                textAccelerometerX.Text = AccelerometerManager.Instance.CurrentSensorData.X.ToString();
                textAccelerometerY.Text = AccelerometerManager.Instance.CurrentSensorData.Y.ToString();
                textAccelerometerZ.Text = AccelerometerManager.Instance.CurrentSensorData.Z.ToString();
            });
        }

        /*
        void RotationManager_CalibrationDataChanged()
        {
            
        }
        */

        public void OnResult(Java.Lang.Object result)
        {
            var locationSettingsResult = result as LocationSettingsResult;

            Statuses status = locationSettingsResult.Status;
            switch (status.StatusCode)
            {
                case CommonStatusCodes.Success:
                    RequestLocationUpdates();
                    break;
                case CommonStatusCodes.ResolutionRequired:
                    try
                    {
                        status.StartResolutionForResult(this, requestCheckSettings);
                    }
                    catch (IntentSender.SendIntentException)
                    {
                    }
                    break;
                case LocationSettingsStatusCodes.SettingsChangeUnavailable:
                    break;
            }
            //throw new NotImplementedException();
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == requestCheckSettings)
            {
                if (resultCode == Result.Ok)
                {
                    RequestLocationUpdates();
                }
                else
                {
                    Toast.MakeText(this, "Please Enable GPS.", ToastLength.Long).Show();
                }
            }
        }

        public void OnConnected(Bundle connectionHint)
        {
            BuildLocationRequest();
            textConnectionStatus.Text = "ON";
            //throw new NotImplementedException();
        }

        public void OnConnectionSuspended(int cause)
        {
            textConnectionStatus.Text = "SUSPEND";
            //throw new NotImplementedException();
        }

        public void OnConnectionFailed(ConnectionResult result)
        {
            textConnectionStatus.Text = "OFF";
            IsConnected = false;
            if (IsStarted)
            {
                StopTracking();
                ShowInstallGooglePlayServicesDialog();
            }
            //throw new NotImplementedException();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            //base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (CheckCallingOrSelfPermission(Android.Manifest.Permission.AccessFineLocation) == Permission.Granted &&
                    CheckCallingOrSelfPermission(Android.Manifest.Permission.AccessCoarseLocation) == Permission.Granted)
            {
                LocationSettingsRequest.Builder builder = new LocationSettingsRequest.Builder().AddLocationRequest(_locRequest);
                var result = LocationServices.SettingsApi.CheckLocationSettings(_apiClient, builder.Build());
                result.SetResultCallback(this);
            }
        }

        #endregion

        #region GooglePlayServices Methods

        [Obsolete]
        bool IsGooglePlayServicesInstalled()
        {
            var NetworkInfo = cm.ActiveNetworkInfo;
            if (NetworkInfo.IsConnected)
            {
                if (NetworkInfo.Type != ConnectivityType.Wifi && NetworkInfo.Type != ConnectivityType.Mobile)
                    return false;
            }

            int queryResult = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);
            if (queryResult == ConnectionResult.Success)
            {
                return true;
            }

            if (GoogleApiAvailability.Instance.IsUserResolvableError(queryResult))
            {
                string error = GoogleApiAvailability.Instance.GetErrorString(queryResult);
                Toast.MakeText(this, error, ToastLength.Long).Show();
            }

            return false;
        }

        void ConnectToGooglePlayServicesClient()
        {
            if (_apiClient == null)
                _apiClient = new GoogleApiClient.Builder(this)
                    .AddApi(LocationServices.API)
                    .AddConnectionCallbacks(this)
                    .AddOnConnectionFailedListener(this)
                    .Build();

            if (!_apiClient.IsConnected)
                _apiClient.Connect();
        }

        void BuildLocationRequest()
        {
            _locRequest = new LocationRequest();
            _locRequest.SetPriority(LocationRequest.PriorityHighAccuracy);
            _locRequest.SetFastestInterval(4 * 1000);
            _locRequest.SetInterval(5 * 1000);

            int sdk = (int)Android.OS.Build.VERSION.SdkInt;
            if (sdk < 23 ||
                (this.CheckCallingOrSelfPermission(Android.Manifest.Permission.AccessFineLocation) == Permission.Granted &&
                    this.CheckCallingOrSelfPermission(Android.Manifest.Permission.AccessCoarseLocation) == Permission.Granted))
            {
                LocationSettingsRequest.Builder builder = new LocationSettingsRequest.Builder().AddLocationRequest(_locRequest);
                builder.SetAlwaysShow(true);

                var result = LocationServices.SettingsApi.CheckLocationSettings(_apiClient, builder.Build());
                result.SetResultCallback(this);
            }
            else
            {
                Android.Support.V4.App.ActivityCompat.RequestPermissions(this, new System.String[] { Android.Manifest.Permission.AccessFineLocation, Android.Manifest.Permission.AccessCoarseLocation }, 1);
            }
        }
        void RequestLocationUpdates()
        {
            LocationManager.Instance.SetGoogleApiClient(_apiClient, _locRequest);
            LocationManager.Instance.RequestLocation();
        }

        #endregion

        #region Dialogs

        protected void ShowExitDialog()
        {
            Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(this);
            alert.SetTitle("Exit");
            alert.SetMessage("Stop tracking and close application?");
            alert.SetPositiveButton("Yes", (senderAlert, args) =>
            {
                try
                {
                    if (AppWrapper.ServiceIntent != null)
                        StopService(AppWrapper.ServiceIntent);

                    AppWrapper.ServiceIntent = null;

                    System.Environment.Exit(0);
                }
                catch (Exception e)
                {
                    Toast.MakeText(this, "Exit operation error: " + e.ToString(), ToastLength.Short).Show();
                }
            });
            alert.SetNegativeButton("Cancel", (senderAlert, args) =>
            {
            });

            Dialog dialog = alert.Create();
            dialog.Show();
        }

        protected void ShowNoSensorsDialog()
        {
            buttonStart.Text = "START TRACKING";
            IsStarted = false;

            Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(this);
            alert.SetTitle("No sensors");
            alert.SetMessage("Device sensors invalid.");
            alert.SetPositiveButton("Accept", (senderAlert, args) =>
            {
            });

            Dialog dialog = alert.Create();
            dialog.Show();
        }

        public void ShowInstallGooglePlayServicesDialog()
        {
            Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(this);
            alert.SetTitle("Google Play Services");
            alert.SetMessage("Google Play Services not installed. Verify if mobile data or Wi-Fi is enabled.");
            alert.SetPositiveButton("Cancel", (senderAlert, args) =>
            {
                buttonStart.Text = "START TRACKING";
                IsStarted = false;
            });

            Dialog dialog = alert.Create();
            dialog.Show();
        }

        #endregion
    }
}
