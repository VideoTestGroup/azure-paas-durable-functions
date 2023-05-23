using System;
using System.Collections.Generic;
using System.IO.Enumeration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageIngest.Functions.Model
{
    // this is the class that Collector inserts into the queue, to be consumed by the zipper function
    public class TagBatchQueueItem
    {
        public string Namespace { get; set; } = "default";
        public string BatchId { get; set; }

        public string Container { get; set; }

        public BlobTags[] Tags {get;set; }

    }
   
}
