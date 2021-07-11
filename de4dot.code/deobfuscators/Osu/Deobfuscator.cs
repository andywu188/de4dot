using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace de4dot.code.deobfuscators.Osu {
	public class DeobfuscatorInfo : DeobfuscatorInfoBase {
		public const string THE_NAME = "Osu";
		public const string THE_TYPE = "os";
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
		public override string Name => "Osu Obfuscator";

		internal Deobfuscator(Options options)
			: base(options) => KeepTypes = true;

		protected override int DetectInternal() {
			foreach (var type in module.Types) {
				var fn = type.DefinitionAssembly.FullName;
				if (fn == "osu!, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null") {
					Logger.n("[Osu Code Clean] [Steesha]第一次拖入请勿使用delegate解密字符串，第一次cleaned后的程序才能deleage解密字符串，否则将会解密失败。");
					return 999;
				}
			}
			return 0;
		}

		protected override void ScanForObfuscator() {
		}

		public override void DeobfuscateBegin() {
			base.DeobfuscateBegin();
			AntiDe4dot anti = new AntiDe4dot(module);
			anti.doantiantide4();
		}

		public override IEnumerable<int> GetStringDecrypterMethods() => new List<int>();
	}
}
