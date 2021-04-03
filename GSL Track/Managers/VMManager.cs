using System;
using Android.Hardware;
using GSL_Track.Models;

namespace GSL_Track.Managers
{
    public static class VMManager
    {
        public static AccelerometerDataModel ToAccelerometerDataModel(SensorEvent e)
        {
            var model = new AccelerometerDataModel();

            model.X = Convert.ToSingle(Math.Round(e.Values[0], 2));
            model.Y = Convert.ToSingle(Math.Round(e.Values[1], 2));
            model.Z = Convert.ToSingle(Math.Round(e.Values[2], 2));

            model.Time = DateTime.UtcNow;

            return model;
        }
    }
}