using Microsoft.AspNet.SignalR;
using Microsoft.WindowsAzure.MediaServices.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace AzureMediaServicesDemo
{
    public class MediaServcies
    {
        private static IHubContext _hubContext;
        private static readonly string accountName = ConfigurationManager.AppSettings["MediaServicesAccountName"];
        private static readonly string accountKey = ConfigurationManager.AppSettings["MediaServicesAccountKey"];
        private static CloudMediaContext context = null;
        private static MediaServicesCredentials cachedCredentials = null;
        private static string HubId = string.Empty;

        public static void InitMediaServices()
        {
            try
            {
                cachedCredentials = new MediaServicesCredentials(accountName, accountKey);
                context = new CloudMediaContext(cachedCredentials);
                _hubContext = GlobalHost.ConnectionManager.GetHubContext<ProgressHub>();
            }
            catch
            {
                //Catching shit....just in case
            }
        }


        public static async Task<bool> UploadMedia(HttpPostedFileBase file, string hubid)
        {
            IAsset _asset = null;

            try
            {

                if (file != null)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file.FileName);

                    //I am using SignalR to show the progress of flow and what is happening behind the scenes.
                    //Keeping in mind that multiple users will be using a web app, I am using unique Hub ID which
                    //is being generated whenever a new hub connection is created. Hence when a new user access our web app
                    //he will be given a unique Hub ID from SignalR. If I don't do this, then it is not possible to keep track of the
                    //tasks performed in the background for a specific user.
                    HubId = hubid;

                    _hubContext.Clients.Client(hubid).reportprogress("Creating Asset");

                    _asset = context.Assets.Create(fileName, AssetCreationOptions.None);

                    _hubContext.Clients.Client(hubid).reportprogress("Asset Creation Successfull");

                    _hubContext.Clients.Client(hubid).reportprogress("Creating Access Policy and Locator");
                    IAccessPolicy accessPolicy = context.AccessPolicies.Create(fileName, TimeSpan.FromDays(1), AccessPermissions.Write | AccessPermissions.List);
                    ILocator locator = context.Locators.CreateLocator(LocatorType.Sas, _asset, accessPolicy);
                    _hubContext.Clients.Client(hubid).reportprogress("Access Policy and Locator Creation Successfull");

                    //Keep this to 1 if there are no multiple uploads.
                    BlobTransferClient _client = new BlobTransferClient();
                    _client.NumberOfConcurrentTransfers = 1;
                    _client.ParallelTransferThreadCount = 1;

                    _hubContext.Clients.Client(hubid).reportprogress("Creating Asset File");
                    IAssetFile _assetFile = _asset.AssetFiles.Create(file.FileName);
                    _hubContext.Clients.Client(hubid).reportprogress("Asset File Creation Successfull");

                    _hubContext.Clients.Client(hubid).reportprogress("Upload Started");

                    await _assetFile.UploadAsync(file.InputStream, _client, locator, CancellationToken.None);

                    _hubContext.Clients.Client(hubid).reportprogress("Upload Completed");

                    _hubContext.Clients.Client(hubid).reportprogress("Removing Locator And Policies");
                    await locator.DeleteAsync();
                    await accessPolicy.DeleteAsync();

                    _hubContext.Clients.Client(HubId).reportProgress("Submitting Encoding Job");
                    IAsset _outputAsset = EncodeMedia(_asset);

                    _hubContext.Clients.Client(hubid).reportprogress("Getting Smooth Streaming URL");
                    string streamingUrl = GetPublishUrl(_outputAsset);

                    //Returning the streaming URL from SignalR because I am lazy.
                    _hubContext.Clients.Client(hubid).reportprogress("URL://" + streamingUrl);
                }
            }
            catch (Exception)
            {
                //Just in case, if any shit happens!!
            }

            return true;
        }

        /// <summary>
        /// Creating a job to encode the uploaded video. This job is then submitted to Azure where it gets executed. 
        /// The time it takes to encode the video is dependent on the size of the video uploaded.
        /// </summary>
        /// <param name="asset"></param>
        /// <returns>Returns the asset after encoding</returns>
        private static IAsset EncodeMedia(IAsset asset)
        {
            IJob job = context.Jobs.CreateWithSingleTask("Media Encoder Standard", "Adaptive Streaming", asset, "Adaptive Bitrate MP4", AssetCreationOptions.None);

            job.Submit();

            _hubContext.Clients.Client(HubId).reportProgress("Encoding Job Started");

            job = job.StartExecutionProgressTask(j =>
            {

            }, CancellationToken.None).Result;

            _hubContext.Clients.Client(HubId).reportProgress("Encoding Job Completed Successfully");

            IAsset outputAsset = job.OutputMediaAssets[0];

            return outputAsset;
        }


        /// <summary>
        /// Function to get the publish URL for the uploaded video after the encoding
        /// </summary>
        /// <param name="asset"></param>
        /// <returns>Smooth streaming URL</returns>
        private static string GetPublishUrl(IAsset asset)
        {
            //Creating a locator and requesting for the media with read permission.
            context.Locators.Create(LocatorType.OnDemandOrigin, asset, AccessPermissions.Read, TimeSpan.FromDays(30));
            context.Locators.Create(LocatorType.Sas, asset, AccessPermissions.Read, TimeSpan.FromDays(30));

            //Get all the asset files which ends with 
            IEnumerable<IAssetFile> mp4AssetFiles = asset
                    .AssetFiles
                    .ToList()
                    .Where(af => af.Name.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase));

            return asset.GetSmoothStreamingUri().ToString();
        }
    }
}