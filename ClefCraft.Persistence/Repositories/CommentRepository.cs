using ClefCraft.Application.Contracts.Comments;
using ClefCraft.Domain;
using ClefCraft.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;

namespace ClefCraft.Persistence.Repositories
{
    public class CommentRepository : GenericRepository<Comment>, ICommentRepository
    {
        public CommentRepository(ClefCraftDatabaseContext context) : base(context)
        {
        }

        public async Task<List<Comment>> GetByEntityAsync(string entityType, int entityId, int skip, int take)
        {
            return await _context.Comments
                .Where(c => c.EntityType == entityType && c.EntityId == entityId)
                .OrderBy(c => c.DateCreated)
                .AsNoTracking()
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> CountByEntityAsync(string entityType, int entityId)
        {
            return await _context.Comments
                .Where(c => c.EntityType == entityType && c.EntityId == entityId)
                .CountAsync();
        }

        public async Task<List<CommentMention>> GetMentionsByCommentIdsAsync(IEnumerable<int> commentIds)
        {
            var ids = commentIds as ICollection<int> ?? commentIds.ToList();
            if (ids.Count == 0) return new List<CommentMention>();

            return await _context.CommentMentions
                .Where(m => ids.Contains(m.CommentId))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AddMentionsAsync(IEnumerable<CommentMention> mentions)
        {
            await _context.CommentMentions.AddRangeAsync(mentions);
        }

        public async Task RemoveMentionsAsync(int commentId)
        {
            var existing = await _context.CommentMentions
                .Where(m => m.CommentId == commentId)
                .ToListAsync();

            _context.CommentMentions.RemoveRange(existing);
        }
    }
}
