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
        IMediaService _mediaService;
        IExchangeMailWebApi _exchangeMailWebApi;

        public MailService(IMediaService mediaService, IExchangeMailWebApi exchangeMailWebApi)
        {
            _mediaService = mediaService;
            _exchangeMailWebApi = exchangeMailWebApi;
        }

        public void SendMail(OutgoingMailModel mailModel)
        {
            // Send mail to recipient
            try
            {
                var attachments = new Dictionary<string, byte[]>();
                if (mailModel.attachments != null)
                {
                    var originalFilenamePattern = new Regex(@"^cthb-[a-zA-Z0-9]+-[0-9]+-(.*\.(?:pdf|tif|tiff))", RegexOptions.IgnoreCase);
                    foreach (var mediaId in mailModel.attachments)
                    {
                        var c = _mediaService.GetById(mediaId);
                        if (c != null)
                        {
                            var data = System.IO.File.ReadAllBytes(HostingEnvironment.MapPath(c.GetValue("file").ToString()));
                            var match = originalFilenamePattern.Match(c.Name);
                            if (match.Groups.Count > 1)
                            {
                                attachments.Add(match.Groups[1].Value, data);
                            }
                            else
                            {
                                throw new Exception("Failed to extract file name for attachment " + c.Name + ". Can only send pdf, tif and tiff files.");
                            }
                        }
                        else
                        {
                            throw new Exception("Failed to fetch media item for id " + mediaId + ".");
                        }
                    }
                }
                string body = mailModel.message;
                _exchangeMailWebApi.ConnectToExchangeService(ConfigurationManager.AppSettings["chalmersIllExhangeLogin"], ConfigurationManager.AppSettings["chalmersIllExhangePass"]);
                _exchangeMailWebApi.SendMailMessage(mailModel.OrderId, body, ConfigurationManager.AppSettings["chalmersILLMailSubject"], mailModel.recipientName, mailModel.recipientEmail, attachments);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}