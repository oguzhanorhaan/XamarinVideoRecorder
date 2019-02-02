using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace XamarinVideoRecorder
{
    //NOTE: There are messaging center implementations in CustomRenderers for your usages, you can usage data in this class by implementing your messaging center in here 

	public class CameraPage : ContentPage
	{
		public CameraPage ()
		{
            BackgroundColor = Color.Black;
            NavigationPage.SetHasNavigationBar(this, false);
           
        }
	}
}