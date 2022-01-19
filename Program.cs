using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.IO;
using System.Windows.Forms;

namespace PrivacyRootPatcher
{
    /*class ArrowSelectControl
    {
        public class ArrowSelectItem
        {
            public ArrowSelectItem()
            {
                ForeColor = SelectedForeColor = Console.ForegroundColor;
                BackColor = SelectedBackColor = Console.BackgroundColor;
            }

            public string Text {get;set;}
            public string Subtext { get; set; }

            public ConsoleColor ForeColor {get;set;}
            public ConsoleColor BackColor {get;set;}

            public ConsoleColor SelectedForeColor { get; set; }
            public ConsoleColor SelectedBackColor { get; set; }
        }

        public ArrowSelectControl()
        {
            Items = new List<ArrowSelectItem>();
            SelectionCharacter = '>';
        }

        public List<ArrowSelectItem> Items { get; set; }

        public int CursorLeftPadding { get; set; }
        public char SelectionCharacter { get; set; }

        public int Draw(int x, int y, int startSelection = 0)
        {
            Console.SetCursorPosition(x, y);
            int selection = startSelection;
            while (true)
            {
                for (int x_ = 0; x_ < Items.Count; x++)
                {
                    string drawText = "";
                    ArrowSelectItem item = Items[x_];
                    if (selection == x_)
                    {
                        Program.ChangeColor(item.SelectedForeColor, item.SelectedBackColor);
                        drawText += SelectionCharacter + " ";
                    }
                    else
                    {
                        Program.ChangeColor(item.ForeColor, item.BackColor);
                        drawText += "  ";
                    }
                    Console.WriteLine(drawText + item.Text);
                    ConsoleKeyInfo keyInfo = Console.ReadKey();
                    switch (keyInfo.Key)
                    {
                        case ConsoleKey.DownArrow: selection = selection + 1 > Items.Count ? Items.Count : selection++; break;
                        case ConsoleKey.UpArrow: selection = selection - 1 < 0 ? 0 : selection--; break;
                        case ConsoleKey.RightArrow:
                        case ConsoleKey.Enter:
                            return selection;
                    }
                }
            }
        }
    }*/

    class Program
    {
        static readonly string uactivatedFieldName = "UActivated";

        class AppIdentity
        {
            public string Version { get; set; }
            public TargetType Type { get; set; }
        }

        static List<ModuleDefMD> asms = new List<ModuleDefMD>();
        static List<string> log = new List<string>();
        static List<AppIdentity> appIds = new List<AppIdentity>();
        static List<string> paths = new List<string>();

