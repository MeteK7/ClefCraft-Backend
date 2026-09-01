using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Persistence;

public interface IBoardItemRelationRepository
{
    Task<List<BoardItemRelation>> GetForItemAsync(int itemId);

    Task<BoardItemRelation?> GetByIdAsync(int id);

    Task AddAsync(BoardItemRelation relation);

    Task DeleteAsync(int id);

    Task<bool> ExistsAsync(
        int sourceId,
        int targetId,
        BoardItemRelationType relationType);

    Task<List<BoardItem>> SearchItemsAsync(
        string search,
        int excludeItemId);

    Task<List<BoardItem>> SearchBoardItemsAsync(
    int boardId,
    string searchTerm,
    int excludeItemId);
}