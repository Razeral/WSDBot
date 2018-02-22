using System;
using System.Threading.Tasks;

using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Net.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

using System.Collections.Generic;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    

    [Serializable]
    public class EchoDialog : IDialog<object>
    {
        protected int count = 1;

        //https://wsdbot87ce.blob.core.windows.net/wsdbotimages

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {


            var message = await argument;

            if (message.Attachments.Count > 0)
            {
                System.Diagnostics.Trace.TraceInformation("In attachment path");
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(System.Environment.GetEnvironmentVariable("AzureBlobStorageConnectionString"));
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(System.Environment.GetEnvironmentVariable("AzureBlobStorageContainerReference"));
                var blobRef = message.Conversation.Id + message.Timestamp.ToString();
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobRef);
                System.Diagnostics.Trace.TraceInformation("In attachment path - after blob setup");
                System.Diagnostics.Trace.TraceInformation("ContentURL - " + message.Attachments[0].ContentUrl);

                blockBlob.UploadFromFile(message.Attachments[0].ContentUrl);
                //System.Diagnostics.Trace.TraceInformation("Content - " + message.Attachments[0].Content.ToString());
                System.Diagnostics.Trace.TraceInformation("In attachment path - after blob setup2");
                await context.PostAsync($"Has attachments + {message.Attachments[0].ContentType}");
                System.Diagnostics.Trace.TraceInformation("In attachment path - after blob setup3");
                CloudBlockBlob blockBlob2 = container.GetBlockBlobReference(blobRef);
                System.Diagnostics.Trace.TraceInformation("In attachment path - after blob setup4");
                var replyMessage = context.MakeMessage();
                replyMessage.Attachments = new List<Attachment>();
                replyMessage.Attachments.Add(new Attachment()
                {
                    ContentUrl = blockBlob2.Uri.ToString(),
                    ContentType = message.Attachments[0].ContentType,
                    Name = "1.jpg"
                });
                System.Diagnostics.Trace.TraceInformation("In attachment path - after blob setup5");
                await context.PostAsync(replyMessage);
            }

            if (message.Text == "reset")
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