        static void Main(string[] args)
        {
            Console.CursorVisible = false;
            Console.Title = "PrivacyRootPatcher v1.0 - RaidForums - bfalk";
            foreach (string arg in args)
            {
                try
                {
                    if (File.Exists(arg))
                    {
                        ModuleDefMD temp = ModuleDefMD.Load(arg);
                        asms.Add(temp);
                        paths.Add(arg);
                    }
                }
                catch { }
            }
            //asm = ModuleDefMD.Load(args[0]);
            ChangeForeColor(ConsoleColor.White);
            Console.Write("PrivacyRootPatcher v1.0 - ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("https://github.com/git-bfalk/PRP");
            ChangeForeColor(ConsoleColor.White);
            Console.Write(" - ");
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine("RaidForums");
            Console.WriteLine();
            ChangeForeColor(ConsoleColor.White);
            Console.WriteLine("Detected:");
            foreach (ModuleDefMD asm in asms)
            {
                Console.Write(" - ");
                AppIdentity appId = Identify(asm);
                ChangeColorFromType(appId.Type);
                Console.Write("'" + TargetTypeToString(appId.Type) + "'");
                ChangeForeColor(ConsoleColor.White);
                Console.Write(" (build ");
                ChangeForeColor(ConsoleColor.Magenta);
                Console.Write(appId.Version);
                ChangeForeColor(ConsoleColor.White);
                Console.WriteLine(")");
                Console.ResetColor();
                appIds.Add(appId);
            }
            /*ArrowSelectControl asc = new ArrowSelectControl() { Items = new List<ArrowSelectControl.ArrowSelectItem>() {
                new ArrowSelectControl.ArrowSelectItem() { Text = "Forced Activation", Subtext = "(Most Stable) - Hardcodes 'UActivated' to true" },
                //new ArrowSelectControl.ArrowSelectItem() { Text = "" }
            } };
            asc.Draw(2,5);*/
            for (int x = 0; x < asms.Count; x++)
            {
                BasicPatchForceUActivate(asms[x], appIds[x]);
                Console.WriteLine();
                AddToLog(" Saving Patch...", true, ConsoleColor.DarkGreen);
                asms[x].Write(paths[x].Replace(".exe", null) + "_patched.exe");
                AddToLog(" Saved Patch as '" + Path.GetFileName(paths[x]).Replace(".exe", null) + "_patched.exe'!", true, ConsoleColor.Green);
            }
            Console.WriteLine();
            Console.WriteLine("-------------------------");
            AddToLog("   " + asms.Count + " file(s) patched!", true, ConsoleColor.Green);
            Console.WriteLine();
            Console.WriteLine(" Press any key to exit...");
            Console.ReadKey();
        }

        static void ChangeColorFromType(TargetType type)
        {
            switch (type)
            {
                case TargetType.SecureDelete: ChangeForeColor(ConsoleColor.Red); break;
                case TargetType.SecretDisk: ChangeForeColor(ConsoleColor.Cyan); break;
                case TargetType.PreventRestore:
                case TargetType.DuplicateFileFinder: ChangeForeColor(ConsoleColor.DarkYellow); break;
                case TargetType.Wipe: ChangeForeColor(ConsoleColor.DarkRed); break;
            }
        }

        static void BasicPatchForceUActivate(ModuleDefMD asm, AppIdentity appId)
        {
            Console.WriteLine();
            AddToLog("############## - Patching ", false, ConsoleColor.White);
            ChangeColorFromType(appId.Type);
            Console.Write("'" + TargetTypeToString(appId.Type) + "'");
            AddToLog("... - ##############", true, ConsoleColor.White);
            Console.WriteLine();
            AddToLog(" Searching for '" + uactivatedFieldName + "'...", true, ConsoleColor.Yellow);
            FieldDef uactivate = GetField(uactivatedFieldName, GetType(asm, TargetTypeToString(appId.Type), "UF"));
            if (uactivate == null)
            {
                uactivate = GetField(asm, uactivatedFieldName);
                if (uactivate == null) {
                    AddToLog(" Could not find '" + uactivatedFieldName + "'!", true, ConsoleColor.Red);
                    Console.WriteLine();
                    Console.WriteLine(" ### END ###");
                    Console.WriteLine();
                    throw new Exception("Could not find '" + uactivatedFieldName + "'!");
                }
            }
            AddToLog(" '" + uactivatedFieldName + "' found!", true, ConsoleColor.Green);
            Console.WriteLine();
            foreach (TypeDef type in asm.Types)
            {
                for (int x = 0; x < type.Methods.Count; x++)
                {
                    MethodDef method = type.Methods[x];
                    if (!method.HasBody) { continue; }
                    method.Body.KeepOldMaxStack = true;
                    for (int i = 0; i < method.Body.Instructions.Count; i++)
                    {
                        Instruction inst = method.Body.Instructions[i];
                        if (inst.OpCode.Equals(OpCodes.Stsfld) &&
                            method.Body.Instructions[i - 1].OpCode.Equals(OpCodes.Ldc_I4_0))
                        {
                            Console.Write(" ");
                            AddToLog("Patched ", false, ConsoleColor.White);
                            AddToLog("'IL_" + method.Body.Instructions[i - 1].Offset.ToString("X4") + "'", false, ConsoleColor.Cyan);
                            AddToLog(" at ", false, ConsoleColor.White);
                            AddToLog("'" + GenerateTypePath(type) + "'", false, ConsoleColor.DarkCyan);
                            AddToLog("!", true, ConsoleColor.White);
                            method.Body.Instructions.Insert(i - 1, new Instruction(OpCodes.Ldc_I4_1));
                            method.Body.Instructions.RemoveAt(i);
                        }
                    }
                }
            }
        }

        static string GenerateTypePath(TypeDef type)
        {
            return type.Namespace + "." + type.Name;
        }

        static void ChangeForeColor(ConsoleColor ForeColor) { ChangeColor(ForeColor, Console.BackgroundColor); }
        static void ChangeBackColor(ConsoleColor BackColor) { ChangeColor(Console.ForegroundColor, BackColor); }
        public static void ChangeColor(ConsoleColor ForeColor, ConsoleColor BackColor) { Console.ForegroundColor = ForeColor; Console.BackgroundColor = BackColor; }

        static void AddToLog(string text, bool endLine = true, ConsoleColor ForeColor = ConsoleColor.Gray, ConsoleColor BackColor = ConsoleColor.Black)
        {
            ChangeColor(ForeColor, BackColor);
            if (endLine) { Console.WriteLine(text); }
            else { Console.Write(text); }
            Console.ResetColor();
        }

        static TypeDef GetType(ModuleDefMD asm, string name)
        {
            foreach (TypeDef type in asm.Types) { if (type.Name == name) { return type; } }
            return null;
        }
        static TypeDef GetType(ModuleDefMD asm, string nameSpace, string name)
        {
            foreach (TypeDef type in asm.Types) { if (type.Name == name && type.Namespace == nameSpace) { return type; } }
            return null;
        }

        static FieldDef GetField(string name, TypeDef type)
        {
            if (!type.HasFields) { return null; }
            foreach (FieldDef field in type.Fields) { if (field.Name == name) { return field; } }
            return null;
        }
        static FieldDef GetField(ModuleDefMD asm, string name)
        {
            FieldDef result = null;
            foreach (TypeDef type in asm.Types) { result = GetField(name, type); if (result == null) { break; } }
            return result;
        }

        static AppIdentity Identify(ModuleDefMD asm)
        {
            AppIdentity id = new AppIdentity();
            foreach (CustomAttribute ca in asm.Assembly.CustomAttributes)
            {
                foreach (CAArgument caa in ca.ConstructorArguments)
                {
                    switch (ca.Constructor.DeclaringType.FullName.Replace("System.Reflection.", null))
                    {
                        case "AssemblyFileVersionAttribute": id.Version = caa.Value.ToString(); break;
                        case "AssemblyTitleAttribute":
                            switch (caa.Value.ToString())
                            {
                                case "Securely erase files or folders": case "Secure Delete": id.Type = TargetType.SecureDelete; break;
                                case "Duplicate File Finder": id.Type = TargetType.DuplicateFileFinder; break;
                                case "Secret Disk": id.Type = TargetType.SecretDisk; break;
                                case "Prevent Restore": id.Type = TargetType.PreventRestore; break;
                                case "Wipe": id.Type = TargetType.Wipe; break;
                            }
                            break;
                        case "AssemblyDescriptionAttribute":
                            switch (caa.Value.ToString())
                            {
                                case "Creates virtual disk": id.Type = TargetType.SecretDisk; break;
                                case "Find duplicate files to avoid mess": id.Type = TargetType.DuplicateFileFinder; break;
                                case "Deletes selected files securely without chance for recovery": id.Type = TargetType.SecureDelete; break;
                                case "Prevents recovery of already deleted files": id.Type = TargetType.PreventRestore; break;
                                case "Deletes personal traces and garbage": id.Type = TargetType.Wipe; break;
                            }
                            break;
                    }
                    try
                    {
                        
                    }
                    catch { }
                }
            }
            return id;
        }

        static string TargetTypeToString(TargetType target)
        {
            switch (target)
            {
                case TargetType.DuplicateFileFinder: return "DuplicateFileFinder";
                case TargetType.SecureDelete: return "SecureDelete";
                case TargetType.SecretDisk: return "SecretDisk";
                case TargetType.Wipe: return "Wipe";
                case TargetType.PreventRestore: return "PreventRestore";
                default: return "Unknown";
            }
        }

        public enum TargetType
        {
            DuplicateFileFinder,
            Wipe,
            SecureDelete,
            SecretDisk,
            PreventRestore
        }
    }
}
