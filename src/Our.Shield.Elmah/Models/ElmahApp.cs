﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Our.Shield.Core.Attributes;
using Our.Shield.Core.Helpers;
using Our.Shield.Core.Models;
using Our.Shield.Core.Operation;
using Umbraco.Core;

namespace Our.Shield.Elmah.Models
{
    [AppEditor("/App_Plugins/Shield.Elmah/Views/Elmah.html?version=1.0.4")]
    public class ElmahApp : App<ElmahConfiguration>
    {
        private readonly string _allowKey = Guid.NewGuid().ToString();

        /// <inheritdoc />
        public override string Description => "Lock down access to Elmah reporting page to only be viewed by Authenticated Umbraco Users and/or secure via IP restrictions";

        /// <inheritdoc />
        public override string Icon => "icon-combination-lock red";

        /// <inheritdoc />
        public override string Id => nameof(Elmah);

        /// <inheritdoc />
        public override string Name => Id;

        public override string[] Tabs => new [] {"Configuration", "Reporting", "Journal"};

        /// <inheritdoc />
        public override IConfiguration DefaultConfiguration => new ElmahConfiguration
        {
            UmbracoUserEnable = true,
            IpAccessRules = new IpAccessControl
            {
                AccessType = IpAccessControl.AccessTypes.AllowAll,
                Exceptions = Enumerable.Empty<IpAccessControl.Entry>()
            },
            Unauthorized = new TransferUrl
            {
                TransferType = TransferTypes.Redirect,
                Url = new UmbracoUrl
                {
                    Type = UmbracoUrlTypes.Url,
                    Value = string.Empty
                }
            }
        };

        private readonly IpAccessControlService _ipAccessControlService;
        public ElmahApp()
        {
            _ipAccessControlService = new IpAccessControlService();
        }

        public override bool Execute(IJob job, IConfiguration c)
        {
            ApplicationContext.Current.ApplicationCache.RuntimeCache.ClearCacheItem(_allowKey);
            job.UnwatchWebRequests();

            if (!c.Enable || !job.Environment.Enable)
                return true;

            if (!(c is ElmahConfiguration config))
            {
                job.WriteJournal(new JournalMessage("Error: Config passed into Elmah was not of the correct type"));
                return false;
            }

            foreach (var error in new IpAccessControlService().InitIpAccessControl(config.IpAccessRules))
                job.WriteJournal(
                    new JournalMessage($"Error: Invalid IP Address {error}, unable to add to exception list"));

            job.WatchWebRequests(PipeLineStages.AuthenticateRequest, new Regex("^/elmah.axd$", RegexOptions.IgnoreCase), 400000, (count, httpApp) =>
            {
                if (config.UmbracoUserEnable && !AccessHelper.IsRequestAuthenticatedUmbracoUser(httpApp)
                    || !_ipAccessControlService.IsValid(config.IpAccessRules,
                        httpApp.Context.Request.UserHostAddress))
                    return new WatchResponse(config.Unauthorized);

                return new WatchResponse(WatchResponse.Cycles.Continue);
            });

            return true;
        }
    }
}