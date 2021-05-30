using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Events.Core.Abstraction;

namespace Events.Core
{
    public class Event : IEvent
    {
        private readonly DayOfWeek[] daysOfWeek;
        private readonly TimeSpan[] timeSpans;
        private readonly TimeSpan timeSpan;
        private readonly TimeSpan start;
        private readonly TimeSpan end;
        private readonly EventTypes eventType;
        public event EventHandler<PointEventArgs> Point;
        private Timer timer;
        private EventWaitHandle eventWaitHandle;
        private Event()
        {
            eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        }
        private Event(DayOfWeek[] daysOfWeek) : this()
        {
            this.daysOfWeek = daysOfWeek?.OrderBy(x => x).ToArray();
        }
        public Event(TimeSpan[] timeSpans, DayOfWeek[] daysOfWeek) : this(daysOfWeek)
        {
            this.timeSpans = timeSpans.OrderBy(x => x.Ticks).ToArray();
            eventType = EventTypes.StrongSchedule;
        }
        public Event(TimeSpan[] timeSpans) : this(timeSpans, null)
        {
            eventType = EventTypes.WeakSchedule;
        }
        public Event(TimeSpan timeSpan, TimeSpan start, TimeSpan end, DayOfWeek[] dayOfWeeks) : this(dayOfWeeks)
        {
            this.timeSpan = timeSpan;
            this.start = start;
            this.end = end;
            eventType = EventTypes.StrongInterval;
        }
        public Event(TimeSpan timeSpan) : this(timeSpan, default, default, null)
        {
            eventType = EventTypes.LooseInterval;
        }
        public Event(TimeSpan timeSpan, TimeSpan start, TimeSpan end) : this(timeSpan, start, end, null)
        {
            eventType = EventTypes.WeakInterval;
        }
        public void Start(Boolean fireImmediately) {
            _start(fireImmediately);
        }
        public void Start(TimeSpan initialDelay) {
            _start(true, initialDelay);
        }
        private void _start(Boolean fireImmediately, TimeSpan initialDelay = default)
        {
            switch (eventType)
            {
                case EventTypes.StrongSchedule:
                    {
                        TimeSpan waitTime = getWaitTime(getNextTime());
                        timer = new Timer((state) =>
                        {
                            waitTime = getWaitTime(getNextTime());
                            if (daysOfWeek.Contains(DateTime.Now.DayOfWeek) && waitTime.Ticks > 0)
                            {
                                Point?.Invoke(this, new PointEventArgs());
                                timer.Change(waitTime, default);
                            }
                            else
                            {
                                timer.Change(getWaitTime(getNextDay() + timeSpans[0]), default);
                            }
                        }, this, fireImmediately && initialDelay == default ? default(TimeSpan) : fireImmediately && initialDelay != default ? initialDelay : daysOfWeek.Contains(DateTime.Now.DayOfWeek) && waitTime.Ticks > 0 ? waitTime : getWaitTime(getNextDay() + timeSpans[0]), default);
                        break;
                    }
                case EventTypes.WeakSchedule:
                    {
                        timer = new Timer((state) =>
                        {
                            Point?.Invoke(this, new PointEventArgs());
                            var tt = getWaitTimeAbs(getNextTime());
                            timer.Change(getWaitTimeAbs(getNextTime()), default);
                        }, this, fireImmediately && initialDelay == default ? default(TimeSpan) : fireImmediately && initialDelay != default ? initialDelay : getWaitTimeAbs(getNextTime()), default);
                        break;
                    }
                case EventTypes.StrongInterval:
                    {
                        TimeSpan now = DateTime.Now.TimeOfDay;
                        timer = new Timer((state) =>
                        {
                            now = DateTime.Now.TimeOfDay;
                            if (daysOfWeek.Contains(DateTime.Now.DayOfWeek) && now >= start && now < end)
                            {
                                Point?.Invoke(this, new PointEventArgs());
                                timer.Change(timeSpan, default);
                            }
                            else
                            {
                                var tt = getWaitTime(getNextDay() + start);
                                timer.Change(getWaitTime(getNextDay() + start), default);
                            }
                        }, this, fireImmediately && initialDelay == default ? default(TimeSpan) : fireImmediately && initialDelay != default ? initialDelay : daysOfWeek.Contains(DateTime.Now.DayOfWeek) && now >= start && now < end ? timeSpan : getWaitTime(getNextDay() + start), default);
                        break;
                    }
                case EventTypes.WeakInterval:
                    {
                        TimeSpan now = DateTime.Now.TimeOfDay;
                        timer = new Timer((state) =>
                        {
                            now = DateTime.Now.TimeOfDay;
                            if (now >= start && now < end)
                            {
                                Point?.Invoke(this, new PointEventArgs());
                                timer.Change(timeSpan, TimeSpan.FromTicks(0));
                            }
                            else
                            {
                                var tt = getWaitTime(TimeSpan.FromDays(1) + start);
                                timer.Change(getWaitTime(TimeSpan.FromDays(1) + start), default);
                            }
                        }, this, fireImmediately && initialDelay == default ? default(TimeSpan) : fireImmediately && initialDelay != default ? initialDelay : now >= start && now < end ? timeSpan : getWaitTime(TimeSpan.FromDays(1) + start), default);
                        break;
                    }
                case EventTypes.LooseInterval:
                    {
                        timer = new Timer((state) =>
                        {
                            Point?.Invoke(this, new PointEventArgs());
                            timer.Change(timeSpan, default);
                        }, this, fireImmediately && initialDelay == default ? default(TimeSpan) : fireImmediately && initialDelay != default ? initialDelay : timeSpan, default);
                        break;
                    }
            }
        }
        public void Stop() {
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            timer.Dispose();
        }
        private TimeSpan getNextDay()
        {
            if (daysOfWeek != null)
            {
                DayOfWeek today = DateTime.Now.DayOfWeek;
                int diff = 0;
                for (int i = 0; i < daysOfWeek.Length; i++)
                {
                    if (daysOfWeek[i] > today)
                    {
                        diff = Math.Abs(daysOfWeek[i] - today);
                        return (DateTime.Now.AddDays(diff) - DateTime.Now.Date);
                    }
                }
                diff = (7 - (int)today) + (int)daysOfWeek[0];
                return (DateTime.Now.Date.AddDays(diff) - DateTime.Now.Date);
            }
            return default;
        }
        private TimeSpan getNextTime()
        {
            if (timeSpans != null)
            {
                TimeSpan now = DateTime.Now.TimeOfDay;
                for (int i = 0; i < timeSpans.Length; i++)
                {
                    if (timeSpans[i] > now)
                    {
                        return timeSpans[i];
                    }
                }
            }
            return default;
        }
        private TimeSpan getWaitTime(TimeSpan timeSpan)
        {
            return (timeSpan - DateTime.Now.TimeOfDay);
        }

        private TimeSpan getWaitTimeAbs(TimeSpan timeSpan)
        {
            return TimeSpan.FromTicks(Math.Abs((timeSpan - DateTime.Now.TimeOfDay).Ticks));
        }
        private TimeSpan getIntervalAbs()
        {
            TimeSpan now = DateTime.Now.TimeOfDay;
            if (now >= start && now < end)
            {
                return timeSpan;
            }
            else
            {
                return TimeSpan.FromTicks(Math.Abs((now - start).Ticks));
            }
        }
    }
    enum EventTypes
    {
        StrongSchedule,
        WeakSchedule,
        LooseInterval,
        WeakInterval,
        StrongInterval
    }
}