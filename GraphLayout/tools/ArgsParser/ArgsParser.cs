
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
        public bool GetDoubleOptionValue(string s, out double v) {
            string svalue = GetStringOptionValue(s);
            if (svalue == null) { v = 0; return false; }
            bool ret = double.TryParse(svalue, out v);
            if (!ret) {
                System.Diagnostics.Debug.WriteLine("for option '{0}' cannot parse value of '{1}'", s, GetStringOptionValue(s));
                return false;
            }
            return true;
        }

        public bool GetIntOptionValue(string s, out int v)
        {
            string svalue = GetStringOptionValue(s);
            if (svalue == null) { v = 0; return false; }
            bool ret = int.TryParse(svalue, out v);
            if (!ret)
            {
                System.Diagnostics.Debug.WriteLine("for option '{0}' cannot parse value of '{1}'", s, GetStringOptionValue(s));
                return false;
            }
            return true;
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

        public string GetStringOptionValue(string option) {
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
                ret += allowedOption.Key + " " + (string.IsNullOrEmpty(allowedOption.Value) ? "" : ":" + allowedOption.Value) + "\n";
            foreach (var s in allowedOptionWithAfterString) {
                ret += s.Key +" "+( String.IsNullOrEmpty(s.Value)? " \"option value\"":("\""+ s.Value+"\"")) + "\n";
            }
            return ret;
        }
    }
}