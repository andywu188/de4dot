using de4dot.blocks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace de4dot.code.deobfuscators.PCL {
	class Fucker {
		public ModuleDefMD module;
		public Fucker(ModuleDefMD module) {
			this.module = module;
		}

		public void startFuck() {
			foreach (var type in module.GetTypes()) {
				FindEveryTypes(type);
			}
		}

		void FindEveryTypes(TypeDef type) {
			var foundResult_1 = DotNetUtils.FindMethods(type.Methods, "System.Void", new string[] { "System.Object", "System.Windows.StartupEventArgs" }, false);
			var foundResult_2 = DotNetUtils.FindMethods(type.Methods, "System.String", new string[] { "System.String", "System.String", "System.Byte[]", "System.String", "System.Int32", "System.Collections.Generic.Dictionary`2<System.String,System.String>" }, true);
			var foundResult_3 = DotNetUtils.FindMethods(type.Methods, "System.Boolean", new string[] { "System.Int32", "System.Boolean", "System.String" }, true);
			var foundResult_4 = DotNetUtils.FindMethods(type.Methods, "System.Boolean", new string[] { "System.String" }, true);
			var foundResult_5 = DotNetUtils.FindMethods(type.Methods, "System.Boolean", new string[] { "System.String", "System.String" }, true);
			var foundResult_6 = DotNetUtils.FindMethods(type.Methods, "System.Boolean", new string[] { "System.Int32" }, true);
			var foundResult_7 = DotNetUtils.FindMethods(type.Methods, "System.Void", new string[] { }, false);
			var foundResult_8 = DotNetUtils.FindMethods(type.Methods, "System.Void", new string[] { }, false);
			var foundResult_9 = DotNetUtils.FindMethods(type.Methods, "System.Void", new string[] { "System.String", "System.Boolean", "System.String", "System.Boolean" }, true);

			//Patch a simple check in ApplicationStart
			foreach (var method in foundResult_1) {
				if (!method.HasBody)
					continue;
				var instructions = method.Body.Instructions;
				for (int i = 0; i < instructions.Count; i++) {
					if (i != 0 && i < 40 && instructions[i].OpCode == OpCodes.Call) {
						if (instructions[i - 1].OpCode == OpCodes.Ldc_I4_0) {
							Logger.n("[1]found progSize/emptyMark check {0} [{1}]", method.Name, method.MDToken.ToString());
							instructions[i - 1] = new Instruction(OpCodes.Nop);
							instructions[i] = new Instruction(OpCodes.Nop);
						}
					}
				}
			}

			//Fix de4dot Deobfucate bug in JIT
			foreach (var method in foundResult_2) {
				if (!method.HasBody)
					continue;
				if (method.MethodSig.Params.Count == 6) {
					Logger.n("[2]found deobf bug, fixing..... {0} [{1}]", method.Name, method.MDToken.ToString());
					method.MethodSig.Params[2] = method.MethodSig.Params[0];
				}
			}

			//Patch ThemeUnlock
			foreach (var method in foundResult_3) {
				if (!method.HasBody)
					continue;
				var instructions = method.Body.Instructions;
				if (instructions.Count < 100 && instructions.Count > 50) {
					Logger.n("[3]found ThemeUnlock(), cracking..... {0} [{1}]", method.Name, method.MDToken.ToString());
					instructions.Clear();
					instructions.Add(new Instruction(OpCodes.Ldc_I4_1));
					instructions.Add(new Instruction(OpCodes.Ret));
				}
			}

			//Patch GoldTheme(verbose, because it checks in the removingRSA)
			foreach (var method in foundResult_4) {
				if (!method.HasBody)
					continue;
				var instructions = method.Body.Instructions;
				if (instructions.Count < 40 && instructions.Count > 10) {
					Logger.n("[4]found ThemeCheckGold(), cracking..... {0} [{1}]", method.Name, method.MDToken.ToString());
					instructions.Clear();
					instructions.Add(new Instruction(OpCodes.Ldc_I4_1));
					instructions.Add(new Instruction(OpCodes.Ret));
				}
			}

			//Patch RemovingRSA
			foreach (var method in foundResult_5) {
				if (!method.HasBody)
					continue;
				var instructions = method.Body.Instructions;
				if (instructions.Count > 10 && instructions.Count < 50 && method.Body.ExceptionHandlers.Count == 1) {
					Logger.n("[5]found SecureRemoveRsa(), cracking..... {0} [{1}]", method.Name, method.MDToken.ToString());
					instructions.Clear();
					method.Body.ExceptionHandlers.Clear();
					instructions.Add(new Instruction(OpCodes.Ldc_I4_1));
					instructions.Add(new Instruction(OpCodes.Ret));
				}
			}

			//Patch CheckOneTheme
			foreach (var method in foundResult_6) {
				if (!method.HasBody)
					continue;
				var instructions = method.Body.Instructions;
				if (instructions.Count > 50 && method.Body.ExceptionHandlers.Count == 0) {
					if (instructions[0].OpCode == OpCodes.Ldarg_0 && instructions[1].OpCode == OpCodes.Ldc_I4_8) {
						Logger.n("[6]found ThemeCheckOne(), cracking..... {0} [{1}]", method.Name, method.MDToken.ToString());
						instructions.Clear();
						instructions.Add(new Instruction(OpCodes.Ldc_I4_1));
						instructions.Add(new Instruction(OpCodes.Ret));
					}
				}
			}

			//Patch All Themes
			foreach (var method in foundResult_7) {
				if (!method.HasBody)
					continue;
				var instructions = method.Body.Instructions;
				if (instructions.Count > 300 && method.Body.ExceptionHandlers.Count == 2) {
					Logger.n("[7]found ThemeCheckAll_delegate(), cracking..... {0} [{1}]", method.Name, method.MDToken.ToString());
					for (int i = 0; i < instructions.Count; i++) {
						if (instructions[i].OpCode == OpCodes.Ldstr && i < 20 && i != 0) {
							if (((string)instructions[i].Operand).Equals("UiLauncherThemeHide2")) {
								/*
								0   0000    nop
								1   0001    ldsfld  class PCL.ModSetup PCL.ModBase::Setup
								2	0006	ldstr   "UiLauncherThemeHide2"
								3	000B    ldnull
								4	000C    callvirt    instance object PCL.ModSetup::Get(string, class PCL.ModMinecraft/McVersion)
								5	0011	call string[Microsoft.VisualBasic] Microsoft.VisualBasic.CompilerServices.Conversions::ToString(object)
								6	0016	stloc.0
								*/
								instructions[i - 1] = new Instruction(OpCodes.Nop);
								instructions[i].Operand = "1|2|3|4|5|6|7|8|9|10|11|12|13|14|15|16|17|18|19|20|21|22|23";
								instructions[i + 1] = new Instruction(OpCodes.Nop);
								instructions[i + 2] = new Instruction(OpCodes.Nop);
								instructions[i + 3] = new Instruction(OpCodes.Nop);
								method.Body.ExceptionHandlers[1].TryStart = instructions[i];
							}
						}
					}
				}
			}

			//Patch SystemCount (the messagebox on first open and 99 clicks tip)
			foreach (var method in foundResult_8) {
				if (!method.HasBody)
					continue;
				var instructions = method.Body.Instructions;
				if (instructions.Count < 70 && instructions.Count > 50 && method.Body.ExceptionHandlers.Count == 0) {
					for (int i = 0; i < instructions.Count; i++) {
						if (instructions[i].OpCode == OpCodes.Ldstr) {
							if (((string)instructions[i].Operand).Equals("SystemCount") && i < 10) {
								Logger.n("[8]found RunCount(), removing..... {0} [{1}]", method.Name, method.MDToken.ToString());
								instructions.Clear();
								method.Body.ExceptionHandlers.Clear();
								instructions.Add(new Instruction(OpCodes.Ret));
								break;
							}
						}
					}

				}
			}

			//Patch Upgrade Process.
			foreach (var method in foundResult_9) {
				if (!method.HasBody)
					continue;
				var instructions = method.Body.Instructions;
				if (instructions.Count > 150 && method.Body.ExceptionHandlers.Count == 2) {
					for (int i = 0; i < instructions.Count; i++) {
						if (instructions[i].OpCode == OpCodes.Ldstr) {
							if (((string)instructions[i].Operand).Equals("输入的更新密钥验证失败。") && i < 100) {
								Logger.n("[9]found update(), cracking..... {0} [{1}]", method.Name, method.MDToken.ToString());
								instructions[i + 1] = new Instruction(OpCodes.Pop);
								instructions[i + 2] = new Instruction(OpCodes.Nop);
								break;
							}
						}
					}
				}
			}

		}
	}
}
