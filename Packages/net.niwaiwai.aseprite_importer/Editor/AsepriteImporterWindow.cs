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

namespace Editor
{
    public class AsepriteImporterWindow : EditorWindow
    {
        private string _pathUxml = "Packages/net.niwaiwai.aseprite_importer/Editor/AsepriteImporterWindowTemplate.uxml";
        private string _pathUss = "Packages/net.niwaiwai.aseprite_importer/Editor/AsepriteImporterWindowStyle.uss";
        private Texture2D _bindingTexture;
        private TextAsset _bindingJson;
        private Vector2Int _bindingPivot;
        private AsepriteSpriteInfo _bindingJsonData;
        private ObjectField _fieldTexture;
        private ObjectField _fieldJson;
        private Vector2IntField _fieldPivot;
        private TextElement _textFrameSize;
        private Button _buttonApply;

        [MenuItem("Window/Aseprite Importer")]
        public static void ShowWindow()
        {
            AsepriteImporterWindow wnd = GetWindow<AsepriteImporterWindow>();
            wnd.titleContent = new GUIContent("Aseprite Importer");
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            ImportUxml(root);
            ImportUss(root);
            ImportPivotConfig();

            CurateElements(root);
            InitElements();

            ValidateUserInput(); // NOTE: Validate once for initial state.
        }

        /**
		 * <summary>Import markup and add it into root visual element.</summary>
		 * <param name="root">Root visual element</param>
		 */
        private void ImportUxml(VisualElement root)
        {
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(_pathUxml);
            VisualElement tree = visualTree.CloneTree();
            root.Add(tree);
        }

        /**
		 * <summary>Import styles and apply it to root visual element.</summary>
		 * <param name="root">Root visual element</param>
		 */
        private void ImportUss(VisualElement root)
        {
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(_pathUss);
            root.styleSheets.Add(styleSheet);
        }

        // TODO: 要る？
        private void ImportPivotConfig() { }

        /**
		 * <summary>Curate UI elements from root visual element.</summary>
		 * <param name="root">Root visual element</param>
		 */
        private void CurateElements(VisualElement root)
        {
            _fieldTexture = root.Query<ObjectField>("field-aseprite");
            _fieldJson = root.Query<ObjectField>("field-json");
            _textFrameSize = root.Query<TextElement>("label-frame-size");
            _fieldPivot = root.Query<Vector2IntField>("field-pivot");
            _buttonApply = root.Query<Button>("button-apply");
        }

