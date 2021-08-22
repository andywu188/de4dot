using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

			foreach (var method in foundResult_2) {
				if (!method.HasBody)
					continue;
				var instructions = method.Body.Instructions;
				for (int i = 0; i < instructions.Count; i++) { 
					if(instructions[i].OpCode == OpCodes.Callvirt) {
						MethodDef trand = (MethodDef)instructions[i].Operand;
						//TODO replace object::toString to string::toString
						if (trand.FullName == "System.String System.Object::ToString()") {
							Logger.n("[2]found deobf bug, fixing..... {0} [{1}]", method.Name, method.MDToken.ToString());
						}
					}
				}
			}
		}
	}
}
