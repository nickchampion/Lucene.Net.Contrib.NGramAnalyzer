﻿using System;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Tokenattributes;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Contrib.NGramAnalyzer
{
    public class NGramAnalyzer : Analyzer
    {
        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            var tokenizer = new StandardTokenizer(Version.LUCENE_30, reader) { MaxTokenLength = 255 };
            TokenStream filter = new StandardFilter(tokenizer);
            filter = new LowerCaseFilter(filter);
            filter = new StopFilter(false, filter, StandardAnalyzer.STOP_WORDS_SET);
            return new NGramTokenFilter(filter, 3, 6);
        }
    }

    public class NGramTokenFilter : TokenFilter
    {
        public static int DefaultMinNgramSize = 1;
        public static int DefaultMaxNgramSize = 2;

        private readonly int _maxGram;
        private readonly int _minGram;
        private readonly IOffsetAttribute _offsetAtt;
        private readonly ITermAttribute _termAtt;

        private int _curGramSize;
        private char[] _curTermBuffer;
        private int _curTermLength;
        private int _tokStart;

        /**
                * Creates NGramTokenFilter with given min and max n-grams.
                * <param name="input"><see cref="TokenStream"/> holding the input to be tokenized</param>
                * <param name="minGram">the smallest n-gram to generate</param>
                * <param name="maxGram">the largest n-gram to generate</param>
                */

        public NGramTokenFilter(TokenStream input, int minGram, int maxGram)
            : base(input)
        {
            if (minGram < 1)
            {
                throw new ArgumentException("minGram must be greater than zero");
            }
            if (minGram > maxGram)
            {
                throw new ArgumentException("minGram must not be greater than maxGram");
            }
            _minGram = minGram;
            _maxGram = maxGram;

            _termAtt = AddAttribute<ITermAttribute>();
            _offsetAtt = AddAttribute<IOffsetAttribute>();
        }

        /**
                * Creates NGramTokenFilter with default min and max n-grams.
                * <param name="input"><see cref="TokenStream"/> holding the input to be tokenized</param>
                */

        public NGramTokenFilter(TokenStream input)
            : this(input, DefaultMinNgramSize, DefaultMaxNgramSize)
        {
        }

        /** Returns the next token in the stream, or null at EOS. */

        public override bool IncrementToken()
        {
            while (true)
            {
                if (_curTermBuffer == null)
                {
                    if (!input.IncrementToken())
                    {
                        return false;
                    }
                    _curTermBuffer = (char[])_termAtt.TermBuffer().Clone();
                    _curTermLength = _termAtt.TermLength();
                    _curGramSize = _minGram;
                    _tokStart = _offsetAtt.StartOffset;
                }
                while (_curGramSize <= Math.Min(_maxGram, _curTermLength))
                {
                    ClearAttributes();
                    _termAtt.SetTermBuffer(_curTermBuffer, 0, _curGramSize);
                    _offsetAtt.SetOffset(_tokStart, _tokStart + _curGramSize);
                    _curGramSize++; // increase n-gram size
                    return true;
                }
                _curTermBuffer = null;
            }
        }

        public override void Reset()
        {
            base.Reset();
            _curTermBuffer = null;
        }
    }
}
