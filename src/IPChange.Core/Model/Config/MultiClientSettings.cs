using System.Collections.Generic;
using System.Text;

namespace IPChange.Core.Model.Config
{
    public class MultiClientSettings
    {
        public string ClientName { get; set; }

        public MultiClientRoute53ProviderSettings Route53 { get; set; }
    }
}
