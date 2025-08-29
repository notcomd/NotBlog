using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Domain.AggregatesModel.UserAggregate;

public class WIthResultSafetyDto
{
    public WIthResultSafetyDto(EnUserStatus userStatus, EnBlackOrWhite blackOrWhite)
    {
        UserStatus = userStatus;
        BlackOrWhite = blackOrWhite;
    }

    public EnUserStatus UserStatus { get;  }

    public EnBlackOrWhite BlackOrWhite { get;  }
}
