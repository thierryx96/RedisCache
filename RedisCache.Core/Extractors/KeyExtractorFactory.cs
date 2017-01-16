using System;

namespace PEL.Framework.Redis.Extractors
{
    public class KeyExtractor<TValue> : IKeyExtractor<TValue>
    {
        private readonly Func<TValue, string> _extractor;

        public KeyExtractor(Func<TValue, string> extractor)
        {
            _extractor = extractor;
        }

        public string ExtractKey(TValue value) => _extractor(value);

        public static IKeyExtractor<TValue> Create(Func<TValue, string> extractor)
            => new KeyExtractor<TValue>(extractor);
    }
}