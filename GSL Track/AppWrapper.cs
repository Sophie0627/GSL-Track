using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using GSL_Track.Services;

namespace GSL_Track
{
    [Application]
    public class AppWrapper : Application
    {
        public static Intent ServiceIntent { get; set; }

        public static MotionService Service { get; set; }

        public AppWrapper(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();
        }
    }
}