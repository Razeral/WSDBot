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
            /*System.Diagnostics.Trace.TraceInformation("SERVICEURI - " + message.ServiceUrl);
            System.Diagnostics.Trace.TraceInformation("SERVICEURI - toId - " + message.From.Id);
            System.Diagnostics.Trace.TraceInformation("SERVICEURI - toName - " + message.From.Name);
            System.Diagnostics.Trace.TraceInformation("SERVICEURI - fromId - " + message.Recipient.Id);
            System.Diagnostics.Trace.TraceInformation("SERVICEURI - fromName - " + message.Recipient.Name);
            System.Diagnostics.Trace.TraceInformation("SERVICEURI - serviceUrl - " + message.ServiceUrl);
            System.Diagnostics.Trace.TraceInformation("SERVICEURI - channelId - " + message.ChannelId);
            System.Diagnostics.Trace.TraceInformation("SERVICEURI - conversationId - " + message.Conversation.Id);*/

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(System.Environment.GetEnvironmentVariable("AzureBlobStorageConnectionString"));
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(System.Environment.GetEnvironmentVariable("AzureBlobStorageContainerReference"));

            if (message.Attachments.Count > 0)
            {
                System.Diagnostics.Trace.TraceInformation("[In attachment path]");
                
                //var blobRef = message.Conversation.Id + "/" + message.Timestamp.Value.ToUnixTimeSeconds().ToString();
                var blobRef = message.From.Id + "/" + message.Timestamp.Value.ToUnixTimeSeconds().ToString();
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobRef);
                System.Diagnostics.Trace.TraceInformation("[In attachment path] - Error Check 1");

                try
                {
                    blockBlob.Properties.ContentType = message.Attachments[0].ContentType;
                    System.Diagnostics.Trace.TraceInformation("[In attachment path] - Error Check 2");
                    //blockBlob.SetProperties();
                    System.Diagnostics.Trace.TraceInformation("[In attachment path] - Error Check 3");

                    // Get the attachment
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(message.Attachments[0].ContentUrl);
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    using (Stream inputStream = response.GetResponseStream())
                    {
                        blockBlob.UploadFromStream(inputStream);
                    }
                    System.Diagnostics.Trace.TraceInformation("[In attachment path] - Attachment uploaded");
                }
                catch (Exception e)
                {
                    System.Diagnostics.Trace.TraceInformation("[In attachment path] - Error Check 4 (In Exception)");
                    System.Diagnostics.Trace.TraceError(e.Message);
                }
                System.Diagnostics.Trace.TraceInformation("[In attachment path] - after blob setup: BlobRef = " + blobRef);
                System.Diagnostics.Trace.TraceInformation("ContentURL - " + message.Attachments[0].ContentUrl);
                /*
                try
                {
                    // Section for echoing back attachment
                    CloudBlockBlob blockBlob2 = container.GetBlockBlobReference(blobRef);
                    var replyMessage = context.MakeMessage();
                    replyMessage.Text = "PIC";
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
                }*/
            }

            if (message.Text.ToLower() == "ping")
            {
                try
                {
                    // Use the data stored previously to create the required objects.
                    var userAccount = new ChannelAccount("454115979", "Razeral");
                    
                    var connector = new ConnectorClient(new Uri("https://telegram.botframework.com"));
                    MicrosoftAppCredentials.TrustServiceUrl("https://telegram.botframework.com");

                    var botAccount = new ChannelAccount("WSDBot1_bot", "WSDBot");
                    var conversationId = "454115979";
                    var channelId = "telegram";

                    // Create a new message.
                    var replyMessage = context.MakeMessage();
                    //IMessageActivity message = Activity.CreateMessageActivity();
                    if (!string.IsNullOrEmpty(conversationId) && !string.IsNullOrEmpty(channelId))
                    {
                        // If conversation ID and channel ID was stored previously, use it.
                        replyMessage.ChannelId = channelId;
                    }
                    else
                    {
                        // Conversation ID was not stored previously, so create a conversation. 
                        // Note: If the user has an existing conversation in a channel, this will likely create a new conversation window.
                        conversationId = (await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount)).Id;
                    }

                    // Set the address-related properties in the message and send the message.
                    replyMessage.From = botAccount;
                    replyMessage.Recipient = userAccount;
                    replyMessage.Conversation = new ConversationAccount(id: conversationId);
                    replyMessage.Text = "Hello, this is a notification";
                    replyMessage.Locale = "en-us";
                    await connector.Conversations.SendToConversationAsync((Activity)message);
                    //await context.PostAsync(replyMessage);
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
                int countx = 0;
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
                            Name = "1" + countx++.ToString() + ".jpg"
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
            /*else
            {
                await context.PostAsync($"{this.count++}: You saidd {message.Text}");
                context.Wait(MessageReceivedAsync);
            }*/
            context.Wait(MessageReceivedAsync);
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