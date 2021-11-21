using Editor;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor {
	public class EditorTestUtil {
		public static Texture2D GetTexture(string path) {
			return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		}

		public static AsepriteSpriteData GetData(string path) {
			var json = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
			return JsonUtility.FromJson<AsepriteSpriteData>(json.text);
		}
	}
}
