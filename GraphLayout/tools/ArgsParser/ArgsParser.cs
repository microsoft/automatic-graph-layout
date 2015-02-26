/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;

namespace ArgsParser {
    public class ArgsParser {
        Dictionary<string,string> allowedOptions = new Dictionary<string,string>();

        Dictionary<string,string> allowedOptionWithAfterString = new Dictionary<string,string>();

        Set<string> usedOptions = new Set<string>();

        Dictionary<string, string> usedOptionsWithAfterString = new Dictionary<string, string>();

        public List<string> FreeArgs=new List<string>();

        public ArgsParser(string[] args) {
            this.args = args;
        }

        
        public void AddAllowedOption(string s) {
            AddAllowedOptionWithHelpString(s,"");           
        }

        public void AddAllowedOptionWithHelpString(string s, string helpString) {
            allowedOptions[s]=helpString;
        }

        public void AddOptionWithAfterString(string s) {
            AddOptionWithAfterStringWithHelp(s, "");
        }

        public void AddOptionWithAfterStringWithHelp(string s, string helpString) {
            allowedOptionWithAfterString[s]=helpString;
        }


        public bool Parse() {
            bool statusIsOk = true;
            for (int i = 0; i < args.Length; i++) {
                string ar = args[i];
                if (allowedOptions.ContainsKey(ar))
                    usedOptions.Insert(ar);
                else if (allowedOptionWithAfterString.ContainsKey(ar)) {
                    if (i == args.Length - 1) {
                        ErrorMessage = "Argument is missing after "+ar;
                        return false;
                    }
                    i++;
                    usedOptionsWithAfterString[ar] = args[i];
                } else {
                    if (ar.StartsWith("-") || ar.StartsWith("//")) {
                        ErrorMessage = "Unknown option " + ar;
                        statusIsOk = false;
                    }

                    FreeArgs.Add(ar);
                }
            }
            return statusIsOk;
        }
        
        string[] args;
        public string ErrorMessage;

        public bool OptionIsUsed(string option) {
            return usedOptions.Contains(option) || usedOptionsWithAfterString.ContainsKey(option);
        }

        public string GetValueOfOptionWithAfterString(string option) {
            string val;
            return usedOptionsWithAfterString.TryGetValue(option, out val) ? val : null;
        }

        public string UsageString() {
            string ret = "";
            var unknownOptions = FreeArgs.Where(s => s.StartsWith("-") || s.StartsWith("\\"));
            if (unknownOptions.Any())
                ret = "Unknown options:";

            foreach (var unknownOption in unknownOptions) {
                ret += unknownOption;
                ret += ",";
            }
            ret += "\n";
            ret += "Usage:\n";
            foreach (var allowedOption in allowedOptions)
                ret += allowedOption.Key + " " + (string.IsNullOrEmpty(allowedOption.Value) ? "" : "/" + allowedOption.Value) + "\n";
            foreach (var s in allowedOptionWithAfterString) {
                ret += s.Key +" "+( String.IsNullOrEmpty(s.Value)? " \"option value\"":("\""+ s.Value+"\"")) + "\n";
            }
            return ret;
        }
    }
}