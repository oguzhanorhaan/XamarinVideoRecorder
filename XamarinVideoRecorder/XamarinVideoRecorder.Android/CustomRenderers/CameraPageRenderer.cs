using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.IO;
using Java.Lang;
using Plugin.CurrentActivity;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using XamarinVideoRecorder;
using XamarinVideoRecorder.Droid.CustomRenderers;
using static Android.Hardware.Camera;

[assembly: Xamarin.Forms.ExportRenderer(typeof(CameraPage), typeof(CameraPageRenderer))]
namespace XamarinVideoRecorder.Droid.CustomRenderers
{
    public class CameraPageRenderer : PageRenderer, ISurfaceHolderCallback, MediaRecorder.IOnInfoListener
    {
        MediaRecorder recorder;
        ISurfaceHolder Holder;
        bool isVideoStarted = false;
        global::Android.Widget.Button captureButton;
        Android.Widget.RelativeLayout buttonHolder;
        global::Android.Views.View view;
        static Android.Hardware.Camera camera = null;
        string path = "";
        Activity CurrentContext => CrossCurrentActivity.Current.Activity;
        private Chronometer timer;
        Android.Widget.RelativeLayout.LayoutParams captureButtonParams;
        VideoView videoView;
        Activity Activity => this.Context as Activity;
        Android.Widget.RelativeLayout mainLayout;

        public CameraPageRenderer(Context context) : base(context)
        {

        }

        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.Page> e)
        {
            base.OnElementChanged(e);
            if (camera != null)
            {
                camera.Release();
                camera = null;
            }
            camera = Android.Hardware.Camera.Open();
            camera.SetDisplayOrientation(90);
            Parameters parameters = camera.GetParameters();
            parameters.FocusMode = Parameters.FocusModeContinuousVideo;
            camera.SetParameters(parameters);

            SetupUserInterface();
            SetupEventHandlers();
            AddView(view);

        }

        void SetupUserInterface()
        {
            var metrics = Resources.DisplayMetrics;
            view = CurrentContext.LayoutInflater.Inflate(Resource.Layout.CameraLayout, this, false);

            videoView = view.FindViewById<VideoView>(Resource.Id.textureView);
            timer = view.FindViewById<Chronometer>(Resource.Id.timerId);
            mainLayout = new Android.Widget.RelativeLayout(Context);


            Android.Widget.RelativeLayout.LayoutParams liveViewParams = new Android.Widget.RelativeLayout.LayoutParams(
               Android.Widget.RelativeLayout.LayoutParams.FillParent,
               Android.Widget.RelativeLayout.LayoutParams.FillParent);

            liveViewParams.Width = metrics.WidthPixels;  // 80%
            liveViewParams.Height = (metrics.HeightPixels / 5) * 4;

            captureButton = view.FindViewById<Android.Widget.Button>(Resource.Id.takePhotoButton);

            buttonHolder = new Android.Widget.RelativeLayout(Context);

            captureButtonParams = new Android.Widget.RelativeLayout.LayoutParams(
                Android.Widget.RelativeLayout.LayoutParams.FillParent,
                Android.Widget.RelativeLayout.LayoutParams.FillParent);
            captureButtonParams.Width = metrics.WidthPixels;
            captureButtonParams.Height = (metrics.HeightPixels / 5) * 1;
            captureButtonParams.AddRule(LayoutRules.AlignParentBottom);
            buttonHolder.LayoutParameters = captureButtonParams;

            timer.Visibility = ViewStates.Invisible;

            Holder = videoView.Holder;
            Holder.AddCallback(this);
            Holder.SetType(SurfaceType.PushBuffers);
        }



        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            base.OnLayout(changed, l, t, r, b);
            var msw = MeasureSpec.MakeMeasureSpec(r - l, MeasureSpecMode.Exactly);
            var msh = MeasureSpec.MakeMeasureSpec(b - t, MeasureSpecMode.Exactly);

            view.Measure(msw, msh);
            view.Layout(0, 0, r - l, b - t);

