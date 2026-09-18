using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.Calendar.Commands.DeleteSeries;
using ClefCraft.Domain;
using MediatR;

namespace ClefCraft.Application.Features.Calendar.Commands.DeleteFromOccurrence
{
    public class DeleteFromOccurrenceCommandHandler
        : IRequestHandler<DeleteFromOccurrenceCommand>
    {
        private readonly IRecurrenceSeriesRepository _seriesRepo;
        private readonly ICalendarEventSegmentRepository _segmentRepo;
        private readonly ICalendarEventExceptionRepository _exceptionRepo;
        private readonly ICalendarAccessService _calendarAccessService;
        private readonly IUserService _userService;
        private readonly IUnitOfWork _uow;
        private readonly IMediator _mediator;

        public DeleteFromOccurrenceCommandHandler(
            IRecurrenceSeriesRepository seriesRepo,
            ICalendarEventSegmentRepository segmentRepo,
            ICalendarEventExceptionRepository exceptionRepo,
            ICalendarAccessService calendarAccessService,
            IUserService userService,
            IUnitOfWork uow,
            IMediator mediator)
        {
            _seriesRepo = seriesRepo;
            _segmentRepo = segmentRepo;
            _exceptionRepo = exceptionRepo;
            _calendarAccessService = calendarAccessService;
            _userService = userService;
            _uow = uow;
            _mediator = mediator;
        }

        public async Task<Unit> Handle(
            DeleteFromOccurrenceCommand request,
            CancellationToken cancellationToken)
        {
            await _calendarAccessService.EnsureSeriesOwnedByUserAsync(request.SeriesUid, _userService.UserId);

            var series = await _seriesRepo.GetBySeriesUidAsync(request.SeriesUid);

            if (series == null)
                throw new NotFoundException(nameof(RecurrenceSeries), request.SeriesUid);

            var activeSegment = await _segmentRepo.GetActiveSegmentAsync(
                series.Id,
                request.OccurrenceDate);

            if (activeSegment == null)
                throw new NotFoundException(nameof(CalendarEventSegment), request.OccurrenceDate.ToString());

            var removesSegmentEntirely = activeSegment.EffectiveFrom == request.OccurrenceDate;

            // If this delete would remove the only segment the series has, do NOT leave a
            // zero-segment RecurrenceSeries behind. RecurringEventProjectionService.ProjectAsync
            // falls back to legacy expansion off the root event's own (stale) RecurrenceRuleJson
            // whenever series == null || !series.Segments.Any() — leaving zero segments here would
            // silently resurrect every future occurrence via that fallback instead of deleting
            // them. Delegate to the same full series+event delete DeleteSeriesCommandHandler
            // performs, rather than reimplementing it here.
            if (removesSegmentEntirely && series.Segments.Count == 1)
            {
                await _mediator.Send(new DeleteSeriesCommand { SeriesUid = request.SeriesUid }, cancellationToken);
                return Unit.Value;
            }

            if (removesSegmentEntirely)
            {
                await _segmentRepo.DeleteAsync(activeSegment);
            }
            else
            {
                activeSegment.EffectiveTo = request.OccurrenceDate;
                await _segmentRepo.UpdateAsync(activeSegment);
            }

            // Unlike UpdateFromOccurrenceCommandHandler, no continuation segment is created —
            // there is nothing to continue.
            await _exceptionRepo.DeleteFromDateAsync(request.SeriesUid, request.OccurrenceDate);

            await _uow.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
