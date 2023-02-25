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

using System.Text;
using de4dot.blocks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace de4dot.code.deobfuscators.Obfuscar {
	class StringDecrypter {
		const int STRING_DECRYPTER_KEY_CONST = 170;
		ModuleDefMD module;
		TypeDef stringDecrypterType;
		MethodDef stringDecrypterMethod;
		FieldDef stringArrayField;
		byte[] decryptedData;

		public bool Detected => stringDecrypterMethod != null;
		public TypeDef Type => stringDecrypterType;
		public MethodDef Method => stringDecrypterMethod;

		public StringDecrypter(ModuleDefMD module) => this.module = module;

		public void Find() {
			foreach (var type in module.Types) {
				if (!type.HasFields)
					continue;
				if (!type.HasMethods)
					continue;
				if (type.HasProperties || type.HasEvents)
					continue;
				if (type.FindStaticConstructor() == null)
					continue;
				if (DotNetUtils.FindFieldType(type, "System.Byte[]", true) == null)
					continue;

				MethodDef method = null;
				foreach (var m in type.Methods) {
					if (m.Name == ".ctor" || m.Name == ".cctor")
						continue;
					if (DotNetUtils.IsMethod(m, "System.String", "(System.Int32,System.Int32,System.Int32)")) {
						method = m;
						continue;
					}
					break;
				}
				if (method == null || method.Body == null)
					continue;

				bool foundConstant = false;
				bool foundArrayField = false;
				var cctor = type.FindStaticConstructor();
				foreach (var instr in cctor.Body.Instructions) {
					if (instr.OpCode.Code == Code.Ldtoken) {
						stringArrayField = GetByteArrayField(cctor);
						if (stringArrayField != null)
							foundArrayField = true;
					}
					if (instr.IsLdcI4() && instr.GetLdcI4Value() == STRING_DECRYPTER_KEY_CONST)
						foundConstant = true;
				}
				if (!foundConstant || !foundArrayField)
					continue;

				decryptedData = stringArrayField?.InitialValue;
				stringDecrypterType = type;
				stringDecrypterMethod = method;
				break;
			}

			DecryptData();
		}

		FieldDef GetByteArrayField(MethodDef method) {
			if (method == null || method.Body == null)
				return null;
			var instrs = method.Body.Instructions;
			for (int i = 0; i < instrs.Count; i++) {
				if (instrs[i].OpCode.Code != Code.Ldtoken)
					continue;
				var arrayField = instrs[i].Operand as FieldDef;
				if (arrayField == null)
					continue;

				i++;
				for (; i < instrs.Count; i++) {
					var instr = instrs[i];
					if (instr.OpCode.Code != Code.Stsfld)
						continue;
					var field = instr.Operand as FieldDef;
					if (field == null || !field.IsStatic || field.DeclaringType != method.DeclaringType)
						continue;
					if (field.FieldSig.GetFieldType().GetFullName() != "System.Byte[]")
						continue;
					return arrayField;
				}
			}
			return null;
		}

		void DecryptData() {
			if (decryptedData == null || decryptedData.Length == 0)
				return;
			for (int i = 0; i < decryptedData.Length; i++)
				decryptedData[i] = (byte)(decryptedData[i] ^ i ^ STRING_DECRYPTER_KEY_CONST);
		}

		public string Decrypt(int index, int count) {
			return Encoding.UTF8.GetString(decryptedData, index, count);
		}

		public void InlineStringMethods() {
			foreach (var type in module.GetTypes()) {
				foreach (var method in type.Methods) {
					if (method == null || method.Body == null)
						continue;
					var instrs = method.Body.Instructions;
					foreach (var call in instrs) {
						if (call.OpCode.Code != Code.Call)
							continue;
						var calledMethod = call.Operand as MethodDef;
						if (calledMethod == null)
							continue;
						if (calledMethod.DeclaringType != stringDecrypterType)
							continue;
						if (calledMethod.ReturnType.FullName != "System.String")
							continue;
						var strings = DotNetUtils.GetCodeStrings(calledMethod);
						if (strings.Count == 1) {
							call.OpCode = OpCodes.Ldstr;
							call.Operand = strings[0];
						}
					}
				}
			}
		}
	}
}
