using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cliq.Api.Models.Messages
{
    public class SICALApiRequestDto
    {
        public string LoginUserName { get; set; }
        public string FullName { get; set; }
        public string BioId { get; set; }
        public string Department { get; set; }
        public string RequestBioId { get; set; }
        public string StaffName { get; set; }
        public string Otp { get; set; }
        public string ExpiryTime { get; set; }
    }
}