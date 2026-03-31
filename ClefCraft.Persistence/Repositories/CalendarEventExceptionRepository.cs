using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
using ClefCraft.Persistence.DatabaseContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Persistence.Repositories
{
    public class CalendarEventExceptionRepository : GenericRepository<CalendarEventException>, ICalendarEventExceptionRepository
    {
        public CalendarEventExceptionRepository(ClefCraftDatabaseContext context) : base(context)
        {
        }
    }
}
