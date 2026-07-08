using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Persistence;

public interface IBoardItemRelationRepository
{
    Task<List<BoardItemRelation>> GetForItemAsync(int itemId);

    Task AddAsync(BoardItemRelation relation);

    Task DeleteAsync(int id);

    Task<bool> ExistsAsync(
        int sourceId,
        int targetId,
        int relationType);

    Task<List<BoardItem>> SearchItemsAsync(
        string search,
        int excludeItemId);
}