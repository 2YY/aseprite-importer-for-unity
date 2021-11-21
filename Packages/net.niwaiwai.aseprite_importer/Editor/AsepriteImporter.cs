using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Editor {
	public class AsepriteImporter {
		/**
		 * <summary>Load user input data and export animation assets.</summary>
		 * <param name="texture">Texture that will slice</param>
		 * <param name="data">Slicing order data that exported from Aseprite</param>
		 * <param name="pivot">Pivot coords that will apply to each slices</param>
		 */
		public void Import(Texture2D texture, AsepriteSpriteData data, Vector2Int pivot) {
			SliceAndApplyPivot(texture, data, pivot);
			GenerateAnimationAndController(texture, data);
		}

		/**
		 * <summary>Apply slice and pivot position based on inputted JSON and pivot position to texture.</summary>
		 * <param name="texture">Texture2D asset that applyied slicing and pivot info</param>
		 * <param name="data">Slicing info that exported from Aseprite</param>
		 * <param name="pivot">Pivot coords info that inputted from UI</param>
		 */
		private void SliceAndApplyPivot(Texture2D texture, AsepriteSpriteData data, Vector2Int pivot) {
			string assetPath = AssetDatabase.GetAssetPath(texture);
			TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
			importer.isReadable = true;

			List<SpriteMetaData> newSliceData = new List<SpriteMetaData>();
			AsepriteSpriteMetaSizeInfo texSize = data.meta.size;

			foreach (AsepriteSpriteFrameTagInfo tag in data.meta.frameTags) {
				int count = 1;
				for (int i = tag.from; i <= tag.to; i++) {
					AsepriteSpriteFrameRectInfo frameRect = data.frames[i].frame;

					SpriteMetaData d = new SpriteMetaData();
					d.name = tag.name + "__" + count;
					d.rect = new Rect(frameRect.x, texSize.h - frameRect.h - frameRect.y, frameRect.w, frameRect.h);
					d.alignment = 9; // NOTE: 9 is mean "SpriteAlignment.Custom". Need to use custom pivot position.
					d.pivot = new Vector2((float)pivot.x / frameRect.w, 1f - ((float)pivot.y / frameRect.h));

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
		 * <param name="texture">Sliced texture</param>
		 * <param name="data">Slicing info that exported from Aseprite</param>
		 */
		private void GenerateAnimationAndController(Texture2D texture, AsepriteSpriteData data) {
			// NOTE: If animation is not defined in JSON, skip generation.
			if (data.meta.frameTags.Length > 0) {
				string texturePath = AssetDatabase.GetAssetPath(texture);
				string dirTexture = GetDirPathFromAsset(texture);
				string fileNameTextureWithoutExtension = Path.GetFileNameWithoutExtension(texturePath);
				Sprite[] sprites = GetSpritesFromAssetPath(texturePath, data.meta.frameTags);

				// NOTE: Save animator controller asset
				AnimatorController animController = AnimatorController.CreateAnimatorControllerAtPath
					(dirTexture + "/" + fileNameTextureWithoutExtension + ".controller");

				// NOTE: Save animation clips by each tags. And associate them to animator controller.
				foreach (AsepriteSpriteFrameTagInfo tag in data.meta.frameTags) {
					AnimationClip clip = new AnimationClip();
					EditorCurveBinding curveBinding = new EditorCurveBinding();
					curveBinding.type = typeof(SpriteRenderer);
					curveBinding.path = "";
					curveBinding.propertyName = "m_Sprite";
					int keyframeLength = tag.to - tag.from + 1;
					ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[keyframeLength];
					for (int i = 0; i <= keyframeLength - 1; i++) {
						keyframes[i] = new ObjectReferenceKeyframe();
						keyframes[i].time = GetKeyframeTime(data, tag, i);
						keyframes[i].value = GetKeyframeSprite(data, tag, sprites, i);
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
		private int GetKeyframeTime(AsepriteSpriteData config, AsepriteSpriteFrameTagInfo tag, int i) {
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
		private Sprite GetKeyframeSprite(AsepriteSpriteData config, AsepriteSpriteFrameTagInfo tag, Sprite[] sprites, int i) {
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
		private Sprite[] GetSpritesFromAssetPath(string path, AsepriteSpriteFrameTagInfo[] tags) {
			var assets = AssetDatabase.LoadAllAssetsAtPath(path);
			List<Sprite> sprites = new List<Sprite>();

			foreach (var asset in assets) {
				if (asset is Sprite) {
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
		private string GetDirPathFromAsset(Object obj) {
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
		public int CountFramesInTag(AsepriteSpriteFrameTagInfo tag) {
			return tag.to - tag.from + 1;
		}

		/**
		 * <summary>Get sprite array sorted by frame tag order.</summary>
		 * <param name="spriteNames">Names of each sprites</param>
		 * <param name="tags">Tags of sprites (Defined in Aseprite)</param>
		 * <returns>Sprite array sorted by frame tag order</returns>
		 */
		private string[] GetSpriteArrSortedByFrameTagOrder(string[] spriteNames, AsepriteSpriteFrameTagInfo[] tags) {
			List<string> lst = new List<string>();

			foreach (AsepriteSpriteFrameTagInfo tag in tags) {
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
		private Func<int, int, int> Sum() {
			return (x, y) => x + y;
		}
	}
}
