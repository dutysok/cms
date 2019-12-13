﻿using System.Collections.Generic;
using SiteServer.Abstractions;
using SiteServer.CMS.Dto;

namespace SiteServer.API.Controllers.Pages.Cms.Editor
{
    public partial class PagesEditorController
    {
        public class ConfigRequest
        {
            public int SiteId { get; set; }
            public int ChannelId { get; set; }
            public int ContentId { get; set; }
        }

        public class ConfigResult
        {
            public User User { get; set; }
            public Abstractions.Config Config { get; set; }
            public Site Site { get; set; }
            public Channel Channel { get; set; }
            public IEnumerable<string> GroupNames { get; set; }
            public IEnumerable<string> TagNames { get; set; }
            public IEnumerable<TableStyle> Styles { get; set; }
            public List<Select<int>> CheckedLevels { get; set; }
            public Content Content { get; set; }
            public IEnumerable<Cascade<int>> SiteOptions { get; set; }
            public IEnumerable<Cascade<int>> ChannelOptions { get; set; }
        }
    }
}