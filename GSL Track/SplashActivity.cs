using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;

namespace GSL_Track
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.Splash", MainLauncher = true, NoHistory = true)]
    public class SplashActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
        }

        // Launches the startup task
        protected override void OnResume()
        {
            base.OnResume();

            StartActivity(new Intent(Application.Context, typeof(MainActivity)));

            Finish();
        }

        // Prevent the back button from canceling the startup process
        public override void OnBackPressed() { }
        
    }
}