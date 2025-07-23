using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Domain.AggregatesModel.UserAggregate;

public abstract class UserClaim :Entity
{
    public Guid UserGuid { get; private set; }

    public int Age { get; private set; }

    public string Sex { get; private set; }

    private List<string> _statusTitles = new List<string>();

    public ReadOnlyCollection<string> StatusTitles => _statusTitles.AsReadOnly();

}
