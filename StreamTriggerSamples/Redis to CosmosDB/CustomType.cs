using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.Redis.Samples
{
    public class Data
    {
        public string id { get; set; }
        public Dictionary<string, string> values { get; set; }
    }

}
