﻿//-----------------------------------------------------------------------
// <copyright file="StatementBlockNode.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
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
using System.Linq;

namespace Microsoft.PSharp.Parsing.Syntax
{
    /// <summary>
    /// Statement block node.
    /// </summary>
    internal sealed class StatementBlockNode : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The machine parent node.
        /// </summary>
        internal readonly MachineDeclarationNode Machine;

        /// <summary>
        /// The state parent node.
        /// </summary>
        internal readonly StateDeclarationNode State;

        /// <summary>
        /// The left curly bracket token.
        /// </summary>
        internal Token LeftCurlyBracketToken;

        /// <summary>
        /// List of statement nodes.
        /// </summary>
        internal List<StatementNode> Statements;

        /// <summary>
        /// The right curly bracket token.
        /// </summary>
        internal Token RightCurlyBracketToken;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="machineNode">MachineDeclarationNode</param>
        /// <param name="stateNode">StateDeclarationNode</param>
        internal StatementBlockNode(MachineDeclarationNode machineNode, StateDeclarationNode stateNode)
        {
            this.Machine = machineNode;
            this.State = stateNode;
            this.Statements = new List<StatementNode>();
        }

        /// <summary>
        /// Returns the rewritten text.
        /// </summary>
        /// <returns>string</returns>
        internal override string GetRewrittenText()
        {
            return base.TextUnit.Text;
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="program">Program</param>
        internal override void Rewrite(IPSharpProgram program)
        {
            foreach (var stmt in this.Statements)
            {
                stmt.Rewrite(program);
            }

            var text = "\n";

            if (this.LeftCurlyBracketToken != null &&
                this.RightCurlyBracketToken != null)
            {
                text += this.LeftCurlyBracketToken.TextUnit.Text + "\n";
            }

            foreach (var stmt in this.Statements)
            {
                text += stmt.GetRewrittenText();
            }

            if (this.LeftCurlyBracketToken != null &&
                this.RightCurlyBracketToken != null)
            {
                text += this.RightCurlyBracketToken.TextUnit.Text + "\n";
            }

            if (this.LeftCurlyBracketToken != null &&
                this.RightCurlyBracketToken != null)
            {
                base.TextUnit = new TextUnit(text, this.LeftCurlyBracketToken.TextUnit.Line);
            }
            else
            {
                base.TextUnit = new TextUnit(text, this.Statements.First().TextUnit.Line);
            }
        }

        #endregion
    }
}