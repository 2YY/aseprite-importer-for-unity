using System.Reflection;
using Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor {
	public class AsepriteImporterWindowSpec {

		private AsepriteImporterWindow _win;
		private MethodInfo _isValidMethod;
		private Texture2D _texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/net.niwaiwai.aseprite_importer/Tests/Editor/AsepriteImporterSpecBase.png");
		private AsepriteSpriteData _data = EditorTestUtil.GetData("Packages/net.niwaiwai.aseprite_importer/Tests/Editor/AsepriteImporterSpec.json");
		private Vector2Int _pivot = new Vector2Int(0, 0);

		[SetUp]
		public void SetUp() {
			_win = ScriptableObject.CreateInstance<AsepriteImporterWindow>();

			var type = typeof(AsepriteImporterWindow);
			_isValidMethod = type.GetMethod("IsValid", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new [] { typeof(Texture2D), typeof(AsepriteSpriteData?), typeof(Vector2Int) }, null);
		}

		[Test]
		public void ShouldApplyButtonDisabledIfTextureIsNotInputted() {
			bool result = (bool) _isValidMethod.Invoke(_win, new object[] { null, _data, _pivot });
			Assert.IsFalse(result);
		}

		[Test]
		public void ShouldApplyButtonDisabledIfJsonIsNotInputted() {
			bool result = (bool) _isValidMethod.Invoke(_win, new object[] { _texture, null, _pivot });
			Assert.IsFalse(result);
		}

		[Test]
		public void ShouldApplyButtonDisabledIfPivotIsNotInputted() {
			bool result = (bool) _isValidMethod.Invoke(_win, new object[] { _texture, _data, null });
			Assert.IsFalse(result);
		}

		[Test]
		public void ShouldApplyButtonEnabledIfAllValuesAreInputted() {
			bool result = (bool) _isValidMethod.Invoke(_win, new object[] { _texture, _data, _pivot });
			Assert.IsTrue(result);
		}
	}
}
