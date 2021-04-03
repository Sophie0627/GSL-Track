using Android.Hardware;
using Android.Runtime;
using GSL_Track.Models;

namespace GSL_Track.Managers
{
    public delegate void RotationDataChangedHandler();

    public class RotationManager : Java.Lang.Object, ISensorEventListener
    {
        public SensorManager SensorManager { get; set; }
        public static event RotationDataChangedHandler RotationDataChanged;
        public AccelerometerDataModel CurrentSensorData { get; private set; }

        #region Singletone

        private static RotationManager instance;

        private RotationManager()
        {
        }

        public static RotationManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new RotationManager();
                }
                return instance;
            }
        }

        #endregion

        #region Public Methods

        public void StartRotationTracking()
        {
            SensorManager.RegisterListener(this, SensorManager.GetDefaultSensor(SensorType.RotationVector), SensorDelay.Normal);
        }

        public void StopRotationTracking()
        {
            SensorManager.UnregisterListener(this);
        }

        #endregion

        #region ISensorEventListener Implementation

        public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
        {
            //throw new System.NotImplementedException();
        }

        public void OnSensorChanged(SensorEvent e)
        {
            var model = VMManager.ToAccelerometerDataModel(e);

            CurrentSensorData = model;

            //StopRotationTracking();

            //RotationDataChanged?.Invoke();
            //throw new System.NotImplementedException();
        }

        #endregion
    }
}