using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Editor {
	public class AsepriteImporterWindow : EditorWindow {
		private AsepriteImporter _asepriteImporter = new AsepriteImporter();
		private string _pathUxml = "Packages/net.niwaiwai.aseprite_importer/Editor/AsepriteImporterWindowTemplate.uxml";
		private string _pathUss = "Packages/net.niwaiwai.aseprite_importer/Editor/AsepriteImporterWindowStyle.uss";
		private Texture2D _bindingTexture;
		private TextAsset _bindingJson;
		private Vector2Int? _bindingPivot = null;
		private AsepriteSpriteData? _bindingJsonData = null;
		private ObjectField _fieldTexture;
		private ObjectField _fieldJson;
		private Vector2IntField _fieldPivot;
		private TextElement _textFrameSize;
		private Button _buttonApply;

		[MenuItem("Window/Aseprite Importer")]
		public static void ShowWindow() {
			AsepriteImporterWindow wnd = GetWindow<AsepriteImporterWindow>();
			wnd.titleContent = new GUIContent("Aseprite Importer");
		}

		public void CreateGUI() {
			VisualElement root = rootVisualElement;

			ImportUxml(root);
			ImportUss(root);

			CurateElements(root);
			InitElements();

			ValidateUserInput(); // NOTE: Validate once for initial state.
		}

		/**
		 * <summary>Import markup and add it into root visual element.</summary>
		 * <param name="root">Root visual element</param>
		 */
		private void ImportUxml(VisualElement root) {
			VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(_pathUxml);
			VisualElement tree = visualTree.CloneTree();
			root.Add(tree);
		}

		/**
		 * <summary>Import styles and apply it to root visual element.</summary>
		 * <param name="root">Root visual element</param>
		 */
		private void ImportUss(VisualElement root) {
			StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(_pathUss);
			root.styleSheets.Add(styleSheet);
		}

		/**
		 * <summary>Curate UI elements from root visual element.</summary>
		 * <param name="root">Root visual element</param>
		 */
		private void CurateElements(VisualElement root) {
			_fieldTexture = root.Query<ObjectField>("field-texture");
			_fieldJson = root.Query<ObjectField>("field-json");
			_textFrameSize = root.Query<TextElement>("label-frame-size");
			_fieldPivot = root.Query<Vector2IntField>("field-pivot");
			_buttonApply = root.Query<Button>("button-apply");
		}

		/**
		 * <summary>Enable input validation and set callback method when input value changed.</summary>
		 */
		private void InitElements() {
			_fieldTexture.objectType = typeof(Texture2D);
			_fieldJson.objectType = typeof(TextAsset);

			_fieldTexture.RegisterCallback<ChangeEvent<Object>>(UpdateTextureValue);
			_fieldJson.RegisterCallback<ChangeEvent<Object>>(ParseAsepriteJsonData);
			_fieldPivot.RegisterCallback<ChangeEvent<Vector2Int>>(UpdatePivotValue);

			_buttonApply.clicked += Apply;
		}

		/**
		 * <summary>
		 * Validate user inputted values.
		 * If all values is valid, enable "Apply" button.
		 * Unless, disable "Apply" button.
		 * </summary>
		 */
		private void ValidateUserInput() {
			if (IsValid(_bindingTexture, _bindingJsonData, _bindingPivot)) {
				_buttonApply.SetEnabled(true);
			}
			else {
				_buttonApply.SetEnabled(false);
			}
		}

		/**
		 * <summary>Validate user inputted values and return the result.</summary>
		 * <returns>If all values is valid, return true. Unless, return false.</returns>
		 */
		private bool IsValid(Texture2D texture, AsepriteSpriteData? data, Vector2Int? pivot) {
			if (texture == null || data == null || pivot == null) {
				return false;
			}

			bool isPivotXGreaterEqualThanFrameX = pivot.Value.x >= data.Value.frames[0].frame.x;
			bool isPivotYGreaterEqualThanFrameY = pivot.Value.y >= data.Value.frames[0].frame.y;
			bool isPivotXLessThanFrameW = data.Value.frames[0].frame.w > pivot.Value.x;
			bool isPivotYLessThanFrameH = data.Value.frames[0].frame.h > pivot.Value.y;
			bool isPivotInTheFrameRect =
				isPivotXGreaterEqualThanFrameX &&
				isPivotYGreaterEqualThanFrameY &&
				isPivotXLessThanFrameW &&
				isPivotYLessThanFrameH;

			return isPivotInTheFrameRect;
		}

		/**
		 * <summary>Store inputted texture. And also, revalidate user input values.</summary>
		 * <param name="e">Event object</param>
		 */
		private void UpdateTextureValue(ChangeEvent<Object> e) {
			Texture2D val = (Texture2D)_fieldTexture.value;
			_bindingTexture = val;
			ValidateUserInput();
		}

		/**
		 * <summary>
		 * 1. Parse and store inputted Aseprite JSON data.
		 * 2. Print frame size to UI.
		 * 3. Set pivot position input value to bottom center of frame size.
		 * 4. Revalidate user input values.
		 * </summary>
		 * <param name="e">Event object</param>
		 */
		private void ParseAsepriteJsonData(ChangeEvent<Object> e) {
			if (_fieldJson.value != null) {
				TextAsset val = (TextAsset)_fieldJson.value;
				_bindingJson = val;

				_bindingJsonData = JsonUtility.FromJson<AsepriteSpriteData>(val.text);

				AsepriteSpriteFrameRectInfo frameRect = _bindingJsonData.Value.frames[0].frame; // NOTE: Each frames size is same.
				_textFrameSize.text = "Frame Size = (" + frameRect.w + ", " + frameRect.h + ")";
				_textFrameSize.style.display = DisplayStyle.Flex;

				Vector2Int newPivot = new Vector2Int(frameRect.w / 2, frameRect.h - 1);
				_fieldPivot.SetValueWithoutNotify(newPivot);
				_bindingPivot = newPivot;
			}

			ValidateUserInput();
		}

		/**
		 * <summary>Store inputted pivot position. And also. revalidate user input values.</summary>
		 * <param name="e">Event object</param>
		 */
		private void UpdatePivotValue(ChangeEvent<Vector2Int> e) {
			_bindingPivot = _fieldPivot.value;
			ValidateUserInput();
		}

		/**
		 * <summary>
		 * Update slice info of texture based on JSON data that exported from Aseprite.
		 * And then, generate animator controller and animation clip based on updated slice info of texture.
		 * (If already generated animator controller and animation clip, then overwrite.)
		 * </summary>
		 */
		private void Apply() {
			_fieldTexture.SetValueWithoutNotify(null);
			_fieldJson.SetValueWithoutNotify(null);

			_asepriteImporter.Import(_bindingTexture, _bindingJsonData.Value, _bindingPivot.Value);
		}
	}
}
