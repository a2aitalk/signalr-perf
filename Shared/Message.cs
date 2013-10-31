using System;
using Newtonsoft.Json;

namespace Shared
{
    public class Message
    {
        [JsonProperty("body")] private readonly string _body;
        [JsonProperty("dateTime")] private readonly DateTime _dateTime;

        [JsonConstructor]
        public Message(DateTime dateTime, string body)
        {
            _dateTime = dateTime;
            _body = body;
        }

        public DateTime DateTime
        {
            get { return _dateTime; }
        }

        public string Body
        {
            get { return _body; }
        }
    }
}