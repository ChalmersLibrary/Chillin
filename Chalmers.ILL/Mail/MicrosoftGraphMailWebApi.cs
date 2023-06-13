﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Chalmers.ILL.Configuration;
using Chalmers.ILL.Models.Mail;
using Chalmers.ILL.Utilities;
using HtmlAgilityPack;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Umbraco.Core.Logging;

namespace Chalmers.ILL.Mail
{
    public class MicrosoftGraphMailWebApi : IExchangeMailWebApi
    {
        // With client credentials flows the scopes is ALWAYS of the shape "resource/.default", as the 
        // application permissions need to be set statically (in the portal or by PowerShell), and then granted by
        // a tenant administrator
        private string[] scopes = new string[] { "https://graph.microsoft.com/.default" };

        private IConfidentialClientApplication _app;
        private HttpClient _httpClient;
        private IConfiguration _config;

        public MicrosoftGraphMailWebApi(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        /// <summary>
        /// Connect to Exchange Service with credentials
        /// </summary>
        /// <returns>The Service reference</returns>
        public void ConnectToExchangeService(string username, string password)
        {
            _app = ConfidentialClientApplicationBuilder.Create(_config.MicrosoftGraphClientId)
                .WithClientSecret(_config.MicrosoftGraphClientSecret)
                .WithAuthority(new Uri(_config.MicrosoftGraphAuthority))
                .Build();
        }

        public string ArchiveMailMessage(MailQueueModel mqm)
        {
            // Find out Year and Month to archive on
            var parts = mqm.DateTimeReceived.Split('-');
            string year = parts[0];
            string month = parts[1];

            // Check if Year folder exists below Inbox
            var rootMailFoldersResponse = GetFromMicrosoftGraph(_config.MicrosoftGraphApiEndpoint + "/users/" + _config.MicrosoftGraphApiUserId + "/mailFolders/inbox/childFolders");
            var rootMailFolders = rootMailFoldersResponse.value as IEnumerable<dynamic>;
            var yearFolder = rootMailFolders.FirstOrDefault(x => x.displayName == year);

            // Create folder for Year if it wasn't found
            if (yearFolder == null)
            {
                yearFolder = PostToMicrosoftGraph(_config.MicrosoftGraphApiEndpoint + "/users/" + _config.MicrosoftGraphApiUserId + "/mailFolders/inbox/childFolders", 
                    "{ \"displayName\":\"" + year + "\" }");
            }

            // Check if Month folder exists below Year folder
            var childMailFoldersResponse = GetFromMicrosoftGraph(_config.MicrosoftGraphApiEndpoint + "/users/" + _config.MicrosoftGraphApiUserId + "/mailFolders/" + yearFolder.id + "/childFolders");
            var childMailFolders = childMailFoldersResponse.value as IEnumerable<dynamic>;
            var monthFolder = childMailFolders.FirstOrDefault(x => x.displayName == month);

            // Create folder for Month if it wasn't found
            if (monthFolder == null)
            {
                monthFolder = PostToMicrosoftGraph(_config.MicrosoftGraphApiEndpoint + "/users/" + _config.MicrosoftGraphApiUserId + "/mailFolders/" + yearFolder.id + "/childFolders",
                    "{ \"displayName\":\"" + month + "\" }");
            }

            // Move the Mail Message to Month folder below Year folder
            PostToMicrosoftGraph(_config.MicrosoftGraphApiEndpoint + "/users/" + _config.MicrosoftGraphApiUserId + "/messages/" + mqm.Id + "/move", "{ \"destinationId\":\"" + monthFolder.id + "\" }");

            // Return the FolderId which we moved to
            return monthFolder.id;
        }

        public void ForwardMailMessage(MailQueueModel mqm, string recipientAddress, string extraText = "", bool delete = true)
        {
            // Create the prefixed content to add to the forwarded message body.
            string messageBodyPrefix = extraText == "" ? "Detta meddelande har vidarebefodrats av Chalmers.ILL för " + mqm.From + " <" + mqm.Sender + ">" : extraText;

            // Send the forwarded message
            var msg = "{ \"comment\": \"" + messageBodyPrefix + "\", \"toRecipients\": [{ \"emailAddress\": { \"address\": \"" + recipientAddress + "\" } } ] }";
            PostToMicrosoftGraph(_config.MicrosoftGraphApiEndpoint + "/users/" + _config.MicrosoftGraphApiUserId + "/mailFolders/inbox/messages/" + mqm.Id + "/forward", msg);

            if (delete)
            {
                DeleteToMicrosoftGraph(_config.MicrosoftGraphApiEndpoint + "/users/" + _config.MicrosoftGraphApiUserId + "/mailFolders/inbox/messages/" + mqm.Id);
            }
        }

        public List<MailQueueModel> ReadMailQueue()
        {
            var res = new List<MailQueueModel>();

            try
            {
                var mailInboxData = GetFromMicrosoftGraph(_config.MicrosoftGraphApiEndpoint + "/users/" + _config.MicrosoftGraphApiUserId + "/mailFolders/inbox/messages");
                    
                foreach (var mailData in mailInboxData.value)
                {
                    var m = new MailQueueModel();

                    // Load Message Body as HTML with HtmlAgilityPack
                    HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
                    htmlDoc.LoadHtml(MailBodyFixer.RemoveHtmlAroundLinks(mailData.body.content.ToString()));

                    // Query for OrderItem properties in MailQueueModel
                    if (htmlDoc.DocumentNode.HasChildNodes && htmlDoc.GetElementbyId("chalmers.ill.orderitem") != null)
                    {
                        m.PatronName = htmlDoc.GetElementbyId("PatronName").InnerText;
                        m.PatronEmail = htmlDoc.GetElementbyId("PatronEmail").InnerText;
                        m.PatronCardNo = htmlDoc.GetElementbyId("PatronCardNo").InnerText;
                        m.OriginalOrder = htmlDoc.GetElementbyId("OriginalOrder").InnerText;
                        if (htmlDoc.GetElementbyId("Purchase") != null)
                        {
                            m.IsPurchaseRequest = Convert.ToBoolean(htmlDoc.GetElementbyId("Purchase").InnerText);
                        }
                        else
                        {
                            m.IsPurchaseRequest = false;
                        }
                        m.DeliveryLibrary = htmlDoc.GetElementbyId("DeliveryLibrary").InnerText;
                    }

                    // Load all attachments
                    var attachmentList = new List<MailAttachment>();
                    if ((bool)(mailData.hasAttachments as JValue))
                    {
                        var attachments = GetFromMicrosoftGraph(_config.MicrosoftGraphApiEndpoint + "/users/" + _config.MicrosoftGraphApiUserId + "/mailFolders/inbox/messages/" + 
                            mailData.id + "/attachments");
                        foreach (var attachmentData in attachments.value)
                        {
                            if (attachmentData != null && attachmentData["@odata.type"] != null && attachmentData["@odata.type"].ToString() == "#microsoft.graph.fileAttachment")
                            {
                                var memStream = new System.IO.MemoryStream(Convert.FromBase64String(attachmentData.contentBytes.ToString()));
                                attachmentList.Add(new MailAttachment(attachmentData.name.ToString(), memStream, attachmentData.contentType.ToString()));
                            }
                        }
                    }

                    // Bind different fields to MailQueueModel
                    m.Id = mailData.id.ToString();
                    m.To = mailData.toRecipients[0].emailAddress.address.ToString();
                    m.From = mailData.from.emailAddress.address.ToString();
                    m.Sender = mailData.from.emailAddress.name.ToString();
                    m.Debug = mailData.body.content.ToString();
                    m.Subject = mailData.subject.ToString();
                    m.DateTimeReceived = mailData.receivedDateTime.ToString().Replace("T", " ").Remove(16);
                    m.Attachments = attachmentList;

                    // Message body as text only
                    var sb = new StringBuilder();

                    if (htmlDoc != null && htmlDoc.DocumentNode != null)
                    {
                        var textNodes = htmlDoc.DocumentNode.SelectNodes("//text()");
                        if (textNodes != null)
                        {
                            foreach (HtmlNode node in textNodes)
                            {
                                if (!node.HasChildNodes)
                                {
                                    string text = node.InnerText;
                                    if (!string.IsNullOrEmpty(text))
                                        sb.AppendLine(text.Trim());
                                }
                            }
                        }
                    }

                    var messageText = sb.ToString();
                    m.MessageBody = Helpers.HtmlToPlainText(messageText);
                    
                    PatchToMicrosoftGraph(_config.MicrosoftGraphApiEndpoint + "/users/" + _config.MicrosoftGraphApiUserId + "/mailFolders/inbox/messages/" + m.Id, "{ \"isRead\":true }");

                    // Add this message to list
                    res.Add(m);
                }
            }
            catch (Exception e)
            {
                LogHelper.Error<MicrosoftGraphMailWebApi>("Failed fetching mail from Microsoft Graph.", e);
            }

            return res;
        }

        public void SendMailMessage(string orderId, string body, string subject, string recipientName, string recipientAddress, IDictionary<string, byte[]> attachments)
        {
            string senderEmail = ConfigurationManager.AppSettings["chalmersIllSenderAddress"];

            dynamic message = new ExpandoObject();
            message.subject = subject + " #" + orderId;

            dynamic messageBody = new ExpandoObject();
            messageBody.contentType = "html";
            messageBody.content = body.Replace("\n", "<br />");
            message.body = messageBody;

            dynamic toRecipient = new ExpandoObject();
            dynamic toEmailAddress = new ExpandoObject();
            toEmailAddress.address = recipientAddress;
            toRecipient.emailAddress = toEmailAddress;
            message.toRecipients = new List<dynamic>() { toRecipient };

            dynamic replyToRecipient = new ExpandoObject();
            dynamic replyToEmailAddress = new ExpandoObject();
            replyToEmailAddress.address = senderEmail;
            replyToRecipient.emailAddress = replyToEmailAddress;
            message.replyTo = new List<dynamic>() { replyToRecipient };

            message.attachments = new List<dynamic>();

            IDictionary<string, byte[]> largeAttachments = new Dictionary<string, byte[]>();
            foreach (var attachment in attachments)
            {
                if (attachment.Value.Length < 3000000)
                {
                    dynamic fileAttachment = new ExpandoObject();
                    fileAttachment.name = attachment.Key;
                    fileAttachment.contentBytes = Convert.ToBase64String(attachment.Value);
                    var dictFileAttachment = (IDictionary<string, object>)fileAttachment;
                    dictFileAttachment.Add("@odata.type", "#microsoft.graph.fileAttachment");
                    message.attachments.Add(dictFileAttachment);
                }
                else
                {
                    // Upload these after the email has been created on the mail server
                    largeAttachments.Add(attachment.Key, attachment.Value);
                }
            }

            dynamic messageContainer = new ExpandoObject();
            messageContainer.message = message;

            if (largeAttachments.Count > 0)
            {
                // Create the email on the mail server
                var uploadedMail = PostToMicrosoftGraph(_config.MicrosoftGraphApiEndpoint + "/users/" + _config.MicrosoftGraphApiUserId + "/messages", JsonConvert.SerializeObject(message));

                // Upload large files
                foreach (var largeAttachment in largeAttachments)
                {
                    UploadAndConnectToEmail(largeAttachment, Convert.ToString(uploadedMail.id));
                }

                // Send it all away
                PostToMicrosoftGraph(_config.MicrosoftGraphApiEndpoint + "/users/" + _config.MicrosoftGraphApiUserId + "/messages/" + uploadedMail.id + "/send");
            }
            else
            {
                // Send the email message and save a copy.
                PostToMicrosoftGraph(_config.MicrosoftGraphApiEndpoint + "/users/" + _config.MicrosoftGraphApiUserId + "/sendMail", JsonConvert.SerializeObject(messageContainer));
            }
        }

        public void SendPlainMailMessage(string body, string subject, string recipientAddress)
        {
            string senderEmail = ConfigurationManager.AppSettings["chalmersIllSenderAddress"];

            dynamic message = new ExpandoObject();
            message.subject = subject;

            dynamic messageBody = new ExpandoObject();
            messageBody.contentType = "html";
            messageBody.content = body.Replace("\n", "<br />");
            message.body = messageBody;

            dynamic toRecipient = new ExpandoObject();
            dynamic toEmailAddress = new ExpandoObject();
            toEmailAddress.address = recipientAddress;
            toRecipient.emailAddress = toEmailAddress;
            message.toRecipients = new List<dynamic>() { toRecipient };

            dynamic replyToRecipient = new ExpandoObject();
            dynamic replyToEmailAddress = new ExpandoObject();
            replyToEmailAddress.address = senderEmail;
            replyToRecipient.emailAddress = replyToEmailAddress;
            message.replyTo = new List<dynamic>() { replyToRecipient };

            dynamic messageContainer = new ExpandoObject();
            messageContainer.message = message;

            // Send the email message and save a copy.
            PostToMicrosoftGraph(_config.MicrosoftGraphApiEndpoint + "/users/" + _config.MicrosoftGraphApiUserId + "/sendMail", JsonConvert.SerializeObject(messageContainer));
        }

        private void UploadAndConnectToEmail(KeyValuePair<string, byte[]> attachment, string mailId)
        {
            dynamic createUploadSessionContainer = new ExpandoObject();
            dynamic createUploadSessionAttachmentItem = new ExpandoObject();
            createUploadSessionAttachmentItem.attachmentType = "file";
            createUploadSessionAttachmentItem.name = attachment.Key;
            createUploadSessionAttachmentItem.size = attachment.Value.Length;
            createUploadSessionContainer.AttachmentItem = createUploadSessionAttachmentItem;
            var uploadSessionResponse = PostToMicrosoftGraph(_config.MicrosoftGraphApiEndpoint + "/users/" + _config.MicrosoftGraphApiUserId + "/messages/" + mailId +
                "/attachments/createUploadSession", JsonConvert.SerializeObject(createUploadSessionContainer));
            var uploadUrl = Convert.ToString(uploadSessionResponse.uploadUrl);
            var currentBytePos = 0;

            while (uploadSessionResponse != null && uploadSessionResponse.nextExpectedRanges != null)
            {
                var endBytePos = currentBytePos + 3000000;
                endBytePos = endBytePos > attachment.Value.Length ? attachment.Value.Length : endBytePos;
                var chunkSize = endBytePos - currentBytePos;

                var chunk = new byte[chunkSize];
                Array.Copy(attachment.Value, currentBytePos, chunk, 0, chunkSize);

                string contentRangeHeaderValue = "bytes " + currentBytePos + "-" + (endBytePos - 1) + "/" + attachment.Value.Length;
                uploadSessionResponse = PutToUpload(uploadUrl, chunk, new Dictionary<string, string>()
                {
                    { "Content-Type", "application/octet-stream" },
                    { "Content-Length", chunkSize.ToString() },
                    { "Content-Range", contentRangeHeaderValue }
                });

                if (uploadSessionResponse != null && uploadSessionResponse.nextExpectedRanges != null && uploadSessionResponse.nextExpectedRanges.Count > 0)
                {
                    currentBytePos = int.Parse(Convert.ToString(uploadSessionResponse.nextExpectedRanges[0]));
                }
            }
        }

        private dynamic GetFromMicrosoftGraph(string url)
        {
            dynamic res = null;

            var tokenTask = _app.AcquireTokenForClient(scopes).ExecuteAsync();
            tokenTask.Wait();
            var tokenResult = tokenTask.Result;

            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url))
            {
                requestMessage.Headers.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                requestMessage.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);

