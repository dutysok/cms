﻿using SiteServer.Abstractions;
using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Datory.Utils;
using SiteServer.CMS.Framework;
using SiteServer.CMS.Repositories;

namespace SiteServer.CMS.Core.Office
{
    public static class ExcelObject
    {
        public static async Task CreateExcelFileForContentsAsync(string filePath, Site site,
            Channel channel, IEnumerable<int> contentIdList, List<string> displayAttributes, bool isPeriods, string startDate,
            string endDate, bool? checkedState)
        {
            DirectoryUtils.CreateDirectoryIfNotExists(DirectoryUtils.GetDirectoryPath(filePath));
            FileUtils.DeleteFileIfExists(filePath);

            var head = new List<string>();
            var rows = new List<List<string>>();

            var tableName = await DataProvider.ChannelRepository.GetTableNameAsync(site, channel);
            var styleList = ColumnsManager.GetContentListStyles(await DataProvider.TableStyleRepository.GetContentStyleListAsync(channel, tableName));

            foreach (var style in styleList)
            {
                if (displayAttributes.Contains(style.AttributeName))
                {
                    head.Add(style.DisplayName);
                }
            }

            if (contentIdList == null)
            {
                contentIdList = await DataProvider.ContentRepository.GetContentIdsAsync(site, channel, isPeriods,
                    startDate, endDate, checkedState);
            }

            foreach (var contentId in contentIdList)
            {
                var contentInfo = await DataProvider.ContentRepository.GetAsync(site, channel, contentId);
                if (contentInfo != null)
                {
                    var row = new List<string>();

                    foreach (var style in styleList)
                    {
                        if (displayAttributes.Contains(style.AttributeName))
                        {
                            var value = contentInfo.Get<string>(style.AttributeName);
                            row.Add(StringUtils.StripTags(value));
                        }
                    }

                    rows.Add(row);
                }
            }

            CsvUtils.Export(filePath, head, rows);
        }

        public static async Task CreateExcelFileForContentsAsync(string filePath, Site site,
            Channel channel, List<Content> contentInfoList, List<string> columnNames)
        {
            DirectoryUtils.CreateDirectoryIfNotExists(DirectoryUtils.GetDirectoryPath(filePath));
            FileUtils.DeleteFileIfExists(filePath);

            var head = new List<string>();
            var rows = new List<List<string>>();

            var columns = await ColumnsManager.GetContentListColumnsAsync(site, channel, ColumnsManager.PageType.Contents);

            foreach (var column in columns)
            {
                if (StringUtils.ContainsIgnoreCase(columnNames, column.AttributeName))
                {
                    head.Add(column.DisplayName);
                }
            }

            foreach (var contentInfo in contentInfoList)
            {
                var row = new List<string>();

                foreach (var column in columns)
                {
                    if (StringUtils.ContainsIgnoreCase(columnNames, column.AttributeName))
                    {
                        var value = contentInfo.Get<string>(column.AttributeName);
                        row.Add(StringUtils.StripTags(value));
                    }
                }

                rows.Add(row);
            }

            CsvUtils.Export(filePath, head, rows);
        }

        public static async Task CreateExcelFileForUsersAsync(string filePath, bool? checkedState)
        {
            DirectoryUtils.CreateDirectoryIfNotExists(DirectoryUtils.GetDirectoryPath(filePath));
            FileUtils.DeleteFileIfExists(filePath);

            var head = new List<string>
            {
                "用户名",
                "姓名",
                "邮箱",
                "手机",
                "注册时间",
                "最后一次活动时间"
            };
            var rows = new List<List<string>>();



            List<int> userIdList;
            if (checkedState.HasValue)
            {
                userIdList = (await DataProvider.UserRepository.GetIdListAsync(checkedState.Value)).ToList();
            }
            else
            {
                userIdList = (await DataProvider.UserRepository.GetIdListAsync(true)).ToList();
                userIdList.AddRange(await DataProvider.UserRepository.GetIdListAsync(false));
            }

            foreach (var userId in userIdList)
            {
                var userInfo = await DataProvider.UserRepository.GetByUserIdAsync(userId);

                rows.Add(new List<string>
                {
                    userInfo.UserName,
                    userInfo.DisplayName,
                    userInfo.Email,
                    userInfo.Mobile,
                    DateUtils.GetDateAndTimeString(userInfo.CreateDate),
                    DateUtils.GetDateAndTimeString(userInfo.LastActivityDate)
                });
            }

            CsvUtils.Export(filePath, head, rows);
        }

        public static async Task CreateExcelFileForAdministratorsAsync(string filePath)
        {
            DirectoryUtils.CreateDirectoryIfNotExists(DirectoryUtils.GetDirectoryPath(filePath));
            FileUtils.DeleteFileIfExists(filePath);

            var head = new List<string>
            {
                "用户名",
                "姓名",
                "邮箱",
                "手机",
                "注册时间",
                "最后一次活动时间"
            };
            var rows = new List<List<string>>();

            var userIdList = await DataProvider.AdministratorRepository.GetUserIdListAsync();

            foreach (var userId in userIdList)
            {
                var administrator = await DataProvider.AdministratorRepository.GetByUserIdAsync(userId);

                rows.Add(new List<string>
                {
                    administrator.UserName,
                    administrator.DisplayName,
                    administrator.Email,
                    administrator.Mobile,
                    DateUtils.GetDateAndTimeString(administrator.CreationDate),
                    DateUtils.GetDateAndTimeString(administrator.LastActivityDate)
                });
            }

            CsvUtils.Export(filePath, head, rows);
        }

        public static async Task<List<Content>> GetContentsByCsvFileAsync(string filePath, Site site,
            Channel node)
        {
            var contentInfoList = new List<Content>();

            CsvUtils.Import(filePath, out var head, out var rows);

            if (rows.Count <= 0) return contentInfoList;

            var tableName = await DataProvider.ChannelRepository.GetTableNameAsync(site, node);
            var styleList = ColumnsManager.GetContentListStyles(await DataProvider.TableStyleRepository.GetContentStyleListAsync(node, tableName));
            var nameValueCollection = new NameValueCollection();
            foreach (var style in styleList)
            {
                nameValueCollection[style.DisplayName] = style.AttributeName.ToLower();
            }

            var attributeNames = new List<string>();
            foreach (var columnName in head)
            {
                attributeNames.Add(!string.IsNullOrEmpty(nameValueCollection[columnName])
                    ? nameValueCollection[columnName]
                    : columnName);
            }

            foreach (var row in rows)
            {
                if (row.Count != attributeNames.Count) continue;

                var dict = new Dictionary<string, object>();

                for (var i = 0; i < attributeNames.Count; i++)
                {
                    var attributeName = attributeNames[i];
                    if (!string.IsNullOrEmpty(attributeName))
                    {
                        dict[attributeName] = row[i];
                    }
                }

                var contentInfo = new Content(dict);

                if (!string.IsNullOrEmpty(contentInfo.Title))
                {
                    contentInfo.SiteId = site.Id;
                    contentInfo.ChannelId = node.Id;
                    contentInfo.LastEditDate = DateTime.Now;

                    contentInfoList.Add(contentInfo);
                }
            }

            return contentInfoList;
        }
    }
}