        /**
		 * <summary>Enable input validation and set callback method when input value changed.</summary>
		 */
        private void InitElements()
        {
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
        private void ValidateUserInput()
        {
            if (IsValid())
            {
                _buttonApply.SetEnabled(true);
            }
            else
            {
                _buttonApply.SetEnabled(false);
            }
        }

        /**
		 * <summary>Validate user inputted values and return the result.</summary>
		 * <returns>If all values is valid, return true. Unless, return false.</returns>
		 */
        private bool IsValid()
        {
            bool isExistsSpriteAssets = _bindingTexture != null && _bindingJson != null;
            if (!isExistsSpriteAssets)
            {
                return false;
            }

            bool isPivotXGreaterEqualThanFrameX = _bindingPivot.x >= _bindingJsonData.frames[0].frame.x;
            bool isPivotYGreaterEqualThanFrameY = _bindingPivot.y >= _bindingJsonData.frames[0].frame.y;
            bool isPivotXLessThanFrameW = _bindingJsonData.frames[0].frame.w > _bindingPivot.x;
            bool isPivotYLessThanFrameH = _bindingJsonData.frames[0].frame.h > _bindingPivot.y;
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
        private void UpdateTextureValue(ChangeEvent<Object> e)
        {
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
        private void ParseAsepriteJsonData(ChangeEvent<Object> e)
        {
            if (_fieldJson.value != null)
            {
                TextAsset val = (TextAsset)_fieldJson.value;
                _bindingJson = val;

                _bindingJsonData = JsonUtility.FromJson<AsepriteSpriteInfo>(val.text);

                AsepriteSpriteFrameRectInfo frameRect = _bindingJsonData.frames[0].frame; // NOTE: Each frames size is same.
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
        private void UpdatePivotValue(ChangeEvent<Vector2Int> e)
        {
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
        private void Apply()
        {
            _fieldTexture.SetValueWithoutNotify(null);
            _fieldJson.SetValueWithoutNotify(null);

            SliceAndApplyPivot();
            GenerateAnimationAndController();
        }

        /**
		 * <summary>Apply slice and pivot position based on inputted JSON and pivot position to texture.</summary>
		 */
        private void SliceAndApplyPivot()
        {
            string assetPath = AssetDatabase.GetAssetPath(_bindingTexture);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            importer.isReadable = true;

            List<SpriteMetaData> newSliceData = new List<SpriteMetaData>();
            AsepriteSpriteMetaSizeInfo texSize = _bindingJsonData.meta.size;

            foreach (AsepriteSpriteFrameTagInfo tag in _bindingJsonData.meta.frameTags)
            {
                int count = 1;
                for (int i = tag.from; i <= tag.to; i++)
                {
                    AsepriteSpriteFrameRectInfo frameRect = _bindingJsonData.frames[i].frame;

                    SpriteMetaData d = new SpriteMetaData();
                    d.name = tag.name + "__" + count;
                    d.rect = new Rect(frameRect.x, texSize.h - frameRect.h - frameRect.y, frameRect.w, frameRect.h);
                    d.alignment = 9; // NOTE: 9 is mean "SpriteAlignment.Custom". Need to use custom pivot position.
                    d.pivot = new Vector2((float)_bindingPivot.x / frameRect.w, 1f - ((float)_bindingPivot.y / frameRect.h));

                    newSliceData.Add(d);
                    count++;
                }
            }

            importer.spritesheet = newSliceData.ToArray();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        /**
		 * <summary>
		 * Generate animator controller and animation clips based on inputted texture and JSON.
		 * Generated animation clips will associate to generated animator controller.
		 * If animation clips and animator controller as already generated, overwrite. (But keep same GUID)
		 * Also, transition is not define in animator controller.
		 * Use "Animator.CrossFadeInFixedTime" to toggle animation.
		 * </summary>
		 */
        private void GenerateAnimationAndController()
        {
            // NOTE: If animation is not defined in JSON, skip generation.
            if (_bindingJsonData.meta.frameTags.Length > 0)
            {
                string texturePath = AssetDatabase.GetAssetPath(_bindingTexture);
                string dirTexture = GetDirPathFromAsset(_bindingTexture);
                string fileNameTextureWithoutExtension = Path.GetFileNameWithoutExtension(texturePath);
                Sprite[] sprites = GetSpritesFromAssetPath(texturePath, _bindingJsonData.meta.frameTags);

                // NOTE: Save animator controller asset
                AnimatorController animController = AnimatorController.CreateAnimatorControllerAtPath
                    (dirTexture + "/" + fileNameTextureWithoutExtension + ".controller");

                // NOTE: Save animation clips by each tags. And associate them to animator controller.
                foreach (AsepriteSpriteFrameTagInfo tag in _bindingJsonData.meta.frameTags)
                {
                    AnimationClip clip = new AnimationClip();
                    EditorCurveBinding curveBinding = new EditorCurveBinding();
                    curveBinding.type = typeof(SpriteRenderer);
                    curveBinding.path = "";
                    curveBinding.propertyName = "m_Sprite";
                    int keyframeLength = tag.to - tag.from + 1;
                    ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[keyframeLength];
                    for (int i = 0; i <= keyframeLength - 1; i++)
                    {
                        keyframes[i] = new ObjectReferenceKeyframe();
                        keyframes[i].time = GetKeyframeTime(_bindingJsonData, tag, i);
                        keyframes[i].value = GetKeyframeSprite(_bindingJsonData, tag, sprites, i);
                    }

                    AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keyframes);
                    AssetDatabase.CreateAsset(clip,
                        dirTexture + "/" + fileNameTextureWithoutExtension + "__" + tag.name + ".anim");
                    var state = animController.layers[0].stateMachine.AddState(tag.name);
                    state.motion = clip;
                    state.writeDefaultValues = false;
                }
            }
        }

        /**
		 * <summary>Get keyframe time of the frame in the tag.</summary>
		 * <param name="config">JSON data that exported from Aseprite</param>
		 * <param name="tag">Tag of animation</param>
		 * <param name="i">Index of frame in the tag</param>
		 * <returns>Keyframe time of the frame</returns>
		 */
        private int GetKeyframeTime(AsepriteSpriteInfo config, AsepriteSpriteFrameTagInfo tag, int i)
        {
            return new ArraySegment<int>(config.frames.Select(v => v.duration).ToArray(), tag.from, i + 1)
                .ToArray()
                .Aggregate((acc, cur) => acc + cur);
        }

        /**
		 * <summary>Get keyframe sprite of the frame in the tag.</summary>
		 * <param name="config">JSON data that exported from Aseprite</param>
		 * <param name="tag">Tag of animation</param>
		 * <param name="sprites">Sprites includes the sprite</param>
		 * <param name="i">Index of frame in the tag</param>
		 * <returns>Keyframe sprite of the frame</returns>
		 */
        private Sprite GetKeyframeSprite(AsepriteSpriteInfo config, AsepriteSpriteFrameTagInfo tag, Sprite[] sprites, int i)
        {
            int indexTheTag = config.meta.frameTags.ToList().FindIndex(v => v.name == tag.name);
            int offset = config.meta.frameTags
                .ToList()
                .Where((v, ii) => indexTheTag > ii)
                .Select(v => CountFramesInTag(v))
                .Aggregate(0, Sum());
            int count = tag.to - tag.from + 1;
            return new ArraySegment<Sprite>(sprites, offset, count).ToArray()[i];
        }

        /**
		 * <summary>Get each sprites from texture.</summary>
		 * <param name="path">Texture asset path</param>
		 * <param name="tags">Tags of sprites (Defined in Aseprite)</param>
		 * <returns>Sprites array (Sorted by tag)</returns>
		 */
        private Sprite[] GetSpritesFromAssetPath(string path, AsepriteSpriteFrameTagInfo[] tags)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            List<Sprite> sprites = new List<Sprite>();

            foreach (var asset in assets)
            {
                if (asset is Sprite)
                {
                    sprites.Add(asset as Sprite);
                }
            }

            // NOTE: Sort by meta.frameTags order. (Because, GetKeyframeSprite method assume the order)
            string[] order = GetSpriteArrSortedByFrameTagOrder(sprites.ToList().Select(v => v.name).ToArray(), tags);

            return order
                .ToList()
                .Select(spriteName => sprites.ToList().First(sprite => sprite.name == spriteName))
                .ToArray();
        }

        /**
		 * <summary>Get path of directory contained the asset.</summary>
		 * <returns>Path of directory contained the asset</returns>
		 */
        private string GetDirPathFromAsset(Object obj)
        {
            string[] arr = AssetDatabase.GetAssetPath(obj).Split('/');
            List<string> lst = new List<string>(arr);
            lst.RemoveAt(arr.Length - 1);
            return string.Join("/", lst);
        }

        /**
		 * <summary>Count frames included in the tag.</summary>
		 * <returns>Amount of frames included in the tag</returns>
		 * <param name="tag">Tag</param>
		 */
        public int CountFramesInTag(AsepriteSpriteFrameTagInfo tag)
        {
            return tag.to - tag.from + 1;
        }

        /**
		 * <summary>Get sprite array sorted by frame tag order.</summary>
		 * <param name="spriteNames">Names of each sprites</param>
		 * <param name="tags">Tags of sprites (Defined in Aseprite)</param>
		 * <returns>Sprite array sorted by frame tag order</returns>
		 */
        private string[] GetSpriteArrSortedByFrameTagOrder(string[] spriteNames, AsepriteSpriteFrameTagInfo[] tags)
        {
            List<string> lst = new List<string>();

            foreach (AsepriteSpriteFrameTagInfo tag in tags)
            {
                List<string> extracted = spriteNames
                    .ToList()
                    .Where(v => new Regex("^" + tag.name + "__").IsMatch(v))
                    .OrderBy(v => v)
                    .ToList();
                lst = lst.Concat(extracted).ToList();
            }

            return lst.ToArray();
        }

        /**
		 * <summary>Return function that adding 2 numbers.</summary>
		 * <returns>Function that adding 2 numbers</returns>
		 */
        private Func<int, int, int> Sum()
        {
            return (x, y) => x + y;
        }
    }
}
