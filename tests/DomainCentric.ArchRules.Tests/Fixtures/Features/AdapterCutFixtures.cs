// DCA-USE-018 fixtures - a module grouped into two features, Booking and Calendar. Good: one adapter per
// feature for the commands; the booking adapter also reads the calendar's query, which is allowed. Bad: one
// adapter drives the commands of both features.

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Good.Scheduling.Application.Booking.BookSlot
{
    public sealed record BookSlotCommand(System.Guid Id);

    public interface IBookSlotInputPort : DomainCentric.BuildingBlocks.Hexagonal.Ports.In.IUseCase<BookSlotCommand, bool>
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Good.Scheduling.Application.Booking.CancelSlot
{
    public sealed record CancelSlotCommand(System.Guid Id);

    public interface ICancelSlotInputPort : DomainCentric.BuildingBlocks.Hexagonal.Ports.In.IUseCase<CancelSlotCommand, bool>
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Good.Scheduling.Application.Calendar.ShowCalendar
{
    public sealed record ShowCalendarQuery(System.Guid Id);

    public interface IShowCalendarInputPort : DomainCentric.BuildingBlocks.Hexagonal.Ports.In.IUseCase<ShowCalendarQuery, bool>
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Good.Scheduling.Application.Calendar.BlockDay
{
    public sealed record BlockDayCommand(System.Guid Id);

    public interface IBlockDayInputPort : DomainCentric.BuildingBlocks.Hexagonal.Ports.In.IUseCase<BlockDayCommand, bool>
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Bad.Scheduling.Application.Booking.BookSlot
{
    public sealed record BookSlotCommand(System.Guid Id);

    public interface IBookSlotInputPort : DomainCentric.BuildingBlocks.Hexagonal.Ports.In.IUseCase<BookSlotCommand, bool>
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Bad.Scheduling.Application.Booking.CancelSlot
{
    public sealed record CancelSlotCommand(System.Guid Id);

    public interface ICancelSlotInputPort : DomainCentric.BuildingBlocks.Hexagonal.Ports.In.IUseCase<CancelSlotCommand, bool>
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Bad.Scheduling.Application.Calendar.ShowCalendar
{
    public sealed record ShowCalendarQuery(System.Guid Id);

    public interface IShowCalendarInputPort : DomainCentric.BuildingBlocks.Hexagonal.Ports.In.IUseCase<ShowCalendarQuery, bool>
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Bad.Scheduling.Application.Calendar.BlockDay
{
    public sealed record BlockDayCommand(System.Guid Id);

    public interface IBlockDayInputPort : DomainCentric.BuildingBlocks.Hexagonal.Ports.In.IUseCase<BlockDayCommand, bool>
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Good.Scheduling.Adapter.Incoming.Web.Booking
{
    using DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Good.Scheduling.Application.Booking.BookSlot;
    using DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Good.Scheduling.Application.Booking.CancelSlot;
    using DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Good.Scheduling.Application.Calendar.ShowCalendar;

    /// <summary>The commands of one feature, and the query of a neighbouring one.</summary>
    public sealed class BookingPageController
    {
        private readonly IBookSlotInputPort _book;
        private readonly ICancelSlotInputPort _cancel;
        private readonly IShowCalendarInputPort _calendar;

        public BookingPageController(IBookSlotInputPort book, ICancelSlotInputPort cancel, IShowCalendarInputPort calendar)
        {
            _book = book;
            _cancel = cancel;
            _calendar = calendar;
        }
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Good.Scheduling.Adapter.Incoming.Web.Calendar
{
    using DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Good.Scheduling.Application.Calendar.BlockDay;
    using DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Good.Scheduling.Application.Calendar.ShowCalendar;

    public sealed class CalendarPageController
    {
        private readonly IShowCalendarInputPort _show;
        private readonly IBlockDayInputPort _block;

        public CalendarPageController(IShowCalendarInputPort show, IBlockDayInputPort block)
        {
            _show = show;
            _block = block;
        }
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Bad.Scheduling.Adapter.Incoming.Web
{
    using DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Bad.Scheduling.Application.Booking.BookSlot;
    using DomainCentric.ArchRules.Tests.Fixtures.Features.AdapterCut.Bad.Scheduling.Application.Calendar.BlockDay;

    /// <summary>Drives the commands of two features.</summary>
    public sealed class SchedulingPageController
    {
        private readonly IBookSlotInputPort _book;
        private readonly IBlockDayInputPort _block;

        public SchedulingPageController(IBookSlotInputPort book, IBlockDayInputPort block)
        {
            _book = book;
            _block = block;
        }
    }
}
