using GoogleNetPlaces;
using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;

#if WINDOWS_UWP
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Windows.System.Display;
using Windows.System.Threading;
using Windows.UI.Xaml;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using WindowsMLDemos.Common.Helper;
#endif
// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

public class AIDemoService
{
    public bool isPreviewing = false;
    GoogLeNetPlacesModelModel model;
    public string DetectResult = string.Empty;
    public string EvalutionTime = string.Empty;
    public byte[] PreviewData;
    private static Lazy<AIDemoService> lazy = new Lazy<AIDemoService>();
    public static AIDemoService Instance
    {
        get
        {
            return lazy.Value;
        }
    }
#if WINDOWS_UWP
    MediaCapture mediaCapture;
    ThreadPoolTimer timer;
    VideoFrame currentFrame;
    MediaFrameReader mediaFrameReader;
    VideoEncodingProperties previewProperties;
#endif
    public async Task StartDetectAsync(int PreviewInterval, int ImageTargetWidth, int ImageTargetHeight, bool WideImage)
    {
#if WINDOWS_UWP
        if (mediaCapture != null)
        {
            var targetWidth = ImageTargetWidth;
            var targetHeight = ImageTargetHeight;
            try
            {
                var isWideImage = WideImage;
                timer = ThreadPoolTimer.CreatePeriodicTimer(async (source) =>
                {
                    if (mediaCapture != null)
                    {
                        try
                        {


                            //if (previewProperties.Width != (uint)ImageTargetWidth &&
                            //previewProperties.Height != (uint)ImageTargetHeight)
                            //{
                            //    previewProperties.Width = (uint)ImageTargetWidth;
                            //    previewProperties.Height = (uint)ImageTargetHeight;
                            //    await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, previewProperties);
                            //}
                            // Get information about the preview
                            VideoFrame previewFrame;
                            if (currentFrame != null)
                            {
                                if (isWideImage)
                                {
                                    var wideFrame = new VideoFrame(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, targetWidth, targetHeight);
                                    await currentFrame.CopyToAsync(wideFrame);
                                    previewFrame = wideFrame;
                                }
                                else
                                {
                                    previewFrame = currentFrame;
                                }
                                if (previewFrame != null)
                                {
                                    VideoFrame resizedFrame;
                                    if (isWideImage)
                                    {
                                        resizedFrame = previewFrame;
                                    }
                                    else
                                    {
                                        resizedFrame = await ImageHelper.ResizeVideoFrameAsync(previewFrame, previewProperties, targetWidth, targetHeight);
                                    }
                                    var startTime = DateTime.Now;
                                    DetectResult = await EvaluteImageAsync(resizedFrame);
                                    EvalutionTime = (DateTime.Now - startTime).TotalSeconds.ToString();
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogException(ex);
                        }

                    }
                }, TimeSpan.FromSeconds(PreviewInterval));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }
#endif
    }

    public async Task StartPreviewAsync()
    {
#if WINDOWS_UWP
        var frameSourceGroups = await MediaFrameSourceGroup.FindAllAsync();

        MediaFrameSourceGroup selectedGroup = null;
        MediaFrameSourceInfo colorSourceInfo = null;

        foreach (var sourceGroup in frameSourceGroups)
        {
            foreach (var sourceInfo in sourceGroup.SourceInfos)
            {
                if (sourceInfo.MediaStreamType == MediaStreamType.VideoRecord
                    && sourceInfo.SourceKind == MediaFrameSourceKind.Color)
                {
                    colorSourceInfo = sourceInfo;
                    break;
                }
            }
            if (colorSourceInfo != null)
            {
                selectedGroup = sourceGroup;
                break;
            }
        }
        var settings = new MediaCaptureInitializationSettings()
        {
            SourceGroup = selectedGroup,
            SharingMode = MediaCaptureSharingMode.ExclusiveControl,
            MemoryPreference = MediaCaptureMemoryPreference.Auto,
            StreamingCaptureMode = StreamingCaptureMode.Video
        };

        try
        {
            mediaCapture = new MediaCapture();
            await mediaCapture.InitializeAsync(settings);

            var colorFrameSource = mediaCapture.FrameSources[colorSourceInfo.Id];
            mediaFrameReader = await mediaCapture.CreateFrameReaderAsync(colorFrameSource, MediaEncodingSubtypes.Argb32);
            mediaFrameReader.FrameArrived += ColorFrameReader_FrameArrived;
            await mediaFrameReader.StartAsync();
            isPreviewing = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("MediaCapture initialization failed: " + ex.Message);
            return;
        }
#endif
    }



    public async Task LoadModelAsync()
    {
#if WINDOWS_UWP
        if (model == null)
        {
            var modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Data/StreamingAssets/GoogLeNetPlaces.onnx"));
            if (modelFile != null)
            {
                model = new GoogLeNetPlacesModelModel();
                await MLHelper.CreateModelAsync(modelFile, model);
            }
        }
#endif
    }
#if WINDOWS_UWP

    private async void ColorFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        var mediaFrameReference = sender.TryAcquireLatestFrame();
        var videoMediaFrame = mediaFrameReference?.VideoMediaFrame;
        if (videoMediaFrame != null)
        {
            var sourceFrame = videoMediaFrame.GetVideoFrame();
            if (currentFrame == null)
            {
                if (previewProperties == null)
                {
                    previewProperties = mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;
                }

                currentFrame = new VideoFrame(BitmapPixelFormat.Bgra8, (int)previewProperties.Width, (int)previewProperties.Height);
            }
            await sourceFrame.CopyToAsync(currentFrame);
            sourceFrame.Dispose();
        }
        mediaFrameReference?.Dispose();
    }

    private void _mediaCapture_CaptureDeviceExclusiveControlStatusChanged(MediaCapture sender, MediaCaptureDeviceExclusiveControlStatusChangedEventArgs args)
    {
    }

    private async Task<string> EvaluteImageAsync(VideoFrame videoFrame)
    {
        await LoadModelAsync();
        var input = new GoogLeNetPlacesModelModelInput()
        {
            sceneImage = videoFrame
        };

        try
        {
            var res = await model.EvaluateAsync(input) as GoogLeNetPlacesModelModelOutput;
            if (res != null)
            {
                return res.sceneLabel.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogException(ex);
        }
        return null;
    }

    private async Task<byte[]> EncodedBytes(SoftwareBitmap soft, Guid encoderId)
    {
        byte[] array = null;

        // First: Use an encoder to copy from SoftwareBitmap to an in-mem stream (FlushAsync)
        // Next:  Use ReadAsync on the in-mem stream to get byte[] array

        using (var ms = new InMemoryRandomAccessStream())
        {
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(encoderId, ms);
            encoder.SetSoftwareBitmap(soft);

            try
            {
                await encoder.FlushAsync();
            }
            catch (Exception ex) { return new byte[0]; }

            array = new byte[ms.Size];
            await ms.ReadAsync(array.AsBuffer(), (uint)ms.Size, InputStreamOptions.None);
        }
        return array;
    }
#endif


}
