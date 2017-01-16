using PEL.Framework.Redis.Extractors;

namespace PEL.Framework.Redis.Configuration
{
    public class IndexSettings<TValue>
    {
        /// <summary>
        ///     The key for the index is unique, if set as unique Redis will treat this internal as a hashset
        ///     having a unique index makes the retrieval and writing faster. However redis is not performing
        ///     any constraint unique check on this index.
        /// </summary>
        public bool Unique { get; set; }

        /// <summary>
        ///     Include the payload (the data/entity) with the index. This increase performance, while consuming more ram on the
        ///     redis server.
        /// </summary>
        public bool WithPayload { get; set; }

        /// <summary>
        ///     Extractor for the key of the index. This take the entity as a parameter
        ///     and extracts the value to index (=the key of the index)
        /// </summary>
        public IKeyExtractor<TValue> Extractor { get; set; }

        /// <summary>
        ///     name of the index, if not provided, it will use the extractor class name.
        /// </summary>
        public string Name { get; set; }
    }
}