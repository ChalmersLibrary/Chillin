using Chalmers.ILL.MediaItems;
using Chalmers.ILL.Models;
using Chalmers.ILL.Models.Mail;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;
using Umbraco.Core.Services;

namespace Chalmers.ILL.Mail
{
    public class MailService : IMailService
    {
        IMediaItemManager _mediaItemManager;
        IExchangeMailWebApi _exchangeMailWebApi;

        public MailService(IMediaItemManager mediaItemManager, IExchangeMailWebApi exchangeMailWebApi)
        {
            _mediaItemManager = mediaItemManager;
            _exchangeMailWebApi = exchangeMailWebApi;
        }

        public void SendMail(OutgoingMailModel mailModel)
        {
            // Send mail to recipient
            var attachments = new Dictionary<string, byte[]>();
            if (mailModel.attachments != null)
            {
                var originalFilenamePattern = new Regex(@"^cthb-[a-zA-Z0-9]+-[0-9]+-(.*\.(?:pdf|tif|tiff))", RegexOptions.IgnoreCase);
                foreach (var mediaId in mailModel.attachments)
                {
                    var mediaItem = _mediaItemManager.GetOne(mediaId);
                    if (mediaItem != null)
                    {
                        byte[] data = new byte[mediaItem.Data.Length];
                        mediaItem.Data.Read(data, 0, data.Length);

                        var match = originalFilenamePattern.Match(mediaItem.Name);
                        if (match.Groups.Count > 1)
                        {
                            attachments.Add(match.Groups[1].Value, data);
                        }
                        else
                        {
                            throw new Exception("Failed to extract file name for attachment " + mediaItem.Name + ". Can only send pdf, tif and tiff files.");
                        }
                    }
                    else
                    {
                        throw new Exception("Failed to fetch media item for id " + mediaId + ".");
                    }

                    // Dispose stream that is no longer needed. Handle this in some better way?
                    mediaItem.Data.Dispose();
                }
            }
            string body = mailModel.message;
            _exchangeMailWebApi.ConnectToExchangeService(ConfigurationManager.AppSettings["chalmersIllExhangeLogin"], ConfigurationManager.AppSettings["chalmersIllExhangePass"]);
            _exchangeMailWebApi.SendMailMessage(mailModel.OrderId, body, ConfigurationManager.AppSettings["chalmersILLMailSubject"], mailModel.recipientName, mailModel.recipientEmail, attachments);
        }
    }
}