            captureButton.Visibility = ViewStates.Visible;

        }


        private void initRecorder()
        {
            recorder = new MediaRecorder();
            path = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/PincidentReport.mp4";

            recorder.SetCamera(camera);
            string manufacturer = Build.Manufacturer;

            if (ActivityCompat.CheckSelfPermission(CurrentContext, Manifest.Permission.RecordAudio) != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(CurrentContext, new System.String[] { Manifest.Permission.RecordAudio },
                       10);
            }
            else
            {
                if (manufacturer.ToLower().Contains("samsung"))
                {
                    recorder.SetAudioSource(AudioSource.VoiceCommunication);
                }
                else
                {
                    recorder.SetAudioSource(AudioSource.Default);
                }
            }

            recorder.SetVideoSource(VideoSource.Camera);

            recorder.SetOutputFile(path);

            recorder.SetOnInfoListener(this);
            recorder.SetPreviewDisplay(videoView.Holder.Surface);
            recorder.SetOutputFormat(OutputFormat.Default);
            var SupportedVideoFrameRateByCamera = camera.GetParameters().SupportedPreviewFrameRates;
            var SmallToBigFrameRates = SupportedVideoFrameRateByCamera.Reverse();

            var maxFrameRate = SmallToBigFrameRates.First().IntValue();

            recorder.SetVideoFrameRate(maxFrameRate);



            var SupportedSizesByCamera = camera.GetParameters().SupportedVideoSizes;

            var SmallToBigSizesList = SupportedSizesByCamera.Reverse();

            Android.Hardware.Camera.Size size = new Android.Hardware.Camera.Size(camera, 0, 0);


            foreach (var videoSize in SmallToBigSizesList) // The closest Height to 480
            {
                if (videoSize.Height == 480)
                {
                    size = videoSize;
                    break;
                }
                else if (videoSize.Height > 480)
                {
                    size = videoSize;
                    break;
                }

            }

            if (size.Height == 0)
            {
                size = SmallToBigSizesList.ElementAt(0);
            }

            recorder.SetVideoSize(size.Width, size.Height);

            recorder.SetVideoEncoder(VideoEncoder.Mpeg4Sp);// MPEG_4_SP
            recorder.SetAudioEncoder(AudioEncoder.Aac);

            recorder.SetMaxDuration(15000);

            recorder.SetOrientationHint(90);

        }



        private void prepareRecorder()
        {
            try
            {
                recorder.Prepare();
            }
            catch (IllegalStateException e)
            {

            }
            catch (Java.IO.IOException e)
            {

            }
        }




        void SetupEventHandlers()
        {
            //capturePhotoButton
            captureButton.Click += async (sender, e) =>
            {
                if (isVideoStarted == false)
                {

                    isVideoStarted = true;
                    captureButton.Background = Resources.GetDrawable(Resource.Drawable.captureButton_red);
                    recorder.Start();
                    //---------------------------------------------------------------
                    timer.Base = SystemClock.ElapsedRealtime();
                    timer.Visibility = ViewStates.Visible;
                    timer.Start();
                }
                else
                {
                    StopRecording();
                }
            };
        }


        private void StopRecording()
        {
            timer.Stop();
            timer.Visibility = ViewStates.Invisible;
            //--------------------------------------------------
            isVideoStarted = false;
            recorder.Stop();
            recorder.Release();
            camera.Lock();
            camera.StopPreview();
            camera.Release();


            FileInputStream inputStream = null;
            inputStream = new FileInputStream(path);
            byte[] bytes;
            byte[] buffer = new byte[8192];
            int bytesRead;


            ByteArrayOutputStream output = new ByteArrayOutputStream();
            try
            {
                while ((bytesRead = inputStream.Read(buffer)) != -1)
                {
                    output.Write(buffer, 0, bytesRead);
                }
            }
            catch (Java.Lang.Exception exception)
            {
                //  e.PrintStackTrace();
            }
            bytes = output.ToByteArray();
            string attachedFile = Base64.EncodeToString(bytes, Base64.Default);

            captureButton.RemoveFromParent();

            //on the CameraPage you can get base64 and video path
            MessagingCenter.Send<string, string>("VideoBase64Ready", "VideoIsReady", attachedFile);

            MessagingCenter.Send<string, string>("VideoPathReady", "VideoPathReady", path);
        }



        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
        {

        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {

            //If authorisation not granted for camera
            if (ContextCompat.CheckSelfPermission(CurrentContext, Manifest.Permission.Camera) != Permission.Granted)
                //ask for authorisation
                ActivityCompat.RequestPermissions(CurrentContext, new System.String[] { Manifest.Permission.Camera }, 50);
            else
            {
                if (camera != null)
                {
                    camera.Release();
                    camera = null;
                }
                camera = Android.Hardware.Camera.Open();
                camera.SetDisplayOrientation(90);


                Parameters parameters = camera.GetParameters();
                parameters.FocusMode = Parameters.FocusModeContinuousVideo;
                camera.SetParameters(parameters);

                camera.SetPreviewDisplay(holder);
                camera.StartPreview();
                initRecorder();
                camera.Unlock();
            }
            
            prepareRecorder();

        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            if (isVideoStarted)
            {
                recorder.Stop();
                isVideoStarted = false;
            }
            recorder.Release();
            camera.Release();
        }

        public void OnInfo(MediaRecorder mr, [GeneratedEnum] MediaRecorderInfo what, int extra)
        {
            if (what == MediaRecorderInfo.MaxDurationReached)
            {
                StopRecording();
            }
        }
    }
}