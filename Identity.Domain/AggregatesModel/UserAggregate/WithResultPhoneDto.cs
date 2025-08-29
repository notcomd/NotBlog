using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Domain.AggregatesModel.UserAggregate
{
    public class WithResultPhoneDto
    {
        public WithResultPhoneDto(int regionCode, string phoneNumber)
        {
            RegionCode = regionCode;
            PhoneNumber = phoneNumber;
        }

        public int RegionCode { get; }

        public string PhoneNumber { get; }
    }
}
