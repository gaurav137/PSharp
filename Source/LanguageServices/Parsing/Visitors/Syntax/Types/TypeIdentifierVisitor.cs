﻿//-----------------------------------------------------------------------
// <copyright file="TypeIdentifierVisitor.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing.Syntax
{
    /// <summary>
    /// The P# type identifier parsing visitor.
    /// </summary>
    internal sealed class TypeIdentifierVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        public TypeIdentifierVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the text unit.
        /// </summary>
        /// <param name="textUnit">TextUnit</param>
        public void Visit(ref TextUnit textUnit)
        {
            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                base.TokenStream.Peek().Type != TokenType.Void &&
                base.TokenStream.Peek().Type != TokenType.Object &&
                base.TokenStream.Peek().Type != TokenType.String &&
                base.TokenStream.Peek().Type != TokenType.Sbyte &&
                base.TokenStream.Peek().Type != TokenType.Byte &&
                base.TokenStream.Peek().Type != TokenType.Short &&
                base.TokenStream.Peek().Type != TokenType.Ushort &&
                base.TokenStream.Peek().Type != TokenType.Int &&
                base.TokenStream.Peek().Type != TokenType.Uint &&
                base.TokenStream.Peek().Type != TokenType.Long &&
                base.TokenStream.Peek().Type != TokenType.Ulong &&
                base.TokenStream.Peek().Type != TokenType.Char &&
                base.TokenStream.Peek().Type != TokenType.Bool &&
                base.TokenStream.Peek().Type != TokenType.Decimal &&
                base.TokenStream.Peek().Type != TokenType.Float &&
                base.TokenStream.Peek().Type != TokenType.Double &&
                base.TokenStream.Peek().Type != TokenType.Identifier))
            {
                throw new ParsingException("Expected type identifier.", base.TokenStream.Peek(),
                    TokenType.Identifier);
            }
            
            var line = base.TokenStream.Peek().TextUnit.Line;

            bool expectsDot = false;
            while (!base.TokenStream.Done)
            {
                if (!expectsDot &&
                    (base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                    base.TokenStream.Peek().Type != TokenType.Void &&
                    base.TokenStream.Peek().Type != TokenType.Object &&
                    base.TokenStream.Peek().Type != TokenType.String &&
                    base.TokenStream.Peek().Type != TokenType.Sbyte &&
                    base.TokenStream.Peek().Type != TokenType.Byte &&
                    base.TokenStream.Peek().Type != TokenType.Short &&
                    base.TokenStream.Peek().Type != TokenType.Ushort &&
                    base.TokenStream.Peek().Type != TokenType.Int &&
                    base.TokenStream.Peek().Type != TokenType.Uint &&
                    base.TokenStream.Peek().Type != TokenType.Long &&
                    base.TokenStream.Peek().Type != TokenType.Ulong &&
                    base.TokenStream.Peek().Type != TokenType.Char &&
                    base.TokenStream.Peek().Type != TokenType.Bool &&
                    base.TokenStream.Peek().Type != TokenType.Decimal &&
                    base.TokenStream.Peek().Type != TokenType.Float &&
                    base.TokenStream.Peek().Type != TokenType.Double &&
                    base.TokenStream.Peek().Type != TokenType.Identifier) ||
                    (expectsDot && base.TokenStream.Peek().Type != TokenType.Dot))
                {
                    break;
                }

                if (base.TokenStream.Peek().Type == TokenType.MachineDecl ||
                    base.TokenStream.Peek().Type == TokenType.Void ||
                    base.TokenStream.Peek().Type == TokenType.Object ||
                    base.TokenStream.Peek().Type == TokenType.String ||
                    base.TokenStream.Peek().Type == TokenType.Sbyte ||
                    base.TokenStream.Peek().Type == TokenType.Byte ||
                    base.TokenStream.Peek().Type == TokenType.Short ||
                    base.TokenStream.Peek().Type == TokenType.Ushort ||
                    base.TokenStream.Peek().Type == TokenType.Int ||
                    base.TokenStream.Peek().Type == TokenType.Uint ||
                    base.TokenStream.Peek().Type == TokenType.Long ||
                    base.TokenStream.Peek().Type == TokenType.Ulong ||
                    base.TokenStream.Peek().Type == TokenType.Char ||
                    base.TokenStream.Peek().Type == TokenType.Bool ||
                    base.TokenStream.Peek().Type == TokenType.Decimal ||
                    base.TokenStream.Peek().Type == TokenType.Float ||
                    base.TokenStream.Peek().Type == TokenType.Double ||
                    base.TokenStream.Peek().Type == TokenType.Identifier)
                {
                    expectsDot = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Dot)
                {
                    expectsDot = false;
                }

                if (base.TokenStream.Peek().Type == TokenType.MachineDecl)
                {
                    // TODOswap needed?    base.TokenStream.Swap(base.TokenStream.Peek().TextUnit.WithText("MachineId"), TokenType.MachineDecl);
                }

                var peekTextUnit = base.TokenStream.Peek().TextUnit;
                var initialText = (textUnit == null) ? string.Empty : textUnit.Text;
                textUnit = (textUnit == null)
                    ? new TextUnit(peekTextUnit.Text, line, peekTextUnit.Start)
                    : textUnit + peekTextUnit;

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Done && base.TokenStream.PrevNonWhitespaceType() != TokenType.Identifier)
            {
                throw new ParsingException("Expected identifier.", base.TokenStream.Peek(),
                    TokenType.Identifier);
            }

            if (base.TokenStream.Peek().Type == TokenType.LeftAngleBracket)
            {
                textUnit += base.TokenStream.Peek().TextUnit;

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                int counter = 1;
                while (!base.TokenStream.Done)
                {
                    if (base.TokenStream.Peek().Type == TokenType.LeftAngleBracket)
                    {
                        counter++;
                    }
                    else if (base.TokenStream.Peek().Type == TokenType.RightAngleBracket)
                    {
                        counter--;
                    }

                    if (counter == 0 ||
                        (base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                        base.TokenStream.Peek().Type != TokenType.Void &&
                        base.TokenStream.Peek().Type != TokenType.Object &&
                        base.TokenStream.Peek().Type != TokenType.String &&
                        base.TokenStream.Peek().Type != TokenType.Sbyte &&
                        base.TokenStream.Peek().Type != TokenType.Byte &&
                        base.TokenStream.Peek().Type != TokenType.Short &&
                        base.TokenStream.Peek().Type != TokenType.Ushort &&
                        base.TokenStream.Peek().Type != TokenType.Int &&
                        base.TokenStream.Peek().Type != TokenType.Uint &&
                        base.TokenStream.Peek().Type != TokenType.Long &&
                        base.TokenStream.Peek().Type != TokenType.Ulong &&
                        base.TokenStream.Peek().Type != TokenType.Char &&
                        base.TokenStream.Peek().Type != TokenType.Bool &&
                        base.TokenStream.Peek().Type != TokenType.Decimal &&
                        base.TokenStream.Peek().Type != TokenType.Float &&
                        base.TokenStream.Peek().Type != TokenType.Double &&
                        base.TokenStream.Peek().Type != TokenType.Identifier &&
                        base.TokenStream.Peek().Type != TokenType.Dot &&
                        base.TokenStream.Peek().Type != TokenType.Comma &&
                        base.TokenStream.Peek().Type != TokenType.LeftSquareBracket &&
                        base.TokenStream.Peek().Type != TokenType.RightSquareBracket &&
                        base.TokenStream.Peek().Type != TokenType.LeftAngleBracket &&
                        base.TokenStream.Peek().Type != TokenType.RightAngleBracket))
                    {
                        break;
                    }

                    if (base.TokenStream.Peek().Type == TokenType.MachineDecl)
                    {
                        // TODOswap needed?    base.TokenStream.Swap(base.TokenStream.Peek().TextUnit.WithText("MachineId"), TokenType.MachineDecl);
                    }

                    textUnit += base.TokenStream.Peek().TextUnit;

                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                }

                if (base.TokenStream.Done ||
                    base.TokenStream.Peek().Type != TokenType.RightAngleBracket)
                {
                    throw new ParsingException("Expected \">\".", base.TokenStream.Peek(),
                        TokenType.RightAngleBracket);
                }

                textUnit += base.TokenStream.Peek().TextUnit;

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Peek().Type == TokenType.LeftSquareBracket)
            {
                textUnit += base.TokenStream.Peek().TextUnit;

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                    base.TokenStream.Peek().Type != TokenType.RightSquareBracket)
                {
                    throw new ParsingException("Expected \"]\".", base.TokenStream.Peek(),
                        TokenType.RightSquareBracket);
                }

                textUnit += base.TokenStream.Peek().TextUnit;

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }
        }
    }
}
