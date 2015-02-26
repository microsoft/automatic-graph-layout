set ycmd="gp\gppg.exe"
set lcmd="gp\gplex.exe"

%ycmd% /no-lines /gplex Dot.y  > Dot.cs
%lcmd% Dot.lex 


set lang="gp\QUT.ShiftReduceParser.dll"

csc /r:%lang% /r:bin\Debug\Microsoft.Msagl.dll /target:library /r:bin\Debug\Microsoft.Msagl.Drawing.dll /debug+ AttributeValuePair.cs dot.cs DotLex.cs 