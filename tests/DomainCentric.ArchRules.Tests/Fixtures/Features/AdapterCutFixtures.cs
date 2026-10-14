// DCA-USE-018 fixtures — a module grouped into two features, Booking and Calendar. Good: one adapter per
// feature, a context-wide port beside them. Bad: one adapter drives the input ports of both features.

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Good.Scheduling.Application.Booking.BookSlot
{
    public interface IBookSlotInputPort : DomainCentric.BuildingBlocks.Hexagonal.Ports.In.IInputPort
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Good.Scheduling.Application.Booking.CancelSlot
{
    public interface ICancelSlotInputPort : DomainCentric.BuildingBlocks.Hexagonal.Ports.In.IInputPort
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Good.Scheduling.Application.Calendar.ShowCalendar
{
    public interface IShowCalendarInputPort : DomainCentric.BuildingBlocks.Hexagonal.Ports.In.IInputPort
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Good.Scheduling.Adapter.Incoming.Web.Booking
{
    using DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Good.Scheduling.Application.Booking.BookSlot;
    using DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Good.Scheduling.Application.Booking.CancelSlot;

    /// <summary>Two use cases of one feature.</summary>
    public sealed class BookingPageController
    {
        private readonly IBookSlotInputPort _book;
        private readonly ICancelSlotInputPort _cancel;

        public BookingPageController(IBookSlotInputPort book, ICancelSlotInputPort cancel)
        {
            _book = book;
            _cancel = cancel;
        }
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Good.Scheduling.Adapter.Incoming.Web.Calendar
{
    using DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Good.Scheduling.Application.Calendar.ShowCalendar;

    public sealed class CalendarPageController
    {
        private readonly IShowCalendarInputPort _show;

        public CalendarPageController(IShowCalendarInputPort show)
        {
            _show = show;
        }
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Bad.Scheduling.Application.Booking.BookSlot
{
    public interface IBookSlotInputPort : DomainCentric.BuildingBlocks.Hexagonal.Ports.In.IInputPort
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Bad.Scheduling.Application.Calendar.ShowCalendar
{
    public interface IShowCalendarInputPort : DomainCentric.BuildingBlocks.Hexagonal.Ports.In.IInputPort
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Bad.Scheduling.Adapter.Incoming.Web
{
    using DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Bad.Scheduling.Application.Booking.BookSlot;
    using DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Bad.Scheduling.Application.Calendar.ShowCalendar;

    /// <summary>Drives the input ports of two features.</summary>
    public sealed class SchedulingPageController
    {
        private readonly IBookSlotInputPort _book;
        private readonly IShowCalendarInputPort _show;

        public SchedulingPageController(IBookSlotInputPort book, IShowCalendarInputPort show)
        {
            _book = book;
            _show = show;
        }
    }
}
