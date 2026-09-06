using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;

namespace ClefCraft.Application.Contracts.Comments
{
    public interface ICommentRepository : IGenericRepository<Comment>
    {
        Task<List<Comment>> GetByEntityAsync(string entityType, int entityId, int skip, int take);
        Task<int> CountByEntityAsync(string entityType, int entityId);

        // Batch fetch of mentions for a set of comments, used to attach
        // MentionedUserIds to each CommentDto without a query per row.
        Task<List<CommentMention>> GetMentionsByCommentIdsAsync(IEnumerable<int> commentIds);

        Task AddMentionsAsync(IEnumerable<CommentMention> mentions);
        Task RemoveMentionsAsync(int commentId);
    }
}
