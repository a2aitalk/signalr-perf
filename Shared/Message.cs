using System;
using Newtonsoft.Json;

namespace Shared
{
    public class Message
    {
        [JsonProperty("body")] private readonly string _body;
        [JsonProperty("dateTime")] private readonly long _ticks;

        [JsonConstructor]
        public Message(long ticks, string body)
        {
            _ticks = ticks;
            _body = body;
        }

        public long Ticks
        {
            get { return _ticks; }
        }

        public string Body
        {
            get { return _body; }
        }
    }
}