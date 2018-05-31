﻿using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Umbraco.Web.Models.ContentEditing;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence.DatabaseModelDefinitions;
using Umbraco.Web.Mvc;

namespace Umbraco.Web.Editors
{
    /// <summary>
    /// The API controller used for getting log history
    /// </summary>
    [PluginController("UmbracoApi")]
    public class LogController : UmbracoAuthorizedJsonController
    {
        public PagedResult<AuditLog> GetPagedEntityLog(int id,
            int pageNumber = 1,
            int pageSize = 0,
            Direction orderDirection = Direction.Descending,
            DateTime? sinceDate = null)
        {
            long totalRecords;
            var dateQuery = sinceDate.HasValue ? SqlContext.Query<IAuditItem>().Where(x => x.CreateDate >= sinceDate) : null;
            var result = Services.AuditService.GetPagedItemsByEntity(id, pageNumber - 1, pageSize, out totalRecords, orderDirection, customFilter: dateQuery);
            var mapped = Mapper.Map<IEnumerable<AuditLog>>(result);

            var page = new PagedResult<AuditLog>(totalRecords, pageNumber, pageSize)
            {
                Items = MapAvatarsAndNames(mapped)
            };

            return page;
        }

        public PagedResult<AuditLog> GetPagedCurrentUserLog(
            int pageNumber = 1,
            int pageSize = 0,
            Direction orderDirection = Direction.Descending,
            DateTime? sinceDate = null)
        {
            long totalRecords;
            var dateQuery = sinceDate.HasValue ? SqlContext.Query<IAuditItem>().Where(x => x.CreateDate >= sinceDate) : null;
            var userId = Security.GetUserId().ResultOr(0);
            var result = Services.AuditService.GetPagedItemsByUser(userId, pageNumber - 1, pageSize, out totalRecords, orderDirection, customFilter:dateQuery);
            var mapped = Mapper.Map<IEnumerable<AuditLog>>(result);
            return new PagedResult<AuditLog>(totalRecords, pageNumber + 1, pageSize)
            {
                Items = MapAvatarsAndNames(mapped)
            };
        }

        [Obsolete("Use GetPagedLog instead")]
        public IEnumerable<AuditLog> GetEntityLog(int id)
        {
            long totalRecords;
            var result = Services.AuditService.GetPagedItemsByEntity(id, 1, int.MaxValue, out totalRecords);
            return Mapper.Map<IEnumerable<AuditLog>>(result);
        }

        //TODO: Move to CurrentUserController?
        [Obsolete("Use GetPagedCurrentUserLog instead")]
        public IEnumerable<AuditLog> GetCurrentUserLog(AuditType logType, DateTime? sinceDate)
        {
            long totalRecords;
            var dateQuery = sinceDate.HasValue ? SqlContext.Query<IAuditItem>().Where(x => x.CreateDate >= sinceDate) : null;
            var userId = Security.GetUserId().ResultOr(0);
            var result = Services.AuditService.GetPagedItemsByUser(userId, 1, int.MaxValue, out totalRecords, auditTypeFilter: new[] {logType},customFilter: dateQuery);
            return Mapper.Map<IEnumerable<AuditLog>>(result);
        }

        [Obsolete("Use GetPagedLog instead")]
        public IEnumerable<AuditLog> GetLog(AuditType logType, DateTime? sinceDate)
        {
            if (sinceDate == null)
                sinceDate = DateTime.Now.Subtract(new TimeSpan(7, 0, 0, 0, 0));

            return Mapper.Map<IEnumerable<AuditLog>>(
                Services.AuditService.GetLogs(logType, sinceDate.Value));
        }

        private IEnumerable<AuditLog> MapAvatarsAndNames(IEnumerable<AuditLog> items)
        {
            var userIds = items.Select(x => x.UserId).ToArray();
            var userAvatars = Services.UserService.GetUsersById(userIds)
                .ToDictionary(x => x.Id, x => x.GetUserAvatarUrls(ApplicationCache.RuntimeCache));
            var userNames = Services.UserService.GetUsersById(userIds).ToDictionary(x => x.Id, x => x.Name);
            foreach (var item in items)
            {
                if (userAvatars.TryGetValue(item.UserId, out var avatars))
                {
                    item.UserAvatars = avatars;
                }
                if (userNames.TryGetValue(item.UserId, out var name))
                {
                    item.UserName = name;
                }
                

            }
            return items;
        }
    }
}
