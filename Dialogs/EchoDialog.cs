using System;
using System.Threading.Tasks;

using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Net.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

using System.Collections.Generic;
using System.Net;
using System.IO;
using Newtonsoft.Json;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    [Serializable]
    public class EchoDialog : IDialog<object>
    {
        protected int count = 1;

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;

            string json = JsonConvert.SerializeObject(message.ChannelData, Formatting.Indented);
            System.Diagnostics.Trace.TraceInformation("CHANNELDATA - " + json);

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(System.Environment.GetEnvironmentVariable("AzureBlobStorageConnectionString"));
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(System.Environment.GetEnvironmentVariable("AzureBlobStorageContainerReference"));

            if (message.Attachments.Count > 0)
            {
                System.Diagnostics.Trace.TraceInformation("[In attachment path]");
                
                //var blobRef = message.Conversation.Id + "/" + message.Timestamp.Value.ToUnixTimeSeconds().ToString();
                var blobRef = message.From.Id + "/" + message.Timestamp.Value.ToUnixTimeSeconds().ToString();
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobRef);

                try
                {
                    blockBlob.Properties.ContentType = message.Attachments[0].ContentType;
                    blockBlob.SetProperties();
                }
                catch (Exception e)
                {
                    System.Diagnostics.Trace.TraceError(e.Message);
                }
                System.Diagnostics.Trace.TraceInformation("[In attachment path] - after blob setup: BlobRef = " + blobRef);
                System.Diagnostics.Trace.TraceInformation("ContentURL - " + message.Attachments[0].ContentUrl);

                try
                {
                    // Get the attachment
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(message.Attachments[0].ContentUrl);
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    using (Stream inputStream = response.GetResponseStream())
                    {
                        blockBlob.UploadFromStream(inputStream);
                    }
                    System.Diagnostics.Trace.TraceInformation("[In attachment path] - Attachment uploaded");

                    // Section for echoing back attachment
                    CloudBlockBlob blockBlob2 = container.GetBlockBlobReference(blobRef);
                    var replyMessage = context.MakeMessage();
                    replyMessage.Attachments = new List<Attachment>();
                    replyMessage.Attachments.Add(new Attachment()
                    {
                        ContentUrl = blockBlob2.Uri.AbsoluteUri,
                        ContentType = message.Attachments[0].ContentType,
                        Name = "1.jpg"
                    });
                    await context.PostAsync(replyMessage);
                    System.Diagnostics.Trace.TraceInformation("[Exiting Attachment Path]");
                }
                catch (Exception e)
                {
                    System.Diagnostics.Trace.TraceError(e.Message);
                }
            }

            if (message.Text.ToLower() == "show")
            {
                CloudBlobDirectory blockBlob = container.GetDirectoryReference(message.From.Id);

                var replyMessage = context.MakeMessage();
                int count = 0;
                replyMessage.Attachments = new List<Attachment>();

                foreach (IListBlobItem item in blockBlob.ListBlobs())
                {
                    if (item.GetType() == typeof(CloudBlockBlob))
                    {
                        CloudBlockBlob blob = (CloudBlockBlob)item;
                        replyMessage.Attachments.Add(new Attachment()
                        {
                            ContentUrl = blob.Uri.AbsoluteUri,
                            ContentType = blob.Properties.ContentType,
                            Name = "1" + count++.ToString() + ".jpg"
                        });
                        System.Diagnostics.Trace.TraceInformation("[In attachment path] - added item to reply");
                    }
                }
                await context.PostAsync(replyMessage);
            }

            if (message.Text.ToLower() == "reset")
            {
                PromptDialog.Confirm(
                    context,
                    AfterResetAsync,
                    "Are you sure you want to reset the count?",
                    "Didn't get that!",
                    promptStyle: PromptStyle.Auto);
            }
            else
            {
                await context.PostAsync($"{this.count++}: You saidd {message.Text}");
                context.Wait(MessageReceivedAsync);
            }
        }

        public async Task AfterResetAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                this.count = 1;
                await context.PostAsync("Reset count.");
            }
            else
            {
                await context.PostAsync("Did not reset count.");
            }
            context.Wait(MessageReceivedAsync);
        }

    }
}