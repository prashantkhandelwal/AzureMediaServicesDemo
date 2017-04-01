using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;

//[assembly: OwinStartup(typeof(AzureMediaServicesDemo.ProgressHub))]
namespace AzureMediaServicesDemo
{
    public class ProgressHub : Hub
    {
        public void ReportProgress(string status, string hubid)
        {
            Clients.Client(hubid).reportprogress(status);
        }
    }
}