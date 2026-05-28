using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Commands.UpdateSeries
{
    /// <summary>
    /// Rewrites the entire series definition across all segments and
    /// purges all per-occurrence exceptions.
    ///
    /// This corresponds to the "edit ALL occurrences" UX path.
    ///
    /// IMPORTANT: unlike UpdateSeriesPreserveExceptions, this command
    /// intentionally deletes every CalendarEventException for the series
    /// so that stale single-occurrence overrides do not survive into the
    /// freshly redefined series.  If the caller needs to preserve exceptions,
    /// they should route through UpdateSeriesPreserveExceptions instead.
    /// </summary>
    public class UpdateSeriesOverrideAllCommandHandler
        : IRequestHandler<UpdateSeriesOverrideAllCommand>
    {
        private readonly ICalendarEventSegmentRepository _segmentRepo;
        private readonly ICalendarEventExceptionRepository _exceptionRepo;
        private readonly IUnitOfWork _uow;

        public UpdateSeriesOverrideAllCommandHandler(
            ICalendarEventSegmentRepository segmentRepo,
            ICalendarEventExceptionRepository exceptionRepo,
            IUnitOfWork uow)
        {
            _segmentRepo = segmentRepo;
            _exceptionRepo = exceptionRepo;
            _uow = uow;
        }

        public async Task<Unit> Handle(
            UpdateSeriesOverrideAllCommand request,
            CancellationToken cancellationToken)
        {
            // ------------------------------------------------------------------
            // 1. Update every segment in the series.
            //    Null fields mean "do not change that specific property",
            //    but RecurrenceRuleJson is always replaced (it is required
            //    because that is the whole point of "override all").
            // ------------------------------------------------------------------
            var segments = await _segmentRepo
                .GetBySeriesUidAsync(request.SeriesUid);

            foreach (var segment in segments)
            {
                if (request.Subject != null)
                    segment.Subject = request.Subject;

                if (request.Location != null)
                    segment.Location = request.Location;

                if (request.Comment != null)
                    segment.Comment = request.Comment;

                // RecurrenceRuleJson is required on this command — replace always.
                segment.RecurrenceRuleJson = request.RecurrenceRuleJson;
            }

            // ------------------------------------------------------------------
            // 2. Purge all per-occurrence exceptions.
            //
            //    After a full series override the exception rows that existed
            //    before this call are keyed against occurrence dates from the
            //    old recurrence pattern.  Keeping them would cause phantom
            //    "modified" or "cancelled" markers to appear on dates that
            //    may no longer even be valid occurrences in the new pattern.
            // ------------------------------------------------------------------
            await _exceptionRepo.DeleteAllForSeriesAsync(request.SeriesUid);

            await _uow.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}