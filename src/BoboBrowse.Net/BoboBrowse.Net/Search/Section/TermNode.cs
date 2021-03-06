﻿//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug.
//*
//* Copyright (C) 2005-2015  John Wang
//*
//* Licensed under the Apache License, Version 2.0 (the "License");
//* you may not use this file except in compliance with the License.
//* You may obtain a copy of the License at
//*
//*   http://www.apache.org/licenses/LICENSE-2.0
//*
//* Unless required by applicable law or agreed to in writing, software
//* distributed under the License is distributed on an "AS IS" BASIS,
//* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//* See the License for the specific language governing permissions and
//* limitations under the License.

// Version compatibility level: 4.0.2
namespace BoboBrowse.Net.Search.Section
{
    using Lucene.Net.Index;
    using Lucene.Net.Util;

    public class TermNode : AbstractTerminalNode
    {
        protected int m_positionInPhrase;

        public TermNode(Term term, AtomicReader reader)
            : this(term, 0, reader)
        {
        }

        public TermNode(Term term, int positionInPhrase, AtomicReader reader)
            : base(term, reader)
        {
            m_positionInPhrase = positionInPhrase; // relative position in a phrase
        }

        /// <summary>
        /// Added in the .NET version as an accessor to the _positionInPhrase field.
        /// </summary>
        internal virtual int PositionInPhrase
        {
            get { return m_positionInPhrase; }
        }

        public override int FetchSec(int targetSec)
        {
            if (m_posLeft > 0)
            {
                while (true)
                {
                    m_curPos = m_dp.NextPosition();
                    m_posLeft--;

                    if (ReadSecId() >= targetSec) return m_curSec;

                    if (m_posLeft <= 0) break;
                }
            }
            m_curSec = SectionSearchQueryPlan.NO_MORE_SECTIONS;
            return m_curSec;
        }

        // NOTE: Added this method so FetchPos() can be utilized internally
        // without changing the scope of FetchPos() method from protected.
        internal virtual int FetchPosInternal()
        {
            return this.FetchPos();
        }

        protected override int FetchPos()
        {
            if (m_posLeft > 0)
            {
                m_curPos = m_dp.NextPosition();
                m_posLeft--;
                return m_curPos;
            }
            m_curPos = SectionSearchQueryPlan.NO_MORE_POSITIONS;
            return m_curPos;
        }

        public virtual int ReadSecId()
        {
            BytesRef payload = m_dp.GetPayload();
            if (payload != null)
            {
                m_curSec = m_intDecoders[payload.Length].Decode(payload.Bytes);
            }
            else
            {
                m_curSec = -1;
            }
            return m_curSec;
        }

        /// <summary>
        /// NOTE: This was IntDecoder in bobo-browse
        /// </summary>
        private abstract class Int32Decoder
        {
            public abstract int Decode(byte[] d);
        }

        /// <summary>
        /// NOTE: This was IntDecoder1 in bobo-browse
        /// </summary>
        private class Int32Decoder1 : Int32Decoder
        {
            public override int Decode(byte[] d)
            {
                return 0;
            }
        }

        /// <summary>
        /// NOTE: This was IntDecoder2 in bobo-browse
        /// </summary>
        private class Int32Decoder2 : Int32Decoder
        {
            public override int Decode(byte[] d)
            {
                return (d[0] & 0xff);
            }
        }

        /// <summary>
        /// NOTE: This was IntDecoder3 in bobo-browse
        /// </summary>
        private class Int32Decoder3 : Int32Decoder
        {
            public override int Decode(byte[] d)
            {
                return (d[0] & 0xff) | ((d[1] & 0xff) << 8);
            }
        }

        /// <summary>
        /// NOTE: This was IntDecoder4 in bobo-browse
        /// </summary>
        private class Int32Decoder4 : Int32Decoder
        {
            public override int Decode(byte[] d)
            {
                return (d[0] & 0xff) | ((d[1] & 0xff) << 8) | ((d[2] & 0xff) << 16);
            }
        }

        /// <summary>
        /// NOTE: This was IntDecoder5 in bobo-browse
        /// </summary>
        private class Int32Decoder5 : Int32Decoder
        {
            public override int Decode(byte[] d)
            {
                return (d[0] & 0xff) | ((d[1] & 0xff) << 8) | ((d[2] & 0xff) << 16) | ((d[3] & 0xff) << 24);
            }
        }

        private readonly static Int32Decoder[] m_intDecoders = new Int32Decoder[]
        {
            new Int32Decoder1(),
            new Int32Decoder2(),
            new Int32Decoder3(),
            new Int32Decoder4(),
            new Int32Decoder5()
        };
    }
}
