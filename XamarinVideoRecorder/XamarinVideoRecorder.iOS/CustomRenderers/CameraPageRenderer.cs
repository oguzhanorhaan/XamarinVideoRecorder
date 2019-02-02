using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using AssetsLibrary;
using AVFoundation;
using CoreGraphics;
using CoreMedia;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using XamarinVideoRecorder;
using XamarinVideoRecorder.iOS.CustomRenderers;

[assembly: ExportRenderer(typeof(CameraPage), typeof(CameraPageRenderer))]
namespace XamarinVideoRecorder.iOS.CustomRenderers
{
    public class CameraPageRenderer : PageRenderer, IAVCaptureVideoDataOutputSampleBufferDelegate
    {
        bool weAreRecording;
        AVCaptureMovieFileOutput output;
        AVCaptureDevice device;
        AVCaptureDevice audioDevice;

        AVCaptureDeviceInput input;
        AVCaptureDeviceInput audioInput;
        AVCaptureSession session;

        AVCaptureVideoPreviewLayer previewlayer;
        UIButton btnStartRecording;
        UIButton btnCancelPage;
        NSUrl url;
        UIView cameraView;
        NSTimer timer;
        UILabel timerLabel;
        public UIActivityIndicatorView activityIndicator;

        int videoLength = 15;

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

        }




        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            weAreRecording = false;


            btnStartRecording = UIButton.FromType(UIButtonType.Custom);
            btnStartRecording.Frame = new RectangleF(100, 100, 60, 50);
            btnStartRecording.SetImage(UIImage.FromFile("captureButton.png"), UIControlState.Normal);

            btnStartRecording.SetTitle("Start Recording", UIControlState.Normal);

            var screenSize = UIScreen.MainScreen.Bounds;
            var screenWidth = screenSize.Width;
            var screenHeight = screenSize.Height;

            activityIndicator = new UIActivityIndicatorView();
            activityIndicator.Frame = new RectangleF(100, 100, 60, 50);
            activityIndicator.Center = new CGPoint(screenWidth / 2, screenHeight / 2);
            
            btnStartRecording.Center = new CGPoint(screenWidth / 2, screenHeight - 40);

            //Set up session
            session = new AVCaptureSession();

            btnCancelPage = UIButton.FromType(UIButtonType.InfoLight);
            btnCancelPage.Frame = new RectangleF(200, 200, 160, 150);
            btnCancelPage.SetImage(UIImage.FromFile("icon_closemap.png"), UIControlState.Normal);
           
            btnCancelPage.Center = new CGPoint(15, 30);
            
            //Set up inputs and add them to the session
            //this will only work if using a physical device!

            Console.WriteLine("getting device inputs");
            try
            {
                //add video capture device
                device = AVCaptureDevice.DefaultDeviceWithMediaType(AVMediaType.Video);
                input = AVCaptureDeviceInput.FromDevice(device);
                session.AddInput(input);

                //add audio capture device
                audioDevice = AVCaptureDevice.DefaultDeviceWithMediaType(AVMediaType.Audio);
                audioInput = AVCaptureDeviceInput.FromDevice(audioDevice);
                session.AddInput(audioInput);

            }
            catch (Exception ex)
            {
                //show the label error.  This will always show when running in simulator instead of physical device.
                //lblError.Hidden = false;
                return;
            }
            
            //Set up preview layer (shows what the input device sees)
            Console.WriteLine("setting up preview layer");
            previewlayer = new AVCaptureVideoPreviewLayer(session);
            previewlayer.Frame = this.View.Bounds;

            //this code makes UI controls sit on top of the preview layer!  Allows you to just place the controls in interface builder
            cameraView = new UIView();
            cameraView.Layer.AddSublayer(previewlayer);
            this.View.AddSubview(cameraView);
            this.View.SendSubviewToBack(cameraView);

            Console.WriteLine("Configuring output");
            output = new AVCaptureMovieFileOutput();

            long totalSeconds = 10000;
            Int32 preferredTimeScale = 30;
            CMTime maxDuration = new CMTime(totalSeconds, preferredTimeScale);
            output.MinFreeDiskSpaceLimit = 1024 * 1024;
            output.MaxRecordedDuration = maxDuration;

            if (session.CanAddOutput(output))
            {
                session.AddOutput(output);
            }

            session.SessionPreset = AVCaptureSession.Preset640x480;

            Console.WriteLine("About to start running session");

            session.StartRunning();

            //toggle recording button was pushed.
            btnStartRecording.TouchUpInside += startStopPushed;

            btnCancelPage.TouchUpInside += (s, e) =>
            {
                (Element as CameraPage).Navigation.PopAsync();
                if (session.Running == true)
                {
                    session.StopRunning();
                }

                //session = null;
                session.RemoveInput(input);
                session.RemoveInput(audioInput);
                session.Dispose();
                DismissViewController(true, null);
            };
            View.AddSubview(btnCancelPage);

            View.AddSubview(btnStartRecording);

