using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cliq.Api.Models.Messages
{
    public class VoiceAlertMessageRequest
    {

        public List<string> Zuids { get; set; }

        public string Message { get; set; }
    }
}

