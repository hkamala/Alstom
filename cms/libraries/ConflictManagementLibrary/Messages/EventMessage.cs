using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ConflictManagementLibrary.Messages
{
    public class EventMessage
    {
        public string EventTimeStamp { get; set; } = null!;
        public string EventName { get; set; } = null!;
        public string EventLevel { get; set; } = null!;
        public string MessageOfEvent { get; set; } = null!;

        private EventMessage(string eventName, string eventLevel, string messageOfEvent)
        {
            EventName = eventName;
            EventLevel = eventLevel;
            MessageOfEvent = messageOfEvent;
            EventTimeStamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss:fff");
        }
        public static EventMessage CreateInstance(string eventName, string eventLevel, string messageOfEvent)
        {
            return new EventMessage(eventName, eventLevel, messageOfEvent);
        }
        [JsonConstructor]
        public EventMessage()
        {
            
        }
    }
}
