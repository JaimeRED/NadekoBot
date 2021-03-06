﻿using NadekoBot.Services.Database.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace NadekoBot.Services.Database.Repositories.Impl
{
    public class QuoteRepository : Repository<Quote>, IQuoteRepository
    {
        public QuoteRepository(DbContext context) : base(context)
        {
        }

        public IEnumerable<Quote> GetAllQuotesByKeyword(ulong guildId, string keyword) => 
            _set.Where(q => q.GuildId == guildId && q.Keyword == keyword);

        public IEnumerable<Quote> GetGroup(ulong guildId, int skip, int take) => 
            _set.Where(q=>q.GuildId == guildId).OrderBy(q => q.Keyword).Skip(skip).Take(take).ToList();

        public Task<Quote> GetRandomQuoteByKeywordAsync(ulong guildId, string keyword)
        {
            var rng = new NadekoRandom();
            return _set.Where(q => q.GuildId == guildId && q.Keyword == keyword).OrderBy(q => rng.Next()).FirstOrDefaultAsync();
        }
    }
}
