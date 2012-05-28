﻿using System;
using Elide.Scintilla;

namespace Elide.ElaCode
{
    internal sealed class FoldingManager
    {
        private readonly ScintillaControl sci;

        internal FoldingManager(ScintillaControl sci)
        {
            this.sci = sci;
        }

        internal void Fold(FoldNeededEventArgs e)
        {
            var lineCount = sci.LineCount;
            var firstLine = e.StartLine;
            var lastLine = e.EndLine;
            e.AddFoldRegion(0, firstLine, lastLine);
            
            var lastStart = -1;

            for (int line = firstLine; line < lastLine + 2; line++)
            {
                var li = sci.GetLineIndentation(line);
                var posLine = sci.GetPositionFromLine(line);
                var colEnd = sci.GetLineEndColumn(line);
                var cmt = sci.GetStyleAt(posLine) == TextStyle.MultilineStyle1;
                var hasLet = sci.CharAt(posLine) == 'l' && sci.CharAt(posLine + 1) == 'e' && sci.CharAt(posLine + 2) == 't' && !cmt;

                if ((li == 0 || colEnd == li) && lastStart > -1 && line - lastStart > 1 && !cmt)
                {
                    e.AddFoldRegion(1, lastStart, line);
                    lastStart = -1;
                }
                else if (li == 0 || colEnd == li)
                    lastStart = -1;
                
                if (hasLet)
                    lastStart = line;

                if (line == lastLine + 1 && lastStart != -1 && lastLine < lineCount)
                    lastLine++;
            }

            if (lastStart > -1 && lastLine == lineCount - 1)
                e.AddFoldRegion(1, lastStart, lineCount - 1);
        }
    }
}