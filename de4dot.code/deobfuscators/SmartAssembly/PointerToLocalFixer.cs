using System.Collections.Generic;
using System.Linq;
using de4dot.blocks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace de4dot.code.deobfuscators.SmartAssembly {
	class PointerToLocalFixer {
		readonly ModuleDef module;
		Blocks blocks;
		Local baseLocal;
		Dictionary<int, Local> locals = new Dictionary<int, Local>();

		public PointerToLocalFixer(ModuleDef module) => this.module = module;

		public bool Deobfuscate(Blocks blocks) {
			this.blocks = blocks;
			GetLocals();

			if (locals.Count > 0)
				Deobfuscate();

			return locals.Count > 0;
		}

		void GetLocals() {
			locals.Clear();

			if (GetLocals1())
				GetLocals2();
		}

		bool GetLocals1() {
			var isFound = false;

			foreach (var block in blocks.MethodBlocks.GetAllBlocks()) {
				var instrs = block.Instructions;

				if (!isFound) {
					for (int i = 0; i < instrs.Count; i++) {
						var stloc = instrs[i];
						if (stloc.IsStloc() && i - 3 >= 0) {
							var convu = instrs[i - 1];
							var localloc = instrs[i - 2];
							var ldci4 = instrs[i - 3];
							if (convu.OpCode.Code == Code.Conv_U
								&& localloc.OpCode.Code == Code.Localloc
								&& ldci4.IsLdcI4()) {
								baseLocal = stloc.Instruction.GetLocal(blocks.Method.Body.Variables);
								isFound = true;

								block.Remove(i - 3, 4);
								break;
							}
						}
					}
				}

				if (!isFound)
					continue;

				for (int i = 0; i < instrs.Count; i++) {
					var instr = instrs[i];
					if (IsStind(instr) || IsLdind(instr)) {
						var pushes = DotNetUtils.GetArgPushes(instrs, i);
						if (pushes?.Count > 0) {
							var value = 0;
							var ldloc = instr;

							var ldlocOrAdd = pushes[0];
							if (IsBaseLdlocal(ldlocOrAdd)) {
								ldloc = ldlocOrAdd;
							}
							else if (ldlocOrAdd.OpCode.Code == Code.Add) {
								var index = instrs.IndexOf(ldlocOrAdd);
								if (index - 2 < instrs.Count) {
									var ldci4 = instrs[index - 1];
									ldloc = instrs[index - 2];
									if (ldci4.IsLdcI4() && IsBaseLdlocal(ldloc))
										value = ldci4.GetLdcI4Value();
								}
							}

							if (IsBaseLdlocal(ldloc)) {
								if (!locals.ContainsKey(value)) {
									var local = new Local(GetTypeFromStLdInd(instr));
									blocks.Locals.Add(local);
									locals.Add(value, local);
								}
							}
						}
					}
				}
			}

			return isFound;
		}

		void GetLocals2() {
			foreach (var block in blocks.MethodBlocks.GetAllBlocks()) {
				var instrs = block.Instructions;

				for (int i = 0; i < instrs.Count; i++) {
					var instr = instrs[i];
					if (IsCallOrNewobj(instr)) {
						var method = instr.Operand as IMethod;
						if (method == null)
							continue;
						if (!method.MethodSig.Params.Any(x => x.IsByRef))
							continue;

						var pushes = DotNetUtils.GetArgPushes(instrs, i);
						if (pushes == null)
							continue;
						if (pushes.Count == 0)
							continue;
						if (!pushes.Any(x => x.Instruction.OpCode.Code == Code.Add || IsBaseLdlocal(x)))
							continue;

						var args = DotNetUtils.GetArgs(method);
						for (var j = 0; j < pushes.Count; j++) {
							var value = 0;

							var arg = args[j];
							var ldloc = pushes[j];

							if (ldloc.OpCode.Code == Code.Add) {
								var index = instrs.IndexOf(ldloc);
								if (index < 2)
									continue;
								var ldci4 = instrs[index - 1];
								ldloc = instrs[index - 2];
								if (ldci4.IsLdcI4() && IsBaseLdlocal(ldloc))
									value = ldci4.GetLdcI4Value();
							}

							if (IsBaseLdlocal(ldloc)) {
								if (!locals.ContainsKey(value)) {
									var local = new Local(arg.ScopeType.ToTypeSig());
									blocks.Locals.Add(local);
									locals.Add(value, local);
								}
							}
						}
					}
				}
			}
		}

		void Deobfuscate() {
			foreach (var block in blocks.MethodBlocks.GetAllBlocks()) {
				Deobfuscate1(block);
				Deobfuscate2(block);
				Deobfuscate3(block);
			}
		}

		void Deobfuscate1(Block block) {
			var instrs = block.Instructions;

			for (int i = 0; i < instrs.Count; i++) {
				var instr = instrs[i];
				if (IsStind(instr) || IsLdind(instr)) {
					var pushes = DotNetUtils.GetArgPushes(instrs, i);
					if (pushes?.Count > 0) {
						var value = 0;

						var ldloc = instr;
						var add = pushes[0];
						if (IsBaseLdlocal(add)) {
							ldloc = add;
						}
						else if (add.OpCode.Code == Code.Add) {
							var index = instrs.IndexOf(add);
							if (index - 2 < instrs.Count) {
								var ldci4 = instrs[index - 1];
								ldloc = instrs[index - 2];
								if (ldci4.IsLdcI4() && IsBaseLdlocal(ldloc)) {
									value = ldci4.GetLdcI4Value();

									ldci4.Instruction.OpCode = OpCodes.Nop;
									add.Instruction.OpCode = OpCodes.Nop;
								}
							}
						}

						if (IsBaseLdlocal(ldloc)) {
							ldloc.Instruction.OpCode = OpCodes.Nop;

							if (IsStind(instr))
								instr.Instruction.OpCode = OpCodes.Stloc;
							else if (IsLdind(instr))
								instr.Instruction.OpCode = OpCodes.Ldloc;

							instr.Instruction.Operand = locals[value];
						}
					}
				}
			}
		}

		void Deobfuscate2(Block block) {
			var instrs = block.Instructions;

			for (int i = 0; i < instrs.Count; i++) {
				var ldloc = instrs[i];
				if (IsBaseLdlocal(ldloc)) {
					var value = 0;

					if (i + 2 < instrs.Count) {
						var ldci4 = instrs[i + 1];
						var add = instrs[i + 2];
						if (ldci4.IsLdcI4() && add.OpCode.Code == Code.Add) {
							value = ldci4.GetLdcI4Value();

							ldci4.Instruction.OpCode = OpCodes.Nop;
							add.Instruction.OpCode = OpCodes.Nop;
						}
					}

					ldloc.Instruction.OpCode = OpCodes.Ldloca;
					ldloc.Instruction.Operand = locals[value];
				}
			}
		}

		void Deobfuscate3(Block block) {
			var instrs = block.Instructions;

			for (int i = 0; i < instrs.Count; i++) {
				var instr = instrs[i];
				if (IsCallOrNewobj(instr)) {
					var method = instr.Operand as IMethod;
					if (method == null)
						continue;

					var pushes = DotNetUtils.GetArgPushes(instrs, i);
					if (pushes == null)
						continue;
					if (pushes.Count == 0)
						continue;

					var args = DotNetUtils.GetArgs(method);
					for (var j = 0; j < pushes.Count; j++) {
						var arg = args[j];
						if (!arg.IsByRef)
							continue;
						if (!pushes[j].IsLdloc())
							continue;
						var local = pushes[j].Instruction.GetLocal(blocks.Locals);
						if (!locals.ContainsValue(local)) 
							continue;
						pushes[j].Instruction.OpCode = OpCodes.Ldloca;
						pushes[j].Instruction.Operand = local;
					}
				}
			}
		}

		CorLibTypeSig GetTypeFromStLdInd(Instr instr) {
			CorLibTypeSig result;

			switch (instr.OpCode.Code) {
			case Code.Stind_I1:
			case Code.Ldind_I1:
				result = module.CorLibTypes.Boolean;
				break;

			case Code.Stind_I2:
			case Code.Ldind_I2:
				result = module.CorLibTypes.Int16;
				break;

			case Code.Stind_I4:
			case Code.Ldind_I4:
				result = module.CorLibTypes.Int32;
				break;

			case Code.Stind_I8:
			case Code.Ldind_I8:
				result = module.CorLibTypes.Int64;
				break;

			case Code.Stind_R4:
			case Code.Ldind_R4:
				result = module.CorLibTypes.Single;
				break;

			case Code.Stind_R8:
			case Code.Ldind_R8:
				result = module.CorLibTypes.Double;
				break;

			case Code.Ldind_U1:
				result = module.CorLibTypes.Byte;
				break;

			case Code.Ldind_U2:
				result = module.CorLibTypes.UInt16;
				break;

			case Code.Ldind_U4:
				result = module.CorLibTypes.UInt32;
				break;

			default:
				result = null;
				break;
			}

			return result;
		}

		bool IsBaseLdlocal(Instr instr) => instr.IsLdloc() && instr.Instruction.GetLocal(blocks.Locals) == baseLocal;

		bool IsCallOrNewobj(Instr instr) => instr.OpCode.Code == Code.Call || instr.OpCode.Code == Code.Callvirt || instr.OpCode.Code == Code.Newobj;

		bool IsStind(Instr instr) {
			switch (instr.OpCode.Code) {
			case Code.Stind_I:
			case Code.Stind_I1:
			case Code.Stind_I2:
			case Code.Stind_I4:
			case Code.Stind_I8:
			case Code.Stind_R4:
			case Code.Stind_R8:
				return true;

			default:
				return false;
			}
		}

		bool IsLdind(Instr instr) {
			switch (instr.OpCode.Code) {
			case Code.Ldind_I:
			case Code.Ldind_I1:
			case Code.Ldind_I2:
			case Code.Ldind_I4:
			case Code.Ldind_I8:
			case Code.Ldind_R4:
			case Code.Ldind_R8:
			case Code.Ldind_U1:
			case Code.Ldind_U2:
			case Code.Ldind_U4:
				return true;

			default:
				return false;
			}
		}
	}
}
