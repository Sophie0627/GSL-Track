using Android.Hardware;
using Android.Runtime;
using GSL_Track.Models;

namespace GSL_Track.Managers
{
    public delegate void AccelerometerDataChangedHandler();

    public class AccelerometerManager : Java.Lang.Object, ISensorEventListener
    {
        static readonly object _syncLock = new object();

        public SensorManager SensorManager { get; set; }
        public AccelerometerDataModel CurrentSensorData { get; private set; }

        public static event AccelerometerDataChangedHandler AccelerometerDataChanged;

        #region Singletone

        private static AccelerometerManager instance;

        private AccelerometerManager()
        {
        }

        public static AccelerometerManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AccelerometerManager();
                }
                return instance;
            }
        }

        #endregion


        #region Public Methods

        public void StartAccelerometerTracking()
        {
            //currentInterval = -1;

            SensorManager.RegisterListener(this, SensorManager.GetDefaultSensor(SensorType.LinearAcceleration), SensorDelay.Normal);
        }

        public void StopAccelerometerTracking()
        {
            if (SensorManager != null)
                SensorManager.UnregisterListener(this);
        }

        #endregion


        #region ISensorEventListener Implementation

        public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
        {
            // throw new NotImplementedException();
        }

        public void OnSensorChanged(SensorEvent e)
        {
            lock (_syncLock)
            {
                var model = VMManager.ToAccelerometerDataModel(e);
                CurrentSensorData = model;

                AccelerometerDataChanged?.Invoke();
            }
            
            // throw new NotImplementedException();
        }

        #endregion


        //int currentInterval = -1;
        //List<AccelerometerDataModel> packetData = new List<AccelerometerDataModel>();
    }
}