﻿using System.Collections.Generic;
using System.Diagnostics;
using ImageFramework.Model.Equation.Token;

namespace ImageFramework.Model.Equation.Markov
{
    class RuleValueOperationValue : MarkovRule
    {
        public RuleValueOperationValue(Token.Token.Type operationType)
        {
            Tokens = new List<Token.Token.Type>
            {
                Token.Token.Type.Value,
                operationType,
                Token.Token.Type.Value
            };
        }

        protected override List<Token.Token> Apply(List<Token.Token> match)
        {
            Debug.Assert(match.Count == 3);
            return new List<Token.Token> { new CombinedValueToken(match[0], match[1], match[2]) };
        }
    }
}