                System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                var sendTask = _httpClient.SendAsync(requestMessage);
                sendTask.Wait();
                var response = sendTask.Result;
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Non success status code [" + response.StatusCode + "] on get from Microsoft Graph: " + response.ReasonPhrase);
                }
                var readTask = response.Content.ReadAsStringAsync();
                readTask.Wait();
                res = JsonConvert.DeserializeObject<dynamic>(readTask.Result);
            }

            return res;
        }

        private dynamic PostToMicrosoftGraph(string url, string data = null)
        {
            dynamic res = null;

            var tokenTask = _app.AcquireTokenForClient(scopes).ExecuteAsync();
            tokenTask.Wait();
            var tokenResult = tokenTask.Result;

            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, url))
            {
                requestMessage.Headers.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                requestMessage.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);
                if (data != null)
                {
                    requestMessage.Content = new StringContent(data,
                        Encoding.UTF8,
                        "application/json");
                }

                System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                var sendTask = _httpClient.SendAsync(requestMessage);
                sendTask.Wait();
                var response = sendTask.Result;
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Non success status code [" + response.StatusCode + "] on post to Microsoft Graph: " + response.ReasonPhrase);
                }
                var readTask = response.Content.ReadAsStringAsync();
                readTask.Wait();
                res = JsonConvert.DeserializeObject<dynamic>(readTask.Result);
            }

            return res;
        }

        private dynamic PutToUpload(string url, byte[] data, IDictionary<string, string> headers = null)
        {
            dynamic res = null;

            using (var requestMessage = new HttpRequestMessage(HttpMethod.Put, url))
            {
                requestMessage.Headers.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                requestMessage.Content = new ByteArrayContent(data);

                if (headers != null)
                {
                    foreach (KeyValuePair<string, string> newHeader in headers)
                    {
                        if (newHeader.Key.Contains("Content"))
                        {
                            requestMessage.Content.Headers.Add(newHeader.Key, newHeader.Value);
                        }
                        else
                        {
                            requestMessage.Headers.Add(newHeader.Key, newHeader.Value);
                        }
                    }
                }

                System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                var sendTask = _httpClient.SendAsync(requestMessage);
                sendTask.Wait();
                var response = sendTask.Result;
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Non success status code [" + response.StatusCode + "] on put to Microsoft Graph: " + response.ReasonPhrase);
                }
                var readTask = response.Content.ReadAsStringAsync();
                readTask.Wait();
                res = JsonConvert.DeserializeObject<dynamic>(readTask.Result);
            }

            return res;
        }

        private dynamic DeleteToMicrosoftGraph(string url)
        {
            dynamic res = null;

            var tokenTask = _app.AcquireTokenForClient(scopes).ExecuteAsync();
            tokenTask.Wait();
            var tokenResult = tokenTask.Result;

            using (var requestMessage = new HttpRequestMessage(HttpMethod.Delete, url))
            {
                requestMessage.Headers.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                requestMessage.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);

                System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                var sendTask = _httpClient.SendAsync(requestMessage);
                sendTask.Wait();
                var response = sendTask.Result;
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Non success status code [" + response.StatusCode + "] on delete to Microsoft Graph: " + response.ReasonPhrase);
                }
                var readTask = response.Content.ReadAsStringAsync();
                readTask.Wait();
                res = JsonConvert.DeserializeObject<dynamic>(readTask.Result);
            }

            return res;
        }

        private dynamic PatchToMicrosoftGraph(string url, string data)
        {
            dynamic res = null;

            var tokenTask = _app.AcquireTokenForClient(scopes).ExecuteAsync();
            tokenTask.Wait();
            var tokenResult = tokenTask.Result;

            using (var requestMessage = new HttpRequestMessage(new HttpMethod("PATCH"), url))
            {
                requestMessage.Headers.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                requestMessage.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);
                requestMessage.Content = new StringContent(data,
                    Encoding.UTF8,
                    "application/json");

                System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                var sendTask = _httpClient.SendAsync(requestMessage);
                sendTask.Wait();
                var response = sendTask.Result;
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Non success status code [" + response.StatusCode + "] on patch to Microsoft Graph: " + response.ReasonPhrase);
                }
                var readTask = response.Content.ReadAsStringAsync();
                readTask.Wait();
                res = JsonConvert.DeserializeObject<dynamic>(readTask.Result);
            }

            return res;
        }
    }
}