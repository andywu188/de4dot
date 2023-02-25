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

namespace de4dot.code.deobfuscators.Obfuscar {
	public class DeobfuscatorInfo : DeobfuscatorInfoBase {
		public const string THE_NAME = "Obfuscar";
		public const string THE_TYPE = "ob";
		const string DEFAULT_REGEX = @"^[\u2E80-\u9FFFa-zA-Z_<{$][\u2E80-\u9FFFa-zA-Z_0-9<>{}$.`-]{1,}$";

		public DeobfuscatorInfo()
			: base(DEFAULT_REGEX) {
		}

		public override string Name => THE_NAME;
		public override string Type => THE_TYPE;

		public override IDeobfuscator CreateDeobfuscator() =>
			new Deobfuscator(new Deobfuscator.Options {
				ValidNameRegex = validNameRegex.Get(),
			});
	}

	class Deobfuscator : DeobfuscatorBase {
		StringDecrypter stringDecrypter;

		internal class Options : OptionsBase {
		}

		public override string Type => DeobfuscatorInfo.THE_TYPE;
		public override string TypeLong => DeobfuscatorInfo.THE_NAME;
		public override string Name => TypeLong;

		public Deobfuscator(Options options)
			: base(options) {
		}

		protected override int DetectInternal() {
			int val = 0;

			if (stringDecrypter.Detected)
				val += 100;

			return val;
		}

		protected override void ScanForObfuscator() {
			stringDecrypter = new StringDecrypter(module);
			stringDecrypter.Find();
		}

		public override void DeobfuscateBegin() {
			base.DeobfuscateBegin();

			staticStringInliner.Add(stringDecrypter.Method, (method, gim, args) => stringDecrypter.Decrypt((int)args[1], (int)args[2]));
			DeobfuscatedFile.StringDecryptersAdded();
		}

		public override void DeobfuscateEnd() {
			if (CanRemoveStringDecrypterType) {
				stringDecrypter.InlineStringMethods();
				AddTypeToBeRemoved(stringDecrypter.Type, "String decrypter type");
			}

			base.DeobfuscateEnd();
		}

		public override IEnumerable<int> GetStringDecrypterMethods() {
			var list = new List<int>();
			if (stringDecrypter.Method != null)
				list.Add(stringDecrypter.Method.MDToken.ToInt32());
			return list;
		}
	}
}