            timerLabel = new UILabel(new RectangleF(50, 50, 50, 50)) { TextColor = UIColor.White };
            timerLabel.Text = "00:" + videoLength;
            timerLabel.Center = new CGPoint(screenWidth / 2, 30);

            timerLabel.TextColor = UIColor.White;
            View.AddSubview(timerLabel);
            
        }
        
        void startStopPushed(object sender, EventArgs ea)
        {
            if (!weAreRecording)
            {
                var screenSize = UIScreen.MainScreen.Bounds;
                var screenWidth = screenSize.Width;
                var screenHeight = screenSize.Height;

                var time = 16;
                
                timer = NSTimer.CreateRepeatingScheduledTimer(TimeSpan.FromSeconds(1.0), delegate {

                    time -= 1;
                    if (time >= 10)
                    {
                        timerLabel.Text = "00:" + time.ToString();
                    }
                    else
                    {
                        timerLabel.Text = "00:0" + time.ToString();
                    }

                    if (time == 0)
                    {
                        StopRecording();
                    }
                });

                timer.Fire();

                var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var library = System.IO.Path.Combine(documents, "..", "Library");
                var urlpath = System.IO.Path.Combine(library, "Report.mov");

                url = new NSUrl(urlpath, false);

                NSFileManager manager = new NSFileManager();
                NSError error = new NSError();

                if (manager.FileExists(urlpath))
                {
                    Console.WriteLine("Deleting File");
                    manager.Remove(urlpath, out error);
                    Console.WriteLine("Deleted File");
                }

                AVCaptureFileOutputRecordingDelegate avDel = new MyRecordingDelegate(cameraView, this, Element);
                output.StartRecordingToOutputFile(url, avDel);
                Console.WriteLine(urlpath);
                weAreRecording = true;
                btnStartRecording.SetImage(UIImage.FromFile("captureButton_red.png"), UIControlState.Normal);
                timer.Fire();
                View.AddSubview(timerLabel);

            }
            //we were already recording.  Stop recording
            else
            {
                StopRecording();
            }
        }


        private void StopRecording()
        {
            timer.Invalidate();
            timer.Dispose();
            timer = null;

            output.StopRecording();

            weAreRecording = false;
            nfloat width = this.View.Frame.Size.Width;
            nfloat height = this.View.Frame.Size.Height;


            session.StopRunning();
            btnStartRecording.RemoveFromSuperview();
            btnCancelPage.RemoveFromSuperview();

            this.View.AddSubview(activityIndicator);
            activityIndicator.StartAnimating();
           
        }

        
        public override void ViewWillDisappear(bool animated)
        {

            if (session == null)
            {
            }

            else
            {
                if (session.Running == true)
                {
                    session.StopRunning();
                    session.RemoveInput(input);
                    session.RemoveInput(audioInput);
                }
            }

            this.btnStartRecording.TouchUpInside -= startStopPushed;


            foreach (var view in this.View.Subviews)
            {

                view.RemoveFromSuperview();
            }


            base.ViewWillDisappear(animated);
        }

        protected override void Dispose(bool disposing)
        {
            Console.WriteLine(String.Format("{0} controller disposed - {1}", this.GetType(), this.GetHashCode()));
            base.Dispose(disposing);
        }
    }

    public class MyRecordingDelegate : AVCaptureFileOutputRecordingDelegate
    {
        UIView cameraView;
        CameraPageRenderer view;
        Element element;

        public MyRecordingDelegate(UIView cameraView, CameraPageRenderer view, Element e)
        {
            this.cameraView = cameraView;
            this.view = view;
            this.element = e;
        }

        public override async void FinishedRecording(AVCaptureFileOutput captureOutput, NSUrl outputFileUrl, NSObject[] connections, NSError error)
        {
            if (UIVideo.IsCompatibleWithSavedPhotosAlbum(outputFileUrl.Path))
            {
                var library = new ALAssetsLibrary();
                library.WriteVideoToSavedPhotosAlbum(outputFileUrl, async (path, e2) =>
                {
                    if (e2 != null)
                    {
                        new UIAlertView("", e2.ToString(), null, "Error Occurred", null).Show();
                    }
                    else
                    {
                        view.activityIndicator.StopAnimating();
                        new UIAlertView("", "Saved to Photos", null, "Ok", null).Show();

                        //by using messaging center we can send data to portable CameraPage
                        NSData data = NSData.FromUrl(outputFileUrl);
                        byte[] dataBytes = new byte[data.Length];
                        System.Runtime.InteropServices.Marshal.Copy(data.Bytes, dataBytes, 0, Convert.ToInt32(data.Length));
                        MessagingCenter.Send<string, byte[]>("VideoByteArrayReady", "ByteArrayIsReady", dataBytes);
                        MessagingCenter.Send<string, string>("VideoPathReady", "VideoPathReady", outputFileUrl.AbsoluteString);

                        await (element as CameraPage).Navigation.PopAsync();

                    }
                });
            }
            else
            {
                new UIAlertView("Incompatible", "Incompatible", null, "Ok", null).Show();
            }
        }
    }

}