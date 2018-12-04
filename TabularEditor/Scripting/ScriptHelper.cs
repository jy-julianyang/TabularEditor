﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TabularEditor.TextServices;
using TabularEditor.TOMWrapper;
using TabularEditor.UI;
using TabularEditor.UI.Actions;
using TabularEditor.UI.Dialogs;
using TabularEditor.UIServices;

namespace TabularEditor.Scripting
{
    public static class ScriptHelper
    {
        [ScriptMethod]
        public static void CheckModelMetadata(ProviderDataSource source)
        {
            var changes = TableMetadata.GetChanges(source);

            if(Program.CommandLineMode)
            {
                foreach(var change in changes)
                {
                    var msg = change.ToString();
                    if (change.ChangeType == MetadataChangeType.SourceColumnNotFound || change.ChangeType == MetadataChangeType.SourceQueryError) Error(msg);
                    else Warning(msg);
                }
            }
            else
            {
                if(changes.Count == 0)
                {
                    MessageBox.Show("No changes detected.", "Refresh Table Metadata", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else 
                    SchemaDiffDialog.Show(changes);
            }
        }

        [ScriptMethod]
        public static string ConvertDax(string dax, bool useSemicolons = true)
        {
            return useSemicolons ? ExpressionParser.CommasToSemicolons(dax) : ExpressionParser.SemicolonsToCommas(dax);
        }

        [ScriptMethod]
        public static string FormatDax(string dax)
        {
            var textToFormat = "x :=" + dax;
            try
            {
                var result = TabularEditor.Dax.DaxFormatterProxy.FormatDax(textToFormat, false).FormattedDax;
                if (string.IsNullOrWhiteSpace(result))
                {
                    return dax;
                }
                return result.Substring(6).Trim();
            }
            catch
            {
                return dax;
            }
        }

        [ScriptMethod]
        public static string ReadFile(string filePath)
        {
            using (var fileStream = new System.IO.FileStream(filePath,
                System.IO.FileMode.Open,
                System.IO.FileAccess.Read,
                System.IO.FileShare.ReadWrite))
            using (var textReader = new System.IO.StreamReader(fileStream))
            {
                return textReader.ReadToEnd();
            }
        }

        [ScriptMethod]
        public static void SaveFile(string filePath, string content)
        {
            System.IO.File.WriteAllText(filePath, content);
        }

        [ScriptMethod]
        public static void Output(this object value, int lineNumber = -1)
        {
            if (Program.CommandLineMode)
            {
                Info(string.Format(lineNumber != -1 ? "Script output line #{0}: {1}" : "Script output: {1}", lineNumber, value));
                return;
            }

            if (ScriptOutputForm.DontShow) return;

            var caption = string.Format("Script output{0}", lineNumber > 0 ? " at line " + lineNumber : "");

            var isHourglass = Hourglass.Enabled;
            if (isHourglass) Hourglass.Enabled = false;
            ScriptOutputForm.ShowObject(value, caption);
            if (isHourglass) Hourglass.Enabled = true;
        }

        [ScriptMethod]
        public static void Info(string message, int lineNumber = -1)
        {
            var header = string.Format("Script info{0}", lineNumber >= 0 ? " (line " + lineNumber + ")": "", message);

            if (Program.CommandLineMode) Program.cw.WriteLine(message);
            else MessageBox.Show(message, header, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        [ScriptMethod]
        public static void Warning(string message, int lineNumber = -1)
        {
            var header = string.Format("Script warning{0}", lineNumber >= 0 ? " (line " + lineNumber + ")" : "", message);

            if (Program.CommandLineMode) Program.Warning(header + ": " + message);
            else MessageBox.Show(message, header, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        [ScriptMethod]
        public static void Error(string message, int lineNumber = -1)
        {
            var header = string.Format("Script error{0}", lineNumber >= 0 ? " (line " + lineNumber + ")" : "", message);

            if (Program.CommandLineMode) Program.Error(header + ": " + message);
            else MessageBox.Show(message, header, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void OutputErrors(IEnumerable<TabularNamedObject> items)
        {
            if (ScriptOutputForm.DontShow) return;

            var caption = string.Format("Objects with errors ({0})", items.Count());
            ScriptOutputForm.ShowObject(items, caption, true);
        }

        [ScriptMethod, IntelliSense("Invoke the custom action with the given name.")]
        public static void CustomAction(string actionName)
        {
            var act = UI.UIController.Current.Actions.OfType<CustomAction>().FirstOrDefault(a => a.BaseName == actionName);
            if (act != null)
            {
                act.ExecuteInScript(null);
            }
            else throw new InvalidOperationException(string.Format("There is no Custom Action with the name '{0}'.", actionName));
        }

        [ScriptMethod, IntelliSense("Invoke the custom action on the given set of objects with the given name.")]
        public static void CustomAction(this IEnumerable<ITabularNamedObject> selection, string actionName)
        {
            var act = UI.UIController.Current.Actions.OfType<CustomAction>().FirstOrDefault(a => a.BaseName == actionName);
            if (act != null)
            {
                act.ExecuteWithSelection(null, selection);
            }
            else throw new InvalidOperationException(string.Format("There is no Custom Action with the name '{0}'.", actionName));
        }

        [ScriptMethod, IntelliSense("Invoke the custom action on the given object with the given name.")]
        public static void CustomAction(this ITabularNamedObject selection, string actionName)
        {
            var act = UI.UIController.Current.Actions.OfType<CustomAction>().FirstOrDefault(a => a.BaseName == actionName);
            if (act != null)
            {
                act.ExecuteWithSelection(null, Enumerable.Repeat(selection, 1));
            }
            else throw new InvalidOperationException(string.Format("There is no Custom Action with the name '{0}'.", actionName));
        }
    }
}
