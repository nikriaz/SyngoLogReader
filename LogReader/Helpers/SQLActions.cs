using System;
using System.Collections.Generic;
using System.Linq;
using LogReader.Models;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace LogReader.Helpers
{
    public interface ISQLActions
    {
        Task SQLSaveAsync(List<Message> logList);
        Task<ViewResult> SQLReadAsync(FilterSet filterSet = null, CancellationToken token = default);
        Task SQLCleanAsync();
        Task SQLCountAsync();
    }
    public class SQLActions : ISQLActions
    {
        private readonly MessagingContext _context;
        public SQLActions(MessagingContext context)
        {
            _context = context;
        }

        public async Task SQLSaveAsync(List<Message> logList)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                await _context.BulkInsertAsync(logList, new BulkConfig {SetOutputIdentity = true});
                var child = logList.Select(m => m.FTSMessage).ToList();
                await _context.BulkInsertAsync(child);
                transaction.Commit();
            }
        }

        public async Task<ViewResult> SQLReadAsync(FilterSet filterSet = null, CancellationToken token = default)
        {
            var viewResult = new ViewResult();
            IQueryable<Message> query = null;

            if (filterSet != null)
            {
                query = _context.Messages.Where(f => filterSet.SeveretiesChecked.Contains(f.Severity)).Include(f => f.FTSMessage).AsNoTracking();
                query = query.Where(dt => dt.EventDateTime >= filterSet.FromDt && dt.EventDateTime <= filterSet.ToDt).AsNoTracking();

                if (!String.IsNullOrWhiteSpace(filterSet.MessageText))
                {
                    query = query.Where(m => m.FTSMessage.Match == filterSet.MessageText).AsNoTracking();
                }

                if (!String.IsNullOrWhiteSpace(filterSet.MessageId))
                {
                    var messageId = Int32.Parse(filterSet.MessageId);
                    query = query.Where(m => m.EventId == messageId).AsNoTracking();
                }

                if (!String.IsNullOrWhiteSpace(filterSet.SourceName) && filterSet.SourceName != "*" )
                {
                    var sourceName = filterSet.SourceName;
                    query = query.Where(m => EF.Functions.Like(m.SourceName, $"%{sourceName}%")).AsNoTracking();
                }

                query = query.OrderByDescending(m => m.EventDateTime).AsNoTracking(); 
            }
            else
            {
                query = _context.Messages;
            }

            viewResult.Counter = await query.CountAsync();
            if (viewResult.Counter != 0 )
            {
                try
                {
                    viewResult.LogList = await query.ToListAsync(token);
                    viewResult.ViewMinDt = await query.MinAsync(m => m.EventDateTime);
                    viewResult.ViewMaxDt = (await query.FirstOrDefaultAsync()).EventDateTime;
                }
                catch (OperationCanceledException e)
                {
                    viewResult.Counter = 0;
                }
            }

            return viewResult;
        }

        public async Task SQLCleanAsync()
        {
            await _context.Database.ExecuteSqlRawAsync("delete from " + "Messages");
        }

        public async Task SQLCountAsync()
        {
            Variables.totalMessages = await _context.Messages.CountAsync();

            if (Variables.totalMessages > 0)
            {
                Variables.totalMinDt = _context.Messages.Min(dt => dt.EventDateTime);
                Variables.totalMaxDt = _context.Messages.Max(dt => dt.EventDateTime);
                Variables.distinctDays = await _context.Messages.Select(m => m.EventDateTime.Date).Distinct().CountAsync();
            }

            return;
        }
    }
}
