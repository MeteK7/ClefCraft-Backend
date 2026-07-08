using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.BoardItemRelations.DTOs;
using ClefCraft.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.BoardItemRelations.Queries.GetRelations
{
    public class GetBoardItemRelationsQueryHandler
        : IRequestHandler<GetBoardItemRelationsQuery, RelationshipHubDto>
    {
        private readonly IBoardItemRelationRepository _repository;

        public GetBoardItemRelationsQueryHandler(
            IBoardItemRelationRepository repository)
        {
            _repository = repository;
        }

        public async Task<RelationshipHubDto> Handle(
            GetBoardItemRelationsQuery request,
            CancellationToken cancellationToken)
        {
            var relations = await _repository.GetForItemAsync(request.ItemId);

            var hub = new RelationshipHubDto
            {
                Groups = CreateDefaultGroups()
            };

            foreach (var relation in relations)
            {
                var relatedItem =
                    relation.SourceBoardItemId == request.ItemId
                        ? relation.TargetBoardItem
                        : relation.SourceBoardItem;

                var group = hub.Groups.First(x =>
                    x.RelationType == (int)relation.RelationType);

                group.Items.Add(new RelationshipCardDto
                {
                    RelationId = relation.Id,
                    ItemId = relatedItem.Id,
                    Title = relatedItem.Title,
                    Status = relatedItem.BoardItemStatus?.Status?.Name ?? "",
                    Priority = relatedItem.BoardItemPriority?.Priority?.Name ?? "",
                    AssigneeId = relatedItem.AssigneeId,
                    DueDate = relatedItem.DueDate
                });
            }

            hub.ParentCount =
                hub.Groups.First(x =>
                    x.RelationType == (int)BoardItemRelationType.Parent)
                .Items.Count;

            hub.BlockCount =
                hub.Groups.First(x =>
                    x.RelationType == (int)BoardItemRelationType.Blocks)
                .Items.Count;

            hub.RelatedCount =
                hub.Groups.First(x =>
                    x.RelationType == (int)BoardItemRelationType.Related)
                .Items.Count;

            hub.DependencyCount =
                hub.Groups.First(x =>
                    x.RelationType == (int)BoardItemRelationType.DependsOn)
                .Items.Count;

            return hub;
        }

        private static List<RelationshipGroupDto> CreateDefaultGroups()
        {
            return Enum.GetValues<BoardItemRelationType>()
                .Select(type => new RelationshipGroupDto
                {
                    RelationType = (int)type,
                    Name = GetRelationName(type)
                })
                .ToList();
        }

        private static string GetRelationName(BoardItemRelationType type)
        {
            return type switch
            {
                BoardItemRelationType.Parent => "Parent",
                BoardItemRelationType.Blocks => "Blocks",
                BoardItemRelationType.DependsOn => "Depends On",
                BoardItemRelationType.Related => "Related",
                BoardItemRelationType.Duplicate => "Duplicate",
                BoardItemRelationType.SplitFrom => "Split From",
                _ => type.ToString()
            };
        }
    }
}
