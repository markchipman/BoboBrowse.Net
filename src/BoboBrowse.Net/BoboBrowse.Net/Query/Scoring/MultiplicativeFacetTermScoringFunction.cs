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
namespace BoboBrowse.Net.Query.Scoring
{
    using BoboBrowse.Net.Support;
    using Lucene.Net.Search;
    using Lucene.Net.Support;

    public class MultiplicativeFacetTermScoringFunction : IFacetTermScoringFunction
    {
        private float m_boost = 1.0f;

        public void ClearScores()
        {
            m_boost = 1.0f;
        }

        public float Score(int df, float boost)
        {
            return boost;
        }

        public void ScoreAndCollect(int df, float boost)
        {
            if (boost > 0)
            {
                m_boost *= boost;
            }
        }

        public float GetCurrentScore()
        {
            return m_boost;
        }

        public virtual Explanation Explain(int df, float boost)
        {
            Explanation expl = new Explanation();
            expl.Value = Score(df, boost);
            expl.Description = "boost value of: " + boost;
            return expl;
        }

        public virtual Explanation Explain(params float[] scores)
        {
            Explanation expl = new Explanation();
            float boost = 1.0f;
            foreach (float score in scores)
            {
                boost *= score;
            }
            expl.Value = boost;
            expl.Description = "product of: " + Arrays.ToString(scores);
            return expl;
        }
    }
}
