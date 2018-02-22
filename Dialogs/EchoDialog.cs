using System;
using System.Threading.Tasks;

using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Net.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    

    [Serializable]
    public class EchoDialog : IDialog<object>
    {
        protected int count = 1;

        //https://wsdbot87ce.blob.core.windows.net/wsdbotimages
        //DefaultEndpointsProtocol=https;AccountName=wsdbot87ce;AccountKey=GBZqlXnJHJHyjGVQ67sTEQWYzQe8XyxlgOmaJqGAugSlFXzaJTBM/VGD6asx7ismGFB1MoeZMLlB2GNv9D4BGw==;EndpointSuffix=core.windows.net
        //DefaultEndpointsProtocol=https;AccountName=wsdbot87ce;AccountKey=GBZqlXnJHJHyjGVQ67sTEQWYzQe8XyxlgOmaJqGAugSlFXzaJTBM/VGD6asx7ismGFB1MoeZMLlB2GNv9D4BGw==;

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {


            var message = await argument;

            if (message.Attachments.Count > 0)
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(System.Environment.GetEnvironmentVariable("AzureBlobStorageConnectionString"));
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(System.Environment.GetEnvironmentVariable("AzureBlobStorageContainerReference"));

                await context.PostAsync($"Has attachments + {message.Attachments[0].ContentType}");
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