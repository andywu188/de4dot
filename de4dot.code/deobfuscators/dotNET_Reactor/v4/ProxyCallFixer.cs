/*
    Copyright (C) 2011-2015 de4dot@gmail.com

    This file is part of de4dot.

    de4dot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    de4dot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with de4dot.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using System.IO;
using de4dot.blocks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace de4dot.code.deobfuscators.dotNET_Reactor.v4 {
	class ProxyCallFixer : ProxyCallFixer1 {
		EncryptedResource encryptedResource;
		Dictionary<int, int> dictionary = new Dictionary<int, int>();

		ISimpleDeobfuscator simpleDeobfuscator;

		public ProxyCallFixer(ModuleDefMD module, ISimpleDeobfuscator simpleDeobfuscator)
			: base(module) => this.simpleDeobfuscator = simpleDeobfuscator;

		public ProxyCallFixer(ModuleDefMD module, ProxyCallFixer oldOne)
			: base(module) {
			foreach (var method in oldOne.delegateCreatorMethods)
				SetDelegateCreatorMethod(Lookup(method, "Could not find delegate creator method"));

			simpleDeobfuscator = oldOne.simpleDeobfuscator;
		}

		public void Initialize() {
			if (delegateCreatorMethods.Count == 0)
				return;

			encryptedResource = new EncryptedResource(module);
			encryptedResource.Method = delegateCreatorMethods[0];

			encryptedResource.Initialize(simpleDeobfuscator);
			if (!encryptedResource.FoundResource)
				return;

			GetDictionary();
		}

		protected override object CheckCctor(ref TypeDef type, MethodDef cctor) {
			var instrs = cctor.Body.Instructions;
			if (instrs.Count > 10)
				return null;
			if (instrs.Count > 3)
				simpleDeobfuscator.Deobfuscate(cctor);
			if (instrs.Count > 5)
				return null;
			if (instrs[instrs.Count - 3].OpCode != OpCodes.Ldtoken)
				return null;
			if (instrs[instrs.Count - 2].OpCode != OpCodes.Call || !IsDelegateCreatorMethod(instrs[instrs.Count - 2].Operand as MethodDef))
				return null;
			if (instrs[instrs.Count - 1].OpCode != OpCodes.Ret)
				return null;

			return new object();
		}

		protected override void GetCallInfo(object context, FieldDef field, out IMethod calledMethod, out OpCode callOpcode) {
			callOpcode = OpCodes.Call;

			if (!dictionary.TryGetValue(field.MDToken.ToInt32(), out var token))
				Logger.w("Ignoring invalid method RID: {0:X8}, field: {1:X8}", token, field.MDToken.ToInt32());

			bool isCallvirt = (token & 0x40000000) > 0;
			token &= 0x3fffffff;

			if (isCallvirt)
				callOpcode = OpCodes.Callvirt;

			calledMethod = module.ResolveToken(token) as IMethod;
			if (calledMethod == null)
				Logger.w("Ignoring invalid method RID: {0:X8}, field: {1:X8}", token, field.MDToken.ToInt32());
		}

		public void FindDelegateCreator(ModuleDefMD module) {
			var callCounter = new CallCounter();
			foreach (var type in module.Types) {
				if (type.Namespace != "" || !DotNetUtils.DerivesFromDelegate(type))
					continue;
				var cctor = type.FindStaticConstructor();
				if (cctor == null)
					continue;
				foreach (var method in DotNetUtils.GetMethodCalls(cctor)) {
					if (method.MethodSig.GetParamCount() == 1
						&& method.GetParam(0).FullName == "System.RuntimeTypeHandle")
						callCounter.Add(method);
				}
			}

			var mostCalls = callCounter.Most();
			if (mostCalls == null)
				return;

			SetDelegateCreatorMethod(DotNetUtils.GetMethod(module, mostCalls));
		}

		protected override bool Deobfuscate(Blocks blocks, IList<Block> allBlocks) {
			var removeInfos = new Dictionary<Block, List<RemoveInfo>>();

			foreach (var block in allBlocks) {
				var instrs = block.Instructions;
				for (int i = 0; i < instrs.Count; i++) {
					var instr = instrs[i];
					if (instr.OpCode != OpCodes.Ldsfld)
						continue;

					var di = GetDelegateInfo(instr.Operand as IField);
					if (di == null)
						continue;

					var callInfo = FindProxyCall(di, block, i, new Dictionary<Block, bool>(), 1);
					if (callInfo != null) {
						Add(removeInfos, block, i, null);
						Add(removeInfos, callInfo.Block, callInfo.Index, di);
					}
					else {
						errors++;
						Logger.w("Could not fix proxy call. Method: {0} ({1:X8}), Proxy type: {2} ({3:X8})",
							Utils.RemoveNewlines(blocks.Method),
							blocks.Method.MDToken.ToInt32(),
							Utils.RemoveNewlines(di.field.DeclaringType),
							di.field.DeclaringType.MDToken.ToInt32());
					}
				}
			}

			return FixProxyCalls(blocks.Method, removeInfos);
		}

		public void DeobfuscateAll() {
			foreach (var type in module.GetTypes()) {
				foreach (var method in type.Methods) {
					if (!method.HasBody)
						continue;

					var blocks = new Blocks(method);

					Deobfuscate(blocks);

					blocks.GetCode(out var allInstructions, out var allExceptionHandlers);
					DotNetUtils.RestoreBody(method, allInstructions, allExceptionHandlers);
				}
			}
		}

		BlockInstr FindProxyCall(DelegateInfo di, Block block, int index, Dictionary<Block, bool> visited, int stack) {
			if (visited.ContainsKey(block))
				return null;
			if (index <= 0)
				visited[block] = true;

			var instrs = block.Instructions;
			for (int i = index + 1; i < instrs.Count;) {
				if (stack <= 0)
					return null;

				var instr = instrs[i];

				var calledMethod = instr.Operand as IMethod;
				if (calledMethod == null)
					return null;

				return new BlockInstr {
					Block = block,
					Index = i,
				};
			}
			if (stack <= 0)
				return null;

			foreach (var target in block.GetTargets()) {
				var info = FindProxyCall(di, target, -1, visited, stack);
				if (info != null)
					return info;
			}

			return null;
		}

		void GetDictionary() {
			if (dictionary.Count != 0)
				dictionary.Clear();

			var resource = encryptedResource.Decrypt();
			var length = resource.Length / 8;

			var reader = new BinaryReader(new MemoryStream(resource));

			for (int i = 0; i < length; i++) {
				int key = reader.ReadInt32();
				int value = reader.ReadInt32();
				if (!dictionary.ContainsKey(key))
					dictionary.Add(key, value);
			}

			reader.Close();
		}
	}
}
