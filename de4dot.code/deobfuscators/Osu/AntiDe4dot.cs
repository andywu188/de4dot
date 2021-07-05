using de4dot.blocks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace de4dot.code.deobfuscators.Osu {
	public class AntiDe4dot {
		public ModuleDefMD module;
		public AntiDe4dot(ModuleDefMD module) {
			this.module = module;
		}

		public void doantiantide4() {
			foreach (var type in module.GetTypes()) {
				FindEveyTypes(type);
			}
		}

		void FindEveyTypes(TypeDef type) {
			var foundResult_1 = DotNetUtils.FindMethods(type.Methods, "System.Boolean", new string[] { "System.Diagnostics.StackTrace", "System.Int32" }, true);
			var foundResult_2 = DotNetUtils.FindMethods(type.Methods, "System.Void", new string[] { "System.Int64" }, false);
			var foundResult_3 = DotNetUtils.FindMethods(type.Methods, "System.Int64", new string[] { }, true);
			var foundResult_4 = DotNetUtils.FindMethods(type.Methods, "System.Boolean", new string[] { }, true);
			var foundResult_5 = DotNetUtils.FindMethods(type.Methods, "System.Void", new string[] { "System.Byte[]" }, true);
			var foundResult_6 = DotNetUtils.FindMethods(type.Methods, "System.Int64", new string[] { }, false);

			foreach (var method in foundResult_1) {
				if (!method.HasBody)
					continue;
				var instructions = method.Body.Instructions;
				if (instructions.Count < 20)
					continue;
				Logger.n("[1]found internal stack check {0} [{1}]", method.Name, method.MDToken.ToString());
				//clear all instructions.
				instructions.Clear();
				//add true+return.
				instructions.Add(new Instruction(OpCodes.Ldc_I4_1));
				instructions.Add(new Instruction(OpCodes.Ret));
			}

			foreach (var method in foundResult_2) {
				if (!method.HasBody)
					continue;
				var instructions = method.Body.Instructions;
				if (instructions.Count < 100)
					continue;
				if (instructions[0].OpCode != OpCodes.Call && instructions[1].OpCode != OpCodes.Ldtoken && instructions[2].OpCode != OpCodes.Call)
					continue;
				Logger.n("[2]found internal assembly check {0} [{1}]", method.Name, method.MDToken.ToString());
				//remove check.
				for (int i = 0; i < 9; i++) {
					instructions[i] = new Instruction(OpCodes.Nop);
				}
			}

			foreach (var method in foundResult_3) {
				if (!method.HasBody)
					continue;
				var instructions = method.Body.Instructions;
				if (instructions.Count < 100)
					continue;
				if (instructions[0].OpCode != OpCodes.Call || instructions[1].OpCode != OpCodes.Ldtoken || instructions[2].OpCode != OpCodes.Call)
					continue;
				Logger.n("[3]found internal assembly check {0} [{1}]", method.Name, method.MDToken.ToString());
				//if (true == true && Class119.smethod_2())
				//deobfucator will clean the code after doing this.
				instructions[3] = new Instruction(OpCodes.Ldc_I4_1);
				instructions[0] = new Instruction(OpCodes.Nop);
				instructions[1] = new Instruction(OpCodes.Nop);
				instructions[2] = new Instruction(OpCodes.Ldc_I4_1);
			}

			foreach (var method in foundResult_4) {
				if (!method.HasBody)
					continue;
				var instructions = method.Body.Instructions;
				if (instructions.Count > 50 || instructions.Count < 10)
					continue;
				if (instructions[0].OpCode == OpCodes.Newobj && instructions[1].OpCode == OpCodes.Stloc_0) {
					if (((MemberRef)instructions[0].Operand).FullName.Equals("System.Void System.Diagnostics.StackTrace::.ctor()")) {
						Logger.n("[4]found internal stack check {0} [{1}]", method.Name, method.MDToken.ToString());
						//clear all instructions.
						instructions.Clear();
						//add true+return.
						instructions.Add(new Instruction(OpCodes.Ldc_I4_1));
						instructions.Add(new Instruction(OpCodes.Ret));
						continue;
					}
				}
			}

			foreach (var method in foundResult_5) {
				if (!method.HasBody)
					continue;
				var instructions = method.Body.Instructions;
				if (instructions.Count < 50)
					continue;
				if (instructions[0].OpCode == OpCodes.Call && instructions[1].OpCode == OpCodes.Ldtoken && instructions[2].OpCode == OpCodes.Call) {
					Logger.n("[5]found internal assembly check {0} [{1}]", method.Name, method.MDToken.ToString());
					//if (true == true && Class119.smethod_2())
					//deobfucator will clean the code after doing this.
					instructions[3] = new Instruction(OpCodes.Ldc_I4_1);
					instructions[0] = new Instruction(OpCodes.Nop);
					instructions[1] = new Instruction(OpCodes.Nop);
					instructions[2] = new Instruction(OpCodes.Ldc_I4_1);
				}
			}

			foreach (var method in foundResult_6) {
				if (!method.HasBody)
					continue;
				var instructions = method.Body.Instructions;
				if (instructions.Count < 100)
					continue;
				if (instructions[0].OpCode == OpCodes.Call && instructions[1].OpCode == OpCodes.Ldtoken && instructions[2].OpCode == OpCodes.Call) {
					Logger.n("[6]found internal assembly & mistake-ret check {0} [{1}]", method.Name, method.MDToken.ToString());
					for (int i = 0; i <= 12; i++) {
						/*
						 
						if (Assembly.GetCallingAssembly() != typeof(Class119.Class125).Assembly)
						{
							return 0x2C87F0L;
						}
						if (!Class119.smethod_2())
						{
							return 0x2C87F0L;
						}
						*/
						instructions[i] = new Instruction(OpCodes.Nop);
					}
				}
			}
		}
	}
}
