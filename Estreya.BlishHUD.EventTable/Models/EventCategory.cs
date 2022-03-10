namespace Estreya.BlishHUD.EventTable.Models
{
    using Blish_HUD;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Utils;
    using Microsoft.Xna.Framework;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [Serializable]
    public class EventCategory
    {
        private static readonly Logger Logger = Logger.GetLogger<EventCategory>();

        private readonly TimeSpan updateInterval = TimeSpan.FromMinutes(15);
        private double timeSinceUpdate = 0;

        [JsonProperty("key")]
        public string Key { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("showCombined")]
        public bool ShowCombined { get; set; }

        [JsonProperty("events")]
        private List<Event> _originalEvents = new List<Event>();

        [JsonIgnore]
        private List<Event> _fillerEvents = new List<Event>();

        public List<Event> Events
        {
            get
            {
                return _originalEvents.Concat(_fillerEvents).ToList();
            }
            set => _originalEvents = value;
        }

        public EventCategory()
        {
            this.timeSinceUpdate = updateInterval.TotalMilliseconds;
        }

        private void ModuleSettings_EventSettingChanged(object sender, ModuleSettings.EventSettingsChangedEventArgs e)
        {
            if (this._originalEvents.Any(ev => ev.SettingKey.ToLowerInvariant() == e.Name.ToLowerInvariant()))
            {
                UpdateEventOccurences(null);
            }
        }

        private void UpdateEventOccurences(GameTime gameTime)
        {
            lock (_fillerEvents)
            {
                this._fillerEvents.Clear();
            }

            //var activeEvents = this.Events.Where(e => eventSettings.Find(eventSetting => eventSetting.EntryKey == e.Name).Value).ToList();
            var activeEvents = this._originalEvents.Where(e => !e.IsDisabled()).ToList();

            List<KeyValuePair<DateTime, Event>> activeEventStarts = new List<KeyValuePair<DateTime, Event>>();

            foreach (var activeEvent in activeEvents)
            {
                List<DateTime> eventOccurences = activeEvent.Occurences.Where(oc => (oc >= EventTableModule.ModuleInstance.EventTimeMin || oc.AddMinutes(activeEvent.Duration) >= EventTableModule.ModuleInstance.EventTimeMin) && oc <= EventTableModule.ModuleInstance.EventTimeMax).ToList();//.GetStartOccurences(now, max, min);

                eventOccurences.ForEach(eo => activeEventStarts.Add(new KeyValuePair<DateTime, Event>(eo, activeEvent)));
            }

            activeEventStarts = activeEventStarts.OrderBy(aes => aes.Key).ToList();

            var modifiedEventStarts = activeEventStarts.ToList();

            for (int i = 0; i < activeEventStarts.Count - 1; i++)
            {
                var currentEvent = activeEventStarts.ElementAt(i);
                var nextEvent = activeEventStarts.ElementAt(i + 1);

                var currentStart = currentEvent.Key;
                var currentEnd = currentStart + TimeSpan.FromMinutes(currentEvent.Value.Duration);

                var nextStart = nextEvent.Key;

                var gap = nextStart - currentEnd;
                if (gap > TimeSpan.Zero)
                {
                    Event filler = new Event()
                    {
                        Name = $"{currentEvent.Value.Name} - {nextEvent.Value.Name}",
                        Duration = (int)gap.TotalMinutes,
                        Filler = true,
                        EventCategory = this
                    };

                    modifiedEventStarts.Insert(i + 1, new KeyValuePair<DateTime, Event>(currentEnd, filler));
                }
            }

            if (activeEventStarts.Count > 1)
            {
                var firstEvent = activeEventStarts.FirstOrDefault();
                var lastEvent = activeEventStarts.LastOrDefault();

                // We have a following event
                var nextEvent = activeEvents.Select(ae =>
                {
                    return new KeyValuePair<DateTime, Event>(ae.Occurences.Where(oc => oc > lastEvent.Key && oc < EventTableModule.ModuleInstance.EventTimeMax.AddDays(2))/*GetStartOccurences(now, max.AddDays(2), lastEvent.Key, limitsBetweenRanges: true)*/.FirstOrDefault(), ae);
                }).OrderBy(aeo => aeo.Key).First();
                var nextEventMapping = new KeyValuePair<DateTime, Event>(nextEvent.Key, nextEvent.Value);

                var nextStart = nextEventMapping.Key;
                var nextEnd = nextStart + TimeSpan.FromMinutes(nextEventMapping.Value.Duration);

                if (nextStart - lastEvent.Key > TimeSpan.Zero)
                {
                    Event filler = new Event()
                    {
                        Name = $"{lastEvent.Value.Name} - {nextEventMapping.Value.Name}",
                        Filler = true,
                        EventCategory = this,
                        Duration = (int)(nextStart - lastEvent.Key).TotalMinutes
                    };

                    modifiedEventStarts.Add(new KeyValuePair<DateTime, Event>(lastEvent.Key + TimeSpan.FromMinutes(lastEvent.Value.Duration), filler));
                }

                // We have a previous event
                var prevEvent = activeEvents.Select(ae =>
                {
                    return new KeyValuePair<DateTime, Event>(ae.Occurences.Where(oc => oc > EventTableModule.ModuleInstance.EventTimeMin.AddDays(-2) && oc < firstEvent.Key)/*GetStartOccurences(now, firstEvent.Key, min.AddDays(-2), limitsBetweenRanges: true)*/.LastOrDefault(), ae);
                }).OrderBy(aeo => aeo.Key).Last();

                var prevEventMapping = new KeyValuePair<DateTime, Event>(prevEvent.Key, prevEvent.Value);

                var prevStart = prevEventMapping.Key;
                var prevEnd = prevStart + TimeSpan.FromMinutes(prevEventMapping.Value.Duration);

                if (firstEvent.Key - prevEnd > TimeSpan.Zero)
                {
                    Event filler = new Event()
                    {
                        Name = $"{prevEventMapping.Value.Name} - {firstEvent.Value.Name}",
                        Filler = true,
                        Duration = (int)(firstEvent.Key - prevEnd).TotalMinutes,
                        EventCategory = this
                    };

                    modifiedEventStarts.Add(new KeyValuePair<DateTime, Event>(prevEnd, filler));
                }
            }
            else if (activeEventStarts.Count == 1 && activeEvents.Count >= 1)
            {
                var currentEvent = activeEventStarts.First();
                var currentStart = currentEvent.Key;
                var currentEnd = currentStart + TimeSpan.FromMinutes(currentEvent.Value.Duration);

                // We have a following event
                var nextEvent = activeEvents.Select(ae =>
                {
                    return new KeyValuePair<DateTime, Event>(ae.Occurences.Where(oc => oc > currentEnd && oc < EventTableModule.ModuleInstance.EventTimeMax.AddDays(2))/*GetStartOccurences(now, max.AddDays(2), currentEnd, limitsBetweenRanges: true)*/.FirstOrDefault(), ae);
                }).OrderBy(aeo => aeo.Key).First();
                var nextEventMapping = new KeyValuePair<DateTime, Event>(nextEvent.Key, nextEvent.Value);

                var nextStart = nextEventMapping.Key;
                var nextEnd = nextStart + TimeSpan.FromMinutes(nextEventMapping.Value.Duration);

                if (nextStart - currentEnd > TimeSpan.Zero)
                {
                    Event filler = new Event()
                    {
                        Name = $"{currentEvent.Value.Name} - {nextEventMapping.Value.Name}",
                        Filler = true,
                        Duration = (int)(nextStart - currentEnd).TotalMinutes,
                        EventCategory = this
                    };

                    modifiedEventStarts.Add(new KeyValuePair<DateTime, Event>(currentEnd, filler));
                }

                // We have a previous event
                var prevEvent = activeEvents.Select(ae =>
                {
                    return new KeyValuePair<DateTime, Event>(ae.Occurences.Where(oc => oc > EventTableModule.ModuleInstance.EventTimeMin.AddDays(-2) && oc < currentStart)/*GetStartOccurences(now, currentStart, min.AddDays(-2), limitsBetweenRanges: true)*/.LastOrDefault(), ae);
                }).OrderBy(aeo => aeo.Key).Last();

                var prevEventMapping = new KeyValuePair<DateTime, Event>(prevEvent.Key, prevEvent.Value);

                var prevStart = prevEventMapping.Key;
                var prevEnd = prevStart + TimeSpan.FromMinutes(prevEventMapping.Value.Duration);

                if (currentStart - prevEnd > TimeSpan.Zero)
                {
                    Event filler = new Event()
                    {
                        Name = $"{prevEventMapping.Value.Name} - {currentEvent.Value.Name}",
                        Filler = true,
                        Duration = (int)(currentStart - prevEnd).TotalMinutes,
                        EventCategory = this
                    };

                    modifiedEventStarts.Add(new KeyValuePair<DateTime, Event>(prevEnd, filler));
                }
            }
            else if (activeEventStarts.Count == 0 && activeEvents.Count >= 1)
            {
                var prevEvent = activeEvents.Select(ae =>
                {
                    return new KeyValuePair<DateTime, Event>(ae.Occurences.Where(oc => oc > EventTableModule.ModuleInstance.EventTimeMin.AddDays(-2) && oc < EventTableModule.ModuleInstance.EventTimeMax)/*GetStartOccurences(now, now, min.AddDays(-2), limitsBetweenRanges: true)*/.LastOrDefault(), ae);
                }).OrderBy(aeo => aeo.Key).Last();

                var nextEvent = activeEvents.Select(ae =>
                {
                    return new KeyValuePair<DateTime, Event>(ae.Occurences.Where(oc => oc > EventTableModule.ModuleInstance.EventTimeMin && oc < EventTableModule.ModuleInstance.EventTimeMax.AddDays(2))/*GetStartOccurences(now, max.AddDays(2), now, limitsBetweenRanges: true)*/.FirstOrDefault(), ae);
                }).OrderBy(aeo => aeo.Key).First();

                var prevEventMapping = new KeyValuePair<DateTime, Event>(prevEvent.Key, prevEvent.Value);
                var nextEventMapping = new KeyValuePair<DateTime, Event>(nextEvent.Key, nextEvent.Value);

                var prevStart = prevEventMapping.Key;
                var prevEnd = prevStart + TimeSpan.FromMinutes(prevEventMapping.Value.Duration);
                var nextStart = nextEventMapping.Key;

                Event filler = new Event()
                {
                    Name = $"{prevEventMapping.Value.Name} - {nextEventMapping.Value.Name}",
                    Duration = (int)(nextStart - prevEnd).TotalMinutes,
                    Filler = true,
                    EventCategory = this
                };

                modifiedEventStarts.Add(new KeyValuePair<DateTime, Event>(prevEnd, filler));
            }

            lock (_fillerEvents)
            {
                modifiedEventStarts.Where(e => e.Value.Filler).ToList().ForEach(modEvent => modEvent.Value.Occurences.Add(modEvent.Key));
                var modifiedEvents = modifiedEventStarts.Where(e => e.Value.Filler).Select(e => e.Value);
                this._fillerEvents.AddRange(modifiedEvents);
            }

            //return modifiedEventStarts.OrderBy(mes => mes.Key).ToList();
        }
        public void Finish()
        {
            var now = EventTableModule.ModuleInstance.DateTimeNow.ToUniversalTime();
            DateTime until = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0).AddDays(1);
            EventTableModule.ModuleInstance.HiddenState.Add(this.Key, until, true);
        }

        public bool IsDisabled()
        {
            bool disabled = EventTableModule.ModuleInstance.HiddenState.IsHidden(this.Key);

            return disabled;
        }

        public Task LoadAsync()
        {
            EventTableModule.ModuleInstance.ModuleSettings.EventSettingChanged += ModuleSettings_EventSettingChanged;
            return Task.CompletedTask;
        }


        public void Update(GameTime gameTime)
        {
            this.Events.ForEach(ev => ev.Update(gameTime));
            UpdateCadenceUtil.UpdateWithCadence(UpdateEventOccurences, gameTime, updateInterval.TotalMilliseconds, ref timeSinceUpdate);
        }
    }
}
