using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using de4dot.blocks;
using dnlib.DotNet.Emit;

namespace de4dot.code.deobfuscators.EzHud {
	public class DeobfuscatorInfo : DeobfuscatorInfoBase {
		public const string THE_NAME = "EZD";
		public const string THE_TYPE = "ez";
		const string DEFAULT_REGEX = DeobfuscatorBase.DEFAULT_VALID_NAME_REGEX;

		public DeobfuscatorInfo()
			: base(DEFAULT_REGEX) {
		}

		public override string Name => THE_NAME;
		public override string Type => THE_TYPE;

		public override IDeobfuscator CreateDeobfuscator() =>
			new Deobfuscator(new Deobfuscator.Options {
				RenameResourcesInCode = false,
				ValidNameRegex = validNameRegex.Get(),
			});
	}

	class Deobfuscator : DeobfuscatorBase {

		internal class Options : OptionsBase {
		}

		public override string Type => DeobfuscatorInfo.THE_TYPE;
		public override string TypeLong => DeobfuscatorInfo.THE_NAME;
		public override string Name => "EzHUD Obfuscator";

		internal Deobfuscator(Options options)
			: base(options) => KeepTypes = true;

		protected override int DetectInternal() {
			foreach (var type in module.Types) {
				var fn = type.DefinitionAssembly.FullName;
				if (fn.Contains("EazyHUD")) {
					Logger.n("[EazyHUD] Found.");
					return 999;
				}
			}
			return 0;
		}

		protected override void ScanForObfuscator() {
		}

		public override void DeobfuscateBegin() {
			//开始破解
			foreach (var type in module.GetTypes()) {
				var foundResult_1 = DotNetUtils.FindMethods(type.Methods, "System.Boolean", new string[] { }, true);
				foreach (var method in foundResult_1) {
					if (!method.HasBody)
						continue;
					var instructions = method.Body.Instructions;
					if (instructions.Count < 100)
						continue;
					//locate in method
					if (instructions[0].OpCode != OpCodes.Ldsfld && instructions[1].OpCode != OpCodes.Stloc_0 && instructions[2].OpCode != OpCodes.Ldsfld)
						continue;
					bool flag = false;
					for (int i = 0; i < instructions.Count; i++) {
						if (i < instructions.Count - 10) {
							if (instructions[i].OpCode == OpCodes.Ldsfld &&
								instructions[i + 1].OpCode == OpCodes.Ldloc_S &&
								instructions[i + 2].OpCode == OpCodes.Call &&
								instructions[i + 3].OpCode == OpCodes.Stloc_S) {
								if (instructions[i + 1].Operand.Equals(instructions[i + 3].Operand)) {
									//modify
									instructions[i].OpCode = OpCodes.Ldstr;
									instructions[i].Operand = "MagicText";
									instructions[i + 1].OpCode = OpCodes.Nop;
									instructions[i + 2].OpCode = OpCodes.Nop;
									flag = true;
									break;
								}
							}
						}
						else {
							break;
						}
					}

					if (flag) {
						Logger.n("[0]modify string [{0}]", method.MDToken.ToString());
					}
				}

				var foundResult_2 = DotNetUtils.FindMethods(type.Methods, "System.Void", new string[] { }, true);
				foreach (var method in foundResult_2) {
					if (!method.HasBody)
						continue;
					var instructions = method.Body.Instructions;
					if (instructions.Count < 50)
						continue;
					//locate in method
					if (instructions[0].OpCode != OpCodes.Call && instructions[1].OpCode != OpCodes.Stloc_0 && instructions[2].OpCode != OpCodes.Ldsfld
						&& instructions[4].OpCode != OpCodes.Constrained)
						continue;
					bool founded = false;
					foreach (var instr in instructions) {
						if (instr.OpCode == OpCodes.Ldstr) {
							if (instr.Operand.Equals("small")) {
								founded = true;
							}
						}
					}
					if (!founded) {
						continue;
					}
					Logger.n("[1]modify string [{0}]", method.MDToken.ToString());
					for (int i = 0; i < 8; i++) {
						instructions[i].OpCode = OpCodes.Nop;
					}

					instructions[0].OpCode = OpCodes.Ldstr;
					instructions[0].Operand = "11111111111111111111111111111111";
				}
			}
		}

		public override IEnumerable<int> GetStringDecrypterMethods() => new List<int>();
	}
}
